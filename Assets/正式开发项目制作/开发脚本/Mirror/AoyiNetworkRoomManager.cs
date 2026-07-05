using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace Aoyi.Mirror
{
    /// <summary>
    /// 基于 Mirror NetworkRoomManager 的局域网房间管理器。
    /// 自动处理：玩家加入、英雄选择同步、准备状态、场景切换。
    /// 房间满员自动准备，不需要手动点击按钮。
    /// </summary>
    public class AoyiNetworkRoomManager : NetworkRoomManager
    {
        public static new AoyiNetworkRoomManager singleton => NetworkManager.singleton as AoyiNetworkRoomManager;

        [Header("奥义项目配置")]
        [Tooltip("房间场景名（角色选择面板所在的大厅场景）")]
        public string aoyiRoomScene = "LobbyPanel";

        [Tooltip("战斗场景名列表")]
        public string[] aoyiBattleScenes = new string[] { "dantiao_map", "paiwei_map" };

        [System.NonSerialized]
        public string pendingBattleScene = "";

        [System.NonSerialized]
        public int maxRoomPlayers = 2;

        [System.NonSerialized]
        private bool _battleStarted = false;

        public static bool HasEnoughPlayersToStart(int currentPlayers, int requiredPlayers)
        {
            return currentPlayers >= Mathf.Max(1, requiredPlayers);
        }

        public static bool ShouldStartBattleAfterReady(int currentPlayers, int readyPlayers, int requiredPlayers)
        {
            return HasEnoughPlayersToStart(currentPlayers, requiredPlayers) && readyPlayers >= currentPlayers;
        }

        public bool CanStartBattle()
        {
            return HasEnoughPlayersToStart(roomSlots.Count, maxRoomPlayers);
        }

        public override void Awake()
        {
            if (transport == null)
            {
                transport = GetComponent<Transport>();
                if (transport == null)
                {
                    transport = gameObject.AddComponent<kcp2k.KcpTransport>();
                    Debug.Log("[AoyiNetworkRoomManager] 自动添加 KcpTransport");
                }
            }

            RoomScene = aoyiRoomScene;
            GameplayScene = aoyiBattleScenes.Length > 0 ? aoyiBattleScenes[0] : "dantiao_map";

            autoCreatePlayer = true;

            if (roomPlayerPrefab == null)
            {
                GameObject roomPlayerObj = Resources.Load<GameObject>("MirrorPrefabs/AoyiRoomPlayerPrefab");
                if (roomPlayerObj != null)
                {
                    roomPlayerPrefab = roomPlayerObj.GetComponent<AoyiRoomPlayer>();
                    Debug.Log("[AoyiNetworkRoomManager] 已加载 RoomPlayer prefab");
                }
                else
                {
                    Debug.LogError("[AoyiNetworkRoomManager] RoomPlayer prefab 未找到！请先运行菜单 Tools > 奥义 > 生成 Mirror Prefab");
                }
            }

            if (playerPrefab == null)
            {
                GameObject gamePlayerObj = Resources.Load<GameObject>("MirrorPrefabs/AoyiGamePlayerPrefab");
                if (gamePlayerObj != null)
                {
                    playerPrefab = gamePlayerObj;
                    Debug.Log("[AoyiNetworkRoomManager] 已加载 GamePlayer prefab");
                }
                else
                {
                    Debug.LogError("[AoyiNetworkRoomManager] GamePlayer prefab 未找到！请先运行菜单 Tools > 奥义 > 生成 Mirror Prefab");
                }
            }

            base.Awake();
        }

        public override void OnRoomStartHost()
        {
            _battleStarted = false;
            minPlayers = Mathf.Max(1, maxRoomPlayers);
            Debug.Log($"[AoyiNetworkRoomManager] OnRoomStartHost, roomSlots.Count={roomSlots.Count}, maxRoomPlayers={maxRoomPlayers}");
        }

        public override void OnServerAddPlayer(NetworkConnectionToClient conn)
        {
            if (IsBattleScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name))
            {
                Debug.Log($"[AoyiNetworkRoomManager] 战斗场景，跳过 Mirror 创建 GamePlayer (connId={conn.connectionId})");
                return;
            }

            base.OnServerAddPlayer(conn);
            Debug.Log($"[AoyiNetworkRoomManager] OnServerAddPlayer connId={conn.connectionId}, roomSlots.Count={roomSlots.Count}");

            RecalculateRoomPlayerIndices();

            foreach (var slot in roomSlots)
            {
                if (slot is AoyiRoomPlayer aoyiPlayer && aoyiPlayer.connectionToClient == conn)
                {
                    aoyiPlayer.teamId = roomSlots.Count;
                    Debug.Log($"[AoyiNetworkRoomManager] 为玩家 connId={conn.connectionId} 设置 index={aoyiPlayer.index}, teamId={aoyiPlayer.teamId}");
                    break;
                }
            }

            if (roomSlots.Count >= maxRoomPlayers)
            {
                StartCoroutine(AutoReadyAllPlayers());
            }
        }

        private System.Collections.IEnumerator AutoReadyAllPlayers()
        {
            yield return new WaitForSeconds(0.5f);

            if (!CanStartBattle())
            {
                Debug.Log($"[AoyiNetworkRoomManager] 房间人数不足，暂不开战 ({roomSlots.Count}/{maxRoomPlayers})");
                yield break;
            }

            Debug.Log($"[AoyiNetworkRoomManager] 房间满员 ({roomSlots.Count}/{maxRoomPlayers})，自动准备所有玩家");
            foreach (var slot in roomSlots)
            {
                if (slot is AoyiRoomPlayer aoyiPlayer)
                {
                    aoyiPlayer.ServerSetReady();
                }
            }
        }

        public override void ReadyStatusChanged()
        {
            int currentPlayers = 0;
            int readyPlayers = 0;

            foreach (var slot in roomSlots)
            {
                if (slot == null) continue;

                currentPlayers++;
                if (slot.readyToBegin)
                    readyPlayers++;
            }

            bool shouldStart = ShouldStartBattleAfterReady(currentPlayers, readyPlayers, maxRoomPlayers);
            Debug.Log($"[AoyiNetworkRoomManager] ReadyStatusChanged ready={readyPlayers}/{currentPlayers}, required={maxRoomPlayers}, shouldStart={shouldStart}");
            allPlayersReady = shouldStart;
        }

        public override void OnRoomServerPlayersReady()
        {
            if (!CanStartBattle())
            {
                allPlayersReady = false;
                Debug.LogWarning($"[AoyiNetworkRoomManager] 收到开战请求但人数不足，已拦截 ({roomSlots.Count}/{maxRoomPlayers})");
                return;
            }

            string scene = string.IsNullOrEmpty(pendingBattleScene) ? GameplayScene : pendingBattleScene;
            Debug.Log($"[AoyiNetworkRoomManager] OnRoomServerPlayersReady, 切换到战斗场景: {scene}");
            ServerChangeScene(scene);
        }

        public override bool OnRoomServerSceneLoadedForPlayer(NetworkConnectionToClient conn, GameObject roomPlayer, GameObject gamePlayer)
        {
            if (IsBattleScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name))
            {
                if (gamePlayer != null) Destroy(gamePlayer);
                Debug.Log($"[AoyiNetworkRoomManager] 战斗场景，保留 RoomPlayer 不替换为 GamePlayer (connId={conn.connectionId})");
                return false;
            }
            return base.OnRoomServerSceneLoadedForPlayer(conn, roomPlayer, gamePlayer);
        }

        public override void OnRoomServerSceneChanged(string sceneName)
        {
            base.OnRoomServerSceneChanged(sceneName);
            Debug.Log($"[AoyiNetworkRoomManager] OnRoomServerSceneChanged: {sceneName}, roomSlots.Count={roomSlots.Count}, _battleStarted={_battleStarted}");

            if (IsBattleScene(sceneName))
            {
                if (_battleStarted)
                {
                    Debug.Log("[AoyiNetworkRoomManager] 战斗已启动，跳过重复初始化");
                    return;
                }
                _battleStarted = true;

                var allPlayers = BuildAllPlayersFromRoomSlots();
                Debug.Log($"[AoyiNetworkRoomManager] 战斗场景加载完成，玩家数={allPlayers.Count}, 玩家列表: {string.Join(", ", System.Linq.Enumerable.Select(allPlayers, p => $"{p.userId}(hero={p.HeroId}, team={p.teamId})"))}");

                var mgr = PlayerBasicInfoMgr.Instance;
                if (mgr != null)
                {
                    mgr.SetBattleAllPlayers(allPlayers);
                }

                StartBattleLoad(allPlayers);
            }
        }

        public override void OnRoomClientSceneChanged()
        {
            base.OnRoomClientSceneChanged();
            Debug.Log($"[AoyiNetworkRoomManager] OnRoomClientSceneChanged: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");

            if (global::Mirror.NetworkServer.active && global::Mirror.NetworkClient.active)
            {
                Debug.Log("[AoyiNetworkRoomManager] host 模式，OnRoomClientSceneChanged 跳过（已在 OnRoomServerSceneChanged 处理）");
                return;
            }

            if (IsBattleScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name))
            {
                var allPlayers = BuildAllPlayersFromRoomSlots();
                var mgr = PlayerBasicInfoMgr.Instance;
                if (mgr != null)
                {
                    mgr.SetBattleAllPlayers(allPlayers);
                }

                StartBattleLoad(allPlayers);
            }
        }

        private void StartBattleLoad(List<PlayerData> allPlayers)
        {
            Debug.Log($"[AoyiNetworkRoomManager] StartBattleLoad 开始，玩家数={allPlayers?.Count ?? 0}");

            var loadPanel = FindObjectOfType<GameLoadPanel>();
            if (loadPanel == null)
            {
                Debug.LogWarning("[AoyiNetworkRoomManager] 未找到 GameLoadPanel，自动创建一个");
                GameObject go = new GameObject("GameLoadPanel");
                DontDestroyOnLoad(go);
                loadPanel = go.AddComponent<GameLoadPanel>();
                loadPanel.Init(go.transform);
            }
            Debug.Log($"[AoyiNetworkRoomManager] 调用 loadPanel.LoadGame，玩家数={allPlayers.Count}");
            try
            {
                loadPanel.LoadGame(allPlayers);
                Debug.Log("[AoyiNetworkRoomManager] LoadGame 调用完成");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[AoyiNetworkRoomManager] LoadGame 抛异常: {ex}");
            }
        }

        private List<PlayerData> BuildAllPlayersFromRoomSlots()
        {
            var list = new List<PlayerData>();
            int i = 0;
            foreach (var slot in roomSlots)
            {
                if (slot is AoyiRoomPlayer aoyiPlayer)
                {
                    int userId = i;
                    int teamId = i + 1;
                    list.Add(new PlayerData
                    {
                        userId = userId.ToString(),
                        teamId = teamId,
                        HeroId = aoyiPlayer.heroId > 0 ? aoyiPlayer.heroId : 101
                    });
                }
                i++;
            }
            return list;
        }

        public int GetLocalPlayerIndex()
        {
            int i = 0;
            foreach (var slot in roomSlots)
            {
                if (slot is AoyiRoomPlayer aoyiPlayer && aoyiPlayer.isLocalPlayer)
                {
                    return i;
                }
                i++;
            }
            if (global::Mirror.NetworkServer.active && global::Mirror.NetworkClient.active && roomSlots.Count == 1)
            {
                return 0;
            }
            return -1;
        }

        public int GetLocalPlayerTeamId()
        {
            int i = 0;
            foreach (var slot in roomSlots)
            {
                if (slot is AoyiRoomPlayer aoyiPlayer && aoyiPlayer.isLocalPlayer)
                {
                    return i + 1;
                }
                i++;
            }
            if (global::Mirror.NetworkServer.active && global::Mirror.NetworkClient.active && roomSlots.Count == 1)
            {
                return 1;
            }
            return 0;
        }

        private bool IsBattleScene(string sceneName)
        {
            foreach (var s in aoyiBattleScenes)
            {
                if (s == sceneName) return true;
            }
            return false;
        }
    }
}
