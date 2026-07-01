using MsgFramework;

/// <summary>
/// 内嵌主机 TCP 消息处理器
/// </summary>
public static class EmbeddedMsgHandler
{
    // 处理登录协议（局域网模式跳过账号校验，直接返回成功）
    public static void MsgLoginProf(EmbeddedClientState c, MsgBase msgBase)
    {
        MsgLoginProf msg = (MsgLoginProf)msgBase;
        msg.result = 0;
        msg.ErrType = 0;
        msg.Id = c.tempUserId;
        if (string.IsNullOrEmpty(msg.Name))
        {
            msg.Name = $"Player_{c.tempUserId}";
        }
        c.userName = msg.Name;
        c.isReady = true;
        EmbeddedTcpServer.Current?.Send(c, msg);
        LanHostManager.Log($"[EmbeddedMsgHandler] 玩家 {c.tempUserId} 登录成功");
    }

    // 处理注册协议（局域网模式直接返回成功）
    public static void MsgRegisterProf(EmbeddedClientState c, MsgBase msgBase)
    {
        MsgRegisterProf msg = (MsgRegisterProf)msgBase;
        msg.result = 0;
        msg.Id = c.tempUserId;
        EmbeddedTcpServer.Current?.Send(c, msg);
        LanHostManager.Log($"[EmbeddedMsgHandler] 玩家 {c.tempUserId} 注册成功");
    }

    // 处理上传名字协议（局域网模式直接返回成功）
    public static void MsgUpdateloadName(EmbeddedClientState c, MsgBase msgBase)
    {
        MsgUpdateloadName msg = (MsgUpdateloadName)msgBase;
        msg.result = 0;
        msg.Id = c.tempUserId;
        if (!string.IsNullOrEmpty(msg.Name))
        {
            c.userName = msg.Name;
        }
        EmbeddedTcpServer.Current?.Send(c, msg);
        LanHostManager.Log($"[EmbeddedMsgHandler] 玩家 {c.tempUserId} 更新名字成功");
    }

    // 处理心跳
    public static void MsgPing(EmbeddedClientState c, MsgBase msgBase)
    {
        c.lastPingTime = GetTimeStamp();
        EmbeddedTcpServer.Current?.Send(c, new MsgPong());
    }

    // 处理退出游戏
    public static void MsgQuitGame(EmbeddedClientState c, MsgBase msgBase)
    {
        LanHostManager.Log($"[EmbeddedMsgHandler] 玩家 {c.tempUserId} 退出游戏");
        EmbeddedTcpServer.Current?.Close(c);
    }

    // 未处理协议记录日志
    public static void OnUnhandledMsg(string protoName)
    {
        LanHostManager.Log($"[EmbeddedMsgHandler] 收到未处理协议 {protoName}");
    }

    private static long GetTimeStamp()
    {
        System.TimeSpan ts = System.DateTime.UtcNow - new System.DateTime(1970, 1, 1, 0, 0, 0, 0);
        return System.Convert.ToInt64(ts.TotalSeconds);
    }
}
