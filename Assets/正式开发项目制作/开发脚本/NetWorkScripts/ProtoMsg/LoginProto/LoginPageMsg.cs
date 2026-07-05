using MsgFramework;
//登录协议
public class MsgLoginProf : MsgBase
{
    public MsgLoginProf()
    {
        protoName = "MsgLoginProf";
    }
    public int LoginMehod = 0;//0-ID登录 1-角色名登录
    public string Id = "";
    public string Name = "";
    public string pw = "";
    public int result = 0;//0-成功 1-失败
    public int ErrType = 0;//0-密码 1-不存在账号
}
//注册协议
public class MsgRegisterProf : MsgBase
{
    public MsgRegisterProf()
    {
        protoName = "MsgRegisterProf";
    }
    public string pw = "";//客户端填写发送
    public string Id = "";//服务端填写自动生成返回
    public int result = 0;//0-成功 1-失败

}
//提交名字协议(用于第一次注册时候上传角色名)
public class MsgUpdateloadName : MsgBase
{
    public MsgUpdateloadName()
    {
        protoName = "MsgUpdateloadName";
    }
    public string Id = "";
    public string Name = "";
    public int result = 0;//0成功 1失败名称重复
}
public class MsgQuitGame : MsgBase
{
    public MsgQuitGame()
    {
        protoName = "MsgQuitGame";
    }
}