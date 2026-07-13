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
        public string aoyiRoomScene = GameSceneCatalog.Lobby;

        [Tooltip("战斗场景名列表")]
        public string[] aoyiBattleScenes = { GameSceneCatalog.DantiaoBattle, GameSceneCatalog.PaiweiBattle };

        [System.NonSerialized]
        public string pendingBattleScene = "";

        [System.NonSerialized]
        public int maxRoomPlayers = 2;

        [System.NonSerialized]
        private bool _battleStarted = false;

        [System.NonSerialized]
        private bool _battleSceneChangeStarted = false;

        [System.NonSerialized]
        private readonly List<PlayerData> _battlePlayersSnapshot = new List<PlayerData>();

        [System.NonSerialized]
        private int _localBattleIndexSnapshot = -1;

        [System.NonSerialized]
        private int _localBattleTeamIdSnapshot = 0;

        public static bool HasEnoughPlayersToStart(int currentPlayers, int requiredPlayers)
        {
            return currentPlayers >= Mathf.Max(1, requiredPlayers);
        }

        public static bool ShouldStartBattleAfterReady(int currentPlayers, int readyPlayers, int requiredPlayers)
        {
            return HasEnoughPlayersToStart(currentPlayers, requiredPlayers) && readyPlayers >= currentPlayers;
        }

        public static bool ShouldDisconnectOnlineSessionAfterLobbyReturn(
            string sceneName,
            bool mirrorActive,
            bool serverActive,
            bool clientActive)
        {
            return mirrorActive
                && (serverActive || clientActive)
                && string.Equals(sceneName, GameSceneCatalog.Lobby, System.StringComparison.Ordinal);
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
            GameplayScene = aoyiBattleScenes.Length > 0 ? aoyiBattleScenes[0] : GameSceneCatalog.DantiaoBattle;

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
            _battleSceneChangeStarted = false;
            ClearBattleSnapshot();
            minPlayers = Mathf.Max(1, maxRoomPlayers);
            Debug.Log($"[AoyiNetworkRoomManager] OnRoomStartHost, roomSlots.Count={roomSlots.Count}, maxRoomPlayers={maxRoomPlayers}");
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            NetworkServer.ReplaceHandler<AoyiRawMessage>(MirrorNetBridge.ServerHandleRawMessage);
            Debug.Log("[AoyiNetworkRoomManager] 服务器已启动，注册旧消息桥接");
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            NetworkClient.ReplaceHandler<AoyiRawMessage>(MirrorNetBridge.ClientHandleRawMessage);
            Debug.Log("[AoyiNetworkRoomManager] 客户端已启动，注册旧消息桥接");
        }

        public override void OnServerAddPlayer(NetworkConnectionToClient conn)
        {
            if (IsAoyiBattleScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name))
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

            StartCoroutine(AutoReadyAllPlayers());
        }

        private System.Collections.IEnumerator AutoReadyAllPlayers()
        {
            float timeoutAt = Time.time + 3f;
            while (!CanStartBattle() && Time.time < timeoutAt)
            {
                yield return null;
            }

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
                    if (aoyiPlayer.readyToBegin) continue;
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

            if (_battleSceneChangeStarted)
            {
                Debug.Log("[AoyiNetworkRoomManager] 战斗场景切换已启动，跳过重复请求");
                return;
            }

            _battleSceneChangeStarted = true;
            string scene = string.IsNullOrEmpty(pendingBattleScene) ? GameplayScene : pendingBattleScene;
            CaptureBattleSnapshot("OnRoomServerPlayersReady");
            Debug.Log($"[AoyiNetworkRoomManager] OnRoomServerPlayersReady, 切换到战斗场景: {scene}");
            ServerChangeScene(scene);
        }

        public override bool OnRoomServerSceneLoadedForPlayer(NetworkConnectionToClient conn, GameObject roomPlayer, GameObject gamePlayer)
        {
            if (IsAoyiBattleScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name))
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

            if (ShouldDisconnectOnlineSessionAfterLobbyReturn(
                    sceneName,
                    PlayerBasicInfoMgr.Instance != null
                        && PlayerBasicInfoMgr.Instance.CurrentNetworkMode == NetworkMode.SupabaseOnline,
                    NetworkServer.active,
                    NetworkClient.active))
            {
                StartCoroutine(StopOnlineHostAfterLobbyReturn());
                return;
            }

            if (IsAoyiBattleScene(sceneName))
            {
                if (_battleStarted)
                {
                    Debug.Log("[AoyiNetworkRoomManager] 战斗已启动，跳过重复初始化");
                    return;
                }
                _battleStarted = true;

                var allPlayers = GetBattlePlayersForScene();
                Debug.Log($"[AoyiNetworkRoomManager] 战斗场景加载完成，玩家数={allPlayers.Count}, 玩家列表: {string.Join(", ", System.Linq.Enumerable.Select(allPlayers, p => $"{p.userId}(hero={p.HeroId}, team={p.teamId})"))}");
                MirrorNetBridge.ResetBattleFrameState(PlayerBasicInfoMgr.Instance?.RoomId, allPlayers.Count);

                var mgr = PlayerBasicInfoMgr.Instance;
                if (mgr != null)
                {
                    ApplyLocalBattleIdentity(mgr);
                    mgr.SetBattleAllPlayers(allPlayers);
                }

                StartBattleLoad(allPlayers);
            }
        }

        public override void OnRoomClientSceneChanged()
        {
            base.OnRoomClientSceneChanged();
            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            Debug.Log($"[AoyiNetworkRoomManager] OnRoomClientSceneChanged: {sceneName}");

            if (ShouldDisconnectOnlineSessionAfterLobbyReturn(
                    sceneName,
                    PlayerBasicInfoMgr.Instance != null
                        && PlayerBasicInfoMgr.Instance.CurrentNetworkMode == NetworkMode.SupabaseOnline,
                    NetworkServer.active,
                    NetworkClient.active)
                && !NetworkServer.active)
            {
                StartCoroutine(StopOnlineClientAfterLobbyReturn());
                return;
            }

            if (global::Mirror.NetworkServer.active && global::Mirror.NetworkClient.active)
            {
                Debug.Log("[AoyiNetworkRoomManager] host 模式，OnRoomClientSceneChanged 跳过（已在 OnRoomServerSceneChanged 处理）");
                return;
            }

            if (IsAoyiBattleScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name))
            {
                var allPlayers = GetBattlePlayersForScene();
                var mgr = PlayerBasicInfoMgr.Instance;
                if (mgr != null)
                {
                    ApplyLocalBattleIdentity(mgr);
                    mgr.SetBattleAllPlayers(allPlayers);
                }

                StartBattleLoad(allPlayers);
            }
        }

        private System.Collections.IEnumerator StopOnlineHostAfterLobbyReturn()
        {
            yield return null;

            if (!NetworkServer.active)
            {
                yield break;
            }

            OnlineConnectionLauncher.NotifyMatchedRoomEnded();
            Debug.Log("[AoyiNetworkRoomManager] 在线战斗已回大厅，停止 Host/服务器并结束当前房间会话");
            if (NetworkClient.active)
            {
                StopHost();
            }
            else
            {
                StopServer();
            }
        }

        private System.Collections.IEnumerator StopOnlineClientAfterLobbyReturn()
        {
            yield return null;

            if (!NetworkClient.active || NetworkServer.active)
            {
                yield break;
            }

            OnlineConnectionLauncher.NotifyMatchedRoomEnded();
            Debug.Log("[AoyiNetworkRoomManager] 在线战斗已回大厅，停止 Client 并结束当前房间会话");
            StopClient();
        }

        private void StartBattleLoad(List<PlayerData> allPlayers)
        {
            Debug.Log($"[AoyiNetworkRoomManager] StartBattleLoad 开始，玩家数={allPlayers?.Count ?? 0}");
            LanQuickMatchManager.Instance?.CompleteMatchAndEnterBattle();

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

        private void ApplyLocalBattleIdentity(PlayerBasicInfoMgr mgr)
        {
            int localIndex = GetLocalPlayerIndex();
            int localTeamId = GetLocalPlayerTeamId();

            if (localIndex >= 0)
            {
                mgr.SetBattleId(localIndex.ToString());
            }

            if (localTeamId > 0)
            {
                mgr.UpdateTeamId(localTeamId);
            }
        }

        private void ClearBattleSnapshot()
        {
            _battlePlayersSnapshot.Clear();
            _localBattleIndexSnapshot = -1;
            _localBattleTeamIdSnapshot = 0;
        }

        private void CaptureBattleSnapshot(string reason)
        {
            _battlePlayersSnapshot.Clear();
            _battlePlayersSnapshot.AddRange(BuildAllPlayersFromRoomSlots());
            _localBattleIndexSnapshot = GetLocalPlayerIndexFromRoomSlots();
            _localBattleTeamIdSnapshot = GetLocalPlayerTeamIdFromRoomSlots();
            Debug.Log($"[AoyiNetworkRoomManager] 捕获战斗玩家快照({reason})，玩家数={_battlePlayersSnapshot.Count}, localIndex={_localBattleIndexSnapshot}, localTeam={_localBattleTeamIdSnapshot}");
        }

        private List<PlayerData> GetBattlePlayersForScene()
        {
            if (_battlePlayersSnapshot.Count > 0)
            {
                return new List<PlayerData>(_battlePlayersSnapshot);
            }

            return BuildAllPlayersFromRoomSlots();
        }

        private List<PlayerData> BuildAllPlayersFromRoomSlots()
        {
            var list = new List<PlayerData>();
            List<AoyiRoomPlayer> orderedPlayers = GetOrderedRoomPlayers();

            for (int i = 0; i < orderedPlayers.Count; i++)
            {
                AoyiRoomPlayer aoyiPlayer = orderedPlayers[i];
                int userId = aoyiPlayer.index >= 0 ? aoyiPlayer.index : i;
                int teamId = aoyiPlayer.teamId > 0 ? aoyiPlayer.teamId : i + 1;
                list.Add(new PlayerData
                {
                    userId = userId.ToString(),
                    teamId = teamId,
                    HeroId = aoyiPlayer.heroId > 0 ? aoyiPlayer.heroId : 101
                });
            }
            return list;
        }

        private List<AoyiRoomPlayer> GetOrderedRoomPlayers()
        {
            var players = new List<AoyiRoomPlayer>();
            foreach (var slot in roomSlots)
            {
                if (slot is AoyiRoomPlayer aoyiPlayer)
                {
                    players.Add(aoyiPlayer);
                }
            }

            players.Sort((a, b) => a.index.CompareTo(b.index));
            return players;
        }

        public int GetLocalPlayerIndex()
        {
            int index = GetLocalPlayerIndexFromRoomSlots();
            if (index >= 0)
                return index;

            if (_localBattleIndexSnapshot >= 0)
                return _localBattleIndexSnapshot;

            if (global::Mirror.NetworkServer.active && global::Mirror.NetworkClient.active && roomSlots.Count == 1)
            {
                return 0;
            }
            return -1;
        }

        public int GetLocalPlayerTeamId()
        {
            int teamId = GetLocalPlayerTeamIdFromRoomSlots();
            if (teamId > 0)
                return teamId;

            if (_localBattleTeamIdSnapshot > 0)
                return _localBattleTeamIdSnapshot;

            if (global::Mirror.NetworkServer.active && global::Mirror.NetworkClient.active && roomSlots.Count == 1)
            {
                return 1;
            }
            return 0;
        }

        private int GetLocalPlayerIndexFromRoomSlots()
        {
            List<AoyiRoomPlayer> orderedPlayers = GetOrderedRoomPlayers();
            for (int i = 0; i < orderedPlayers.Count; i++)
            {
                AoyiRoomPlayer aoyiPlayer = orderedPlayers[i];
                if (aoyiPlayer.isLocalPlayer)
                {
                    return aoyiPlayer.index >= 0 ? aoyiPlayer.index : i;
                }
            }

            return -1;
        }

        private int GetLocalPlayerTeamIdFromRoomSlots()
        {
            List<AoyiRoomPlayer> orderedPlayers = GetOrderedRoomPlayers();
            for (int i = 0; i < orderedPlayers.Count; i++)
            {
                AoyiRoomPlayer aoyiPlayer = orderedPlayers[i];
                if (aoyiPlayer.isLocalPlayer)
                {
                    return aoyiPlayer.teamId > 0 ? aoyiPlayer.teamId : i + 1;
                }
            }

            return 0;
        }

        public bool IsAoyiBattleScene(string sceneName)
        {
            return GameSceneCatalog.IsBattleScene(sceneName);
        }
    }
}
