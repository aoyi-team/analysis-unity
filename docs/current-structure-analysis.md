# 当前结构分析（2026-07-05）

本文基于当前工作区文件、Git 状态、Build Settings、Mirror/LAN 源码和最近两段运行日志整理。它不是长期规划，而是“现在项目处在什么状态、还有哪些结构风险、下一步先改哪里”的快照。

## 快速结论

项目已经从“散装资源和多套联机方案并存”进入“Mirror + LAN 主链路收束期”。Photon 主包已经退出 Git 主线，`Web/`、`opt/`、本地 Supabase 配置和本地废弃目录也已被忽略；当前真正悬着的是一组 Mirror/LAN 自动进战斗修复。

当前最重要的判断是：

1. Git 状态已经比旧问题文档描述的状态干净很多，当前只有 4 个 Mirror/LAN 修改和 1 个新分析文档。
2. 最近日志证明 Mirror 已能找到 2 个玩家、自动 ready、切到 `dantiao_map` 并初始化双方角色。
3. 最近日志也暴露 3 个运行时问题：重复 `ServerChangeScene`、客户端进入战斗后仍弹等待面板、客户端 `BattleContext.TeamId=0`。
4. 当前未提交源码已经针对这 3 个问题做了修复，但还需要一次双端构建验证确认它们真的消失。
5. 后续结构整理不应再围绕 Photon，而应围绕 Mirror 房间状态唯一化、旧 UDP 战斗边界、`Resources.Load` 路径清单和生日素材残留归档。

## 当前仓库状态

### Git 状态

最近本地提交：

```text
e2e2c78 Organize remaining project updates
8dd7296 Remove legacy Photon and birthday assets
bd7815d Add Mirror LAN multiplayer integration
```

当前未提交内容：

```text
M  Assets/Mirror/Components/NetworkRoomPlayer.cs
M  Assets/正式开发项目制作/开发脚本/Mirror/AoyiNetworkRoomManager.cs
M  Assets/正式开发项目制作/开发脚本/Mirror/AoyiRoomPlayer.cs
M  Assets/正式开发项目制作/开发脚本/NetWorkScripts/LanDiscovery/LanQuickMatchManager.cs
?? docs/current-structure-analysis.md
```

这 4 个代码修改属于同一组修复：Mirror Room ready、自动切场景、进入战斗后加载双方信息、避免等待面板残留。建议验证通过后单独提交，不要和资源整理混在一起。

### 忽略规则

当前 `.gitignore` 已覆盖 Unity 生成目录、IDE 文件、`Web/`、`opt/`、`--css-path`、本地 Supabase 配置和本地废弃目录：

```gitignore
/Web/
/opt/
/--css-path
Assets/Resources/SupabaseConfig.asset
Assets/Resources/SupabaseConfig.asset.meta
/_AbandonedLocal/
/AbandonedLocal/
/NotForGitHub/
/联盟/
```

这说明“web 添加到忽略、非敏感内容提交、本地废弃目录不上传”的 Git 目标基本已经落地。

### Unity 与构建场景

当前 Unity 版本：

```text
2022.3.61f1c1
```

当前 Build Settings：

| 场景 | 状态 | 结构含义 |
| --- | --- | --- |
| `Assets/Scenes/LoadScene.unity` | 启用 | 登录/初始加载入口 |
| `Assets/Scenes/RegiserScene.unity` | 启用 | 注册入口，场景名拼写沿用现状 |
| `Assets/Scenes/LobbyPanel.unity` | 启用 | 大厅、选角、LAN 匹配入口 |
| `Assets/Scenes/paiwei_map.unity` | 启用 | 排位/多人战斗地图 |
| `Assets/Scenes/dantiao_map.unity` | 启用 | 单挑战斗地图 |
| `Assets/Scenes/CharacterChoose.unity` | 未启用 | 旧选角或备用场景 |
| `Assets/Scenes/Monster_Map.unity` | 未启用 | 旧魔王/怪物模式地图 |

## 当前 Assets 分布

Git 跟踪文件数量最多的目录：

| 目录 | 跟踪文件数 | 判断 |
| --- | ---: | --- |
| `Assets/Mirror` | 2324 | 当前联机 vendor 包，短期不能随意拆 |
| `Assets/排位相关场景物体` | 1965 | 旧场景和玩法资源，仍需分层 |
| `Assets/存储资源夹` | 1056 | 旧资源、旧脚本和魔王模式混合区 |
| `Assets/正式开发项目制作` | 440 | 当前正式开发主线 |
| `Assets/动画文件存放文件夹` | 406 | 动画资源池，仍偏散装 |
| `Assets/TextMesh Pro` | 322 | 第三方 UI 字体资源 |
| `Assets/玩家大厅界面UI` | 306 | 旧 UI 素材区 |
| `Assets/脚本文件` | 197 | 旧角色、旧 UI、旧场景脚本 |
| `Assets/Resources` | 144 | 当前运行时动态加载核心资产 |
| `Assets/2025生日用素材` | 34 | 生日活动 prefab 和 meta 残留 |

`Assets/Photon` 当前 Git 跟踪数为 0，Photon 主包不再是当前结构主问题。`Assets/2025生日用素材` 仍有 34 个跟踪文件，主要是 `单独文字`、`预制体` 下的 prefab 和 `.meta`，建议后续做引用检查后整体移入 `_AbandonedLocal/` 或正式 archive 目录。

## 正式代码结构

当前正式代码主目录：

```text
Assets/正式开发项目制作/开发脚本
```

主要模块：

| 模块 | 当前职责 | 结构状态 |
| --- | --- | --- |
| `Mirror` | Mirror 房间、RoomPlayer、兼容桥接 | 当前联机主线，仍与旧桥接层并存 |
| `NetWorkScripts/LanDiscovery` | LAN 发现、加入、等待 UI、嵌入式旧服务端 | 当前最活跃，但职责过多 |
| `NetWorkScripts/Backend` | 本地/LAN/Supabase 后端抽象 | 方向正确，但还未完全替代旧 TCP 登录 |
| `NetWorkScripts/Supabase` | Supabase Auth/REST/配置 | 可作为在线后端，本地配置已忽略 |
| `NetWorkScripts/Manager` | UI、资源、场景、玩家信息、UDP 管理 | 旧全局单例集中地，耦合最高 |
| `CharactersChosePages` | 选角、模式配置、`GameLoadPanel` | 联机入口和战斗加载入口都在这里 |
| `Battle` | 战斗上下文、加载命令、管理器、定点数学、碰撞、技能 | 初始化链路已经开始变清晰 |
| `LobbyScripts` | 大厅按钮和网络入口 | 与选角/LAN 入口耦合 |

`Battle` 侧已经有比较好的骨架：`BattleContext`、`LoadContext`、`ILoadCommand`、`MacroCommand`、`LoadCommands/`。后续战斗初始化应继续沿这些命令扩展，不要再把战斗细节塞回 `LanQuickMatchManager` 或 `AoyiNetworkRoomManager`。

## Mirror/LAN 当前链路

当前 LAN 双人链路：

```text
ChooseHeroPanel
  -> LanQuickMatchManager.StartQuickMatch
  -> LanBeaconReceiver / LanBeaconSender
  -> AoyiNetworkRoomManager.StartHost / StartClient
  -> AoyiRoomPlayer SyncVar 同步 hero/name/team/ready
  -> AoyiNetworkRoomManager.ServerChangeScene
  -> GameLoadPanel.LoadGame
  -> BattleContext / MacroCommand / LoadCommands
  -> BattleManager / PlayerManager / UDPSocketManager
```

### 最近日志证明了什么

日志里已经出现这些关键成功信号：

```text
[AoyiNetworkRoomManager] 房间满员 (2/2)，自动准备所有玩家
[AoyiNetworkRoomManager] ReadyStatusChanged ready=2/2, required=2, shouldStart=True
[AoyiNetworkRoomManager] OnRoomServerPlayersReady, 切换到战斗场景: dantiao_map
[AoyiNetworkRoomManager] 战斗场景加载完成，玩家数=2
[PlayerManager] spawnPoints 队伍数=2, 玩家数=2
[PlayerManager] 找到本地玩家
```

这说明问题已经不是“Mirror 找不到两个玩家”，而是“两个玩家都找到后，切场景、身份同步、等待 UI、旧 UDP 战斗链路之间还有竞态和残留”。

### 日志暴露的问题与当前源码对应关系

| 日志症状 | 影响 | 当前源码是否已处理 |
| --- | --- | --- |
| `Scene change is already in progress for dantiao_map` | 重复触发 `ServerChangeScene` | 已加 `_battleSceneChangeStarted` 防重复，但需构建验证 |
| 客户端 `BattleContext captured ... TeamId=0` | 客户端战斗身份写入晚于 `LoadGame` | 已加 `ApplyLocalBattleIdentity()`，但需验证时序 |
| 客户端进入战斗后仍 `进入等待对手状态` | 等待 UI 残留遮住战斗 | `LanQuickMatchManager` 已在进入等待前判断当前是否战斗场景，但需验证 |
| `UDPSocketManager ConnectionReset` | 旧 UDP 战斗通道仍会异常 | 未根治，应单独处理旧 UDP 生命周期 |

### 当前最大结构风险：房间状态源重复

现在至少有三套状态源：

| 状态源 | 负责内容 | 风险 |
| --- | --- | --- |
| `AoyiNetworkRoomManager.roomSlots` / `AoyiRoomPlayer` | Mirror 房间玩家、ready、英雄、team | 应成为 LAN/Mirror 房间主状态源 |
| `MirrorNetBridge.ServerPlayers` / `ServerClients` | 旧 Mirror raw message 兼容层 | 容易和 `roomSlots` 分裂 |
| `NetWorkMgr` / `UDPSocketManager` / `PlayerBasicInfoMgr` | 旧 TCP/UDP、玩家 ID、战斗消息 | 战斗仍依赖，不能立即删除，但要收窄入口 |

建议规则：

- 房间人数、ready、开局条件：只读 `AoyiNetworkRoomManager.roomSlots`。
- 英雄选择、昵称、队伍：优先由 `AoyiRoomPlayer` SyncVar 同步。
- 战斗玩家列表：只由 `AoyiNetworkRoomManager.BuildAllPlayersFromRoomSlots()` 生成一次，再写入 `PlayerBasicInfoMgr.SetBattleAllPlayers()`。
- UDP 只负责战斗帧和操作消息，不再参与“是否开局”和“房间玩家是谁”。

## Resources 结构分析

`Assets/Resources` 当前 Git 跟踪文件数为 144。它不是最大体量问题，但仍是运行时强依赖目录。

主要内容：

| 子目录/文件 | 文件数 | 当前角色 |
| --- | ---: | --- |
| `Animations` | 32 | 英雄/UI 动画动态加载 |
| `UISprites` | 22 | 选角 UI 皮肤/模式图标 |
| `HeroAnimSprites` | 22 | 英雄 101 动画帧 |
| `Animator` | 10 | 英雄/UI Animator |
| `HeroPrefabs` | 7 | 英雄 prefab |
| `MapSprits` | 4 | 地图相关图片 |
| `MirrorPrefabs` | 4 | Mirror RoomPlayer/GamePlayer prefab |
| `ModeConfigs` | 2 | 模式配置 |
| `HeroConfigs` | 2 | 英雄配置 |
| `LoginPanel.prefab` / `RegisterPanel.prefab` / `GameLoadPanel.prefab` | 单文件 | UI 动态加载入口 |

当前明确的动态加载路径包括：

```text
SupabaseConfig
MirrorPrefabs/AoyiRoomPlayerPrefab
MirrorPrefabs/AoyiGamePlayerPrefab
UISprites/CharacterChoose/ModesIcon/ModesIcons
GameLoadPanel
LoginPanel
RegisterPanel
UploadNamePanel
LoadAnimPanel
```

因此下一轮资源整理不能只按“看起来没引用”移动文件，必须先建立 `Resources.Load` 路径清单。尤其 `MirrorPrefabs`、UI prefab、模式图标和 Supabase 配置相关路径，移动前必须同步改代码。

## 第三方和生成内容

### Mirror vendor 包

`Assets/Mirror` 现在是跟踪文件最多的目录，包含 Core、Components、Transports、Editor Weaver、Examples、Edgegap Hosting。短期不建议拆包，因为当前修复直接依赖 `NetworkRoomManager`、`NetworkRoomPlayer`、KCP transport 和 Weaver。

但当前有一个维护风险：`Assets/Mirror/Components/NetworkRoomPlayer.cs` 被本项目直接改了 `SetReadyToBegin()`。这能解决 Weaver 跨程序集改 SyncVar 的报错，但它属于 vendor patch。后续升级 Mirror 时，要把这处改动作为本地补丁记录下来，或改成更不侵入的继承/封装方案。

### 旧活动素材

`Assets/2025生日用素材` 已经不是大规模目录，但仍有 34 个 prefab/meta 跟踪项。它不属于当前 Mirror/LAN 主链路，建议后续按 GUID 引用扫描确认后移入本地废弃目录或正式 archive。

### 根目录和设计稿

`game-login-ui/` 更像网页 UI 设计稿/静态原型，不属于 Unity 运行链路。可以保留，但建议后续移到 `docs/design/` 或明确标注用途，避免根目录继续混入运行代码之外的资产。

## 当前问题分级

### P0：先验证并提交 Mirror/LAN 修复

1. 双端构建跑一次 `dantiao` LAN 匹配。
2. 确认日志不再出现 `Scene change is already in progress`。
3. 确认客户端 `BattleContext.TeamId` 不再是 0。
4. 确认进入 `dantiao_map` 后不会再创建或显示等待面板。
5. 验证通过后，把 4 个 Mirror/LAN 修改作为一个本地提交。

### P1：收窄联机边界

1. `LanQuickMatchManager` 拆分职责：房间发现、Mirror 连接、等待 UI 状态。
2. 给 `MirrorNetBridge` 标注 legacy/compat，避免新代码继续读它的玩家列表。
3. `PlayerBasicInfoMgr` 的 battleId/teamId 写入只保留一个权威入口，避免异步流程竞态覆盖。
4. 旧 UDP 只保留战斗帧同步，不再参与匹配/开局判断。

### P2：资源与目录整理

1. 建立 `Resources.Load` 路径清单，再决定下一批可移动资源。
2. 对 `Assets/2025生日用素材` 做 GUID 引用扫描，确认后整体归档。
3. 给 `Assets/存储资源夹`、`Assets/脚本文件`、`Assets/排位相关场景物体` 建 legacy 标签文档。
4. 暂时不做大规模目录重命名，先保证构建版运行稳定。

### P3：工程卫生

1. 把 `docs/current-issues-analysis.md` 标记为旧快照或重写，因为它已经不符合当前 Git 状态。
2. 将 `Unity编译优化总结.md` 移入 `docs/`，让根目录更干净。
3. 为 Mirror 开局条件、双端身份同步、取消匹配清理补 EditMode/PlayMode 回归。
4. 关闭 Unity Editor 后再跑测试，避免编辑器占用和 Test Runner 发现异常。

## 建议下一步顺序

1. 用当前未提交代码做一次双端构建验证。
2. 若验证通过，提交 Mirror/LAN 修复。
3. 更新或废弃 `docs/current-issues-analysis.md`，避免旧 Git 状态误导。
4. 重构 `LanQuickMatchManager`，把等待 UI 的显示条件从异步尾部移到统一状态机。
5. 处理 `UDPSocketManager ConnectionReset`，明确断线、战斗结束、取消匹配时的 socket 生命周期。
6. 建 `Resources.Load` 清单，再继续移动资源。

## 验证清单

每次提交前至少确认：

- `git status --short` 只包含本次预期文件。
- `Assets/Resources/SupabaseConfig.asset` 仍被 `.gitignore` 忽略。
- Build Settings 仍包含 `LoadScene`、`RegiserScene`、`LobbyPanel`、`paiwei_map`、`dantiao_map`。
- 双端 LAN 日志不出现：

```text
Scene change is already in progress
TeamId=0
进入等待对手状态
```

- 战斗初始化日志能看到双方玩家，并且本地玩家 ID 与 teamId 都正确。
- 若关闭 Unity Editor 后可运行，执行 EditMode 或 PlayMode 测试并保存结果日志。
