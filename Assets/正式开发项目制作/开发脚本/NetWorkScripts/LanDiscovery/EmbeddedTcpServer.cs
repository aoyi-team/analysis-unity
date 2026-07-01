using MsgFramework;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;

/// <summary>
/// 内嵌主机 TCP 服务器
/// </summary>
public class EmbeddedTcpServer
{
    private Socket _listenfd;
    private Thread _thread;
    private bool _running;
    private Dictionary<Socket, EmbeddedClientState> _clients = new Dictionary<Socket, EmbeddedClientState>();
    private List<Socket> _checkList = new List<Socket>();
    private int _nextUserId = 1;
    private readonly object _lock = new object();

    public static EmbeddedTcpServer Current { get; private set; }

    public IReadOnlyDictionary<Socket, EmbeddedClientState> Clients
    {
        get
        {
            lock (_lock) { return new Dictionary<Socket, EmbeddedClientState>(_clients); }
        }
    }

    public event Action<EmbeddedClientState> OnClientConnected;
    public event Action<EmbeddedClientState> OnClientDisconnected;

    public EmbeddedTcpServer()
    {
        Current = this;
    }

    public void Start(int port)
    {
        Stop();
        try
        {
            _listenfd = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _listenfd.Bind(new IPEndPoint(IPAddress.Any, port));
            _listenfd.Listen(10);
            _running = true;
            _thread = new Thread(RunLoop) { IsBackground = true, Name = "EmbeddedTcpServer" };
            _thread.Start();
            LanHostManager.Log($"[EmbeddedTcpServer] 启动成功，端口 {port}");
        }
        catch (Exception ex)
        {
            LanHostManager.LogError($"[EmbeddedTcpServer] 启动失败: {ex.Message}");
            Stop();
        }
    }

    public void Stop()
    {
        _running = false;
        try { _listenfd?.Close(); } catch { }
        lock (_lock)
        {
            foreach (var kv in _clients)
            {
                try { kv.Key.Close(); } catch { }
            }
            _clients.Clear();
        }
        if (_thread != null && _thread.IsAlive)
        {
            try { _thread.Join(1000); } catch { }
        }
        _listenfd = null;
        _thread = null;
        if (Current == this) Current = null;
    }

    private void RunLoop()
    {
        while (_running)
        {
            ResetCheckList();
            try
            {
                Socket.Select(_checkList, null, null, 1000);
            }
            catch (Exception ex)
            {
                if (_running) LanHostManager.LogError($"[EmbeddedTcpServer] Select 异常: {ex.Message}");
                continue;
            }
            for (int i = _checkList.Count - 1; i >= 0; i--)
            {
                Socket s = _checkList[i];
                if (s == _listenfd) AcceptClient();
                else ReadClient(s);
            }
        }
    }

    private void ResetCheckList()
    {
        _checkList.Clear();
        if (_listenfd != null) _checkList.Add(_listenfd);
        lock (_lock)
        {
            foreach (var c in _clients.Values) _checkList.Add(c.socket);
        }
    }

    private void AcceptClient()
    {
        try
        {
            Socket clientfd = _listenfd.Accept();
            var state = new EmbeddedClientState
            {
                socket = clientfd,
                tempUserId = GenerateTempUserId(),
                lastPingTime = GetTimeStamp()
            };
            lock (_lock) { _clients.Add(clientfd, state); }
            LanHostManager.Log($"[EmbeddedTcpServer] 接受连接 {clientfd.RemoteEndPoint}，分配 ID {state.tempUserId}");
            OnClientConnected?.Invoke(state);
        }
        catch (Exception ex)
        {
            LanHostManager.LogError($"[EmbeddedTcpServer] Accept 失败: {ex.Message}");
        }
    }

    private string GenerateTempUserId()
    {
        int id = Interlocked.Increment(ref _nextUserId) - 1;
        return id.ToString("D6");
    }

    private void ReadClient(Socket clientfd)
    {
        EmbeddedClientState state;
        lock (_lock)
        {
            if (!_clients.TryGetValue(clientfd, out state)) return;
        }
        ByteArray readBuff = state.readBuff;
        if (readBuff.remain <= 0)
        {
            OnReceiveData(state);
            readBuff.MoveBytes();
        }
        if (readBuff.remain <= 0)
        {
            LanHostManager.LogError("[EmbeddedTcpServer] 接收失败，消息长度超过缓冲区");
            Close(state);
            return;
        }
        int count = 0;
        try
        {
            count = clientfd.Receive(readBuff.bytes, readBuff.writeIndex, readBuff.remain, 0);
        }
        catch (SocketException ex)
        {
            LanHostManager.LogError($"[EmbeddedTcpServer] Receive SocketException: {ex.Message}");
            Close(state);
            return;
        }
        if (count <= 0)
        {
            LanHostManager.Log($"[EmbeddedTcpServer] 客户端断开 {clientfd.RemoteEndPoint}");
            Close(state);
            return;
        }
        readBuff.writeIndex += count;
        OnReceiveData(state);
        readBuff.CheckAndMoveBytes();
    }

    private void OnReceiveData(EmbeddedClientState state)
    {
        ByteArray readBuff = state.readBuff;
        if (readBuff.length <= 2) return;
        int readIdx = readBuff.readIndex;
        byte[] bytes = readBuff.bytes;
        Int16 bodyLength = (Int16)((bytes[readIdx + 1] << 8) | bytes[readIdx]);
        if (readBuff.length < bodyLength) return;
        readBuff.readIndex += 2;
        int nameCount = 0;
        string protoName = MsgBase.DecodeName(readBuff.bytes, readBuff.readIndex, out nameCount);
        if (protoName == "")
        {
            LanHostManager.LogError("[EmbeddedTcpServer] 解码协议名失败");
            Close(state);
            return;
        }
        readBuff.readIndex += nameCount;
        int bodyCount = bodyLength - nameCount;
        if (bodyCount <= 0)
        {
            LanHostManager.LogError("[EmbeddedTcpServer] 协议体长度错误");
            Close(state);
            return;
        }
        MsgBase msgBase = MsgBase.Decode(protoName, readBuff.bytes, readBuff.readIndex, bodyCount);
        readBuff.readIndex += bodyCount;
        readBuff.CheckAndMoveBytes();
        if (msgBase != null)
        {
            DispatchMessage(state, protoName, msgBase);
        }
        if (readBuff.length > 2) OnReceiveData(state);
    }

    private void DispatchMessage(EmbeddedClientState state, string protoName, MsgBase msgBase)
    {
        LanHostManager.Log($"[EmbeddedTcpServer] 收到 {protoName}");
        MethodInfo mi = typeof(EmbeddedMsgHandler).GetMethod(protoName);
        if (mi != null)
        {
            try
            {
                object[] o = { state, msgBase };
                mi.Invoke(null, o);
            }
            catch (Exception ex)
            {
                LanHostManager.LogError($"[EmbeddedTcpServer] 处理 {protoName} 异常: {ex.Message}");
            }
        }
        else
        {
            EmbeddedMsgHandler.OnUnhandledMsg(protoName);
        }
    }

    public void Send(EmbeddedClientState cs, MsgBase msg)
    {
        if (cs == null || cs.socket == null || !cs.socket.Connected) return;
        byte[] sendBytes = EncodeMessage(msg);
        try
        {
            cs.socket.BeginSend(sendBytes, 0, sendBytes.Length, 0, null, null);
        }
        catch (SocketException ex)
        {
            LanHostManager.LogError($"[EmbeddedTcpServer] BeginSend 异常: {ex.Message}");
        }
    }

    public void Broadcast(MsgBase msg)
    {
        byte[] sendBytes = EncodeMessage(msg);
        lock (_lock)
        {
            foreach (var kv in _clients)
            {
                if (kv.Key.Connected)
                {
                    try { kv.Key.BeginSend(sendBytes, 0, sendBytes.Length, 0, null, null); }
                    catch (SocketException ex) { LanHostManager.LogError($"[EmbeddedTcpServer] Broadcast 异常: {ex.Message}"); }
                }
            }
        }
    }

    private byte[] EncodeMessage(MsgBase msg)
    {
        byte[] nameBytes = MsgBase.EncodeName(msg);
        byte[] bodyBytes = MsgBase.Encode(msg);
        int len = nameBytes.Length + bodyBytes.Length;
        byte[] sendBytes = new byte[2 + len];
        sendBytes[0] = (byte)(len % 256);
        sendBytes[1] = (byte)(len / 256);
        Array.Copy(nameBytes, 0, sendBytes, 2, nameBytes.Length);
        Array.Copy(bodyBytes, 0, sendBytes, 2 + nameBytes.Length, bodyBytes.Length);
        return sendBytes;
    }

    public void Close(EmbeddedClientState state)
    {
        if (state == null) return;
        bool removed;
        lock (_lock)
        {
            removed = state.socket != null && _clients.Remove(state.socket);
            try { state.socket?.Close(); } catch { }
        }
        if (removed)
        {
            OnClientDisconnected?.Invoke(state);
        }
    }

    private long GetTimeStamp()
    {
        TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
        return Convert.ToInt64(ts.TotalSeconds);
    }
}
