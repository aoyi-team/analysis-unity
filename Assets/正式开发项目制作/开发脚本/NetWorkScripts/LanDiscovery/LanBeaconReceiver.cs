using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 局域网房间 Beacon 接收器
/// 挂载到场景中的 GameObject 上，监听 UDP Beacon 并维护发现的房间列表。
/// </summary>
public class LanBeaconReceiver : MonoBehaviour
{
    /// <summary>房间列表更新事件（在主线程触发）</summary>
    public event Action OnRoomListUpdated;

    private UdpClient _udpClient;
    private CancellationTokenSource _cts;
    private readonly Dictionary<string, LanDiscoveredRoom> _discoveredRooms = new Dictionary<string, LanDiscoveredRoom>();
    private readonly ConcurrentQueue<bool> _pendingUpdateSignals = new ConcurrentQueue<bool>();
    private readonly object _roomsLock = new object();
    private bool _isListening;
    private float _lastTimeoutCheck;

    public bool IsListening => _isListening;

    /// <summary>当前发现的所有房间（返回副本，线程安全）</summary>
    public IReadOnlyList<LanDiscoveredRoom> DiscoveredRooms
    {
        get
        {
            lock (_roomsLock)
            {
                return new List<LanDiscoveredRoom>(_discoveredRooms.Values);
            }
        }
    }

    /// <summary>开始监听 Beacon</summary>
    public void StartListening()
    {
        StopListening();

        try
        {
            // ���� SO_REUSEADDR����ͬһ̨�������ͻ��˿�ͬʱ���� Beacon
            _udpClient = new UdpClient();
            _udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, ServerConfig.LanBroadcastPort));
            _udpClient.EnableBroadcast = true;

            if (!string.IsNullOrEmpty(ServerConfig.LanMulticastAddress))
            {
                _udpClient.JoinMulticastGroup(IPAddress.Parse(ServerConfig.LanMulticastAddress));
            }

            _cts = new CancellationTokenSource();
            _isListening = true;
            _lastTimeoutCheck = Time.time;

            _ = ReceiveLoopAsync(_cts.Token);
            Debug.Log($"[LAN] 开始在端口 {ServerConfig.LanBroadcastPort} 监听 Beacon（已启用地址复用）");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[LAN] 启动 Beacon 监听失败: {ex.Message}");
            StopListening();
        }
    }

    /// <summary>停止监听并释放资源</summary>
    public void StopListening()
    {
        _isListening = false;

        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;

        try { _udpClient?.Close(); } catch { }
        try { _udpClient?.Dispose(); } catch { }
        _udpClient = null;

        lock (_roomsLock)
        {
            _discoveredRooms.Clear();
        }

        while (_pendingUpdateSignals.TryDequeue(out _)) { }
    }

    private async Task ReceiveLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested && _isListening)
        {
            try
            {
                UdpReceiveResult result = await _udpClient.ReceiveAsync();
                ProcessPacket(result.RemoteEndPoint, result.Buffer);
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.Interrupted ||
                                              ex.SocketErrorCode == SocketError.OperationAborted)
            {
                break;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LAN] 接收 Beacon 错误: {ex.Message}");
            }
        }
    }

    private void ProcessPacket(IPEndPoint remoteEndPoint, byte[] data)
    {
        string json = Encoding.UTF8.GetString(data);
        LanBeaconData beacon = LanBeaconData.FromJson(json);
        if (beacon == null || string.IsNullOrEmpty(beacon.RoomId))
            return;

        bool isCompatible = beacon.ProtocolVersion == ServerConfig.ProtocolVersion;

        var endpoint = new NetworkEndpoint(beacon.TcpIp, beacon.TcpPort, beacon.UdpIp, beacon.UdpPort);
        var roomInfo = new RoomInfo
        {
            RoomId = beacon.RoomId,
            RoomName = beacon.RoomName,
            Mode = beacon.Mode,
            HostEndpoint = endpoint,
            CurrentPlayers = beacon.CurrentPlayers,
            MaxPlayers = beacon.MaxPlayers,
            ProtocolVersion = beacon.ProtocolVersion,
            Status = (beacon.MaxPlayers > 0 && beacon.CurrentPlayers >= beacon.MaxPlayers)
                ? RoomStatus.Full
                : RoomStatus.Waiting
        };

        lock (_roomsLock)
        {
            if (_discoveredRooms.TryGetValue(beacon.RoomId, out var existing))
            {
                existing.Refresh(roomInfo, isCompatible);
            }
            else
            {
                _discoveredRooms[beacon.RoomId] = new LanDiscoveredRoom(roomInfo, isCompatible);
            }
        }

        _pendingUpdateSignals.Enqueue(true);
    }

    private void Update()
    {
        bool changed = false;
        while (_pendingUpdateSignals.TryDequeue(out _))
        {
            changed = true;
        }

        // 每秒检查一次超时房间
        if (Time.time - _lastTimeoutCheck >= 1.0f)
        {
            _lastTimeoutCheck = Time.time;
            lock (_roomsLock)
            {
                List<string> toRemove = null;
                DateTime now = DateTime.UtcNow;
                foreach (var kvp in _discoveredRooms)
                {
                    if ((now - kvp.Value.LastSeenTime).TotalSeconds > 5.0)
                    {
                        if (toRemove == null)
                            toRemove = new List<string>();
                        toRemove.Add(kvp.Key);
                    }
                }

                if (toRemove != null)
                {
                    foreach (var key in toRemove)
                    {
                        _discoveredRooms.Remove(key);
                    }
                    changed = true;
                }
            }
        }

        if (changed)
        {
            OnRoomListUpdated?.Invoke();
        }
    }

    private void OnDestroy()
    {
        StopListening();
    }
}
