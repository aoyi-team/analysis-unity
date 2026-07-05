using UnityEngine;

/// <summary>
/// 战斗场景前的网络连接桥接器。
/// 在进入战斗前根据当前 NetworkMode 与 TargetEndpoint 调用 NetWorkMgr 连接目标地址。
/// </summary>
public class BattleConnectionBridge : MonoBehaviour
{
    public static BattleConnectionBridge Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// 在切换到战斗场景前调用，建立 TCP 连接。
    /// </summary>
    public void ConnectBeforeBattle()
    {
        PlayerBasicInfoMgr mgr = PlayerBasicInfoMgr.Instance;
        if (mgr == null)
        {
            Debug.LogWarning("[BattleConnectionBridge] PlayerBasicInfoMgr 为空");
            return;
        }

        NetworkEndpoint endpoint = mgr.TargetEndpoint;
        switch (mgr.CurrentNetworkMode)
        {
            case NetworkMode.LocalServer:
            case NetworkMode.LanClient:
                if (!string.IsNullOrEmpty(endpoint.TcpIp))
                {
                    NetWorkMgr.Instance.Connect(endpoint.TcpIp, endpoint.TcpPort);
                    Debug.Log($"[BattleConnectionBridge] 连接到 {endpoint.TcpIp}:{endpoint.TcpPort}");
                }
                else
                {
                    Debug.LogWarning("[BattleConnectionBridge] TargetEndpoint 未设置");
                }
                break;

            case NetworkMode.LanHost:
                NetWorkMgr.Instance.Connect("127.0.0.1", endpoint.TcpPort);
                Debug.Log($"[BattleConnectionBridge] 局域网房主连接本机 {endpoint.TcpPort}");
                break;

            case NetworkMode.SupabaseOnline:
                if (!string.IsNullOrEmpty(endpoint.TcpIp))
                {
                    NetWorkMgr.Instance.Connect(endpoint.TcpIp, endpoint.TcpPort);
                    Debug.Log($"[BattleConnectionBridge] Supabase 模式连接到 {endpoint.TcpIp}:{endpoint.TcpPort}");
                }
                else
                {
                    Debug.LogWarning("[BattleConnectionBridge] Supabase 模式下 TargetEndpoint 未设置");
                }
                break;
        }
    }
}
