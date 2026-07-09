using System.Collections;

public class CmdRegisterBattleMsg : ILoadCommand
{
    public string Name => "RegisterBattleMsg";

    public IEnumerator Execute(LoadContext ctx)
    {
        NetWorkMgr.AddMsgListener("MsgBattleReady", ctx.OnBattleReadyMsgHandler);
        NetWorkMgr.AddMsgListener("MsgFramePack", ctx.OnFramePackMsgHandler);
        NetWorkMgr.AddMsgListener("MsgBattleOver", ctx.OnFramePackMsgHandler);
        BattleData.Instance.Init(ctx.BattleCtx);
        yield break;
    }
}
