using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CmdGameLoadFinish : ILoadCommand
{
    public string Name => "GameLoadFinish";

    public IEnumerator Execute(LoadContext ctx)
    {
        ctx.StartBattleReadyLoop?.Invoke();
        yield break;
    }
}
