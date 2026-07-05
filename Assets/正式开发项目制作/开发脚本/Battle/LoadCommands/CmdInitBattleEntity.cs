using System.Collections;

public class CmdInitBattleEntity : ILoadCommand
{
    public string Name => "InitBattleEntity";

    public IEnumerator Execute(LoadContext ctx)
    {
        BattleEntityManager.Instance.Init(ctx.ModeConfig);
        yield break;
    }
}
