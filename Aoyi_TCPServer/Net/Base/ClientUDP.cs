using MsgFramework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.UI.WebControls;

//UDP绑定serverConfig定义的端口。
class ClientUDP
{
    private Socket server;
    public const int SIO_UDP_CONNRESET = -1744830452;
    private static ClientUDP instance;
    private static readonly object padlock = new object();
    private byte[] _recvBuffer = new byte[4096]; // 固定缓冲区，避免频繁GC

    public static ClientUDP Instance { get { 
            lock(padlock)
            {
                if (instance == null)
                {
                    instance = new ClientUDP();
                }
            }
            return instance;
        } }

    //关闭服务端程序的时候调用该方法
    public void CloseClientUDP()
    {
        try
        {

            server.Shutdown(SocketShutdown.Both);
            server.Close();
            server.Dispose();

            Console.WriteLine("UDPSocket已经安全关闭");
        }
        catch(Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    public void InitClientUDP()
    {
        server =new Socket(AddressFamily.InterNetwork,SocketType.Dgram, ProtocolType.Udp);
        IPEndPoint endPoint=new IPEndPoint(IPAddress.Any,ServerConfig.UdpServerPort);

        server.Bind(endPoint);
        Console.WriteLine("启动udp" + endPoint);

        server.IOControl(SIO_UDP_CONNRESET,new byte []{ 0,0,0,0},null);

        Thread recvThread = new Thread(RecvThread) { IsBackground = true, Name = "Udp_RecvThread" };

        recvThread.Start();
        Console.Write(" Udp_RecvThread 线程启动成功");

    }

    //接收消息循环
    private void RecvThread()
    {
        EndPoint remotePoint = new IPEndPoint(IPAddress.Any, 0);
        while (true)
        {
            try
            {
                int readIndex = 0;
                int recvLen = server.ReceiveFrom(_recvBuffer, ref remotePoint);
                if (recvLen <= 0 || recvLen > 4096)
                {
                    Console.WriteLine($"超过最大数据范围{recvLen}");
                    continue;
                }
                Int16 bodyLen = (Int16)((_recvBuffer[readIndex + 1] << 8) | _recvBuffer[readIndex]);
                readIndex += 2;
                int nameCount = 0;
                //解析协议名字
                string nameProto = MsgBase.DecodeName(_recvBuffer,readIndex,out nameCount);
                if (nameProto == "")
                {
                    Console.WriteLine("UDP Receive data wrong!");
                    continue;
                }
                readIndex += nameCount;
                int bodyCount = bodyLen - nameCount;
                MsgBase msg = MsgBase.Decode(nameProto, _recvBuffer, readIndex, bodyCount);
                if (msg == null)
                {
                    Console.WriteLine($"UDP解析消息体失败，协议名：{nameProto}，发送方：{remotePoint}");
                    continue;
                }
                switch(nameProto)
                {
                    case "MsgPlayerOp":
                        {
                            HandlePlayerOp((MsgPlayerOp)msg);
                            break;
                        }
                    case "MsgBattleReady":
                        {
                            HandleBattleReady((MsgBattleReady)msg, remotePoint);
                            break;
                        }
                    case "MsgBattleOver":
                        {
                            HandleBattleOver((MsgBattleOver)msg,remotePoint);
                            break;
                        }
                    case "MsgPlayerExit":
                        {
                            HandlePlayerExit((MsgPlayerExit)msg ,remotePoint);
                            break;
                        }
                    default:
                        {
                            Console.WriteLine($"UDP未处理协议：{nameProto}");
                            break;
                        }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("接收失败"+e);
            }
        }
    }

    //udp发送方案
    public void Send(MsgBase msg,IPEndPoint remoteIpEndPoint)
    {
        if(msg == null||remoteIpEndPoint== null)
        {
            Console.WriteLine($"Udp发送失败:{remoteIpEndPoint}"); return;
        }
        byte[] nameBytes=MsgBase.EncodeName(msg);
        byte[] bodyBytes = MsgBase.Encode(msg);
        int len=nameBytes.Length+bodyBytes.Length;
        byte[] sendBytes=new byte[len+2];
        //组装长度
        sendBytes[0] = (byte)(len % 256);
        sendBytes[1] = (byte)(len / 256);
        //组装名字
        Array.Copy(nameBytes, 0, sendBytes, 2, nameBytes.Length);
        //组装消息体
        Array.Copy(bodyBytes, 0, sendBytes, 2 + nameBytes.Length, bodyBytes.Length);

        try
        {
            server.SendTo(sendBytes, remoteIpEndPoint);
        }
        catch (Exception ex)
        {
            Console.WriteLine("发送错误:" + ex);
        }

    }

    #region 调用对应battle接口
    private void HandlePlayerOp(MsgPlayerOp msg)
    {
        if(msg.roomId==null||msg.playerId<=0)
        {
            Console.WriteLine($"非法操作包：roomId或playerId为空");
            return;
        }
        if(!BattleManager.Instance._BattleInstanceDic.TryGetValue(msg.roomId,out  BattleInstance battle))
        {
            Console.WriteLine($"不存在房间{msg.roomId}");
            return;
        }
        var playerId=msg.playerId.ToString("D6");
        if (!battle.IsPlayerContain(msg.playerId.ToString("D6")))
        {
            Console.WriteLine($"玩家{playerId}不在房间{msg.roomId}，丢弃操作");
            return;
        }
        battle.HandleMsg(msg);
    }

    private void HandleBattleReady(MsgBattleReady msg,EndPoint remotEndpoint)
    {
        if (msg.roomId == null || msg.userId <= 0)
        {
            Console.WriteLine($"非法操作包：roomId或playerId为空");
            return;
        }
        if (!BattleManager.Instance._BattleInstanceDic.TryGetValue(msg.roomId, out BattleInstance battle))
        {
            Console.WriteLine($"不存在房间{msg.roomId}");
            return;
        }
        var playerId = msg.userId.ToString("D6");
        if (!battle.IsPlayerContain(msg.userId.ToString("D6")))
        {
            Console.WriteLine($"玩家{playerId}不在房间{msg.roomId}，丢弃操作");
            return;
        }
        battle.RecordPlayerUdpEndPoint(playerId, (IPEndPoint)remotEndpoint);
        Console.WriteLine($"记录玩家{playerId},IpEndPoint为:{remotEndpoint}");
        battle.HandleMsg(msg);
    }
    private void HandleBattleOver(MsgBattleOver msg, EndPoint remotEndpoint)
    {
        if (!BattleManager.Instance._BattleInstanceDic.TryGetValue(msg.roomId, out BattleInstance battle))
        {
            Console.WriteLine($"不存在房间{msg.roomId}");
            return;
        }
        battle.HandleMsg(msg);
    }
    private void HandlePlayerExit(MsgPlayerExit msg, EndPoint remotEndpoint)
    {
        if (!BattleManager.Instance._BattleInstanceDic.TryGetValue(msg.roomId, out BattleInstance battle))
        {
            Console.WriteLine($"不存在房间{msg.roomId}");
            return;
        }
        battle.HandleMsg(msg);
    }
    #endregion
}
