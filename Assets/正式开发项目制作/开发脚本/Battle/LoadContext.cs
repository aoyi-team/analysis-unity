using FixMath;
using MsgFramework;
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 战斗加载期间的共享上下文。
/// 不同命令通过此上下文共享数据，避免互相耦合。
/// </summary>
public class LoadContext
{
    public BattleContext BattleCtx { get; private set; }
    public ModeConfig ModeConfig { get; set; }
    public AsyncOperation SceneAsyncOp { get; set; }

    public Action<MsgBase> UdpMessageHandler { get; set; }
    public NetWorkMgr.MsgListener OnBattleReadyMsgHandler { get; set; }
    public NetWorkMgr.MsgListener OnFramePackMsgHandler { get; set; }
    public Action OnGameReady { get; set; }
    public Action StartBattleReadyLoop { get; set; }

    public LoadContext(BattleContext battleCtx)
    {
        BattleCtx = battleCtx;
    }

    public static LoadContext Create()
    {
        var battleCtx = BattleContext.Capture();
        if (battleCtx == null)
        {
            UnityEngine.Debug.LogError("[LoadContext] BattleContext 构建失败");
            return null;
        }
        return new LoadContext(battleCtx);
    }
}
