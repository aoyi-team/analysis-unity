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
    #endregion
}