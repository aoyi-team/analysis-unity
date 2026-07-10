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

    private static Task<bool> StartPlayerHostedRelayAsync(AoyiNetworkRoomManager nm, OnlineMatchResponse match)
    {
        EdgegapKcpTransport transport = nm.transport as EdgegapKcpTransport;
        if (transport == null)
        {
            Debug.LogError("[OnlineConnectionLauncher] PlayerHostedRelay 需要 EdgegapKcpTransport");
            return Task.FromResult(false);
        }

        if (!TryGetRelayConnectionInfo(match, out EdgegapRelayConnectionInfo connectionInfo))
        {
            Debug.LogError("[OnlineConnectionLauncher] 匹配结果缺少 Edgegap relay_session 连接信息");
            return Task.FromResult(false);
        }

        if (!ConfigureRelayTransport(transport, connectionInfo))
        {
            return Task.FromResult(false);
        }

        if (string.Equals(match.Role, "host", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(StartRelayHost(nm, match.RoomId, connectionInfo));
        }

        if (string.Equals(match.Role, "guest", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(StartRelayClient(nm, match.RoomId, connectionInfo));
        }

        Debug.LogWarning($"[OnlineConnectionLauncher] 未知在线匹配角色：{match.Role}");
        return Task.FromResult(false);
    }

    private static bool StartRelayHost(AoyiNetworkRoomManager nm, string roomId, EdgegapRelayConnectionInfo connectionInfo)
    {
        try
        {
            nm.networkAddress = roomId;
            nm.StartHost();
            Debug.Log($"[OnlineConnectionLauncher] Host 已通过 Edgegap Distributed Relay 启动，relay={connectionInfo.RelayHost}, roomId={roomId}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[OnlineConnectionLauncher] 启动 Edgegap relay host 失败：{ex}");
            return false;
        }
    }

    private static bool StartRelayClient(AoyiNetworkRoomManager nm, string roomId, EdgegapRelayConnectionInfo connectionInfo)
    {
        try
        {
            nm.networkAddress = roomId;
            nm.StartClient();
            Debug.Log($"[OnlineConnectionLauncher] Guest 已通过 Edgegap Distributed Relay 连接，relay={connectionInfo.RelayHost}, roomId={roomId}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[OnlineConnectionLauncher] 连接 Edgegap relay host 失败：{ex}");
            return false;
        }
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

            EdgegapKcpTransport existingTransport = existing.GetComponent<EdgegapKcpTransport>();
            if (existingTransport == null)
            {
                existingTransport = existing.gameObject.AddComponent<EdgegapKcpTransport>();
            }

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
        EdgegapKcpTransport transport = go.AddComponent<EdgegapKcpTransport>();
        AoyiNetworkRoomManager manager = go.AddComponent<AoyiNetworkRoomManager>();
        manager.transport = transport;
        Transport.active = transport;
        Debug.Log("[OnlineConnectionLauncher] 自动创建 AoyiNetworkRoomManager + EdgegapKcpTransport（PlayerHostedRelay）");
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

    private static bool TryGetRelayConnectionInfo(OnlineMatchResponse match, out EdgegapRelayConnectionInfo connectionInfo)
    {
        connectionInfo = null;
        if (match?.Room == null)
        {
            return false;
        }

        var relaySession = match.Room["relay_session"] as Newtonsoft.Json.Linq.JObject;
        if (relaySession == null)
        {
            return false;
        }

        string infoKey = string.Equals(match.Role, "host", StringComparison.OrdinalIgnoreCase)
            ? "host_connection_info"
            : "guest_connection_info";

        var info = relaySession[infoKey] as Newtonsoft.Json.Linq.JObject;
        if (info == null)
        {
            return false;
        }

        connectionInfo = info.ToObject<EdgegapRelayConnectionInfo>();
        return connectionInfo != null;
    }

    private static bool ConfigureRelayTransport(EdgegapKcpTransport transport, EdgegapRelayConnectionInfo connectionInfo)
    {
        if (connectionInfo == null)
        {
            Debug.LogError("[OnlineConnectionLauncher] Edgegap relay 连接信息为空");
            return false;
        }

        if (string.IsNullOrWhiteSpace(connectionInfo.RelayHost) && string.IsNullOrWhiteSpace(connectionInfo.RelayIp))
        {
            Debug.LogError("[OnlineConnectionLauncher] Edgegap relay host/ip 为空");
            return false;
        }

        if (!IsValidUdpPort(connectionInfo.RelayServerPort) || !IsValidUdpPort(connectionInfo.RelayClientPort))
        {
            Debug.LogError($"[OnlineConnectionLauncher] Edgegap relay 端口非法：server={connectionInfo.RelayServerPort}, client={connectionInfo.RelayClientPort}");
            return false;
        }

        transport.relayAddress = string.IsNullOrWhiteSpace(connectionInfo.RelayHost)
            ? connectionInfo.RelayIp
            : connectionInfo.RelayHost;
        transport.relayGameServerPort = (ushort)connectionInfo.RelayServerPort;
        transport.relayGameClientPort = (ushort)connectionInfo.RelayClientPort;
        transport.sessionId = connectionInfo.SessionToken;
        transport.userId = connectionInfo.UserToken;
        transport.relayGUI = false;
        Transport.active = transport;
        return true;
    }

    private static bool IsValidUdpPort(int port)
    {
        return port > 0 && port <= ushort.MaxValue;
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
