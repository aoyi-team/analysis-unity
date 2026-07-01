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

    /// <summary>启动内嵌主机并广播房间信息</summary>
    public void StartHosting(RoomInfo room)
    {
        if (_isHosting)
        {
            StopHosting();
        }

        string localIp = GetLocalIp();
        room.HostEndpoint = new NetworkEndpoint(localIp, ServerConfig.TcpPort, localIp, ServerConfig.UdpPort);
        room.CurrentPlayers = 0;
        room.Status = RoomStatus.Waiting;

        _room = new EmbeddedRoom(room);
        _room.OnPlayerListChanged += OnRoomPlayerListChanged;

        _tcpServer = new EmbeddedTcpServer();
        _tcpServer.OnClientConnected += OnClientConnected;
        _tcpServer.OnClientDisconnected += OnClientDisconnected;
        _tcpServer.Start(ServerConfig.TcpPort);

        _udpServer = new EmbeddedUdpServer();
        _udpServer.OnBattleReadyReceived += OnBattleReadyReceived;
        _udpServer.Start(ServerConfig.UdpPort, room.RoomId);

        _isHosting = true;
        beaconSender.StartBroadcast(_room.ToRoomInfo());
        Log($"[LanHostManager] 开始内嵌主机，房间 {room.RoomId}，IP {localIp}");
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
        _room?.StartGame();
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

    private string GetLocalIp()
    {
        try
        {
            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip))
                {
                    return ip.ToString();
                }
            }
        }
        catch (Exception ex)
        {
            LogError($"[LanHostManager] 获取本地 IP 失败: {ex.Message}");
        }
        return "127.0.0.1";
    }

    private void OnDestroy()
    {
        StopHosting();
    }
}
