public class ServerConfig
{
    // 服务器默认地址（运行时可通过属性修改）
    public static string ServerIp { get; set; } = "127.0.0.1";
    public const int UdpPort = 887;
    public const int TcpPort = 888;

    // һ��20֡
    public static readonly float frameTime=0.05f;

    #region 局域网发现默认配置
    /// <summary>局域网房间广播端口</summary>
    public const int LanBroadcastPort = 889;
    /// <summary>局域网组播地址（可选，为空时使用广播地址 255.255.255.255）</summary>
    public const string LanMulticastAddress = "";
    /// <summary>局域网房间信标发送间隔（秒）</summary>
    public static readonly float LanBeaconInterval = 1.0f;
    #endregion

    #region 协议与在线服务默认配置
    /// <summary>网络协议版本号，用于匹配兼容性校验</summary>
    public const int ProtocolVersion = 1;
    /// <summary>Supabase 项目 URL（运行时替换为实际项目地址）</summary>
    public const string SupabaseUrl = "https://your-project.supabase.co";
    /// <summary>Supabase 匿名公开 API Key</summary>
    public const string SupabaseAnonKey = "your-anon-key";
    /// <summary>Web 在线匹配 API 根地址，例如 https://your-app.vercel.app</summary>
    public const string OnlineMatchApiBaseUrl = "https://aoyi-web.vercel.app";
    /// <summary>Edgegap Lobby Service URL。需要在 Edgegap 部署 Lobby Service 后填入。</summary>
    public const string EdgegapLobbyUrl = "";
    /// <summary>在线匹配默认连接模式。</summary>
    public const OnlineConnectionMode DefaultOnlineConnectionMode = OnlineConnectionMode.DedicatedServer;
    /// <summary>在线匹配是否优先连接 Edgegap Dedicated Server，而不是让玩家创建 Lobby/Host。</summary>
    public const bool UseEdgegapDedicatedServer = true;
    /// <summary>Edgegap Dedicated Server 对外 FQDN。重新部署后可能变化。</summary>
    public const string EdgegapDedicatedHost = "bf5855a39bca.pr.edgegap.net";
    /// <summary>Edgegap Dedicated Server 对外 UDP 端口。重新部署后可能变化。</summary>
    public const int EdgegapDedicatedUdpPort = 30685;
    /// <summary>容器内 Mirror KCP 监听端口，必须和 Edgegap App Version 的 internal port 一致。</summary>
    public const int EdgegapDedicatedServerListenPort = 7777;
    #endregion
}
