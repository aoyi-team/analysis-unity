# 匹配会话与联机生命周期抽象规划

## 目标

把“开始匹配、建立连接、进入房间、进入战斗、结束游戏、返回大厅、关闭远程房间”整理成一条由单一协调器管理的会话生命周期。

当前项目已经能够完成本地服务器、局域网、Supabase 在线匹配、Edgegap Relay 和 Dedicated Server 等多种流程。现在的主要问题不是功能缺失，而是同一个会话状态分别由 `ChooseHeroPanel`、`OnlineMatchManager`、`OnlineConnectionLauncher`、`AoyiNetworkRoomManager`、`BattleManager` 和 `PlayerBasicInfoMgr` 修改。只要其中一个对象提前停止网络、加载场景或清理房间，就容易再次出现以下问题：

- 游戏结束后只有房主返回大厅。
- 客户端已经返回大厅，但 Mirror 仍处于连接状态。
- 玩家可以重新匹配，但 Web 系统仍显示旧房间未结束。
- 取消匹配后，旧的 Relay 重试仍在后台继续。
- 旧异步请求完成后覆盖新一轮匹配状态。

本规划的核心方向是：保留现有 Mirror、Edgegap、Supabase 和旧消息协议，只重新划分状态和生命周期的所有权。

## 当前状态

### 匹配入口

- `ChooseHeroPanel` 根据 `NetworkMode` 决定使用本地服务器、局域网快速匹配还是在线匹配。
- `HeroSelectionMatchController` 已经尝试把模式分支从 UI 事件中抽离，但目前仍位于 `ChooseHeroPanel.cs`，构造函数依赖多个委托。
- `ChooseHeroPanel.cs` 末尾还保留了一份通过 `#if false` 禁用的旧 `OnlineMatchManager` 实现。
- 当前生效的 `OnlineMatchManager` 已经独立到 `NetWorkScripts/OnlineMatch/OnlineMatchManager.cs`，并包含 generation、轮询和取消保护。

### 在线连接

- `OnlineConnectionLauncher` 同时负责匹配结果校验、场景选择、NetworkManager 创建、Transport 配置、Dedicated Server 连接、Relay Host、Relay Guest 重试、Mirror 清理和远程房间关闭。
- Dedicated Server 使用 `KcpTransport`。
- Player Hosted Relay 使用 `EdgegapKcpTransport`。
- 当前 `CancellationToken` 重载只在整个连接调用前后检查取消，内部初始等待、连接轮询和重试等待还没有完整地使用同一个 token。

### 场景加载

- `SceneMgr` 已经知道 `paiwei_solo` 应该复用 `paiwei_map`。
- `OnlineConnectionLauncher` 仍使用 `mode.ToString() + "_map"` 构造场景名。
- 因此在线单人排位可能得到不存在的 `paiwei_solo_map`。
- `LobbyPanel`、`dantiao_map` 和 `paiwei_map` 等场景名也分散在多个 Manager 中。

### 战斗结束与返回大厅

- `BattleManager` 收到 `MsgBattleOver` 后停止战斗循环。
- `BattleManager` 当前还会直接停止 Mirror Client，并直接加载 `LobbyPanel`。
- `AoyiNetworkRoomManager` 同时监听服务器和客户端场景变化，并在返回大厅后停止 Host、Server 或 Client。
- `OnlineConnectionLauncher.NotifyMatchedRoomEnded` 负责调用 Web 匹配接口关闭远程房间。
- Host 和 Guest 都可能走到远程关闭调用，但真正具有房间关闭权的应当只有服务器或房主。

### 全局状态

`PlayerBasicInfoMgr` 当前同时保存：

- 账号 ID 和玩家名称。
- 英雄与皮肤偏好。
- 当前 `NetworkMode`。
- 当前 `IBackendProvider`。
- `TargetEndpoint`。
- `RoomId` 和 `TeamId`。
- 战斗玩家列表和本地战斗 ID。

这些数据并不属于同一个生命周期。账号信息应跨多局保留，匹配房间信息应在每局结束时清理，英雄偏好应持久化，战斗快照只应在当前战斗有效。

## 主要问题

1. 会话状态没有单一所有者，多个 Manager 都能结束或重置同一局游戏。
2. 返回大厅、停止 Mirror、关闭远程房间的顺序没有统一入口。
3. 场景名和 `GameModes` 映射分散，已经出现 `paiwei_solo` 映射不一致。
4. `OnlineConnectionLauncher` 职责过宽，Dedicated 和 Relay 的差异被放在一个静态类中。
5. Relay 取消没有贯穿所有等待和重试边界。
6. `ChooseHeroPanel` 同时负责 UI、资源、动画和匹配模式分发。
7. `MirrorNetBridge` 同时承担协议编码、连接玩家注册、登录/房间消息和战斗帧聚合。
8. `PlayerBasicInfoMgr` 混合账号、会话、战斗和偏好状态，清理一局时很难判断哪些字段应该保留。

## 目标抽象

### `GameSceneCatalog`

只负责稳定的场景常量和战斗模式映射。

```csharp
public static class GameSceneCatalog
{
    public const string Login = "LoadScene";
    public const string Register = "RegiserScene";
    public const string Lobby = "LobbyPanel";
    public const string DantiaoBattle = "dantiao_map";
    public const string PaiweiBattle = "paiwei_map";

    public static string GetBattleScene(GameModes mode);
    public static bool IsBattleScene(string sceneName);
}
```

职责：

- 保证 `paiwei_solo` 和 `paiwei` 都进入 `paiwei_map`。
- 统一大厅、登录和战斗场景名称。
- 给 `SceneMgr`、`AoyiNetworkRoomManager`、`AoyiNetworkManager` 和在线连接流程提供同一来源。

不负责：

- 实际调用 `SceneManager.LoadScene`。
- 决定何时进入战斗或返回大厅。
- 保存当前会话状态。

### `MatchSessionPhase`

定义一局游戏允许出现的阶段。

```csharp
public enum MatchSessionPhase
{
    Idle,
    Searching,
    Matched,
    Connecting,
    InRoom,
    InBattle,
    Ending
}
```

阶段变化必须由会话协调器驱动，UI、Transport 和 Battle Manager 不应直接修改阶段。

### `MatchSessionContext`

保存一局游戏自身的数据，不依赖 Unity 场景或 Mirror 静态状态。

```csharp
public sealed class MatchSessionContext
{
    public MatchSessionPhase Phase { get; }
    public int Generation { get; }
    public NetworkMode NetworkMode { get; }
    public GameModes GameMode { get; }
    public string RoomId { get; }
    public string Role { get; }

    public int BeginSearch(NetworkMode networkMode, GameModes gameMode);
    public bool TryMarkMatched(int generation, string roomId, string role);
    public bool TryBeginConnecting();
    public void MarkInRoom();
    public void MarkInBattle();
    public bool TryBeginEnding();
    public void Reset();
}
```

使用 generation 的目的：

- 每次新匹配都产生新的 generation。
- 异步结果只有 generation 与当前会话一致时才能落地。
- 取消、结束和重开匹配都会让上一代结果失效。
- 旧轮询和旧 Relay 连接不能清除新一轮状态。

### `MatchSessionCoordinator`

作为一局游戏生命周期的唯一协调入口。

```csharp
public sealed class MatchSessionCoordinator : MonoBehaviour
{
    public MatchSessionContext Context { get; }

    public HeroMatchStartResult StartMatch(GameModes mode, int heroId, int skinId);
    public void CancelMatch(GameModes mode);

    public bool MarkMatched(int generation, string roomId, string role);
    public void MarkConnecting();
    public void MarkInRoom();
    public void MarkInBattle();

    public bool RequestReturnToLobby();
    public Task FinalizeLobbyReturnAsync();
}
```

职责：

- 根据当前 `NetworkMode` 分发本地、LAN 或在线匹配入口。
- 维护 `MatchSessionContext`。
- 拒绝过期异步结果。
- 保证结束请求和大厅清理只执行一次。
- Host 使用 `ServerChangeScene` 让所有客户端一起进入大厅。
- 大厅加载完成后再停止 Host、Server 或 Client。
- 在线房间只由服务器或房主关闭一次。
- 清理本局 `RoomId`、Team、TargetEndpoint 和战斗快照。

不负责：

- 编码或解析网络消息。
- 处理英雄 UI 和动画。
- 直接实现 KCP 或 Relay Transport。
- 清除 Supabase 登录状态和玩家长期资料。

### `MatchRequestFactory`

把旧服务器匹配消息的构造从 `ChooseHeroPanel` 中移出。

```csharp
public static class MatchRequestFactory
{
    public static MsgMatchRequest BuildLocalMatchRequest(
        GameModes mode,
        int heroId,
        string userId);

    public static MsgExitRequest BuildExitRequest(
        GameModes mode,
        string userId);
}
```

它只负责构造消息，不负责发送、不读取 UI、不修改会话状态。

### `OnlineRoomLifecycleService`

只负责远程在线房间的关闭行为。

```csharp
public static class OnlineRoomLifecycleService
{
    public static Task<bool> CloseRoomAsync(string roomId);
}
```

使用规则：

- 只有 `NetworkMode.SupabaseOnline` 且当前拥有服务器权威时才调用。
- Guest 返回大厅时只清理本地连接，不再次关闭远程房间。
- 必须等待 API 返回或超时后再完成本次清理记录。
- 失败时保留结构化日志，不能让本地玩家永久卡在 Ending 状态。

### `IOnlineConnectionStrategy`

拆分 Dedicated Server 和 Player Hosted Relay 的连接差异。

```csharp
public interface IOnlineConnectionStrategy
{
    Task<bool> ConnectAsync(
        AoyiNetworkRoomManager manager,
        OnlineMatchResponse match,
        CancellationToken cancellationToken);
}
```

具体实现：

- `DedicatedServerConnectionStrategy`
  - 校验 Dedicated Host 和 UDP 端口。
  - 配置 `KcpTransport`。
  - 启动 Mirror Client。
- `PlayerHostedRelayConnectionStrategy`
  - 解析 Host 或 Guest 的 Relay 连接信息。
  - 配置 `EdgegapKcpTransport`。
  - Host 启动 `StartHost`。
  - Guest 执行可取消的延迟、轮询和有限重试。
- `OnlineRoomManagerFactory`
  - 复用或创建 `AoyiNetworkRoomManager`。
  - 根据连接策略附加正确 Transport。
  - 切换 Transport 前清理旧 Mirror 状态。

### `MirrorBattleFrameCoordinator`

只负责 Mirror 服务端的战斗帧状态。

```csharp
public sealed class MirrorBattleFrameCoordinator
{
    public void Reset(string roomId, int expectedPlayers);
    public MsgBase Handle(MsgBase message);
    public bool IsBattleMessage(string protoName);
}
```

从 `MirrorNetBridge` 中迁移：

- Ready 玩家集合。
- 预期玩家数量。
- Frame ID。
- 随机种子。
- 待处理操作队列。
- 帧历史。
- `MsgBattleReady`、`MsgPlayerOp`、`MsgBattleOver` 和 `MsgPlayerExit` 的服务端处理。

`MirrorNetBridge` 最终只保留：

- 旧消息和 Mirror 字节数据的编码/解码。
- 客户端发送和服务器广播。
- 把房间消息交给房间处理器。
- 把战斗消息交给 `MirrorBattleFrameCoordinator`。

## 职责边界

| 行为 | 唯一负责人 | 其他对象的职责 |
| --- | --- | --- |
| 模式映射到场景 | `GameSceneCatalog` | 调用映射结果 |
| 匹配阶段与 generation | `MatchSessionContext` | 只读或请求转换 |
| 开始/取消匹配 | `MatchSessionCoordinator` | UI 转发点击事件 |
| 在线匹配轮询 | `OnlineMatchManager` | 向协调器报告 matched/failed |
| Dedicated/Relay 连接 | `IOnlineConnectionStrategy` | 协调器保存阶段 |
| Mirror 场景同步 | `AoyiNetworkRoomManager` | 向协调器报告场景已变化 |
| 战斗结束判定 | `BattleManager` | 只请求结束会话 |
| 返回大厅与停止连接 | `MatchSessionCoordinator` | Manager 执行具体 Mirror API |
| 关闭远程房间 | `OnlineRoomLifecycleService` | 仅权威端调用 |
| 战斗帧聚合 | `MirrorBattleFrameCoordinator` | `MirrorNetBridge` 转发消息 |

## 推荐阶段

### 阶段 1：统一场景映射

- 新增 `GameSceneCatalog`。
- 将 `SceneMgr`、`OnlineConnectionLauncher`、`AoyiNetworkRoomManager`、`AoyiNetworkManager` 和 `BattleManager` 的场景名迁移到统一入口。
- 增加 `paiwei_solo -> paiwei_map` 回归测试。
- 不修改网络连接和战斗流程。

这一阶段风险最低，并直接修复在线单人排位可能加载错误场景的问题。

### 阶段 2：建立会话状态模型

- 新增 `MatchSessionPhase` 和 `MatchSessionContext`。
- 用 EditMode 测试覆盖 generation、过期结果、重复结束和 Reset。
- 暂时保留 `OnlineMatchManager` 内部现有标记，先让它同步报告到 Context。
- 不在这一阶段移除旧字段，避免一次性切换所有状态来源。

### 阶段 3：统一游戏结束和返回大厅

- 新增 `MatchSessionCoordinator`。
- `BattleManager` 不再直接停止 Mirror Client，也不再直接加载大厅。
- Host 通过 `ServerChangeScene` 同步所有客户端进入大厅。
- `AoyiNetworkRoomManager` 在大厅加载完成后通知协调器进行最终清理。
- 只有 Host/Server 调用 `OnlineRoomLifecycleService.CloseRoomAsync`。
- 清理结束后保留账号、Backend 和英雄偏好，只清除本局会话和战斗状态。

这一阶段完成后应重点验证：

- 房主和 Guest 都返回大厅。
- 系统中的房间已经关闭。
- 两端都可以再次开始匹配。

### 阶段 4：拆分在线连接策略

- 新增 `IOnlineConnectionStrategy`。
- 把 Dedicated Server 连接迁入独立策略。
- 把 Relay Host/Guest 连接迁入独立策略。
- 把 NetworkManager 与 Transport 创建迁入 `OnlineRoomManagerFactory`。
- 把同一个 `CancellationToken` 传入初始延迟、连接轮询、重试延迟和清理路径。
- 保留旧 `OnlineRelayConnector` 作为临时兼容入口，调用方迁移完成后再删除。

### 阶段 5：清理英雄选择入口

- 新增 `MatchRequestFactory`。
- `ChooseHeroPanel` 的开始按钮只调用 `MatchSessionCoordinator.StartMatch`。
- 取消按钮只调用 `MatchSessionCoordinator.CancelMatch`。
- 删除七个委托组成的 `HeroSelectionMatchController`。
- 删除 `ChooseHeroPanel.cs` 中 `#if false` 的旧 `OnlineMatchManager`。
- 保留英雄资源加载、皮肤选择和动画逻辑，不在本阶段改 UI 架构。

### 阶段 6：拆分 Mirror 战斗帧逻辑

- 新增 `MirrorBattleFrameCoordinator`。
- 保持所有消息名称、帧结构、Frame ID 和随机种子行为不变。
- 让原有 EditMode 帧同步测试直接覆盖新协调器。
- `MirrorNetBridge` 继续承担旧协议适配，不在本阶段删除兼容层。

### 阶段 7：后端接口拆分

完成并验证会话生命周期后，再执行 `docs/backend-abstraction-plan.md`：

- `IAuthProvider`
- `IPlayerProfileProvider`
- `IRoomDirectoryProvider`
- `ITransportConnector`

不建议把后端 Provider 拆分和 Mirror 会话重构放在同一个分支，否则登录失败、房间查询失败和网络结束失败会难以区分。

## 当前最小修复方向

在进入完整重构前，最安全的近期改动顺序是：

1. 用 `GameSceneCatalog` 修正所有 `paiwei_solo` 场景映射。
2. 让 `BattleManager` 只请求结束，不再自行停止 Client。
3. 让 Host 使用 `ServerChangeScene` 带所有 Guest 返回大厅。
4. 让远程房间只由 Host/Server 关闭一次。
5. 把房间关闭结果纳入最终清理日志。
6. 返回大厅后清除 `RoomId`、Team、TargetEndpoint 和战斗快照。
7. 验证同一进程可以再次打开匹配。

## 验证要求

### EditMode 测试

至少覆盖：

- `paiwei_solo` 映射到 `paiwei_map`。
- Session generation 拒绝旧结果。
- `TryBeginEnding` 只能成功一次。
- Reset 清除房间并使旧 generation 失效。
- Guest 不关闭远程房间。
- Host/Server 可以关闭远程房间。
- Dedicated 和 Relay 选择不同策略。
- 预取消 token 在创建 Mirror 状态前终止连接。
- 战斗 Ready 等待所有玩家。
- 帧包保持增量发送。

### 两端 Loopback 验证

连续运行至少三次 Host + Guest 测试，每次验证：

1. 两端进入同一战斗场景。
2. 战斗结束消息只触发一次结束流程。
3. Host 切换网络场景到大厅。
4. Guest 跟随 Host 返回大厅。
5. 大厅加载完成后两端停止 Mirror。
6. 远程房间只关闭一次。
7. 两端不重启进程即可再次匹配。

### Relay 取消验证

1. 进入匹配并等待 Guest 开始 Relay 重试。
2. 在连接成功前取消。
3. 等待超过一次连接超时时间。
4. 确认取消日志出现后不再产生新的 Relay 重试日志。
5. 重新匹配可以创建新的 generation 和新的连接任务。

### 编译和工作区验证

- Unity EditMode 测试失败数为 0。
- `dotnet build "aoyi team2.sln" --no-restore --nologo` 为 0 errors。
- `git diff --check` 不报告本次修改文件的格式问题。
- 不修改场景 YAML、Prefab、字体、贴图、Web 仓库或 TCP 服务端。
- 不清理或重置工作区中已有的无关修改。

## 不建议现在做的事

- 不引入完整 DI 容器。
- 不把所有 Manager 一次性改成接口。
- 不重写 Mirror 或旧消息协议。
- 不在同一阶段修改战斗数值和网络生命周期。
- 不用 Addressables、DOTS 或 Unity 6 升级解决当前状态所有权问题。
- 不先拆 `UIManager` 或 `OnlineMatchApiClient`；它们不是本轮房间结束问题的主要来源。
- 不直接删除 `PlayerBasicInfoMgr`，而是先迁移每局会话字段。

## 完成标准

满足以下条件后，匹配会话抽象才算完成：

1. 所有模式通过一个场景映射入口获取战斗场景。
2. 所有匹配开始和取消都进入同一个会话协调器。
3. 旧 generation 的异步结果不能修改新一局状态。
4. `BattleManager` 不直接停止 Mirror 或加载大厅。
5. Host 和 Guest 都在停止连接前进入大厅。
6. 在线远程房间只由权威端关闭一次。
7. Relay 的全部等待和重试都响应取消 token。
8. `ChooseHeroPanel.cs` 不再包含匹配 Manager 或禁用旧实现。
9. `MirrorNetBridge` 不再直接保存战斗帧聚合状态。
10. EditMode、编译、三轮 Loopback 和同进程二次匹配全部通过。

## 与其他规划文档的关系

- 本文是面向项目成员的架构规划，说明为什么拆、拆成什么、按什么阶段推进。
- `docs/superpowers/plans/2026-07-13-match-session-architecture-refactor.md` 是本文对应的逐任务实施清单，包含具体文件、测试、命令和提交边界。
- `docs/backend-abstraction-plan.md` 负责 Auth、Profile、Room Directory 和 Transport Provider 的接口拆分。
- `docs/workflow-automation-plan.md` 负责统一验证入口和 CI，不改变运行时架构。
