using System;

/// <summary>
/// 网络运行模式枚举
/// </summary>
public enum NetworkMode
{
    /// <summary>本地单进程服务器（调试或单机模式）</summary>
    LocalServer,
    /// <summary>局域网房主模式</summary>
    LanHost,
    /// <summary>局域网客户端模式</summary>
    LanClient,
    /// <summary>Supabase 在线后端模式</summary>
    SupabaseOnline
}

/// <summary>
/// 网络端点信息（TCP + UDP）
/// </summary>
[Serializable]
public struct NetworkEndpoint
{
    /// <summary>TCP 服务 IP</summary>
    public string TcpIp;
    /// <summary>TCP 服务端口</summary>
    public int TcpPort;
    /// <summary>UDP 服务 IP</summary>
    public string UdpIp;
    /// <summary>UDP 服务端口</summary>
    public int UdpPort;

    public NetworkEndpoint(string tcpIp, int tcpPort, string udpIp, int udpPort)
    {
        TcpIp = tcpIp;
        TcpPort = tcpPort;
        UdpIp = udpIp;
        UdpPort = udpPort;
    }
}

/// <summary>
/// 玩家基础信息
/// </summary>
[Serializable]
public struct PlayerBasicInfo
{
    /// <summary>用户唯一标识</summary>
    public string UserId;
    /// <summary>用户显示名称</summary>
    public string UserName;
    /// <summary>最后一次登录时间（UTC）</summary>
    public DateTime LastLoginAt;
}

/// <summary>
/// 房间状态枚举
/// </summary>
public enum RoomStatus
{
    /// <summary>等待中</summary>
    Waiting,
    /// <summary>游戏中</summary>
    Playing,
    /// <summary>已满员</summary>
    Full,
    /// <summary>已关闭</summary>
    Closed
}

/// <summary>
/// 房间信息
/// </summary>
[Serializable]
public struct RoomInfo
{
    /// <summary>房间唯一标识</summary>
    public string RoomId;
    /// <summary>房间显示名称</summary>
    public string RoomName;
    /// <summary>房间游戏模式</summary>
    public GameModes Mode;
    /// <summary>房主网络端点</summary>
    public NetworkEndpoint HostEndpoint;
    /// <summary>当前玩家数</summary>
    public int CurrentPlayers;
    /// <summary>最大玩家数</summary>
    public int MaxPlayers;
    /// <summary>协议版本号（用于匹配兼容性）</summary>
    public int ProtocolVersion;
    /// <summary>房间当前状态</summary>
    public RoomStatus Status;
}
