using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// 后端服务提供者抽象接口
/// 统一封装 Supabase、局域网房间等外部服务的访问方式
/// </summary>
public interface IBackendProvider
{
    /// <summary>
    /// 异步登录
    /// </summary>
    /// <param name="credentials">登录凭证（具体类型由实现决定）</param>
    Task<LoginResult> LoginAsync(object credentials);

    /// <summary>
    /// 异步注册
    /// </summary>
    /// <param name="credentials">注册凭证（具体类型由实现决定）</param>
    Task<RegisterResult> RegisterAsync(object credentials);

    /// <summary>
    /// 根据用户 ID 获取玩家基础信息
    /// </summary>
    Task<PlayerBasicInfo> GetPlayerInfoAsync(string userId);

    /// <summary>
    /// 创建房间
    /// </summary>
    Task<RoomInfo> CreateRoomAsync(CreateRoomRequest request);

    /// <summary>
    /// 获取房间列表
    /// </summary>
    Task<List<RoomInfo>> GetRoomListAsync(GetRoomListRequest request);

    /// <summary>
    /// 加入指定房间
    /// </summary>
    /// <returns>是否加入成功</returns>
    Task<bool> JoinRoomAsync(string roomId);

    /// <summary>
    /// 房间心跳（保持房间存活状态）
    /// </summary>
    Task HeartbeatRoomAsync(string roomId);
}

/// <summary>
/// 登录结果
/// </summary>
public class LoginResult
{
    /// <summary>是否成功</summary>
    public bool Success;
    /// <summary>错误信息（失败时有效）</summary>
    public string ErrorMessage;
    /// <summary>用户唯一标识</summary>
    public string UserId;
    /// <summary>访问令牌</summary>
    public string AccessToken;
    /// <summary>刷新令牌</summary>
    public string RefreshToken;
}

/// <summary>
/// 注册结果
/// </summary>
public class RegisterResult
{
    /// <summary>是否成功</summary>
    public bool Success;
    /// <summary>错误信息（失败时有效）</summary>
    public string ErrorMessage;
    /// <summary>新注册用户的唯一标识</summary>
    public string UserId;
}

/// <summary>
/// 创建房间请求
/// </summary>
public class CreateRoomRequest
{
    /// <summary>房间显示名称</summary>
    public string RoomName;
    /// <summary>游戏模式</summary>
    public GameModes Mode;
    /// <summary>最大玩家数</summary>
    public int MaxPlayers;
    /// <summary>房主对外网络端点</summary>
    public NetworkEndpoint HostEndpoint;
    /// <summary>协议版本号</summary>
    public int ProtocolVersion;
}

/// <summary>
/// 获取房间列表请求
/// </summary>
public class GetRoomListRequest
{
    /// <summary>目标游戏模式，Nullable 表示不筛选</summary>
    public GameModes? Mode;
    /// <summary>协议版本号，用于过滤不兼容房间</summary>
    public int ProtocolVersion;
    /// <summary>最大返回数量</summary>
    public int MaxResults;
}
