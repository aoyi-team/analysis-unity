using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Aoyi.Mirror;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 局域网快速匹配管理器（重构版）。
/// 使用 Mirror NetworkRoomManager 自动处理房间逻辑，不再需要：
/// - PerformLanLoginAsync（Mirror 自动同步 connectionId）
/// - MsgMatchRequest（用 RoomPlayer.CmdSetHero + SyncVar 同步）
/// - BuildMatchSuccessMessage（Mirror 自动 ServerChangeScene）
/// - 手写 ServerPlayers 列表（用 roomSlots）
/// </summary>
public class LanQuickMatchManager : MonoBehaviour
{
    public static LanQuickMatchManager Instance { get; private set; }

    [Header("搜索配置")]
    [SerializeField] private float searchTimeout = 2.5f;
    [SerializeField] private int maxPlayers = 2;

    /// <summary>
    /// 根据游戏模式获取最大玩家数
    /// </summary>
    private int GetMaxPlayersForMode(GameModes mode)
    {
        switch (mode)
        {
            case GameModes.dantiao: return 2;       // 单挑 1v1
            case GameModes.paiwei: return 15;       // 5v5v5 排位
            case GameModes.paiwei_solo: return 1;   // 5v5v5 单人
            case GameModes.shengcun: return 15;     // 生存
            default: return 2;
        }
    }

    /// <summary>
    /// 根据游戏模式获取战斗场景名
    /// </summary>
    private string GetBattleSceneForMode(GameModes mode)
    {
        switch (mode)
        {
            case GameModes.dantiao: return "dantiao_map";
            case GameModes.paiwei:
            case GameModes.paiwei_solo:
            case GameModes.shengcun: return "paiwei_map";
            default: return "dantiao_map";
        }
    }

    /// <summary>
    /// 是否是单人模式（不需要等待其他玩家）
    /// </summary>
    private bool IsSoloMode(GameModes mode)
    {
        return mode == GameModes.paiwei_solo;
    }

    private LanBeaconReceiver _receiver;
    private LanBeaconSender _beaconSender;
    private bool _isMatching;
    private float _searchTimer;
    private bool _isWaiting;
    private RoomInfo _currentRoom;
    /// <summary>取消标志，用于中断 JoinRoom/CreateHostRoom 中的异步等待循环</summary>
    private bool _isCanceled;

    public bool IsMatching => _isMatching;
    public bool IsWaiting => _isWaiting;

    public event Action OnMatchFound;
    public event Action OnHostCreated;
    public event Action OnMatchFailed;
    public event Action OnMatchCanceled;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log("[LanQuickMatchManager] Awake，实例已创建");
    }

    /// <summary>
    /// 开始局域网快速匹配。
    /// </summary>
    public void StartQuickMatch(GameModes mode, int heroId, int skinId)
    {
        if (_isMatching || _isWaiting)
        {
            Debug.LogWarning($"[LanQuickMatchManager] 当前正在匹配或等待中，isMatching={_isMatching}, isWaiting={_isWaiting}");
            return;
        }

        // 防御性清理：上一次测试可能遗留的 Mirror 状态（host/client 未正确关闭）
        CleanupMirrorState();

        _isMatching = true;
        _isCanceled = false;
        _searchTimer = 0f;

        // 保存当前英雄和皮肤选择
        PlayerBasicInfoMgr.Instance.SetCurrentGamemode(mode);
        PlayerBasicInfoMgr.Instance.UpdateHeroCache(heroId, skinId);

        // 根据模式设置最大玩家数
        maxPlayers = GetMaxPlayersForMode(mode);

        // 单人模式：跳过搜索，直接创建房间
        if (IsSoloMode(mode))
        {
            Debug.Log($"[LanQuickMatchManager] 单人模式({mode})，跳过搜索直接创建房间");
            CreateHostRoom();
            return;
        }

        Debug.Log($"[LanQuickMatchManager] StartQuickMatch 开始，mode={mode}, heroId={heroId}, skinId={skinId}, maxPlayers={maxPlayers}");

        // 确保有 Beacon 接收器
        _receiver = FindObjectOfType<LanBeaconReceiver>();
        if (_receiver == null)
        {
            GameObject go = new GameObject("LanBeaconReceiver");
            _receiver = go.AddComponent<LanBeaconReceiver>();
            DontDestroyOnLoad(go);
            Debug.Log("[LanQuickMatchManager] 自动创建 LanBeaconReceiver");
        }

        _receiver.OnRoomListUpdated += OnRoomListUpdated;
        _receiver.StartListening();

        Debug.Log($"[LanQuickMatchManager] 开始搜索局域网房间，超时时间={searchTimeout}秒");
    }

    private void Update()
    {
        if (_isMatching)
        {
            _searchTimer += Time.deltaTime;
            if (_searchTimer >= searchTimeout)
            {
                Debug.Log($"[LanQuickMatchManager] 搜索超时（已等待{_searchTimer:F1}秒），自动创建房间");
                CreateHostRoom();
            }
        }

        // 房主持续更新 Beacon 中的当前玩家数
        if (_isWaiting && _beaconSender != null && !string.IsNullOrEmpty(_currentRoom.RoomId))
        {
            var nm = AoyiNetworkRoomManager.singleton;
            if (nm != null)
            {
                int count = nm.roomSlots.Count;
                if (_currentRoom.CurrentPlayers != count)
                {
                    _currentRoom.CurrentPlayers = count;
                    _beaconSender.StartBroadcast(_currentRoom);
                }
            }
        }
    }

    private void OnRoomListUpdated()
    {
        if (!_isMatching || _receiver == null) return;

        var rooms = _receiver.DiscoveredRooms;
        Debug.Log($"[LanQuickMatchManager] 收到房间列表更新，当前发现 {rooms.Count} 个房间");

        foreach (var room in rooms)
        {
            if (!room.IsCompatible) continue;
            if (room.RoomInfo.Status != RoomStatus.Waiting) continue;
            if (room.RoomInfo.CurrentPlayers >= room.RoomInfo.MaxPlayers) continue;

            Debug.Log($"[LanQuickMatchManager] 找到可加入房间：{room.RoomInfo.RoomId}");
            JoinRoom(room.RoomInfo);
            return;
        }
    }

    private async void JoinRoom(RoomInfo room)
    {
        StopSearch();

        PlayerBasicInfoMgr.Instance.CurrentNetworkMode = NetworkMode.LanClient;
        PlayerBasicInfoMgr.Instance.TargetEndpoint = room.HostEndpoint;
        PlayerBasicInfoMgr.Instance.UpdateRoomID(room.RoomId);
        _currentRoom = room;

        Debug.Log($"[LanQuickMatchManager] 找到房间并加入：{room.RoomName}");
        OnMatchFound?.Invoke();

        var nm = EnsureAoyiNetworkRoomManager();
        if (nm == null)
        {
            Debug.LogError("[LanQuickMatchManager] AoyiNetworkRoomManager 不可用，无法加入房间");
            OnMatchFailed?.Invoke();
            return;
        }

        // 清理之前的 Mirror 状态（避免 "Client already started"）
        CleanupMirrorState();

        Debug.Log($"[LanQuickMatchManager] 准备连接到 {room.HostEndpoint.TcpIp}:{room.HostEndpoint.TcpPort}, transport={nm.transport?.GetType().Name}");
        ConfigureMirrorTransport(room.HostEndpoint.TcpIp, room.HostEndpoint.TcpPort);
        nm.networkAddress = room.HostEndpoint.TcpIp;
        nm.StartClient();

        // 等待 Mirror 客户端连接就绪
        bool connected = false;
        for (int i = 0; i < 50; i++)
        {
            await Task.Delay(100);
            if (_isCanceled)
            {
                Debug.Log("[LanQuickMatchManager] 连接等待被取消");
                return;
            }
            if (Mirror.NetworkClient.active && Mirror.NetworkClient.isConnected)
            {
                connected = true;
                break;
            }
            // 每 10 次打印一次进度
            if (i % 10 == 9)
            {
                Debug.Log($"[LanQuickMatchManager] 等待连接中... {i+1}/50, NetworkClient.active={Mirror.NetworkClient.active}, isConnected={Mirror.NetworkClient.isConnected}");
            }
        }

        Debug.Log($"[LanQuickMatchManager] Mirror 连接房间结果：{connected}, NetworkClient.active={Mirror.NetworkClient.active}, isConnected={Mirror.NetworkClient.isConnected}");
        if (!connected)
        {
            Debug.LogError($"[LanQuickMatchManager] Mirror 连接房间失败: host={room.HostEndpoint.TcpIp}:{room.HostEndpoint.TcpPort}, transport.Port={((nm.transport as kcp2k.KcpTransport)?.Port).ToString()}");
            OnMatchFailed?.Invoke();
            return;
        }

        // 等待本地 RoomPlayer 创建（Mirror 自动创建）
        bool playerReady = false;
        for (int i = 0; i < 50; i++)
        {
            await Task.Delay(100);
            if (_isCanceled)
            {
                Debug.Log("[LanQuickMatchManager] RoomPlayer 等待被取消");
                return;
            }
            if (nm.GetLocalPlayerIndex() >= 0)
            {
                playerReady = true;
                break;
            }
        }

        Debug.Log($"[LanQuickMatchManager] 本地 RoomPlayer 创建结果：{playerReady}");
        if (!playerReady)
        {
            Debug.LogError("[LanQuickMatchManager] 本地 RoomPlayer 创建失败");
            OnMatchFailed?.Invoke();
            return;
        }

        // 设置本地玩家的 teamId（从 RoomPlayer 读取）
        int teamId = nm.GetLocalPlayerTeamId();
        PlayerBasicInfoMgr.Instance.UpdateTeamId(teamId);
        Debug.Log($"[LanQuickMatchManager] 本地玩家 teamId={teamId}");

        if (nm.IsAoyiBattleScene(SceneManager.GetActiveScene().name))
        {
            Debug.Log("[LanQuickMatchManager] 已进入战斗场景，跳过等待对手 UI");
            return;
        }

        EnterWaitingState();
    }

    private async void CreateHostRoom()
    {
        StopSearch();

        var nm = EnsureAoyiNetworkRoomManager();
        if (nm == null)
        {
            Debug.LogError("[LanQuickMatchManager] AoyiNetworkRoomManager 不可用，无法创建房间");
            OnMatchFailed?.Invoke();
            return;
        }

        string localIp = GetLocalIp();
        int mirrorPort = FindAvailablePort(ServerConfig.TcpPort, ServerConfig.TcpPort + 20, ProtocolType.Udp);
        int udpPort = FindAvailablePort(ServerConfig.UdpPort, ServerConfig.UdpPort + 20, ProtocolType.Udp);
        if (udpPort == mirrorPort)
        {
            udpPort = FindAvailablePort(udpPort + 1, ServerConfig.UdpPort + 20, ProtocolType.Udp);
        }
        if (mirrorPort <= 0 || udpPort <= 0)
        {
            Debug.LogError($"[LanQuickMatchManager] 未找到可用端口");
            OnMatchFailed?.Invoke();
            return;
        }

        ConfigureMirrorTransport(localIp, mirrorPort);
        nm.networkAddress = localIp;

        RoomInfo room = new RoomInfo
        {
            RoomId = GenerateShortCode(),
            RoomName = $"{PlayerBasicInfoMgr.Instance.GetName()}的房间",
            Mode = PlayerBasicInfoMgr.Instance.GameMode,
            MaxPlayers = Mathf.Max(1, maxPlayers),
            ProtocolVersion = ServerConfig.ProtocolVersion,
            Status = RoomStatus.Waiting,
            HostEndpoint = new NetworkEndpoint(localIp, mirrorPort, localIp, udpPort),
            CurrentPlayers = 1
        };
        _currentRoom = room;

        ServerConfig.ServerIp = localIp;

        // 清理之前的 Mirror 状态
        CleanupMirrorState();

        nm.maxRoomPlayers = maxPlayers;
        nm.minPlayers = Mathf.Max(1, maxPlayers);
        nm.maxConnections = maxPlayers;
        Debug.Log($"[LanQuickMatchManager] 准备创建 Mirror 房间：{room.RoomId}，端口 {mirrorPort}, maxPlayers={maxPlayers}");
        nm.StartHost();

        // 等待一帧让 Mirror 完成主机初始化
        await Task.Yield();
        await Task.Delay(100);

        PlayerBasicInfoMgr.Instance.CurrentNetworkMode = NetworkMode.LanHost;
        PlayerBasicInfoMgr.Instance.TargetEndpoint = room.HostEndpoint;
        PlayerBasicInfoMgr.Instance.UpdateRoomID(room.RoomId);

        // 设置战斗场景名（根据游戏模式）
        nm.pendingBattleScene = GetBattleSceneForMode(PlayerBasicInfoMgr.Instance.GameMode);

        Debug.Log($"[LanQuickMatchManager] 已创建 Mirror 主机：{room.RoomId}, 场景={nm.pendingBattleScene}, maxPlayers={room.MaxPlayers}");
        OnHostCreated?.Invoke();

        // 单人模式：不广播，直接准备开始战斗
        if (IsSoloMode(PlayerBasicInfoMgr.Instance.GameMode))
        {
            Debug.Log("[LanQuickMatchManager] 单人模式，跳过 Beacon 广播");
        }
        else
        {
            // 启动 Beacon 广播
            StartBeaconBroadcast(room);
        }

        // 等待本地 RoomPlayer 创建
        bool playerReady = false;
        for (int i = 0; i < 50; i++)
        {
            await Task.Delay(100);
            if (_isCanceled)
            {
                Debug.Log("[LanQuickMatchManager] Host RoomPlayer 等待被取消");
                return;
            }
            if (nm.GetLocalPlayerIndex() >= 0)
            {
                playerReady = true;
                break;
            }
        }

        Debug.Log($"[LanQuickMatchManager] host 本地 RoomPlayer 创建结果：{playerReady}");
        if (playerReady)
        {
            int teamId = nm.GetLocalPlayerTeamId();
            PlayerBasicInfoMgr.Instance.UpdateTeamId(teamId);
            Debug.Log($"[LanQuickMatchManager] host 本地玩家 teamId={teamId}");

            // 不再手动 ready：房间满员时 AoyiNetworkRoomManager.OnServerAddPlayer 会自动准备所有玩家
        }

        if (nm.IsAoyiBattleScene(SceneManager.GetActiveScene().name))
        {
            Debug.Log("[LanQuickMatchManager] host 已进入战斗场景，跳过等待对手 UI");
            return;
        }

        EnterWaitingState();
    }

    /// <summary>
    /// 玩家点击"开始战斗"按钮（仅 host）
    /// 调用此方法后，host 设置自己 ready，等待其他玩家也 ready 后 Mirror 自动切换场景
    /// </summary>
    public void HostReadyToBegin()
    {
        var nm = AoyiNetworkRoomManager.singleton;
        if (nm == null) return;

        if (!nm.CanStartBattle())
        {
            Debug.LogWarning($"[LanQuickMatchManager] 人数不足，不能开始战斗：{nm.roomSlots.Count}/{nm.maxRoomPlayers}");
            return;
        }

        // 找到本地 RoomPlayer，设置 ready
        foreach (var slot in nm.roomSlots)
        {
            if (slot is AoyiRoomPlayer aoyiPlayer && aoyiPlayer.isLocalPlayer)
            {
                aoyiPlayer.CmdSetReady();
                Debug.Log($"[LanQuickMatchManager] Host 已设置 ready");
                return;
            }
        }
    }

    /// <summary>
    /// 玩家点击"准备"按钮（客户端）
    /// </summary>
    public void ClientReadyToBegin()
    {
        var nm = AoyiNetworkRoomManager.singleton;
        if (nm == null) return;

        foreach (var slot in nm.roomSlots)
        {
            if (slot is AoyiRoomPlayer aoyiPlayer && aoyiPlayer.isLocalPlayer)
            {
                aoyiPlayer.CmdSetReady();
                Debug.Log($"[LanQuickMatchManager] Client 已设置 ready");
                return;
            }
        }
    }

    /// <summary>
    /// 取消当前匹配或等待。
    /// </summary>
    public void CancelMatch()
    {
        Debug.Log($"[LanQuickMatchManager] CancelMatch 被调用，当前 isMatching={_isMatching}, isWaiting={_isWaiting}");

        if (!_isMatching && !_isWaiting)
        {
            return;
        }

        // 设置取消标志，中断 JoinRoom/CreateHostRoom 中的异步等待循环
        _isCanceled = true;

        StopSearch();

        if (Mirror.NetworkServer.active && Mirror.NetworkClient.active)
        {
            AoyiNetworkRoomManager.singleton?.StopHost();
        }
        else if (Mirror.NetworkClient.active)
        {
            AoyiNetworkRoomManager.singleton?.StopClient();
        }

        if (_beaconSender != null)
        {
            _beaconSender.StopBroadcast();
            _beaconSender = null;
        }

        _isWaiting = false;
        Debug.Log("[LanQuickMatchManager] 已取消匹配");
        OnMatchCanceled?.Invoke();
    }

    /// <summary>
    /// 匹配已经完成并进入战斗场景时的收尾逻辑。
    /// 这里只关闭等待 UI 和房间广播，不能停止 Mirror 连接。
    /// </summary>
    public void CompleteMatchAndEnterBattle()
    {
        StopSearch();
        _isWaiting = false;
        _isCanceled = false;

        if (_beaconSender != null)
        {
            _beaconSender.StopBroadcast();
            _beaconSender = null;
        }

        if (!string.IsNullOrEmpty(_currentRoom.RoomId))
        {
            _currentRoom.Status = RoomStatus.Playing;
        }

        HideWaitingPanel();
        Debug.Log("[LanQuickMatchManager] 匹配完成，已隐藏等待面板并保留 Mirror 连接");
    }

    private void StopSearch()
    {
        _isMatching = false;
        _searchTimer = 0f;

        if (_receiver != null)
        {
            _receiver.OnRoomListUpdated -= OnRoomListUpdated;
            _receiver.StopListening();
        }
    }

    private void EnterWaitingState()
    {
        _isWaiting = true;
        Debug.Log("[LanQuickMatchManager] 进入等待对手状态");

        // 单人模式：不显示等待面板（Mirror 会自动切换场景）
        if (IsSoloMode(PlayerBasicInfoMgr.Instance.GameMode))
        {
            Debug.Log("[LanQuickMatchManager] 单人模式，不显示等待面板");
            return;
        }

        try
        {
            var panel = FindObjectOfType<LanWaitingPanel>();
            if (panel == null)
            {
                panel = CreateWaitingPanel();
            }
            panel.Show();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[LanQuickMatchManager] 打开等待面板异常：{ex.Message}");
        }
    }

    private static void HideWaitingPanel()
    {
        LanWaitingPanel panel = FindObjectOfType<LanWaitingPanel>(true);
        if (panel != null)
        {
            panel.Hide();
        }
    }

    private static string GenerateShortCode()
    {
        return Guid.NewGuid().ToString("N").Substring(0, 6);
    }

    /// <summary>
    /// 清理之前的 Mirror 状态，避免 "Client already started" 或 "Server already started" 错误
    /// </summary>
    private static void CleanupMirrorState()
    {
        var nm = AoyiNetworkRoomManager.singleton;
        if (nm == null) return;

        if (Mirror.NetworkServer.active && Mirror.NetworkClient.active)
        {
            Debug.Log("[LanQuickMatchManager] 清理之前的 Host 状态");
            nm.StopHost();
        }
        else if (Mirror.NetworkClient.active)
        {
            Debug.Log("[LanQuickMatchManager] 清理之前的 Client 状态");
            nm.StopClient();
        }
        else if (Mirror.NetworkServer.active)
        {
            Debug.Log("[LanQuickMatchManager] 清理之前的 Server 状态");
            nm.StopServer();
        }
    }

    private static AoyiNetworkRoomManager EnsureAoyiNetworkRoomManager()
    {
        if (AoyiNetworkRoomManager.singleton != null)
            return AoyiNetworkRoomManager.singleton;

        if (Mirror.NetworkManager.singleton != null && !(Mirror.NetworkManager.singleton is AoyiNetworkRoomManager))
        {
            Debug.LogError("[LanQuickMatchManager] 场景中存在非 AoyiNetworkRoomManager 的 NetworkManager");
            return null;
        }

        // 检查 Resources 中是否有 RoomPlayer prefab（必须先通过菜单生成）
        GameObject roomPlayerObj = Resources.Load<GameObject>("MirrorPrefabs/AoyiRoomPlayerPrefab");
        if (roomPlayerObj == null)
        {
            Debug.LogError("[LanQuickMatchManager] RoomPlayer prefab 未找到！请先在 Unity 编辑器中运行菜单 Tools > 奥义 > 生成 Mirror Prefab");
            return null;
        }

        GameObject go = new GameObject("AoyiNetworkRoomManager");
        DontDestroyOnLoad(go);
        kcp2k.KcpTransport transport = go.AddComponent<kcp2k.KcpTransport>();
        AoyiNetworkRoomManager manager = go.AddComponent<AoyiNetworkRoomManager>();
        manager.transport = transport;
        // roomPlayerPrefab 和 playerPrefab 在 AoyiNetworkRoomManager.Awake 中从 Resources 加载

        Debug.Log("[LanQuickMatchManager] 自动创建 AoyiNetworkRoomManager + KcpTransport");
        return manager;
    }

    private static void ConfigureMirrorTransport(string ip, int port)
    {
        var nm = AoyiNetworkRoomManager.singleton;
        if (nm == null) return;

        nm.networkAddress = ip;
        // KcpTransport 继承 PortTransport，可以通过 Port 属性设置端口
        if (nm.transport is kcp2k.KcpTransport kcpTransport)
        {
            kcpTransport.Port = (ushort)port;
            Debug.Log($"[LanQuickMatchManager] 配置 KcpTransport：{ip}:{port}");
        }
        else
        {
            Debug.LogWarning($"[LanQuickMatchManager] 未知 transport 类型：{nm.transport?.GetType().Name}，无法设置端口");
        }
    }

    private static int FindAvailablePort(int startPort, int endPort, ProtocolType protocol = ProtocolType.Tcp)
    {
        SocketType socketType = protocol == ProtocolType.Udp ? SocketType.Dgram : SocketType.Stream;
        for (int port = startPort; port <= endPort; port++)
        {
            try
            {
                using (var testSocket = new Socket(AddressFamily.InterNetwork, socketType, protocol))
                {
                    testSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    testSocket.Bind(new IPEndPoint(IPAddress.Any, port));
                    return port;
                }
            }
            catch { }
        }
        return -1;
    }

    private void StartBeaconBroadcast(RoomInfo room)
    {
        if (_beaconSender == null)
        {
            GameObject go = new GameObject("LanBeaconSender");
            _beaconSender = go.AddComponent<LanBeaconSender>();
            DontDestroyOnLoad(go);
        }
        _beaconSender.StartBroadcast(room);
    }

    private string GetLocalIp()
    {
        try
        {
            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress bestCandidate = null;
            var privateIps = new System.Collections.Generic.List<IPAddress>();

            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily != AddressFamily.InterNetwork || IPAddress.IsLoopback(ip))
                    continue;

                byte[] bytes = ip.GetAddressBytes();
                if (bytes[0] == 198 && bytes[1] >= 18 && bytes[1] <= 19)
                    continue;

                if (IsPrivateIPv4(bytes))
                    privateIps.Add(ip);
                else if (bestCandidate == null)
                    bestCandidate = ip;
            }

            foreach (var ip in privateIps)
            {
                byte[] bytes = ip.GetAddressBytes();
                if (bytes[0] == 192 && bytes[1] == 168)
                    return ip.ToString();
            }
            foreach (var ip in privateIps)
            {
                byte[] bytes = ip.GetAddressBytes();
                if (bytes[0] == 10)
                    return ip.ToString();
            }
            foreach (var ip in privateIps)
            {
                byte[] bytes = ip.GetAddressBytes();
                if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
                    return ip.ToString();
            }

            if (bestCandidate != null)
                return bestCandidate.ToString();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[LanQuickMatchManager] 获取本地 IP 失败: {ex.Message}");
        }

        return "127.0.0.1";
    }

    private static bool IsPrivateIPv4(byte[] bytes)
    {
        if (bytes[0] == 10) return true;
        if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) return true;
        if (bytes[0] == 192 && bytes[1] == 168) return true;
        return false;
    }

    private LanWaitingPanel CreateWaitingPanel()
    {
        GameObject root = new GameObject("LanWaitingPanel");
        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        root.AddComponent<UnityEngine.UI.CanvasScaler>();
        root.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(root.transform, false);
        UnityEngine.UI.Image bgImg = bg.AddComponent<UnityEngine.UI.Image>();
        bgImg.color = new Color(0, 0, 0, 0.85f);
        RectTransform bgRT = bg.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.sizeDelta = Vector2.zero;

        GameObject roomText = new GameObject("RoomNameText");
        roomText.transform.SetParent(root.transform, false);
        UnityEngine.UI.Text roomName = roomText.AddComponent<UnityEngine.UI.Text>();
        roomName.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        roomName.fontSize = 28;
        roomName.alignment = TextAnchor.MiddleCenter;
        roomName.color = Color.white;
        RectTransform roomRT = roomText.GetComponent<RectTransform>();
        roomRT.anchorMin = new Vector2(0.5f, 0.7f);
        roomRT.anchorMax = new Vector2(0.5f, 0.7f);
        roomRT.sizeDelta = new Vector2(400, 60);

        GameObject playerText = new GameObject("PlayerCountText");
        playerText.transform.SetParent(root.transform, false);
        UnityEngine.UI.Text playerCount = playerText.AddComponent<UnityEngine.UI.Text>();
        playerCount.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        playerCount.fontSize = 22;
        playerCount.alignment = TextAnchor.MiddleCenter;
        playerCount.color = Color.yellow;
        RectTransform playerRT = playerText.GetComponent<RectTransform>();
        playerRT.anchorMin = new Vector2(0.5f, 0.5f);
        playerRT.anchorMax = new Vector2(0.5f, 0.5f);
        playerRT.sizeDelta = new Vector2(400, 50);

        GameObject readyBtn = new GameObject("ReadyBtn");
        readyBtn.transform.SetParent(root.transform, false);
        UnityEngine.UI.Image readyImg = readyBtn.AddComponent<UnityEngine.UI.Image>();
        readyImg.color = new Color(0.2f, 0.8f, 0.2f);
        UnityEngine.UI.Button readyButton = readyBtn.AddComponent<UnityEngine.UI.Button>();
        GameObject readyText = new GameObject("Text");
        readyText.transform.SetParent(readyBtn.transform, false);
        UnityEngine.UI.Text readyBtnText = readyText.AddComponent<UnityEngine.UI.Text>();
        readyBtnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        readyBtnText.fontSize = 24;
        readyBtnText.alignment = TextAnchor.MiddleCenter;
        readyBtnText.color = Color.white;
        readyBtnText.text = "准备/开始";
        RectTransform readyBtnRT = readyBtn.GetComponent<RectTransform>();
        readyBtnRT.anchorMin = new Vector2(0.5f, 0.3f);
        readyBtnRT.anchorMax = new Vector2(0.5f, 0.3f);
        readyBtnRT.sizeDelta = new Vector2(200, 60);
        RectTransform readyTextRT = readyText.GetComponent<RectTransform>();
        readyTextRT.anchorMin = Vector2.zero;
        readyTextRT.anchorMax = Vector2.one;
        readyTextRT.sizeDelta = Vector2.zero;
        readyButton.onClick.AddListener(() =>
        {
            if (Mirror.NetworkServer.active && Mirror.NetworkClient.active)
                HostReadyToBegin();
            else
                ClientReadyToBegin();
        });

        GameObject cancelBtn = new GameObject("CancelBtn");
        cancelBtn.transform.SetParent(root.transform, false);
        UnityEngine.UI.Image cancelImg = cancelBtn.AddComponent<UnityEngine.UI.Image>();
        cancelImg.color = new Color(0.8f, 0.2f, 0.2f);
        UnityEngine.UI.Button cancelButton = cancelBtn.AddComponent<UnityEngine.UI.Button>();
        GameObject cancelText = new GameObject("Text");
        cancelText.transform.SetParent(cancelBtn.transform, false);
        UnityEngine.UI.Text cancelBtnText = cancelText.AddComponent<UnityEngine.UI.Text>();
        cancelBtnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        cancelBtnText.fontSize = 24;
        cancelBtnText.alignment = TextAnchor.MiddleCenter;
        cancelBtnText.color = Color.white;
        cancelBtnText.text = "取消匹配";
        RectTransform cancelBtnRT = cancelBtn.GetComponent<RectTransform>();
        cancelBtnRT.anchorMin = new Vector2(0.5f, 0.15f);
        cancelBtnRT.anchorMax = new Vector2(0.5f, 0.15f);
        cancelBtnRT.sizeDelta = new Vector2(200, 60);
        RectTransform cancelTextRT = cancelText.GetComponent<RectTransform>();
        cancelTextRT.anchorMin = Vector2.zero;
        cancelTextRT.anchorMax = Vector2.one;
        cancelTextRT.sizeDelta = Vector2.zero;
        cancelButton.onClick.AddListener(CancelMatch);

        LanWaitingPanel panel = root.AddComponent<LanWaitingPanel>();
        DontDestroyOnLoad(root);
        Debug.Log("[LanQuickMatchManager] 已自动创建 LanWaitingPanel UI");
        return panel;
    }

    private void OnDestroy()
    {
        CancelMatch();
        if (Instance == this) Instance = null;
        Debug.Log("[LanQuickMatchManager] OnDestroy");
    }
}
