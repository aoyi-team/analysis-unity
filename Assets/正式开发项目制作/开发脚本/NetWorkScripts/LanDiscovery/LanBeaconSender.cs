using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 局域网房间 Beacon 发送器
/// 挂载到场景中的 GameObject 上，由房主调用 StartBroadcast 周期性广播房间信息。
/// </summary>
public class LanBeaconSender : MonoBehaviour
{
    private UdpClient _udpClient;
    private IPEndPoint _targetEndPoint;
    private RoomInfo _currentRoom;
    private CancellationTokenSource _cts;
    private bool _isBroadcasting;

    public bool IsBroadcasting => _isBroadcasting;

    /// <summary>开始广播指定房间信息</summary>
    public void StartBroadcast(RoomInfo room)
    {
        StopBroadcast();
        _currentRoom = room;

        try
        {
            _udpClient = new UdpClient();
            _udpClient.EnableBroadcast = true;

            string targetAddress = string.IsNullOrEmpty(ServerConfig.LanMulticastAddress)
                ? "255.255.255.255"
                : ServerConfig.LanMulticastAddress;

            _targetEndPoint = new IPEndPoint(IPAddress.Parse(targetAddress), ServerConfig.LanBroadcastPort);

            _cts = new CancellationTokenSource();
            _isBroadcasting = true;

            _ = BroadcastLoopAsync(_cts.Token);
            Debug.Log($"[LAN] 开始广播房间 Beacon 到 {targetAddress}:{ServerConfig.LanBroadcastPort}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[LAN] 启动 Beacon 广播失败: {ex.Message}");
            StopBroadcast();
        }
    }

    /// <summary>停止广播并释放资源</summary>
    public void StopBroadcast()
    {
        _isBroadcasting = false;

        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;

        try { _udpClient?.Close(); } catch { }
        try { _udpClient?.Dispose(); } catch { }
        _udpClient = null;
        _targetEndPoint = null;
    }

    private async Task BroadcastLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested && _isBroadcasting)
        {
            try
            {
                SendBeacon();
                await Task.Delay(TimeSpan.FromSeconds(ServerConfig.LanBeaconInterval), token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LAN] Beacon 广播错误: {ex.Message}");
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }

    private void SendBeacon()
    {
        if (_udpClient == null || string.IsNullOrEmpty(_currentRoom.RoomId))
            return;

        var beacon = new LanBeaconData
        {
            RoomId = _currentRoom.RoomId,
            RoomName = _currentRoom.RoomName,
            Mode = _currentRoom.Mode,
            CurrentPlayers = _currentRoom.CurrentPlayers,
            MaxPlayers = _currentRoom.MaxPlayers,
            ProtocolVersion = ServerConfig.ProtocolVersion,
            TcpIp = _currentRoom.HostEndpoint.TcpIp,
            TcpPort = _currentRoom.HostEndpoint.TcpPort,
            UdpIp = _currentRoom.HostEndpoint.UdpIp,
            UdpPort = _currentRoom.HostEndpoint.UdpPort
        };

        string json = beacon.ToJson();
        byte[] bytes = Encoding.UTF8.GetBytes(json);
        _udpClient.Send(bytes, bytes.Length, _targetEndPoint);
    }

    private void OnDestroy()
    {
        StopBroadcast();
    }
}
