using UnityEngine;

/// <summary>
/// Supabase 运行时配置资源。
/// 通过 CreateAssetMenu 创建，运行时从 Resources/SupabaseConfig 加载。
/// </summary>
[CreateAssetMenu(fileName = "SupabaseConfig", menuName = "Config/SupabaseConfig")]
public class SupabaseConfig : ScriptableObject
{
    [Tooltip("Supabase 项目 URL，例如 https://your-project.supabase.co")]
    public string SupabaseUrl = ServerConfig.SupabaseUrl;

    [Tooltip("Supabase 匿名公开 API Key")]
    public string AnonKey = ServerConfig.SupabaseAnonKey;

    [Tooltip("Web 在线匹配 API 根地址，例如 https://your-app.vercel.app")]
    public string OnlineMatchApiBaseUrl = ServerConfig.OnlineMatchApiBaseUrl;

    [Tooltip("Edgegap Lobby Service URL，用于 Mirror EdgegapLobbyKcpTransport")]
    public string EdgegapLobbyUrl = ServerConfig.EdgegapLobbyUrl;

    [Tooltip("在线匹配连接模式：DedicatedServer=Edgegap 专服，PlayerHostedRelay=玩家主机+Relay")]
    public OnlineConnectionMode OnlineConnectionMode = ServerConfig.DefaultOnlineConnectionMode;

    [Tooltip("旧配置字段：请改用 OnlineConnectionMode")]
    [System.Obsolete("Use OnlineConnectionMode instead.")]
    public bool UseEdgegapDedicatedServer = ServerConfig.UseEdgegapDedicatedServer;

    [Tooltip("Edgegap Dedicated Server 对外 FQDN，例如 xxx.pr.edgegap.net")]
    public string EdgegapDedicatedHost = ServerConfig.EdgegapDedicatedHost;

    [Tooltip("Edgegap Dedicated Server 对外 UDP 端口")]
    public int EdgegapDedicatedUdpPort = ServerConfig.EdgegapDedicatedUdpPort;

    [Tooltip("Edgegap 容器内 Mirror KCP 监听端口")]
    public int EdgegapDedicatedServerListenPort = ServerConfig.EdgegapDedicatedServerListenPort;

    [Tooltip("用户资料表名")]
    public string ProfilesTable = "profiles";

    [Tooltip("房间表名")]
    public string RoomsTable = "rooms";

    [Tooltip("REST 请求超时（秒）")]
    public float RequestTimeout = 10f;

    private static SupabaseConfig _instance;

    /// <summary>
    /// 全局单例：优先从 Resources/SupabaseConfig 加载，找不到则使用 ServerConfig 默认值创建内存实例。
    /// </summary>
    public static SupabaseConfig Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Resources.Load<SupabaseConfig>("SupabaseConfig");
                if (_instance == null)
                {
                    _instance = CreateInstance<SupabaseConfig>();
                }
            }
            return _instance;
        }
    }

    /// <summary>
    /// 重置缓存的单例引用，便于运行时切换配置（测试/热更场景）。
    /// </summary>
    public static void ResetInstance()
    {
        _instance = null;
    }
}
