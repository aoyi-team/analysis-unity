using System;
using System.Threading.Tasks;
using Aoyi.Mirror;
using Edgegap;
using Mirror;
using UnityEngine;

public enum OnlineConnectionMode
{
    DedicatedServer = 0,
    PlayerHostedRelay = 1,
    Lan = 2
}

public static class OnlineConnectionLauncher
{
    private const int MaxPlayers = 2;
    private const int LobbyLookupAttempts = 30;
    private const int LobbyLookupDelayMs = 1000;

    public static async Task<bool> StartMatchedRoomAsync(OnlineMatchResponse match, GameModes mode, int heroId, int skinId)
    {
        if (match == null)
        {
            Debug.LogWarning("[OnlineConnectionLauncher] 匹配结果为空，无法启动在线房间");
            return false;
        }

        if (string.IsNullOrWhiteSpace(match.RoomId))
        {
            Debug.LogWarning("[OnlineConnectionLauncher] roomId 为空，无法启动在线房间");
            return false;
        }

        if (string.IsNullOrWhiteSpace(match.Role))
        {
            Debug.LogWarning("[OnlineConnectionLauncher] role 为空，无法判断 host/guest");
            return false;
        }

        OnlineConnectionMode connectionMode = SupabaseConfig.Instance.OnlineConnectionMode;
        AoyiNetworkRoomManager nm = connectionMode == OnlineConnectionMode.DedicatedServer
            ? EnsureDedicatedRoomManager()
            : EnsurePlayerHostedRelayRoomManager();
        if (nm == null)
        {
            return false;
        }

        nm.maxRoomPlayers = MaxPlayers;
        nm.pendingBattleScene = mode.ToString() + "_map";
        PlayerBasicInfoMgr.Instance.CurrentNetworkMode = NetworkMode.SupabaseOnline;
        PlayerBasicInfoMgr.Instance.SetCurrentGamemode(mode);
        PlayerBasicInfoMgr.Instance.UpdateHeroCache(heroId, skinId);
        PlayerBasicInfoMgr.Instance.UpdateRoomID(match.RoomId);

        CleanupMirrorState(nm);

        switch (connectionMode)
        {
            case OnlineConnectionMode.DedicatedServer:
                return StartDedicatedClient(nm, match.RoomId, match.Role);
            case OnlineConnectionMode.PlayerHostedRelay:
                return await StartPlayerHostedRelayAsync(nm, match);
            case OnlineConnectionMode.Lan:
                Debug.LogWarning("[OnlineConnectionLauncher] Supabase 在线匹配不应使用 Lan 连接模式");
                return false;
            default:
                Debug.LogWarning($"[OnlineConnectionLauncher] 未支持的在线连接模式：{connectionMode}");
                return false;
        }
    }

    private static bool StartDedicatedClient(AoyiNetworkRoomManager nm, string roomId, string role)
    {
        string host = (SupabaseConfig.Instance.EdgegapDedicatedHost ?? string.Empty).Trim();
        int port = SupabaseConfig.Instance.EdgegapDedicatedUdpPort;

        if (string.IsNullOrWhiteSpace(host))
        {
            Debug.LogError("[OnlineConnectionLauncher] EdgegapDedicatedHost 未配置，无法连接专服");
            return false;
        }

        if (port <= 0 || port > ushort.MaxValue)
        {
            Debug.LogError($"[OnlineConnectionLauncher] EdgegapDedicatedUdpPort 非法：{port}");
            return false;
        }

        kcp2k.KcpTransport transport = nm.transport as kcp2k.KcpTransport;
        if (transport == null)
        {
            Debug.LogError("[OnlineConnectionLauncher] 专服直连需要 KcpTransport");
            return false;
        }

        nm.networkAddress = host;
        transport.Port = (ushort)port;
        Transport.active = transport;

        try
        {
            nm.StartClient();
            Debug.Log($"[OnlineConnectionLauncher] 在线匹配成功，作为客户端连接 Edgegap 专服 {host}:{port}，roomId={roomId}, role={role}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[OnlineConnectionLauncher] 连接 Edgegap 专服失败：{ex}");
            return false;
        }
    }

    private static async Task<bool> StartPlayerHostedRelayAsync(AoyiNetworkRoomManager nm, OnlineMatchResponse match)
    {
        EdgegapLobbyKcpTransport transport = nm.transport as EdgegapLobbyKcpTransport;
        if (transport == null)
        {
            Debug.LogError("[OnlineConnectionLauncher] PlayerHostedRelay 需要 EdgegapLobbyKcpTransport");
            return false;
        }

        if (string.IsNullOrWhiteSpace(transport.lobbyUrl))
        {
            Debug.LogError("[OnlineConnectionLauncher] EdgegapLobbyUrl 未配置，请在 Resources/SupabaseConfig.asset 中填入 Edgegap Lobby Service URL");
            return false;
        }

        if (string.Equals(match.Role, "host", StringComparison.OrdinalIgnoreCase))
        {
            return StartRelayHost(nm, transport, match.RoomId);
        }

        if (string.Equals(match.Role, "guest", StringComparison.OrdinalIgnoreCase))
        {
            return await StartRelayClientWhenLobbyAppears(nm, transport, match.RoomId);
        }

        Debug.LogWarning($"[OnlineConnectionLauncher] 未知在线匹配角色：{match.Role}");
        return false;
    }

    private static bool StartRelayHost(AoyiNetworkRoomManager nm, EdgegapLobbyKcpTransport transport, string roomId)
    {
        try
        {
            transport.SetServerLobbyParams(roomId, MaxPlayers);
            nm.networkAddress = roomId;
            nm.StartHost();
            Debug.Log($"[OnlineConnectionLauncher] Host 已开始创建 Edgegap relay lobby，name={roomId}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[OnlineConnectionLauncher] 启动 Edgegap relay host 失败：{ex}");
            return false;
        }
    }

    private static async Task<bool> StartRelayClientWhenLobbyAppears(AoyiNetworkRoomManager nm, EdgegapLobbyKcpTransport transport, string roomId)
    {
        for (int i = 0; i < LobbyLookupAttempts; i++)
        {
            LobbyBrief? lobby = await FindLobbyByNameAsync(transport, roomId);
            if (lobby.HasValue)
            {
                nm.networkAddress = lobby.Value.lobby_id;
                nm.StartClient();
                Debug.Log($"[OnlineConnectionLauncher] Guest 已找到 Edgegap relay lobby，name={roomId}, lobbyId={lobby.Value.lobby_id}");
                return true;
            }

            await Task.Delay(LobbyLookupDelayMs);
        }

        Debug.LogWarning($"[OnlineConnectionLauncher] 等待 Edgegap relay lobby 超时，name={roomId}");
        return false;
    }

    private static Task<LobbyBrief?> FindLobbyByNameAsync(EdgegapLobbyKcpTransport transport, string lobbyName)
    {
        var tcs = new TaskCompletionSource<LobbyBrief?>();
        transport.Api.RefreshLobbies(lobbies =>
        {
            if (lobbies != null)
            {
                foreach (LobbyBrief lobby in lobbies)
                {
                    if (lobby.is_joinable && string.Equals(lobby.name, lobbyName, StringComparison.OrdinalIgnoreCase))
                    {
                        tcs.TrySetResult(lobby);
                        return;
                    }
                }
            }

            tcs.TrySetResult(null);
        }, error =>
        {
            Debug.LogWarning($"[OnlineConnectionLauncher] 查询 Edgegap relay lobby 列表失败：{error}");
            tcs.TrySetResult(null);
        });

        return tcs.Task;
    }

    private static AoyiNetworkRoomManager EnsureDedicatedRoomManager()
    {
        AoyiNetworkRoomManager existing = AoyiNetworkRoomManager.singleton;
        if (existing != null)
        {
            CleanupMirrorState(existing);

            kcp2k.KcpTransport existingTransport = existing.GetComponent<kcp2k.KcpTransport>();
            if (existingTransport == null)
            {
                existingTransport = existing.gameObject.AddComponent<kcp2k.KcpTransport>();
            }

            ConfigureDedicatedTransport(existingTransport);
            existing.transport = existingTransport;
            Transport.active = existingTransport;
            return existing;
        }

        if (NetworkManager.singleton != null && !(NetworkManager.singleton is AoyiNetworkRoomManager))
        {
            Debug.LogError("[OnlineConnectionLauncher] 场景中存在非 AoyiNetworkRoomManager 的 NetworkManager");
            return null;
        }

        GameObject go = new GameObject("AoyiOnlineRoomManager");
        UnityEngine.Object.DontDestroyOnLoad(go);
        kcp2k.KcpTransport transport = go.AddComponent<kcp2k.KcpTransport>();
        ConfigureDedicatedTransport(transport);
        AoyiNetworkRoomManager manager = go.AddComponent<AoyiNetworkRoomManager>();
        manager.transport = transport;
        Transport.active = transport;
        Debug.Log("[OnlineConnectionLauncher] 自动创建 AoyiNetworkRoomManager + KcpTransport（DedicatedServer）");
        return manager;
    }

    private static AoyiNetworkRoomManager EnsurePlayerHostedRelayRoomManager()
    {
        AoyiNetworkRoomManager existing = AoyiNetworkRoomManager.singleton;
        if (existing != null)
        {
            CleanupMirrorState(existing);

            EdgegapLobbyKcpTransport existingTransport = existing.GetComponent<EdgegapLobbyKcpTransport>();
            if (existingTransport == null)
            {
                existingTransport = existing.gameObject.AddComponent<EdgegapLobbyKcpTransport>();
            }

            ConfigureRelayTransport(existingTransport);
            existing.transport = existingTransport;
            Transport.active = existingTransport;
            return existing;
        }

        if (NetworkManager.singleton != null && !(NetworkManager.singleton is AoyiNetworkRoomManager))
        {
            Debug.LogError("[OnlineConnectionLauncher] 场景中存在非 AoyiNetworkRoomManager 的 NetworkManager");
            return null;
        }

        GameObject go = new GameObject("AoyiOnlineRoomManager");
        UnityEngine.Object.DontDestroyOnLoad(go);
        EdgegapLobbyKcpTransport transport = go.AddComponent<EdgegapLobbyKcpTransport>();
        ConfigureRelayTransport(transport);
        AoyiNetworkRoomManager manager = go.AddComponent<AoyiNetworkRoomManager>();
        manager.transport = transport;
        Transport.active = transport;
        Debug.Log("[OnlineConnectionLauncher] 自动创建 AoyiNetworkRoomManager + EdgegapLobbyKcpTransport（PlayerHostedRelay）");
        return manager;
    }

    private static void ConfigureDedicatedTransport(kcp2k.KcpTransport transport)
    {
        int port = SupabaseConfig.Instance.EdgegapDedicatedUdpPort;
        if (port > 0 && port <= ushort.MaxValue)
        {
            transport.Port = (ushort)port;
        }
    }

    private static void ConfigureRelayTransport(EdgegapLobbyKcpTransport transport)
    {
        transport.lobbyUrl = SupabaseConfig.Instance.EdgegapLobbyUrl;
        transport.Api = new LobbyApi(transport.lobbyUrl);
        transport.relayGUI = false;
    }

    private static void CleanupMirrorState(AoyiNetworkRoomManager nm)
    {
        if (NetworkServer.active && NetworkClient.active)
        {
            nm.StopHost();
        }
        else if (NetworkClient.active)
        {
            nm.StopClient();
        }
        else if (NetworkServer.active)
        {
            nm.StopServer();
        }
    }
}

/// <summary>
/// Backward-compatible entrypoint for older UI code.
/// New online connection code should call OnlineConnectionLauncher directly.
/// </summary>
public static class OnlineRelayConnector
{
    public static Task<bool> StartMatchedRoomAsync(OnlineMatchResponse match, GameModes mode, int heroId, int skinId)
    {
        return OnlineConnectionLauncher.StartMatchedRoomAsync(match, mode, heroId, skinId);
    }
}
