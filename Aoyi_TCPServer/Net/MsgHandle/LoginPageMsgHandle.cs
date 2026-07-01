using MsgFramework;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;

/// <summary>
/// 该部分记录登录页面协议反馈
/// </summary>
public partial class MsgHandler
{
    //处理注册协议
    public static void MsgRegisterProf(ClientState c,MsgBase msgBase)
    {
        MsgRegisterProf msg=(MsgRegisterProf)msgBase;
        //数据库处理 注册
        string id = DbManager.Register(msg.pw);
        if(id!=null)
        {
            msg.result = 0;
            msg.Id = id;
            c.UpdateUserId(id);
            NetManager.AddActiveClientDic(msg.Id, c);
            Console.WriteLine($"玩家进入游戏{c.userId}");
        }
        else msg.result = 1;
        NetManager.Send(c, msg);
    }
    //处理登录协议
    public static void MsgLoginProf(ClientState c,MsgBase msgBase)
    {
        MsgLoginProf msg=(MsgLoginProf)msgBase;
        LoginResult loginresult = DbManager.LoginCheck(msg.LoginMehod, msg.Id, msg.Name, msg.pw);
        msg.result = loginresult.Result;
        msg.ErrType= loginresult.ErrType;
        NetManager.Send(c, msg);
        if (msg.result == 0)
        {
            c.UpdateUserInfo(msg.Id, msg.Name);
            NetManager.AddActiveClientDic(msg.Id, c);
            Console.WriteLine($"玩家进入游戏{c.userId}");
        }
        Console.WriteLine("MsgLoginProf有发送");
    }
    //处理上传名字协议
    public static void MsgUpdateloadName(ClientState c,MsgBase msgBase)
    {
        MsgUpdateloadName msg=(MsgUpdateloadName)msgBase;
        if(DbManager.IsNameExist(msg.Name))
        {
            msg.result = 1;
            NetManager.Send(c, msg);
            return;
        }
        bool updateNameCheck=DbManager.UpdateAccountName(msg.Id,msg.Name);
        if(updateNameCheck)
        {
            msg.result = 0;
            c.UpdateUserInfo(msg.Id, msg.Name);
            NetManager.AddActiveClientDic(msg.Id, c);
            NetManager.Send(c, msg);
            return;
        }
        else
        {
            msg.result = 1;
            NetManager.Send(c, msg);
        }
    }
    public static void MsgQuitGame(ClientState c,MsgBase msgBase)
    {
        NetManager.Close(c);
    }
}
