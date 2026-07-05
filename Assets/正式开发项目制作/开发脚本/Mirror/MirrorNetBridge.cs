using Mirror;
using MsgFramework;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Aoyi.Mirror
{
    /// <summary>
    /// Mirror 与旧 MsgFramework 之间的桥接器。
    /// 负责：
    /// 1. 把旧消息编码为字节并通过 Mirror 发送。
    /// 2. 接收 Mirror 消息并解码为旧消息，分发给 NetWorkMgr 的监听器。
    /// 3. 在服务器端处理登录、匹配等房间级逻辑。
    /// </summary>
    public static class MirrorNetBridge
    {
        /// <summary>
        /// 服务器端保存每个 Mirror 连接对应的玩家状态。
        /// Key: Mirror connectionId
        /// </summary>
        public static readonly Dictionary<int, MirrorClientState> ServerClients = new Dictionary<int, MirrorClientState>();

        /// <summary>
        /// 服务器端按加入顺序保存的玩家列表（用于构造 MatchSuccess 分配队伍）。
        /// </summary>
        public static readonly List<MirrorClientState> ServerPlayers = new List<MirrorClientState>();

        // RoomInfo 定义在 Assembly-CSharp 中。新架构由 AoyiNetworkRoomManager 管理房间，不再使用此字段。
        // public static RoomInfo ServerRoomInfo = new RoomInfo();

        /// <summary>
        /// 是否启用 Mirror 作为当前传输层。
        /// </summary>
        public static bool IsMirrorActive => NetworkServer.active || NetworkClient.active;

        /// <summary>
        /// 是否处于主机模式。
        /// </summary>
        public static bool IsHost => NetworkServer.active && NetworkClient.active;

        /// <summary>
        /// 编码 MsgBase 为旧协议二进制数据（2字节长度 + 协议名 + JSON 体）。
        /// </summary>
        public static byte[] EncodeMessage(MsgBase msg)
        {
            byte[] nameBytes = MsgBase.EncodeName(msg);
            byte[] bodyBytes = MsgBase.Encode(msg);
            int len = nameBytes.Length + bodyBytes.Length;
            byte[] sendBytes = new byte[2 + len];
            sendBytes[0] = (byte)(len % 256);
            sendBytes[1] = (byte)(len / 256);
            Buffer.BlockCopy(nameBytes, 0, sendBytes, 2, nameBytes.Length);
            Buffer.BlockCopy(bodyBytes, 0, sendBytes, 2 + nameBytes.Length, bodyBytes.Length);
            return sendBytes;
        }

        /// <summary>
        /// 尝试从字节流中解码一条旧协议消息。
        /// </summary>
        public static bool TryDecodeMessage(byte[] raw, out string protoName, out MsgBase msg)
        {
            protoName = null;
            msg = null;
            if (raw == null || raw.Length <= 2) return false;

            int bodyLength = (raw[1] << 8) | raw[0];
            if (bodyLength <= 0 || raw.Length < 2 + bodyLength) return false;

            int nameCount;
            protoName = MsgBase.DecodeName(raw, 2, out nameCount);
            if (string.IsNullOrEmpty(protoName)) return false;

            int bodyCount = bodyLength - nameCount;
            if (bodyCount <= 0) return false;

            msg = MsgBase.Decode(protoName, raw, 2 + nameCount, bodyCount);
            return msg != null;
        }

        /// <summary>
        /// 客户端通过 Mirror 发送旧消息到服务器。
        /// </summary>
        public static void ClientSend(MsgBase msg)
        {
            // 确保客户端已 ready：如果 active 但未 ready，先尝试 Ready()
            if (!NetworkClient.ready)
            {
                if (NetworkClient.active && NetworkClient.connection != null)
                {
                    Debug.Log("[MirrorNetBridge] ClientSend: 客户端未 ready，尝试 NetworkClient.Ready()");
                    NetworkClient.Ready();
                }
                else
                {
                    Debug.LogWarning($"[MirrorNetBridge] 客户端尚未 ready，无法发送消息 (active={NetworkClient.active}, conn={(NetworkClient.connection != null ? "not null" : "null")}), msg={msg.GetType().Name}");
                    return;
                }
            }

            NetworkClient.Send(new AoyiRawMessage { data = EncodeMessage(msg) });
            Debug.Log($"[MirrorNetBridge] ClientSend 成功: msg={msg.GetType().Name}");
        }

        /// <summary>
        /// 服务器通过 Mirror 发送旧消息到指定客户端。
        /// 注意：host 的 localConnection 不在 NetworkServer.connections 中，
        /// 需要单独通过 NetworkServer.localConnection 发送。
        /// </summary>
        public static void ServerSendToClient(int connectionId, MsgBase msg)
        {
            if (!NetworkServer.active)
            {
                Debug.LogWarning($"[MirrorNetBridge] ServerSendToClient 失败：NetworkServer 未激活, connId={connectionId}, msg={msg.GetType().Name}");
                return;
            }

            NetworkConnectionToClient conn = null;
            bool found = false;

            // 1. 优先从 connections 字典查找（远程客户端）
            if (NetworkServer.connections.TryGetValue(connectionId, out conn) && conn != null)
            {
                found = true;
            }
            // 2. host 的 localConnection（connectionId 通常为 0）不在 connections 中，单独处理
            else if (connectionId == 0 && NetworkServer.localConnection != null)
            {
                conn = NetworkServer.localConnection as NetworkConnectionToClient;
                if (conn != null)
                {
                    found = true;
                    Debug.Log($"[MirrorNetBridge] ServerSendToClient 使用 localConnection (host模式), connId={connectionId}, msg={msg.GetType().Name}");
                }
            }

            if (!found || conn == null)
            {
                Debug.LogWarning($"[MirrorNetBridge] ServerSendToClient 失败：未找到连接 connId={connectionId}, connections.Count={NetworkServer.connections.Count}, msg={msg.GetType().Name}");
                return;
            }

            if (!conn.isReady)
            {
                // 远程客户端可能还没发送 ReadyMessage，尝试主动设置 ready
                Debug.LogWarning($"[MirrorNetBridge] ServerSendToClient: 连接未 ready，尝试 SetClientReady, connId={connectionId}, msg={msg.GetType().Name}");
                NetworkServer.SetClientReady(conn);
                if (!conn.isReady)
                {
                    Debug.LogWarning($"[MirrorNetBridge] ServerSendToClient 失败：SetClientReady 后仍未 ready, connId={connectionId}, msg={msg.GetType().Name}");
                    return;
                }
            }

            conn.Send(new AoyiRawMessage { data = EncodeMessage(msg) });
            Debug.Log($"[MirrorNetBridge] ServerSendToClient 成功: connId={connectionId}, msg={msg.GetType().Name}");
        }

        /// <summary>
        /// 服务器广播旧消息到所有客户端。
        /// </summary>
        public static void ServerBroadcast(MsgBase msg)
        {
            if (!NetworkServer.active) return;
            NetworkServer.SendToReady(new AoyiRawMessage { data = EncodeMessage(msg) });
        }

        /// <summary>
        /// 服务器端：有新客户端连接时调用。
        /// </summary>
        public static void OnServerConnect(int connectionId)
        {
            if (ServerClients.ContainsKey(connectionId)) return;

            var state = new MirrorClientState
            {
                connectionId = connectionId,
                tempUserId = System.Math.Abs(connectionId).ToString("D6")
            };
            ServerClients[connectionId] = state;
            ServerPlayers.Add(state);

            Debug.Log($"[MirrorNetBridge] 连接 {connectionId} 加入房间，当前玩家数 {ServerPlayers.Count}");
        }

        /// <summary>
        /// 在 host 启动时主动为 host 的 localConnection 创建玩家状态。
        /// 因为 Mirror 的 OnServerConnect 不会对 host 自己的 localConnection 触发，
        /// 必须手动注册，否则 host 不在 ServerPlayers 中，BuildMatchSuccessMessage 不包含 host。
        /// </summary>
        public static void RegisterHostClient()
        {
            // host 的 localConnection connectionId 固定为 0
            const int hostConnId = 0;
            if (ServerClients.ContainsKey(hostConnId))
            {
                Debug.Log($"[MirrorNetBridge] RegisterHostClient: host client 已存在 (tempUserId={ServerClients[hostConnId].tempUserId})，跳过");
                return;
            }

            var state = new MirrorClientState
            {
                connectionId = hostConnId,
                tempUserId = hostConnId.ToString("D6"),
                userName = "Host",
                isReady = true
            };
            ServerClients[hostConnId] = state;
            ServerPlayers.Add(state);

            Debug.Log($"[MirrorNetBridge] RegisterHostClient: 已注册 host client, tempUserId={state.tempUserId}, ServerPlayers.Count={ServerPlayers.Count}");
        }

        /// <summary>
        /// 服务器端：客户端断开时调用。
        /// </summary>
        public static void OnServerDisconnect(int connectionId)
        {
            if (ServerClients.TryGetValue(connectionId, out MirrorClientState state))
            {
                ServerPlayers.Remove(state);
                ServerClients.Remove(connectionId);
            }

            // ServerRoomInfo 已移除，由 AoyiNetworkRoomManager 管理房间状态
            Debug.Log($"[MirrorNetBridge] 连接 {connectionId} 离开房间，当前玩家数 {ServerPlayers.Count}");
        }

        /// <summary>
        /// 服务器端：根据当前房间玩家构造 MsgMatchSuccess。
        /// </summary>
        public static MsgMatchSuccess BuildMatchSuccessMessage(string roomId)
        {
            var playerInfos = new List<PlayerData>();
            int teamId = 1;
            foreach (var player in ServerPlayers)
            {
                int heroId = player.heroId;
                if (heroId <= 0)
                {
                    // 对于 host（connectionId=0），尝试从 PlayerBasicInfoMgr 的 HeroCache 获取英雄选择
                    if (player.connectionId == 0 && PlayerBasicInfoMgr.Instance != null)
                    {
                        int cacheHeroId = PlayerBasicInfoMgr.Instance.HeroCache.heroId;
                        if (cacheHeroId > 0)
                        {
                            Debug.Log($"[MirrorNetBridge] host 玩家 {player.tempUserId} 的 heroId={heroId}，从 HeroCache 获取：{cacheHeroId}");
                            heroId = cacheHeroId;
                        }
                    }

                    if (heroId <= 0)
                    {
                        Debug.LogWarning($"[MirrorNetBridge] 玩家 {player.tempUserId} 的 heroId={heroId} 无效，使用默认值 101");
                        heroId = 101;
                    }
                }
                playerInfos.Add(new PlayerData
                {
                    userId = player.tempUserId,
                    teamId = teamId++,
                    HeroId = heroId
                });
            }

            return new MsgMatchSuccess
            {
                roomId = roomId,
                playerInfos = playerInfos
            };
        }

        /// <summary>
        /// 客户端收到服务器推送的旧消息。
        /// </summary>
        public static void ClientHandleRawMessage(AoyiRawMessage mirrorMsg)
        {
            DispatchToOldListeners(mirrorMsg.data);
        }

        /// <summary>
        /// 服务器收到客户端发来的旧消息，并处理房间逻辑。
        /// </summary>
        public static void ServerHandleRawMessage(NetworkConnectionToClient conn, AoyiRawMessage mirrorMsg)
        {
            if (!TryDecodeMessage(mirrorMsg.data, out string protoName, out MsgBase msg))
            {
                Debug.LogWarning("[MirrorNetBridge] 服务器解码旧消息失败");
                return;
            }

            HandleServerRoomLogic(conn, protoName, msg);

            // 同时把消息分发给所有旧监听器（服务器端也可能需要监听）
            NetWorkMgr.DispatchMirrorMessage(protoName, msg);
        }

        /// <summary>
        /// 在服务器端处理登录、匹配请求等房间级逻辑。
        /// </summary>
        private static void HandleServerRoomLogic(NetworkConnectionToClient conn, string protoName, MsgBase msg)
        {
            int connId = conn.connectionId;
            if (!ServerClients.TryGetValue(connId, out MirrorClientState state))
            {
                state = new MirrorClientState
                {
                    connectionId = connId,
                    tempUserId = System.Math.Abs(connId).ToString("D6")
                };
                ServerClients[connId] = state;
                // 修复：fallback 创建的 state 必须同时加入 ServerPlayers，
                // 否则 host（OnServerConnect 不触发）永远不在玩家列表中。
                ServerPlayers.Add(state);
                Debug.Log($"[MirrorNetBridge] HandleServerRoomLogic fallback 创建玩家: connId={connId}, tempUserId={state.tempUserId}, ServerPlayers.Count={ServerPlayers.Count}");
            }

            switch (protoName)
            {
                case "MsgLoginProf":
                    {
                        Debug.Log($"[MirrorNetBridge] 收到 MsgLoginProf, connId={connId}, tempUserId={state.tempUserId}, Name='{((MsgLoginProf)msg).Name}'");
                        MsgLoginProf login = (MsgLoginProf)msg;
                        login.result = 0;
                        login.ErrType = 0;
                        login.Id = state.tempUserId;
                        if (string.IsNullOrEmpty(login.Name))
                        {
                            login.Name = $"Player_{state.tempUserId}";
                        }
                        state.userName = login.Name;
                        state.isReady = true;
                        ServerSendToClient(connId, login);
                        Debug.Log($"[MirrorNetBridge] 玩家 {state.tempUserId} 登录处理完成，已发送响应");
                    }
                    break;

                case "MsgRegisterProf":
                    {
                        MsgRegisterProf reg = (MsgRegisterProf)msg;
                        reg.result = 0;
                        reg.Id = state.tempUserId;
                        ServerSendToClient(connId, reg);
                    }
                    break;

                case "MsgUpdateloadName":
                    {
                        MsgUpdateloadName nameMsg = (MsgUpdateloadName)msg;
                        nameMsg.result = 0;
                        nameMsg.Id = state.tempUserId;
                        if (!string.IsNullOrEmpty(nameMsg.Name))
                        {
                            state.userName = nameMsg.Name;
                        }
                        ServerSendToClient(connId, nameMsg);
                    }
                    break;

                case "MsgMatchRequest":
                    {
                        MsgMatchRequest req = (MsgMatchRequest)msg;
                        if (req.playerPack != null && req.playerPack.Count > 0)
                        {
                            state.heroId = req.playerPack[0].selectedHeroId;
                        }
                        Debug.Log($"[MirrorNetBridge] 玩家 {state.tempUserId} 选择英雄 {state.heroId}");
                    }
                    break;

                case "MsgPing":
                    {
                        state.lastPingTime = GetTimeStamp();
                        ServerSendToClient(connId, new MsgPong());
                    }
                    break;

                case "MsgQuitGame":
                    {
                        Debug.Log($"[MirrorNetBridge] 玩家 {state.tempUserId} 退出游戏");
                        conn.Disconnect();
                    }
                    break;
            }
        }

        /// <summary>
        /// 把收到的字节数据解码并分发给旧的 NetWorkMgr 监听器。
        /// </summary>
        public static void DispatchToOldListeners(byte[] raw)
        {
            if (!TryDecodeMessage(raw, out string protoName, out MsgBase msg))
            {
                Debug.LogWarning("[MirrorNetBridge] 客户端解码旧消息失败");
                return;
            }

            NetWorkMgr.DispatchMirrorMessage(protoName, msg);
        }

        private static long GetTimeStamp()
        {
            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return Convert.ToInt64(ts.TotalSeconds);
        }
    }

    /// <summary>
    /// 服务器端保存的 Mirror 连接玩家状态。
    /// </summary>
    public class MirrorClientState
    {
        public int connectionId;
        public string tempUserId;
        public string userName;
        public bool isReady;
        public int heroId;
        public long lastPingTime;
    }
}
