using System;

public partial class EventHandler
{
    public static void OnTimer()
    {
        CheckPing();
    }
    //Ping检查
    public static void CheckPing()
    {
        //现在的时间戳
        long timeNow = NetManager.GetTimeStamp();
        //遍历，删除
        foreach (ClientState s in NetManager.clients.Values)
        {
            if (timeNow - s.lastPingTime > NetManager.pingInterval * 4)
            {
                Console.WriteLine("Ping Close " + s.socket.RemoteEndPoint.ToString());
                NetManager.Close(s);
                return;
            }
        }
    }
}
