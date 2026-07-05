using System.Collections;
using UnityEngine;

public class CmdLoadModeConfig : ILoadCommand
{
    public string Name => "LoadModeConfig";

    public IEnumerator Execute(LoadContext ctx)
    {
        // paiwei_solo 复用 paiwei 的 ModeConfig
        GameModes mode = ctx.BattleCtx.GameMode == GameModes.paiwei_solo ? GameModes.paiwei : ctx.BattleCtx.GameMode;
        ctx.ModeConfig = ResMgr.LoadResource<ModeConfig>($"ModeConfigs/{mode}_ModeConfig");
        if (ctx.ModeConfig == null)
        {
            Debug.LogWarning($"[CmdLoadModeConfig] ModeConfigs/{mode}_ModeConfig 未找到，降级使用 dantiao_ModeConfig");
            ctx.ModeConfig = ResMgr.LoadResource<ModeConfig>("ModeConfigs/dantiao_ModeConfig");
        }
        if (ctx.ModeConfig == null)
        {
            Debug.LogError("[CmdLoadModeConfig] dantiao_ModeConfig 也未找到，无法初始化战斗");
        }
        yield break;
    }
}
