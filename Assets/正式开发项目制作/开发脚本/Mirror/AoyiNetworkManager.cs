using Mirror;
using MsgFramework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Aoyi.Mirror
{
    /// <summary>
    /// 项目专用的 Mirror NetworkManager。
    /// Plan A：保留旧 MsgFramework 消息框架，仅把传输层替换为 Mirror。
    /// </summary>
    public class AoyiNetworkManager : NetworkManager
    {
        [Header("奥义项目配置")]
        [Tooltip("登录/注册场景名")]
        public string loginScene = "LoadScene";

        [Tooltip("大厅场景名")]
        public string lobbyScene = "LobbyPanel";

        [Tooltip("战斗场景名列表")]
        public string[] battleScenes = new string[] { "dantiao_map", "paiwei_map" };

        [Header("调试")]
        [Tooltip("是否在启动时自动作为主机运行（仅编辑器调试用）")]
        public bool autoStartHostInEditor = false;

        public static new AoyiNetworkManager singleton => NetworkManager.singleton as AoyiNetworkManager;

        public override void Awake()
        {
            // 如果 transport 未赋值，尝试从同物体获取或自动添加 KcpTransport
            //（解决运行时动态创建 AoyiNetworkManager 时 transport 尚未赋值的时序问题）
            if (transport == null)
            {
                transport = GetComponent<Transport>();
                if (transport == null)
                {
                    transport = gameObject.AddComponent<kcp2k.KcpTransport>();
                    Debug.Log("[AoyiNetworkManager] 自动添加 KcpTransport");
                }
            }

            base.Awake();
            // Plan A：不通过 Mirror 自动生成玩家，由 PlayerManager 本地生成
            autoCreatePlayer = false;
        }

        public override void Start()
        {
            base.Start();

#if UNITY_EDITOR
            if (autoStartHostInEditor)
            {
                Debug.Log("[AoyiNetworkManager] 编辑器调试模式：自动启动主机");
                StartHost();
            }
#endif
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            NetworkServer.RegisterHandler<AoyiRawMessage>(MirrorNetBridge.ServerHandleRawMessage);
            Debug.Log("[AoyiNetworkManager] 服务器已启动，注册旧消息桥接");
        }

        public override void OnStopServer()
        {
            base.OnStopServer();
            MirrorNetBridge.ServerClients.Clear();
            MirrorNetBridge.ServerPlayers.Clear();
            Debug.Log("[AoyiNetworkManager] 服务器已停止");
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            NetworkClient.RegisterHandler<AoyiRawMessage>(MirrorNetBridge.ClientHandleRawMessage);
            Debug.Log("[AoyiNetworkManager] 客户端已启动，注册旧消息桥接");
        }

        public override void OnStopClient()
        {
            base.OnStopClient();
            Debug.Log("[AoyiNetworkManager] 客户端已停止");
        }

        public override void OnStartHost()
        {
            base.OnStartHost();
            // Mirror 的 OnServerConnect 不会对 host 的 localConnection 触发，
            // 必须在此时主动注册 host client，否则 host 不在 ServerPlayers 中。
            MirrorNetBridge.RegisterHostClient();
            Debug.Log("[AoyiNetworkManager] 主机已启动，已注册 host client");
        }

        public override void OnStopHost()
        {
            base.OnStopHost();
            MirrorNetBridge.ServerClients.Clear();
            MirrorNetBridge.ServerPlayers.Clear();
            Debug.Log("[AoyiNetworkManager] 主机已停止");
        }

        public override void OnClientConnect()
        {
            base.OnClientConnect();
            Debug.Log("[AoyiNetworkManager] 客户端已连接服务器");
        }

        public override void OnClientDisconnect()
        {
            base.OnClientDisconnect();
            Debug.Log("[AoyiNetworkManager] 客户端已断开服务器");
        }

        public override void OnServerConnect(NetworkConnectionToClient conn)
        {
            base.OnServerConnect(conn);
            NetworkServer.SetClientReady(conn);
            MirrorNetBridge.OnServerConnect(conn.connectionId);
            Debug.Log($"[AoyiNetworkManager] 连接 {conn.connectionId} 已就绪");
        }

        public override void OnServerAddPlayer(NetworkConnectionToClient conn)
        {
            // Plan A：玩家由 PlayerManager 在战斗场景本地生成，不通过 Mirror 生成
            Debug.Log($"[AoyiNetworkManager] 忽略 Mirror 自动加玩家（Plan A 由 PlayerManager 处理）");
        }

        public override void OnServerDisconnect(NetworkConnectionToClient conn)
        {
            base.OnServerDisconnect(conn);
            MirrorNetBridge.OnServerDisconnect(conn.connectionId);
            Debug.Log($"[AoyiNetworkManager] 连接 {conn.connectionId} 断开");
        }

        /// <summary>
        /// 切换到指定战斗场景。
        /// </summary>
        public void StartBattle(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogError("[AoyiNetworkManager] 战斗场景名不能为空");
                return;
            }

            if (!NetworkServer.active)
            {
                Debug.LogError("[AoyiNetworkManager] 只有主机可以切换战斗场景");
                return;
            }

            Debug.Log($"[AoyiNetworkManager] 主机切换战斗场景：{sceneName}");
            ServerChangeScene(sceneName);
        }

        /// <summary>
        /// 返回大厅场景。
        /// </summary>
        public void ReturnToLobby()
        {
            if (!NetworkServer.active)
            {
                Debug.LogError("[AoyiNetworkManager] 只有主机可以返回大厅");
                return;
            }

            Debug.Log("[AoyiNetworkManager] 主机返回大厅");
            ServerChangeScene(lobbyScene);
        }
    }
}
