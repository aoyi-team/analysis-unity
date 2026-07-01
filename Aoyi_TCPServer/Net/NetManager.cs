using MsgFramework;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Reflection;

public class NetManager
{
    //CheckReadList用于SelectSocket监听
    static List<Socket> CheckList=new List<Socket>();
    //已连接的客户端Socket存储
    public static Dictionary<Socket,ClientState> clients=new Dictionary<Socket,ClientState>();
    //活跃状态客户端
    public static Dictionary<string,ClientState> _activeClients=new Dictionary<string,ClientState>();

    //对局相关<对局房间id,房间内玩家>后续可能用到，先放着
    //public static Dictionary<string,List<string>> _roomDicPlayers=new Dictionary<string,List<string>>();

    //服务器Socket，用于监听
    public static Socket listenfd;
    //服务器Ping间隔
    public static long pingInterval = 30;
    //循环监听
    public static void StartLoop(int port)
    {
        listenfd=new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        //Bind
        IPAddress ipadr=IPAddress.Parse("127.0.0.1");
        IPEndPoint ipend = new IPEndPoint(ipadr, port);
        listenfd.Bind(ipend);
        //开始监听
        listenfd.Listen(0);//设置最大监听量
        Console.WriteLine("服务器启动成功");
        while(true)
        {
            ResetCheckList();
             Socket.Select(CheckList, null, null, 1000);
            //检出可读写的对象
            for(int i=CheckList.Count-1;i>=0;i--)
            {
                Socket s = CheckList[i];
                if (s == listenfd)
                {
                    ReadListenfd(s);
                }
                else
                {
                    ReadClientfd(s);
                }
            }
            Timer();
        }

    }
    //关闭连接
    public static void Close(ClientState state)
    {
        //关闭
        state.socket.Close();
        clients.Remove(state.socket);
        if (state.userId!=null&& _activeClients.ContainsKey(state.userId))
        {
            _activeClients.Remove(state.userId);
        }

    }
    //获取活跃状态clientState(通过activeDictionary)
    public static ClientState GetActiveClientById(string userId)
    {
        if(_activeClients.ContainsKey(userId))return _activeClients[userId];
        return null;
    }
    public static void AddActiveClientDic(string userId,ClientState state)
    {
        if (!_activeClients.ContainsKey(userId)) _activeClients.Add(userId, state);
        _activeClients[userId] = state;
    }

    //读取Clientfd
    public static void ReadClientfd(Socket clientfd)
    {
        Console.WriteLine("有收到客户端消息");
        ClientState state = clients[clientfd];
        ByteArray readBuff = state.readBuff;
        //接收
        int count = 0;
        //缓冲区不够，清除，若依旧不够，只能返回
        //当单条协议超过缓冲区长度时会发生
        if (readBuff.remain <= 0)
        {
            OnReceiveData(state);
            readBuff.MoveBytes();
        };
        if (readBuff.remain <= 0)
        {
            Console.WriteLine("Receive fail , maybe msg length > buff capacity");
            Close(state);
            return;
        }
        try
        {
            count = clientfd.Receive(readBuff.bytes, readBuff.writeIndex, readBuff.remain, 0);
        }
        catch (SocketException ex)
        {
            Console.WriteLine("Receive SocketException " + ex.ToString());
            Close(state);
            return;
        }
        //客户端关闭
        if (count <= 0)
        {
            Console.WriteLine("Socket Close " + clientfd.RemoteEndPoint.ToString());
            Close(state);
            return;
        }
        //消息处理
        readBuff.writeIndex += count;
        //处理二进制消息
        OnReceiveData(state);
        //移动缓冲区
        readBuff.CheckAndMoveBytes();
    }


    //数据处理
    public static void OnReceiveData(ClientState state)
    {
        ByteArray readBuff = state.readBuff;
        //消息长度
        if (readBuff.length <= 2)
        {
            return;
        }
        //消息体长度
        int readIdx = readBuff.readIndex;
        byte[] bytes = readBuff.bytes;
        Int16 bodyLength = (Int16)((bytes[readIdx + 1] << 8) | bytes[readIdx]);
        if (readBuff.length < bodyLength)
        {
            return;
        }
        readBuff.readIndex += 2;
        //解析协议名
        int nameCount = 0;
        string protoName = MsgBase.DecodeName(readBuff.bytes, readBuff.readIndex, out nameCount);
        if (protoName == "")
        {
            Console.WriteLine("OnReceiveData MsgBase.DecodeName fail");
            Close(state);
            return;
        }
        readBuff.readIndex += nameCount;
        //解析协议体
        int bodyCount = bodyLength - nameCount;
        if (bodyCount <= 0)
        {
            Console.WriteLine("OnReceiveData fail, bodyCount <=0 ");
            Close(state);
            return;
        }
        MsgBase msgBase = MsgBase.Decode(protoName, readBuff.bytes, readBuff.readIndex, bodyCount);
        readBuff.readIndex += bodyCount;
        readBuff.CheckAndMoveBytes();
        //分发消息
        MethodInfo mi = typeof(MsgHandler).GetMethod(protoName);
        object[] o = { state, msgBase };
        Console.WriteLine("Receive " + protoName);
        if (mi != null)
        {
            mi.Invoke(null, o);
        }
        else
        {
            Console.WriteLine("OnReceiveData Invoke fail " + protoName);
        }
        //继续读取消息
        if (readBuff.length > 2)
        {
            OnReceiveData(state);
        }
    }

    //读取连接请求监听
    public static void ReadListenfd(Socket listenfd)
    {
        try
        {
            Socket clientfd = listenfd.Accept();
            Console.WriteLine("Accept " + clientfd.RemoteEndPoint.ToString());
            ClientState state = new ClientState();
            state.socket = clientfd;
            state.lastPingTime = GetTimeStamp();
            clients.Add(clientfd, state);
        }
        catch (SocketException ex)
        {
            Console.WriteLine("Accept fail" + ex.ToString());
        }
    }
    //获取时间戳
    public static long GetTimeStamp()
    {
        TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
        return Convert.ToInt64(ts.TotalSeconds);
    }
    //填充ChecList列表用于Select选择
    public static void ResetCheckList()
    {
        CheckList.Clear();
        CheckList.Add(listenfd);
        foreach(ClientState c in clients.Values)
        {
            CheckList.Add(c.socket);
        }
    }
    //发送
    public static void Send(ClientState cs, MsgBase msg)
    {
        //状态判断
        if (cs == null)
        {
            return;
        }
        if (!cs.socket.Connected)
        {
            return;
        }
        //数据编码
        byte[] nameBytes = MsgBase.EncodeName(msg);
        byte[] bodyBytes = MsgBase.Encode(msg);
        int len = nameBytes.Length + bodyBytes.Length;
        byte[] sendBytes = new byte[2 + len];
        //组装长度
        sendBytes[0] = (byte)(len % 256);
        sendBytes[1] = (byte)(len / 256);
        //组装名字
        Array.Copy(nameBytes, 0, sendBytes, 2, nameBytes.Length);
        //组装消息体
        Array.Copy(bodyBytes, 0, sendBytes, 2 + nameBytes.Length, bodyBytes.Length);
        //为简化代码，不设置回调
        try
        {
            cs.socket.BeginSend(sendBytes, 0, sendBytes.Length, 0, null, null);
        }
        catch (SocketException ex)
        {
            Console.WriteLine("Socket Close on BeginSend" + ex.ToString());
        }

    }
    //定时器
    static void Timer()
    {
        //消息分发
        MethodInfo mei = typeof(EventHandler).GetMethod("OnTimer");
        object[] ob = { };
        mei.Invoke(null, ob);
    }
}
