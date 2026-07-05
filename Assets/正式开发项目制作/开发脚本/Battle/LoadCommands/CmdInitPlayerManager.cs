using System.Collections;
using UnityEngine;

public class CmdInitPlayerManager : ILoadCommand
{
    public string Name => "InitPlayerManager";

    public IEnumerator Execute(LoadContext ctx)
    {
        var playerInfos = ctx.BattleCtx.AllPlayers;
        Debug.Log($"[BattleManager] 即将初始化 PlayerManager，本地ID={ctx.BattleCtx.LocalPlayerId}，playerInfos数量={playerInfos?.Count ?? 0}");
        if (playerInfos != null)
        {
            for (int i = 0; i < playerInfos.Count; i++)
            {
                Debug.Log($"[BattleManager] playerInfo[{i}] userId={playerInfos[i].userId}, teamId={playerInfos[i].teamId}, heroId={playerInfos[i].HeroId}");
            }
        }
        PlayerManager.Instance.Init(ctx.BattleCtx, ctx.ModeConfig);
        yield break;
    }
}
