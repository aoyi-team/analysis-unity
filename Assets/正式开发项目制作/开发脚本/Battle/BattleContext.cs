using System.Collections.Generic;
using Aoyi.Mirror;

public class BattleContext
{
    public string LocalPlayerId { get; set; }
    public int LocalPlayerIntId { get; set; }
    public int LocalTeamId { get; set; }
    public string RoomId { get; set; }
    public GameModes GameMode { get; set; }
    public List<PlayerData> AllPlayers { get; set; }

    public static BattleContext Capture()
    {
        var mgr = PlayerBasicInfoMgr.Instance;
        if (mgr == null)
        {
            UnityEngine.Debug.LogError("[BattleContext] PlayerBasicInfoMgr.Instance is null, cannot capture context");
            return null;
        }

        var ctx = new BattleContext
        {
            LocalPlayerId = mgr.GetBattleId(),
            LocalPlayerIntId = mgr.GetLocalPlayerIndex() >= 0 ? mgr.GetLocalPlayerIndex() : mgr.GetIntId(),
            LocalTeamId = mgr.TeamId,
            RoomId = mgr.RoomId,
            GameMode = mgr.GameMode,
            AllPlayers = mgr.GetBattleAllPlayers()
        };

        // 如果 AllPlayers 未设置，从 RoomManager 读取
        if (ctx.AllPlayers == null || ctx.AllPlayers.Count == 0)
        {
            var nm = AoyiNetworkRoomManager.singleton;
            if (nm != null)
            {
                ctx.AllPlayers = new List<PlayerData>();
                foreach (var slot in nm.roomSlots)
                {
                    if (slot is AoyiRoomPlayer aoyiPlayer)
                    {
                        ctx.AllPlayers.Add(new PlayerData
                        {
                            userId = aoyiPlayer.index.ToString(),
                            teamId = aoyiPlayer.teamId,
                            HeroId = aoyiPlayer.heroId > 0 ? aoyiPlayer.heroId : 101
                        });
                    }
                }
                UnityEngine.Debug.Log($"[BattleContext] 从 roomSlots 读取玩家数据，玩家数={ctx.AllPlayers.Count}");
            }
        }

        UnityEngine.Debug.Log($"[BattleContext] captured: LocalPlayerId={ctx.LocalPlayerId}, TeamId={ctx.LocalTeamId}, RoomId={ctx.RoomId}, GameMode={ctx.GameMode}, AllPlayers={ctx.AllPlayers?.Count ?? 0}");
        return ctx;
    }
}

