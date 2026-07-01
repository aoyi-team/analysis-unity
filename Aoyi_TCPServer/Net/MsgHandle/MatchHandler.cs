// 服务端MsgHandler扩展匹配请求处理
using MsgFramework;

public partial class MsgHandler
{
    //匹配请求
    public static void MsgMatchRequest(ClientState c, MsgBase msgBase)
    {
        MsgMatchRequest req = (MsgMatchRequest)msgBase;
        MatchManager.Instance.HandleMatchRequest(c, req);
    }
    //取消匹配请求
    public static void MsgExitRequest(ClientState c,MsgBase msgBase)
    {
        MsgExitRequest req = (MsgExitRequest)msgBase;
        MatchManager.Instance.HandleExitRequest(c, req);
    }
}