# 后端与登录抽象规划

## 目标

游戏入口必须由 Supabase Auth 进行账号验证，但账号验证不应该和玩家资料、房间目录、TCP/UDP 连接混在一起。

当前代码已经有一些有用的抽象，例如 `IBackendProvider`、`BackendProviderFactory`、`NetworkMode`。问题是 `IBackendProvider` 现在职责过宽：它同时负责登录、注册、玩家资料、创建房间、查询房间、加入房间、房间心跳。这样会导致一个很简单的需求：只用 Supabase 验证账号，也被迫依赖 `profiles`、`rooms` 等数据库表。

## 当前状态

### 登录入口

- `LoginPanel` 当前强制使用 `NetworkMode.SupabaseOnline`。
- 多多号登录等价于账号登录，使用账号输入框和密码。
- 名字登录等价于用户名登录，目前会尝试通过 `profiles.username` 查询邮箱，再调用 Supabase Auth。
- 只有 `LoginAsync` 成功并返回非空 `UserId` 后，才允许进入游戏。

### 后端 Provider

- `BackendProviderFactory` 根据 `NetworkMode` 创建 Provider。
- `SupabaseBackendProvider` 当前同时负责 Supabase Auth、用户名映射、玩家资料 fallback、房间表操作。
- `LocalBackendProvider` 和 `LanBackendProvider` 把旧协议流程适配到了同一个 `IBackendProvider` 接口里。

### 场景加载与传输连接

- `Async_Load` 在场景加载时仍然会调用 `NetWorkMgr.Connect(ServerConfig.ServerIp, ServerConfig.TcpPort)`。
- 这是传输层行为，不是账号验证行为。
- 在 Supabase 登录模式下，如果还没有选择战斗服务器或房间端点，那么 TCP 连接失败警告是预期现象，不应该被当成 Supabase Auth 失败。

## 主要问题

1. `IBackendProvider` 职责过宽。
2. 用户名登录依赖数据库资料表，如果没有 `profiles` 表就会失败。
3. Supabase Auth 和 Supabase REST 表访问被放在同一个后端表面里。
4. 场景加载仍然包含旧 TCP 服务器连接逻辑。
5. 部分中文 Debug 日志所在文件编码不稳定，Unity Console 会出现乱码。
6. 底层错误直接透传到 UI 或 Debug，例如 PostgREST 表不存在、UnityWebRequest HTTP 0、TCP 连接失败、Supabase Auth 原始错误。

## 目标抽象

### `IAuthProvider`

只负责账号验证。

```csharp
Task<AuthLoginResult> LoginAsync(AuthLoginRequest request);
Task<AuthRegisterResult> RegisterAsync(AuthRegisterRequest request);
Task<AuthSession> RefreshAsync(string refreshToken);
```

职责：

- Supabase 邮箱/密码登录。
- 可选的账号 ID 或用户名解析。
- 保存和刷新 session token。
- 统一认证错误。

不负责：

- 玩家显示资料。
- 创建房间。
- TCP/UDP 连接。

### `IPlayerProfileProvider`

只负责玩家展示资料和用户名映射。

```csharp
Task<PlayerBasicInfo> GetOrCreateProfileAsync(string authUserId, string email);
Task<string> ResolveEmailByUsernameAsync(string username);
```

第一版 Supabase Auth 门禁可以先不接这个层。如果必须支持“名字登录 = 用户名登录”，就需要这个层提供用户名到邮箱的映射，或者明确规定用户名本身就是 Supabase Auth 可登录账号。

### `IRoomDirectoryProvider`

只负责房间元数据。

```csharp
Task<RoomInfo> CreateRoomAsync(CreateRoomRequest request);
Task<IReadOnlyList<RoomInfo>> GetRoomListAsync(GetRoomListRequest request);
Task<bool> JoinRoomAsync(string roomId);
Task HeartbeatRoomAsync(string roomId);
```

Supabase 的 `rooms` 表应该属于这个层，不应该属于 Auth 层。

### `ITransportConnector`

只负责实际网络连接。

```csharp
Task<bool> ConnectAsync(NetworkEndpoint endpoint);
bool IsConnected { get; }
void Disconnect();
```

这个层封装 `NetWorkMgr.Connect`、局域网房主/客户端连接，以及未来可能接入的专用战斗服务器连接。

## 轻量错误层

项目需要加一个轻量错误层，但不需要一开始就做复杂异常框架。它的职责是把底层失败转换成稳定的游戏错误码、中文用户提示、中文 Debug 详情。

当前典型底层错误：

- Supabase Auth 返回 `Invalid login credentials`。
- UnityWebRequest 返回 `Cannot connect to destination host (HTTP 0)`。
- PostgREST 返回 `PGRST205`，提示 `public.profiles` 表不存在。
- `Async_Load` 连接旧 TCP 服务器失败，但场景加载继续。

建议结构：

```csharp
public enum GameErrorCode
{
    None,

    AuthInvalidCredentials,
    AuthEmailNotConfirmed,
    AuthUserNotFound,
    AuthUsernameLoginNotConfigured,

    NetworkUnavailable,
    ServerConnectionFailed,

    SupabaseConfigMissing,
    SupabaseTableMissing,
    Unknown
}

public class GameError
{
    public GameErrorCode Code;
    public string UserMessage;
    public string DebugMessage;
}
```

使用规则：

- UI 显示 `UserMessage`，必须是中文。
- Debug 打 `DebugMessage`，必须是中文，并保留必要技术细节。
- 业务流程判断 `Code`，不要匹配原始字符串。
- 底层 SDK、HTTP、数据库错误不要直接拼进玩家可见文案。

短期映射示例：

| 底层失败 | 游戏错误码 | 用户提示 |
| --- | --- | --- |
| `Invalid login credentials` | `AuthInvalidCredentials` | `账号或密码不正确` |
| `Email not confirmed` | `AuthEmailNotConfirmed` | `邮箱还未验证，请先完成验证` |
| HTTP 0 / 无法连接 | `NetworkUnavailable` | `无法连接认证服务，请检查网络后重试` |
| `PGRST205` / 缺少 `profiles` 表 | `AuthUsernameLoginNotConfigured` | `用户名登录暂未配置，请使用账号登录` |
| Supabase URL/key 为空 | `SupabaseConfigMissing` | `认证服务配置缺失，请联系开发者` |

建议接入点：

- `SupabaseAuthClient` 把原始 Auth REST 错误转换成 `GameError`。
- `SupabaseBackendProvider` 把资料表查询失败转换成 `GameError`。
- `LoginResult` 和 `RegisterResult` 保留 `ErrorMessage` 兼容旧代码，同时新增 `GameError Error` 给新代码使用。
- `LoginPanel` 优先显示 `result.Error.UserMessage`。
- `Async_Load` 后续应该发出传输层错误码，而不是只打一个 warning 字符串。

## 推荐阶段

### 阶段 1：稳定 Supabase Auth 门禁

- 进入游戏只以 Supabase Auth 登录成功为准。
- 多多号/账号登录直接调用 Supabase Auth。
- 名字/用户名登录要么先禁用并给清晰提示，要么必须有真实用户名映射表支撑。
- 账号登录不依赖 `profiles` 或 `rooms`。
- Auth 和加载相关 Debug 保留中文，但相关 C# 文件保存为 UTF-8 with BOM，保证 Unity 在 Windows 下稳定读取。
- 为 Supabase Auth 和用户名登录表缺失错误接入轻量 `GameError` 层。

### 阶段 2：拆分接口

- 新增 `IAuthProvider`，把 Supabase Auth 从 `IBackendProvider` 中拆出来。
- 暂时保留 `IBackendProvider` 作为旧代码适配层。
- 新增 `SupabaseAuthProvider`，只处理认证。
- 把错误归一化放进共享 helper，让 `IAuthProvider` 返回稳定的 `GameError`。

### 阶段 3：迁移资料与房间逻辑

- 把用户名到邮箱的查询迁移到 `IPlayerProfileProvider`。
- 把 Supabase 房间表逻辑迁移到 `IRoomDirectoryProvider`。
- `PlayerBasicInfoMgr` 只保留会话状态，不再作为所有后端能力的服务定位器。

### 阶段 4：拆分场景加载与连接

- 移除 `Async_Load` 里的无条件 TCP 连接。
- 进入战斗前由 `ITransportConnector` 根据房间/端点选择进行连接。
- 场景加载只负责加载场景，传输层只负责连接服务器。

## 当前最小修复方向

在当前项目状态下，最安全的近期行为是：

1. 多多号/账号登录只验证 Supabase Auth。
2. 名字/用户名登录在没有用户名映射表时给出清晰中文提示，不阻塞账号登录。
3. Auth 成功后用 `auth.user.id` 创建本地 `PlayerBasicInfo`。
4. `Async_Load` 不再输出乱码中文。
5. Supabase REST 表失败不应该阻塞账号登录，除非当前选择的是明确依赖该表的用户名登录。

## 近期错误层任务

1. 在 `NetWorkScripts/Backend` 或新建 `NetWorkScripts/Errors` 下增加 `GameErrorCode`、`GameError`、`GameErrorFactory`。
2. 给 `LoginResult` 和 `RegisterResult` 增加 `GameError Error`，保留 `ErrorMessage` 兼容旧代码。
3. 先转换 Supabase Auth 错误：
   - 账号或密码错误。
   - 邮箱未验证。
   - HTTP 0 / 网络不可达。
   - Supabase 配置缺失。
4. 再转换用户名登录错误：
   - 资料表不存在。
   - 用户名不存在。
   - 缺少 email 字段。
5. 更新 `LoginPanel` 和 `RegisterPanel`，显示 `Error.UserMessage`，Debug 输出 `Error.DebugMessage`。
6. 所有玩家可见与 Debug 可见文案都保留中文，相关 C# 文件保存为 UTF-8 with BOM。
