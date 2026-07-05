using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

/// <summary>
/// 局域网内嵌主机管理器
/// 挂载到房主场景对象，负责启动 TCP/UDP 内嵌服务器并广播房间 Beacon。
/// </summary>
public class LanHostManager : MonoBehaviour
{
    [SerializeField] private LanBeaconSender beaconSender;
    private EmbeddedTcpServer _tcpServer;
    private EmbeddedUdpServer _udpServer;
    private EmbeddedRoom _room;
    private bool _isHosting;

    public bool IsHosting => _isHosting;
    public EmbeddedRoom Room => _room;
    public EmbeddedTcpServer TcpServer => _tcpServer;
    public EmbeddedUdpServer UdpServer => _udpServer;

    #region 主线程日志与动作队列
    private struct LogEntry
    {
        public bool IsError;
        public string Message;
    }

    private static readonly ConcurrentQueue<LogEntry> _logQueue = new ConcurrentQueue<LogEntry>();
    private static readonly ConcurrentQueue<Action> _mainThreadActions = new ConcurrentQueue<Action>();

    public static void Log(string message)
    {
        _logQueue.Enqueue(new LogEntry { IsError = false, Message = message });
    }

    public static void LogError(string message)
    {
        _logQueue.Enqueue(new LogEntry { IsError = true, Message = message });
    }
    #endregion

    private void Awake()
    {
        if (beaconSender == null)
        {
            beaconSender = gameObject.AddComponent<LanBeaconSender>();
        }
    }

    private void Update()
    {
        while (_mainThreadActions.TryDequeue(out var action))
        {
            try { action(); } catch (Exception ex) { Debug.LogError($"[LanHostManager] 主线程动作异常: {ex.Message}"); }
        }

        while (_logQueue.TryDequeue(out var entry))
        {
            if (entry.IsError) Debug.LogError(entry.Message);
            else Debug.Log(entry.Message);
        }
    }

    /// <summary>启动内嵌主机并广播房间信息。若默认端口被占用，会自动尝试后续端口。</summary>
    public bool StartHosting(RoomInfo room)
    {
        if (_isHosting)
        {
            StopHosting();
        }

        string localIp = GetLocalIp();

        // 尝试在默认端口范围内寻找可用端口
        int tcpPort = FindAvailablePort(ServerConfig.TcpPort, ServerConfig.TcpPort + 20);
        int udpPort = FindAvailablePort(ServerConfig.UdpPort, ServerConfig.UdpPort + 20);

        if (tcpPort <= 0 || udpPort <= 0)
        {
            LogError($"[LanHostManager] 在端口范围 {ServerConfig.TcpPort}-{ServerConfig.TcpPort + 20} / {ServerConfig.UdpPort}-{ServerConfig.UdpPort + 20} 内未找到可用端口，无法启动主机");
            return false;
        }

        room.HostEndpoint = new NetworkEndpoint(localIp, tcpPort, localIp, udpPort);
        room.CurrentPlayers = 0;
        room.Status = RoomStatus.Waiting;

        _room = new EmbeddedRoom(room);
        _room.OnPlayerListChanged += OnRoomPlayerListChanged;

        _tcpServer = new EmbeddedTcpServer();
        _tcpServer.OnClientConnected += OnClientConnected;
        _tcpServer.OnClientDisconnected += OnClientDisconnected;
        bool tcpOk = _tcpServer.Start(tcpPort);

        _udpServer = new EmbeddedUdpServer();
        _udpServer.OnBattleReadyReceived += OnBattleReadyReceived;
        bool udpOk = _udpServer.Start(udpPort, room.RoomId);

        if (!tcpOk || !udpOk)
        {
            LogError($"[LanHostManager] 端口 {tcpPort}/{udpPort} 启动失败，无法启动主机");
            _tcpServer?.Stop();
            _udpServer?.Stop();
            _tcpServer = null;
            _udpServer = null;
            _room = null;
            return false;
        }

        _isHosting = true;
        beaconSender.StartBroadcast(_room.ToRoomInfo());
        Log($"[LanHostManager] 开始内嵌主机，房间 {room.RoomId}，IP {localIp}，TCP端口 {tcpPort}，UDP端口 {udpPort}");
        return true;
    }

    /// <summary>在指定范围内查找一个可用端口</summary>
    private static int FindAvailablePort(int startPort, int endPort)
    {
        for (int port = startPort; port <= endPort; port++)
        {
            try
            {
                using (var testSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    testSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    testSocket.Bind(new IPEndPoint(IPAddress.Any, port));
                    return port;
                }
            }
            catch
            {
                // 端口被占用，继续尝试下一个
            }
        }
        return -1;
    }

    /// <summary>停止内嵌主机并释放资源</summary>
    public void StopHosting()
    {
        if (!_isHosting) return;
        _isHosting = false;

        beaconSender.StopBroadcast();

        _udpServer?.Stop();
        _udpServer = null;

        _tcpServer?.Stop();
        _tcpServer = null;

        if (_room != null)
        {
            _room.OnPlayerListChanged -= OnRoomPlayerListChanged;
            _room = null;
        }

        Log("[LanHostManager] 已停止内嵌主机");
    }

    private void OnClientConnected(EmbeddedClientState state)
    {
        _room?.AddPlayer(state);
    }

    private void OnClientDisconnected(EmbeddedClientState state)
    {
        _udpServer?.RemovePlayerEndpoint(state.tempUserId);
        _room?.RemovePlayer(state);
    }

    private void OnRoomPlayerListChanged(EmbeddedRoom room)
    {
        RoomInfo info = room.ToRoomInfo();
        _mainThreadActions.Enqueue(() =>
        {
            if (_isHosting && beaconSender != null)
            {
                beaconSender.StartBroadcast(info);
            }
        });
    }

    private void OnBattleReadyReceived(MsgBattleReady msg, IPEndPoint endpoint)
    {
        if (_room != null && _room.RoomInfo.Status != RoomStatus.Playing)
        {
            _room.StartGame();
        }
        var state = FindClientByUserId(msg.userId.ToString("D6"));
        if (state != null)
        {
            _tcpServer?.Send(state, msg);
            Log($"[LanHostManager] 转发 BattleReady 给玩家 {msg.userId}");
        }
    }

    private EmbeddedClientState FindClientByUserId(string userId)
    {
        if (_tcpServer == null || userId == null) return null;
        foreach (var kv in _tcpServer.Clients)
        {
            if (kv.Value.tempUserId == userId) return kv.Value;
        }
        return null;
    }

    /// <summary>
    /// 获取本机局域网地址，优先选局域网 IP，排除代理/VPN 虚拟网卡。
    /// </summary>
    private string GetLocalIp()
    {
        try
        {
            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress bestCandidate = null;

            var allIps = new System.Collections.Generic.List<string>();
            var privateIps = new System.Collections.Generic.List<IPAddress>();

            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily != AddressFamily.InterNetwork || IPAddress.IsLoopback(ip))
                    continue;

                byte[] bytes = ip.GetAddressBytes();
                allIps.Add(ip.ToString());

                // 排除代理/VPN 网段（Clash/V2Ray 常用的 198.18.0.0/15）
                if (bytes[0] == 198 && bytes[1] >= 18 && bytes[1] <= 19)
                    continue;

                if (IsPrivateIPv4(bytes))
                {
                    privateIps.Add(ip);
                }
                else if (bestCandidate == null)
                {
                    bestCandidate = ip;
                }
            }

            Log($"[LanHostManager] 本机所有 IPv4: {string.Join(", ", allIps)}");

            // 优先 192.168.x.x
            foreach (var ip in privateIps)
            {
                byte[] bytes = ip.GetAddressBytes();
                if (bytes[0] == 192 && bytes[1] == 168)
                    return ip.ToString();
            }

            // 其次 10.x.x.x
            foreach (var ip in privateIps)
            {
                byte[] bytes = ip.GetAddressBytes();
                if (bytes[0] == 10)
                    return ip.ToString();
            }

            // 172.16-31.x.x（可能是 WSL2，但如果没有其他选择就用它）
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
            LogError($"[LanHostManager] 获取本地 IP 失败: {ex.Message}");
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

    private void OnDestroy()
    {
        StopHosting();
    }
}
