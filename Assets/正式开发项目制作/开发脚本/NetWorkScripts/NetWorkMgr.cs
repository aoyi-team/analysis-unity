using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Sockets;
using System.IO;
using System;
using System.Linq;
using MsgFramework;

public class NetWorkMgr : MonoBehaviour
{
    /// <summary>
    /// 变量定义
    /// </summary>
    //是否启用心跳检测
    public  static bool IsUsePing = false;
    public enum NetBehaviour
    {
        ConnectSuc, ConnectFail, Close
    }
    //接收缓冲区
    static ByteArray readBuff;
    //写入队列
    static Queue<ByteArray> writeQueue;
    //事件委托类型
    public delegate void EventListener(string err);
    //事件监听列表
    private static Dictionary<NetBehaviour, EventListener> eventListeners = new Dictionary<NetBehaviour, EventListener>();
    //是否启用心跳
    public static bool IsUsingPing = true;
    //心跳间隔
    public static int pingInterval = 30;
    //上一次发送Ping时间
    static float LastPingTime = 0;
    //上一次收到Pong时间
    static float LastPongTime = 0;
    //消息列表
    static List<MsgBase> msgList = new List<MsgBase>();
    //消息列表长度
    static int msgCount = 0;
    //每次处理的最大消息量
    readonly static int MAX_MESSAGE_FIRE = 10;
    //消息委托类型
    public delegate void MsgListener(MsgBase msgBase);
    //消息监听列表
    public static Dictionary<string, MsgListener> msgListeners = new Dictionary<string, MsgListener>();
    //事件
    static Socket socket;//套接字
    static bool IsConnecting=false;//是否正在连接
    static bool IsClosing=false;//是否正在关闭
    //网络管理器单例模式
    private static NetWorkMgr _instance;


    public static NetWorkMgr Instance
    {
        get
        {
            if( _instance == null )
            {
                GameObject obj = new GameObject("NetWorkMgr");
                _instance= obj.AddComponent<NetWorkMgr>();
                DontDestroyOnLoad(obj);
            }
            return _instance;
        }
    }
    private void Start()
    {
        if(_instance!=this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }
    //初始化
    private static void InitState()
    {
        //Socket
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        //接收缓冲区
        readBuff = new ByteArray();
        writeQueue = new Queue<ByteArray>();
        //是否正在连接
        IsConnecting = false;
        //是否正在关闭
        IsClosing = false;
        //消息列表
        msgList = new List<MsgBase>();
        //消息列表长度
        msgCount = 0;
        //上次发送Ping时间
        LastPingTime = Time.time;
        //上次收到Pong时间
        LastPongTime = Time.time;
        //监听Pong协议
        if(msgListeners.ContainsKey("MsgPong"))
        {
            AddMsgListener("MsgPong", OnMsgPong);
        }
    }
    //监听PONG协议
    private static void OnMsgPong(MsgBase msg) { LastPongTime = Time.time; }
    //建立连接
    public void Connect(string ip,int port)
    {
        //判断当前状态
        if(socket!=null&&socket.Connected)
        { return; }
        if(IsConnecting) { return; }
        //初始化成员
        InitState();
        //Connect开始连接
        IsConnecting =true;
        socket.NoDelay = true;
        socket.BeginConnect(ip,port,ConnectCallBack,socket);
    }
    //socket接收回调
    private static void ConnectCallBack(IAsyncResult ar)
    {
        try
        {
            Socket socket = (Socket)ar.AsyncState;
            socket.EndConnect(ar);
            IsConnecting = false;
            socket.BeginReceive(readBuff.bytes, readBuff.writeIndex, readBuff.remain, 0, ReceiveCallback, socket);
        }
        catch(SocketException ex)
        {
            Debug.Log(ex.ToString());
            IsConnecting=!false;
        }
    }
    //判断连接是否存在
    public bool IsConnected()
    {
        return socket != null && socket.Connected;
    }
    //Receive回调
    public static void ReceiveCallback(IAsyncResult ar)
    {
        try
        {
            Socket socket = (Socket)ar.AsyncState;
            //获取接收数据长度
            int count = socket.EndReceive(ar);
            readBuff.writeIndex += count;
            //处理二进制消息
            OnReceiveData();
            //继续接收数据
            if (readBuff.remain < 8)
            {
                readBuff.MoveBytes();
                readBuff.ReSize(readBuff.length * 2);
            }
            socket.BeginReceive(readBuff.bytes, readBuff.writeIndex,
                    readBuff.remain, 0, ReceiveCallback, socket);
        }
        catch (SocketException ex)
        {
            Debug.Log("Socket Receive fail" + ex.ToString());
        }
    }
    //数据处理
    public static void OnReceiveData()
    {
        //消息长度
        if (readBuff.length <= 2)
        {
            return;
        }
        //获取消息体长度
        int readIdx = readBuff.readIndex;
        byte[] bytes = readBuff.bytes;
        Int16 bodyLength = (Int16)((bytes[readIdx + 1] << 8) | bytes[readIdx]);
        if (readBuff.length < bodyLength)
            return;
        readBuff.readIndex += 2;
        //解析协议名
        int nameCount = 0;
        string protoName = MsgBase.DecodeName(readBuff.bytes, readBuff.readIndex, out nameCount);
        if (protoName == "")
        {
            Debug.Log("OnReceiveData MsgBase.DecodeName fail");
            return;
        }
        readBuff.readIndex += nameCount;
        //解析协议体
        int bodyCount = bodyLength - nameCount;
        MsgBase msgBase = MsgBase.Decode(protoName, readBuff.bytes, readBuff.readIndex, bodyCount);
        readBuff.readIndex += bodyCount;
        readBuff.CheckAndMoveBytes();
        //添加到消息队列
        lock (msgList)
        {
            msgList.Add(msgBase);
            msgCount++;
        }
        //继续读取消息
        if (readBuff.length > 2)
        {
            OnReceiveData();
        }
    }
    //添加消息监听
    public static void AddMsgListener(string msgName, MsgListener listener)
    {
        //添加
        if (msgListeners.ContainsKey(msgName))
        {
            msgListeners[msgName] += listener;
        }
        //新增
        else
        {
            msgListeners[msgName] = listener;
        }
    }
    //删除消息监听
    public static void RemoveMsgListener(string msgName, MsgListener listener)
    {
        if (msgListeners.ContainsKey(msgName))
        {
            msgListeners[msgName] -= listener;
        }
    }
    //执行消息
    private static void FireMsg(string msgName, MsgBase msgBase)
    {
        if (msgListeners.ContainsKey(msgName))
        {
            msgListeners[msgName](msgBase);
        }
    }
    //更新消息
    public static void MsgUpdate()
    {
        //初步判断，提升效率
        if (msgCount == 0)
        {
            return;
        }
        //重复处理消息
        for (int i = 0; i < MAX_MESSAGE_FIRE; i++)
        {
            //获取第一条消息
            MsgBase msgBase = null;
            lock (msgList)
            {
                if (msgList.Count > 0)
                {
                    msgBase = msgList[0];
                    msgList.RemoveAt(0);
                    msgCount--;
                }
            }
            //分发消息
            if (msgBase != null)
            {
                FireMsg(msgBase.protoName, msgBase);
            }
            //没有消息了
            else
            {
                break;
            }
        }
    }

    //发送PING协议
    private static void PingUpdate()
    {
        //是否启用
        if (!IsUsePing)
        {
            return;
        }
        //发送PING
        if (Time.time - LastPingTime > pingInterval)
        {
            MsgPing msgPing = new MsgPing();
            Send(msgPing);
            LastPingTime = Time.time;
        }
        //检测PONG时间
        if (Time.time - LastPongTime > pingInterval * 4)
        {
            Close();
        }
    }
    //关闭连接
    public static void Close()
    {
        //状态判断
        if (socket == null || !socket.Connected)
        {
            return;
        }
        if (IsConnecting)
        {
            return;
        }
        //还有数据在发送
        if (writeQueue.Count > 0)
        {
            IsClosing = true;
        }
        //没有数据在发送
        else
        {
            socket.Close();
        }
    }
    //发送数据
    public static void Send(MsgBase msg)
    {
        //状态判断
        if (socket == null || !socket.Connected)
        {
            return;
        }
        if (IsConnecting)
        {
            return;
        }
        if (IsClosing)
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
        //写入队列
        ByteArray ba = new ByteArray(sendBytes);
        int count = 0;  //writeQueue的长度
        lock (writeQueue)
        {
            writeQueue.Enqueue(ba);
            count = writeQueue.Count;
        }
        //send
        if (count == 1)
        {
            socket.BeginSend(sendBytes, 0, sendBytes.Length,
                0, SendCallback, socket);
        }
    }
    //Send回调
    public static void SendCallback(IAsyncResult ar)
    {

        //获取state、EndSend的处理
        Socket socket = (Socket)ar.AsyncState;
        //状态判断
        if (socket == null || !socket.Connected)
        {
            return;
        }
        //EndSend
        int count = socket.EndSend(ar);
        //获取写入队列第一条数据            
        ByteArray ba;
        lock (writeQueue)
        {
            ba = writeQueue.First();
        }
        //完整发送
        ba.readIndex += count;
        if (ba.length == 0)
        {
            lock (writeQueue)
            {
                writeQueue.Dequeue();
                ba = writeQueue.First();
            }
        }
        //继续发送
        if (ba != null)
        {
            socket.BeginSend(ba.bytes, ba.readIndex, ba.length,
                0, SendCallback, socket);
        }
        //正在关闭
        else if (IsClosing)
        {
            socket.Close();
        }
    }
    //Update
    private void Update()
    {
        PingUpdate();
        MsgUpdate();
    }
    //关闭游戏应用时调用
    private void OnApplicationQuit()
    {
        Send(new MsgQuitGame());
        Close();
    }
}
