using Mirror;
using UnityEngine;

namespace Aoyi.Mirror
{
    /// <summary>
    /// 局域网房间玩家。挂载在 roomPlayerPrefab 上。
    /// 通过 SyncVar 自动同步英雄选择给所有客户端，不需要 MsgMatchRequest。
    /// </summary>
    public class AoyiRoomPlayer : NetworkRoomPlayer
    {
        [Header("游戏数据（自动同步）")]
        [SyncVar] public int heroId = 101;
        [SyncVar] public int skinId = 1;
        [SyncVar] public string playerName = "Player";
        [SyncVar] public int teamId = 0;

        /// <summary>
        /// 客户端通知服务器自己的英雄选择
        /// </summary>
        [Command]
        public void CmdSetHero(int newHeroId, int newSkinId)
        {
            heroId = newHeroId;
            skinId = newSkinId;
            Debug.Log($"[AoyiRoomPlayer] connId={connectionToClient.connectionId} 设置英雄 heroId={heroId} skinId={skinId}");
        }

        /// <summary>
        /// 客户端通知服务器自己的昵称
        /// </summary>
        [Command]
        public void CmdSetName(string name)
        {
            playerName = name;
        }

        /// <summary>
        /// 客户端通知服务器准备就绪（[Command]，本地玩家调用）。
        /// </summary>
        [Command]
        public void CmdSetReady()
        {
            CmdChangeReadyState(true);
        }

        /// <summary>
        /// 服务端触发准备状态，用于房间满员自动开战。
        /// </summary>
        public void ServerSetReady()
        {
            if (!isServer)
            {
                Debug.LogError("[AoyiRoomPlayer] ServerSetReady 只能在服务器端调用");
                return;
            }

            readyToBegin = true;
            if (NetworkManager.singleton is NetworkRoomManager room)
            {
                room.ReadyStatusChanged();
            }
        }

        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();
            Debug.Log($"[AoyiRoomPlayer] OnStartLocalPlayer index={index}, netId={netId}");

            // 本地玩家启动时，把英雄选择和昵称同步给服务器
            var mgr = PlayerBasicInfoMgr.Instance;
            if (mgr != null)
            {
                CmdSetHero(mgr.HeroCache.heroId, mgr.HeroCache.skinId);
                string name = mgr.GetName();
                if (!string.IsNullOrEmpty(name))
                    CmdSetName(name);
            }
        }

        public override void ReadyStateChanged(bool oldReadyState, bool newReadyState)
        {
            Debug.Log($"[AoyiRoomPlayer] ReadyStateChanged index={index} ready={newReadyState}");
        }
    }
}
