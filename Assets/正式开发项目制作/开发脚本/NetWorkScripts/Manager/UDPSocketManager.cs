using MsgFramework;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading;
using Unity;
using UnityEngine;
using static UnityEngine.Rendering.VirtualTexturing.Debugging;

public class UDPSocketManager:MonoBehaviour
{
    private bool isRunning = false;
    private Thread recvThread;
    private Socket UDPSocket;
    private IPEndPoint ServerIpEndPoint;
    public Action<MsgBase> Handle;
    private static readonly object _msgQueueLock=new object();

    private Queue<MsgBase> msgQueue = new Queue<MsgBase>();

    private static UDPSocketManager instance;
    private byte[] _udpBuffer;
    public static UDPSocketManager Instance
    {
        get
        {
            if(instance == null)
            {
                if(Application.isPlaying)
                {
                    GameObject o = new GameObject("UDPSocketManager");
                    instance = o.AddComponent<UDPSocketManager>();
                    DontDestroyOnLoad(o);
                }
            }
            return instance;
        }
    }

    public void InitUDPSocket()
    {
       _udpBuffer = new byte[4096];
        UDPSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        try
        {
            ServerIpEndPoint = new IPEndPoint(IPAddress.Parse(ServerConfig.ServerIp), ServerConfig.UdpPort);
            UDPSocket.Connect(ServerIpEndPoint);
            Thread.Sleep(100);
            recvThread = new Thread(RecvMsg);
            recvThread.IsBackground = true;
            isRunning = true;
            recvThread.Start();
        }
        catch(Exception ex) { 
            Debug.Log(ex.Message);
        }

    }

    private void RecvMsg()
    {
        while(isRunning)
        {
            try
            {
                int readIndex = 0;
                int recvLen = UDPSocket.Receive(_udpBuffer); ;
                if (recvLen <= 0 || recvLen > 4096)
                {
                    Console.WriteLine($"超过最大数据范围{recvLen}");
                    continue;
                }
                Int16 bodyLen = (Int16)((_udpBuffer[readIndex + 1] << 8) | _udpBuffer[readIndex]);
                readIndex += 2;
                int nameCount = 0;
                //解析协议名字
                string nameProto = MsgBase.DecodeName(_udpBuffer, readIndex, out nameCount);
                if (nameProto == "")
                {
                    Console.WriteLine("UDP Receive data wrong!");
                    continue;
                }
                readIndex += nameCount;
                int bodyCount = bodyLen - nameCount;
                MsgBase msg = MsgBase.Decode(nameProto, _udpBuffer, readIndex, bodyCount);
                if (msg == null)
                {
                    Console.WriteLine($"UDP解析消息体失败，协议名：{nameProto}");
                    continue;
                }
                lock(_msgQueueLock)
                {
                    msgQueue.Enqueue(msg);
                }
            }
            catch (Exception ex){ 
                Debug.Log("错误:"+ex.Message);
            }

        }
    }
    private void Update()
    {
        lock( _msgQueueLock )
        {
            while(msgQueue.Count > 0)
            {
                MsgBase msg=msgQueue.Dequeue();
                Handle?.Invoke(msg);
            }
        }
    }
    public void Send(MsgBase msg)
    {
        if (msg == null)
        {
            Debug.Log("发送失败");
            return;
        }
        byte[] nameBytes = MsgBase.EncodeName(msg);
        byte[] bodyBytes = MsgBase.Encode(msg);
        int len = nameBytes.Length + bodyBytes.Length;
        byte[] sendBytes = new byte[len + 2];
        //组装长度
        sendBytes[0] = (byte)(len % 256);
        sendBytes[1] = (byte)(len / 256);
        //组装名字
        Array.Copy(nameBytes, 0, sendBytes, 2, nameBytes.Length);
        //组装消息体
        Array.Copy(bodyBytes, 0, sendBytes, 2 + nameBytes.Length, bodyBytes.Length);

        try
        {
            UDPSocket.SendTo(sendBytes, ServerIpEndPoint);
        }
        catch(Exception ex)
        {
            Debug.Log("发送失败" + ex);
        }
    }

    private void OnApplicationQuit()
    {
        if(BattleManager.IsInBattle)
        {
            Send(new MsgPlayerExit()
            {
                roomId = BattleData.Instance.BattleId,
                userId = BattleData.Instance.MyId
            });
        }
        CloseUdpSocket();
    }
    public void OnDestroy()
    {
        CloseUdpSocket();
    }

    private void CloseUdpSocket()
    {
        isRunning = false;
        if(UDPSocket!=null)
        {
            UDPSocket.Close();
            UDPSocket.Dispose();
            UDPSocket = null;
        }
    }
}