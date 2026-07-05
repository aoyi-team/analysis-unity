using System.Collections;
using UnityEngine;

public class CmdLoadBattleScene : ILoadCommand
{
    public string Name => "LoadBattleScene";

    public IEnumerator Execute(LoadContext ctx)
    {
        // Mirror 模式下场景已由 NetworkRoomManager 自动加载，跳过
        if (global::Mirror.NetworkServer.active || global::Mirror.NetworkClient.active)
        {
            Debug.Log($"[CmdLoadBattleScene] Mirror 已加载场景，跳过。当前场景: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
            if (ctx.ModeConfig != null)
                Debug.Log($"成功加载模式配置!模式:{ctx.BattleCtx.GameMode},地图:{ctx.ModeConfig.gameMode}");
            yield break;
        }

        var asyop = SceneMgr.Instance.LoadSceneByName(ctx.BattleCtx.GameMode);
        while (!asyop.isDone)
        {
            yield return null;
        }
        asyop.allowSceneActivation = true;
        yield return null;

        if (ctx.ModeConfig != null)
            Debug.Log($"成功加载模式配置!模式:{ctx.BattleCtx.GameMode},地图:{ctx.ModeConfig.gameMode}");
    }
}
