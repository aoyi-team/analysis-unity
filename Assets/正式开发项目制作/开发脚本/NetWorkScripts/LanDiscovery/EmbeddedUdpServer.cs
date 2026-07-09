using MsgFramework;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;

/// <summary>
/// 内嵌主机 UDP 服务器
/// </summary>
public class EmbeddedUdpServer
{
    private Socket _server;
    private Thread _recvThread;
    private Thread _broadcastThread;
    private bool _running;
    private const int SIO_UDP_CONNRESET = -1744830452;
    private byte[] _recvBuffer = new byte[4096];

    // userId(D6 string) -> IPEndPoint
    private Dictionary<string, IPEndPoint> _playerEndpoints = new Dictionary<string, IPEndPoint>();
    private readonly object _endpointLock = new object();

    // 等待打包的当前帧操作
    private ConcurrentQueue<MsgPlayerOp> _pendingOps = new ConcurrentQueue<MsgPlayerOp>();

    private int _frameId = 0;
    private string _roomId = "";
    private int _randSeed = 0;

    public event Action<MsgBattleReady, IPEndPoint> OnBattleReadyReceived;

    public bool Start(int port, string roomId)
    {
        Stop();
        _roomId = roomId;
        _frameId = 0;
        _randSeed = new System.Random().Next(10000, 99999);
        try
        {
            _server = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            _server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _server.Bind(new IPEndPoint(IPAddress.Any, port));
            _server.IOControl(SIO_UDP_CONNRESET, new byte[] { 0, 0, 0, 0 }, null);
            _running = true;
            _recvThread = new Thread(RecvLoop) { IsBackground = true, Name = "EmbeddedUdpRecv" };
            _broadcastThread = new Thread(BroadcastLoop) { IsBackground = true, Name = "EmbeddedUdpBroadcast" };
            _recvThread.Start();
            _broadcastThread.Start();
            LanHostManager.Log($"[EmbeddedUdpServer] 启动成功，端口 {port}");
            return true;
        }
        catch (Exception ex)
        {
            LanHostManager.LogError($"[EmbeddedUdpServer] 启动失败: {ex.Message}");
            Stop();
            return false;
        }
    }

    public void Stop()
    {
        _running = false;
        try { _server?.Close(); } catch { }
        try { _server?.Dispose(); } catch { }
        _server = null;
        try { _recvThread?.Join(500); } catch { }
        try { _broadcastThread?.Join(500); } catch { }
        _recvThread = null;
        _broadcastThread = null;
        lock (_endpointLock) { _playerEndpoints.Clear(); }
        while (_pendingOps.TryDequeue(out _)) { }
    }

    public void RemovePlayerEndpoint(string userId)
    {
        if (string.IsNullOrEmpty(userId)) return;
        lock (_endpointLock)
        {
            _playerEndpoints.Remove(userId);
        }
    }

    private void RecvLoop()
    {
        EndPoint remotePoint = new IPEndPoint(IPAddress.Any, 0);
        while (_running)
        {
            try
            {
                int recvLen = _server.ReceiveFrom(_recvBuffer, ref remotePoint);
                if (recvLen <= 0 || recvLen > 4096) continue;
                int readIndex = 0;
                Int16 bodyLen = (Int16)((_recvBuffer[readIndex + 1] << 8) | _recvBuffer[readIndex]);
                readIndex += 2;
                int nameCount = 0;
                string protoName = MsgBase.DecodeName(_recvBuffer, readIndex, out nameCount);
                if (protoName == "")
                {
                    LanHostManager.Log("[EmbeddedUdpServer] 解码协议名失败");
                    continue;
                }
                readIndex += nameCount;
                int bodyCount = bodyLen - nameCount;
                MsgBase msg = MsgBase.Decode(protoName, _recvBuffer, readIndex, bodyCount);
                if (msg == null)
                {
                    LanHostManager.Log($"[EmbeddedUdpServer] 解码消息体失败 {protoName}");
                    continue;
                }
                HandleMessage(msg, remotePoint);
            }
            catch (SocketException)
            {
                // 关闭时正常异常
            }
            catch (Exception ex)
            {
                if (_running) LanHostManager.LogError($"[EmbeddedUdpServer] 接收异常: {ex.Message}");
            }
        }
    }

    private void HandleMessage(MsgBase msg, EndPoint remotePoint)
    {
        switch (msg.protoName)
        {
            case "MsgPlayerOp":
                _pendingOps.Enqueue((MsgPlayerOp)msg);
                break;
            case "MsgBattleReady":
                HandleBattleReady((MsgBattleReady)msg, (IPEndPoint)remotePoint);
                break;
            case "MsgBattleOver":
                LanHostManager.Log($"[EmbeddedUdpServer] 收到战斗结束 userId={((MsgBattleOver)msg).userId}");
                Broadcast(msg);
                break;
            case "MsgPlayerExit":
                LanHostManager.Log($"[EmbeddedUdpServer] 收到玩家退出 userId={((MsgPlayerExit)msg).userId}");
                break;
            default:
                LanHostManager.Log($"[EmbeddedUdpServer] 未处理协议 {msg.protoName}");
                break;
        }
    }

    private void HandleBattleReady(MsgBattleReady msg, IPEndPoint remotePoint)
    {
        string playerId = msg.userId.ToString("D6");
        lock (_endpointLock)
        {
            _playerEndpoints[playerId] = remotePoint;
        }
        LanHostManager.Log($"[EmbeddedUdpServer] 记录玩家 {playerId} UDP 端点 {remotePoint}");
        OnBattleReadyReceived?.Invoke(msg, remotePoint);
    }

    private void BroadcastLoop()
    {
        while (_running)
        {
            var sw = Stopwatch.StartNew();
            TickFrame();
            int elapsed = (int)sw.ElapsedMilliseconds;
            int sleep = Math.Max(0, 50 - elapsed);
            Thread.Sleep(sleep);
        }
    }

    private void TickFrame()
    {
        List<MsgPlayerOp> ops = new List<MsgPlayerOp>();
        while (_pendingOps.TryDequeue(out MsgPlayerOp op))
        {
            ops.Add(op);
        }

        _frameId++;
        FrameData frameData = new FrameData
        {
            frameId = _frameId,
            randSeed = _randSeed + _frameId,
            allPlayerOps = ops
        };
        MsgFramePack pack = new MsgFramePack
        {
            roomId = _roomId,
            frameId = _frameId,
            frames = new List<FrameData> { frameData }
        };
        Broadcast(pack);
    }

    public void Send(MsgBase msg, IPEndPoint remoteEndPoint)
    {
        if (msg == null || remoteEndPoint == null || _server == null) return;
        byte[] sendBytes = EncodeMessage(msg);
        try
        {
            _server.SendTo(sendBytes, remoteEndPoint);
        }
        catch (Exception ex)
        {
            LanHostManager.LogError($"[EmbeddedUdpServer] Send 异常: {ex.Message}");
        }
    }

    public void Broadcast(MsgBase msg)
    {
        if (_server == null) return;
        byte[] sendBytes = EncodeMessage(msg);
        lock (_endpointLock)
        {
            foreach (var kv in _playerEndpoints)
            {
                try
                {
                    _server.SendTo(sendBytes, kv.Value);
                }
                catch (Exception ex)
                {
                    LanHostManager.LogError($"[EmbeddedUdpServer] Broadcast 到 {kv.Value} 异常: {ex.Message}");
                }
            }
        }
    }

    private byte[] EncodeMessage(MsgBase msg)
    {
        byte[] nameBytes = MsgBase.EncodeName(msg);
        byte[] bodyBytes = MsgBase.Encode(msg);
        int len = nameBytes.Length + bodyBytes.Length;
        byte[] sendBytes = new byte[len + 2];
        sendBytes[0] = (byte)(len % 256);
        sendBytes[1] = (byte)(len / 256);
        Array.Copy(nameBytes, 0, sendBytes, 2, nameBytes.Length);
        Array.Copy(bodyBytes, 0, sendBytes, 2 + nameBytes.Length, bodyBytes.Length);
        return sendBytes;
    }
}
