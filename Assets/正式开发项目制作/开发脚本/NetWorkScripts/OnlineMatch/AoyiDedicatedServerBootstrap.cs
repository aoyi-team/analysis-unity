using Aoyi.Mirror;
using Mirror;
using UnityEngine;

public static class AoyiDedicatedServerBootstrap
{
    private const int MaxPlayers = 2;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void StartDedicatedServer()
    {
#if UNITY_SERVER
        if (NetworkServer.active)
        {
            return;
        }

        AoyiNetworkRoomManager nm = EnsureRoomManager();
        if (nm == null)
        {
            return;
        }

        nm.maxRoomPlayers = MaxPlayers;
        nm.pendingBattleScene = "dantiao_map";
        nm.networkAddress = "0.0.0.0";

        if (nm.transport is kcp2k.KcpTransport transport)
        {
            int port = SupabaseConfig.Instance.EdgegapDedicatedServerListenPort;
            if (port <= 0 || port > ushort.MaxValue)
            {
                port = ServerConfig.EdgegapDedicatedServerListenPort;
            }

            transport.Port = (ushort)port;
            Transport.active = transport;
            Debug.Log($"[AoyiDedicatedServerBootstrap] Dedicated Server 监听 KCP UDP :{port}");
        }
        else
        {
            Debug.LogError("[AoyiDedicatedServerBootstrap] Dedicated Server 需要 KcpTransport");
            return;
        }

        nm.StartServer();
        Debug.Log("[AoyiDedicatedServerBootstrap] Mirror Dedicated Server 已启动");
#endif
    }

#if UNITY_SERVER
    private static AoyiNetworkRoomManager EnsureRoomManager()
    {
        AoyiNetworkRoomManager existing = AoyiNetworkRoomManager.singleton;
        if (existing != null)
        {
            kcp2k.KcpTransport existingTransport = existing.GetComponent<kcp2k.KcpTransport>();
            if (existingTransport == null)
            {
                existingTransport = existing.gameObject.AddComponent<kcp2k.KcpTransport>();
            }

            existing.transport = existingTransport;
            Transport.active = existingTransport;
            return existing;
        }

        if (NetworkManager.singleton != null && !(NetworkManager.singleton is AoyiNetworkRoomManager))
        {
            Debug.LogError("[AoyiDedicatedServerBootstrap] 场景中存在非 AoyiNetworkRoomManager 的 NetworkManager");
            return null;
        }

        GameObject go = new GameObject("AoyiDedicatedRoomManager");
        Object.DontDestroyOnLoad(go);
        kcp2k.KcpTransport transport = go.AddComponent<kcp2k.KcpTransport>();
        AoyiNetworkRoomManager manager = go.AddComponent<AoyiNetworkRoomManager>();
        manager.transport = transport;
        Transport.active = transport;
        return manager;
    }
#endif
}
