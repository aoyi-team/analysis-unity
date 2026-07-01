# aoyi team2 项目分析

## 1. 项目概览

`aoyi team2` 是一个 Unity 2D 多人对战项目，产品名配置为 `奥义Demo`，目标分辨率为 `1920x1080`。项目当前更像一个正在重构中的 Demo 工程：一边保留了早期散装脚本、活动脚本和大量素材，另一边已经在 `Assets/正式开发项目制作/开发脚本` 下搭建了更清晰的正式开发框架。

核心方向可以概括为：

- 登录、注册、加载、大厅、选角、匹配、战斗地图组成完整游戏流程。
- TCP 负责登录、注册、匹配、房间等系统消息。
- UDP 负责战斗准备、玩家操作、服务端帧包等实时战斗消息。
- 战斗逻辑使用定点数 `Fixed64` / `FixedVector2`，目标是降低帧同步中的浮点误差。
- 角色逻辑层和渲染层分离，逻辑帧更新位置，渲染帧做插值显示。
- 项目中引入了 Photon/PUN，但正式开发代码当前主要使用自定义 Socket 通信，不是 Photon 主链路。

## 2. 工程环境

- Unity 版本: `2022.3.61f1c1`
- 主要包:
  - `com.unity.feature.2d`
  - `com.unity.cinemachine`
  - `com.unity.textmeshpro`
  - `com.unity.ugui`
  - `com.unity.shadergraph`
  - `com.unity.nuget.newtonsoft-json`
  - `com.unity.test-framework`
- 主要第三方资源:
  - `Assets/Photon`: Photon Chat / Realtime / PUN 及示例
  - `Assets/Plugins/Demigiant/DOTween`: DOTween
  - `Assets/TextMesh Pro`: TextMesh Pro 资源和示例

## 3. 构建场景

`ProjectSettings/EditorBuildSettings.asset` 中启用的主场景如下：

| 场景 | 状态 | 作用推断 |
| --- | --- | --- |
| `Assets/Scenes/LoadScene.unity` | 启用 | 初始加载/登录入口 |
| `Assets/Scenes/RegiserScene.unity` | 启用 | 注册场景，文件名里 `Regiser` 可能是拼写错误 |
| `Assets/Scenes/LobbyPanel.unity` | 启用 | 大厅场景 |
| `Assets/Scenes/paiwei_map.unity` | 启用 | 排位模式地图 |
| `Assets/Scenes/dantiao_map.unity` | 启用 | 单挑模式地图 |
| `Assets/Scenes/CharacterChoose.unity` | 未启用 | 选角场景或旧选角入口 |
| `Assets/Scenes/Monster_Map.unity` | 未启用 | 魔王/怪物模式地图 |

项目内实际还包含 Photon、TextMesh Pro、字体包等大量示例场景，这些不是当前游戏主流程的一部分。

## 4. 目录结构

### 根目录

- `Assets`: Unity 资源、脚本、场景和第三方插件。
- `Packages`: Unity Package Manager 依赖。
- `ProjectSettings`: Unity 工程配置。
- `Library` / `Temp` / `obj` / `Logs` / `UserSettings`: 本地生成目录，不建议纳入版本主线。
- `.plastic`: Plastic SCM 元数据。

### 关键 Assets 目录

- `Assets/正式开发项目制作/开发脚本`: 当前最值得继续维护的正式代码区。
- `Assets/Resources`: 当前正式代码大量依赖的运行时资源目录。
- `Assets/Scenes`: 游戏主场景。
- `Assets/脚本文件`: 早期玩法、角色、按钮、场景物体、跳转等脚本。
- `Assets/存储资源夹`: 通用资源、旧 UI、魔王模式相关资源和脚本。
- `Assets/预制体存放`: 旧角色、技能、道具、特效等 Prefab。
- `Assets/排位相关场景物体`: 排位场景资源、人物动画帧、地图物体。
- `Assets/登录场景`、`Assets/注册界面`、`Assets/玩家大厅界面UI`: 旧 UI 和界面素材。

## 5. 正式开发代码结构

`Assets/正式开发项目制作/开发脚本` 当前约 79 个 C# 文件，结构如下：

| 模块 | 作用 |
| --- | --- |
| `NetWorkScripts` | TCP/UDP 网络、协议、登录注册、加载页、资源和 UI 管理 |
| `CharactersChosePages` | 选角、皮肤、模式配置、开始游戏按钮 |
| `LobbyScripts` | 大厅按钮和模式入口 |
| `Battle` | 战斗管理、玩家逻辑/表现、定点数、碰撞、技能、数据结构 |
| `PublicUse` | 公共加载动画 |
| `UI_Scripts` | UI 按钮动效 |
| `局内相关` | 局内 UI 和游戏流程控制 |

### 战斗模块拆分

- `Battle/Managers/BattleManager.cs`: 战斗总控，加载模式配置、初始化 UDP、碰撞、实体、场景、玩家、输入和相机。
- `Battle/Managers/PlayerManager.cs`: 根据服务端玩家数据创建玩家信息、逻辑对象、Prefab 和渲染对象。
- `Battle/BasicScript/BasePlayerLogic.cs`: 玩家逻辑基类，处理移动、动画状态更新。
- `Battle/BasicScript/BasePlayerView.cs`: 玩家渲染基类，做位置插值和 Animator 更新。
- `Battle/FixedMathBase`: 定点数实现。
- `Battle/FixedPhysics`: 自定义 2D 碰撞、四叉树、碰撞分发。
- `Battle/SOStruct`: 角色配置、技能配置 ScriptableObject。
- `Battle/FactoryScript/CharacterFactory.cs`: 根据 `Hero_{id}_Logic` 和 `Hero_{id}_View` 命名反射创建角色逻辑和表现类。

## 6. 主流程分析

### 启动、登录、进入大厅

1. `LoadScene` 作为加载/登录入口。
2. `LoginPanel` 通过 `Resources.Load` 加载登录面板 Prefab。
3. 点击登录后发送 `MsgLoginProf`。
4. 登录成功后把玩家 ID、名称写入 `PlayerBasicInfoMgr`。
5. 登录面板异步加载场景索引 `2`，对应构建列表里的 `LobbyPanel.unity`。

相关文件：

- `Assets/正式开发项目制作/开发脚本/NetWorkScripts/LoginPage/LoginPanel.cs`
- `Assets/正式开发项目制作/开发脚本/NetWorkScripts/Manager/PlayerBasicInfoMgr.cs`
- `Assets/正式开发项目制作/开发脚本/NetWorkScripts/Manager/UIManager.cs`

### 大厅、模式、选角

`PlayerBasicInfoMgr` 保存当前游戏模式、房间 ID、队伍 ID、玩家 ID、玩家名和英雄/皮肤选择缓存。英雄选择会通过 `PlayerPrefs` 保存上次选择。

模式配置通过 `Resources/ModeConfigs/{mode}_ModeConfig` 加载。当前 Resources 中看到 `dantiao_ModeConfig.asset`，排位模式配置类存在，但资源是否完整需要在 Unity 中确认。

相关文件：

- `Assets/正式开发项目制作/开发脚本/CharactersChosePages/ChooseHeroPanel.cs`
- `Assets/正式开发项目制作/开发脚本/CharactersChosePages/ModeConfig/ModeConfig.cs`
- `Assets/正式开发项目制作/开发脚本/LobbyScripts/BtnScripts`

### 战斗加载和帧同步

战斗初始化链路集中在 `BattleManager.Init` 和 `LoadAllManagers`：

1. 根据 `PlayerBasicInfoMgr.Instance.GameMode` 加载模式配置。
2. 初始化 UDP Socket，并把 UDP 消息回调绑定到 `BattleManager.HandleMessage`。
3. 初始化 `CollisionManager` 和地图/实体碰撞。
4. 注册 TCP 的 `MsgBattleReady` 监听。
5. 通过 `SceneMgr.LoadSceneByName(mode)` 加载 `{mode}_map` 场景。
6. `PlayerManager.Init` 根据服务端玩家列表创建玩家逻辑、玩家 Prefab 和玩家 View。
7. 初始化输入、相机。
8. 发送 `MsgBattleReady`，收到服务端确认后每 `ServerConfig.frameTime` 秒发送一次 `MsgPlayerOp`。
9. 服务端返回 `MsgFramePack` 后，客户端按帧调用 `PlayerManager.OnLogicFrameUpdate`，再更新渲染 View。

当前 `ServerConfig.frameTime = 0.05f`，也就是 20 帧/秒逻辑帧。

相关文件：

- `Assets/正式开发项目制作/开发脚本/Battle/Managers/BattleManager.cs`
- `Assets/正式开发项目制作/开发脚本/Battle/Managers/PlayerManager.cs`
- `Assets/正式开发项目制作/开发脚本/NetWorkScripts/Manager/UDPSocketManager.cs`
- `Assets/正式开发项目制作/开发脚本/Battle/BasicScript/BasePlayerLogic.cs`
- `Assets/正式开发项目制作/开发脚本/Battle/BasicScript/BasePlayerView.cs`

## 7. 资源组织

正式代码当前高度依赖 `Resources` 路径：

- 面板 Prefab: `Resources/LoginPanel.prefab`、`RegisterPanel.prefab`、`LoadAnimPanel.prefab` 等。
- 英雄配置: `Resources/HeroConfigs/Hero_101.asset`
- 英雄 Prefab: `Resources/HeroPrefabs/{HeroId}/{HeroId}.prefab`
- 模式配置: `Resources/ModeConfigs/{mode}_ModeConfig.asset`
- 选角 UI、皮肤海报、技能图标、头像、小地图等也在 `Resources` 下。

这套路径简单直接，适合 Demo 阶段快速跑通。但如果资源继续增长，建议逐步把运行时资源分成更明确的加载表或 Addressables，避免 `Resources` 目录过大导致包体和内存不可控。

## 8. 当前主要风险

### 高优先级

1. `ModeConfig.cachedSpawnPoints` 没有初始化。
   - 位置: `Assets/正式开发项目制作/开发脚本/CharactersChosePages/ModeConfig/ModeConfig.cs`
   - 证据: 字段声明后直接在 `OnEnable` / `OnValidate` 调用 `cachedSpawnPoints.Clear()`。
   - 影响: 运行时或编辑器刷新配置时可能空引用，进而导致玩家出生点读取失败。
   - 建议: 声明时初始化为 `new List<FixedVector2List>()`，并在使用前做空保护。

2. 运行时代码引用 `UnityEditor`。
   - 位置: `Assets/正式开发项目制作/开发脚本/Battle/Managers/BattleManager.cs`
   - 证据: 文件顶部存在 `using UnityEditor;`，但该文件不是 Editor-only 脚本。
   - 影响: 打包 Player 时可能编译失败。
   - 建议: 如果未使用，直接删除；如果需要编辑器功能，放入 `#if UNITY_EDITOR` 或 Editor 目录。

3. TCP 发送队列在最后一个包发送完成后可能抛异常。
   - 位置: `Assets/正式开发项目制作/开发脚本/NetWorkScripts/NetWorkMgr.cs`
   - 证据: `SendCallback` 中 `Dequeue()` 后立即 `writeQueue.First()`，队列为空时会抛 `InvalidOperationException`。
   - 影响: 网络发送完成时可能断掉回调线程，导致后续消息发送异常。
   - 建议: 出队后先检查 `writeQueue.Count > 0`，没有剩余消息时再根据 `IsClosing` 决定关闭 Socket。

4. TCP 连接失败后状态被错误保持为连接中。
   - 位置: `Assets/正式开发项目制作/开发脚本/NetWorkScripts/NetWorkMgr.cs`
   - 证据: `ConnectCallBack` 捕获异常后设置 `IsConnecting=!false`，实际等于 `true`。
   - 影响: 一次连接失败后可能无法再次连接。
   - 建议: 失败时设为 `false`，并触发连接失败事件。

5. Ping/Pong 监听注册条件疑似写反。
   - 位置: `Assets/正式开发项目制作/开发脚本/NetWorkScripts/NetWorkMgr.cs`
   - 证据: `if(msgListeners.ContainsKey("MsgPong")) AddMsgListener("MsgPong", OnMsgPong);`
   - 影响: 默认没有监听时不会注册，Ping 超时逻辑可能永远收不到 Pong 更新时间。
   - 建议: 初始化时无条件注册，或在不重复注册的前提下注册。

### 中优先级

1. `MsgBase.Decode` 使用 `Type.GetType(protoName)`。
   - 影响: 当消息类型不在默认命名空间、被程序集拆分或 IL2CPP 裁剪时可能解析失败。
   - 建议: 建立协议名到类型的显式注册表，或至少在启动时扫描并缓存。

2. `UDPSocketManager` 缺少包体长度和关闭状态的完整保护。
   - 影响: 异常 UDP 包或关闭过程中的 Socket 异常可能刷日志或影响接收线程退出。
   - 建议: 校验 `bodyLen`、`nameCount`、`bodyCount` 边界；关闭时唤醒/Join 线程。

3. `Hero_101_Logic` 和 `SceneObjInfo` 中仍有 `NotImplementedException`。
   - 影响: 如果后续把这些回调接入碰撞事件，会在运行时直接崩掉对应逻辑。
   - 建议: 未实现前改为空实现或明确日志，不要在核心回调里保留异常。

4. `BattleResourceManager.LoadSkillPrefab` 和 `LoadScenePrefab` 当前加载空路径。
   - 影响: 技能和场景实体资源系统还没有接通，后续功能容易出现空 Prefab。
   - 建议: 建立资源命名规则，例如 `SkillPrefabs/{skillId}`、`EntityPrefabs/{entityId}`。

5. 单例生命周期不完全一致。
   - 影响: `BattleManager`、`NetWorkMgr`、`UIManager`、`SceneMgr`、`InputManager`、`PlayerManager` 有的 `DontDestroyOnLoad`，有的没有；跨场景可能出现状态残留或对象丢失。
   - 建议: 明确全局管理器清单，统一初始化、销毁和战斗结束重置策略。

## 9. 代码健康度判断

当前项目不是“完全混乱”，它已经有清晰的正式开发框架雏形：

- 网络协议有统一的 `MsgBase` 编解码。
- 战斗使用定点数和逻辑/渲染分离，方向是对的。
- 资源路径、角色 ID、模式配置、英雄配置已有初步规范。
- 战斗管理器已经把加载顺序串起来。

但现在还处于“能搭起来，但需要补稳定性”的阶段。最需要优先处理的不是大规模重构，而是先修掉会直接导致空引用、构建失败、Socket 回调异常的几个点。等主流程稳定后，再整理旧资源和旧脚本。

## 10. 建议的下一步

1. 先修复高优先级 5 个稳定性问题。
2. 在 Unity 里按 `LoadScene -> 登录 -> 大厅 -> 选模式/选角 -> 匹配 -> dantiao_map/paiwei_map` 跑一遍主流程。
3. 给 `Assets/正式开发项目制作/开发脚本` 增加一个简单 README，说明正式代码入口和不要再往旧目录新增逻辑。
4. 把旧脚本目录按状态标记为 `legacy`、`candidate`、`unused`，不要一次性删除。
5. 把 `Resources` 下正式运行依赖整理成资源清单，确认每个路径都能被代码加载。
6. 等主流程稳定后，再考虑 Addressables、协议注册表、对象池、战斗重连/补帧等工程化增强。

## 11. 本次分析范围

本次分析基于文件系统和源码静态检查，没有启动 Unity Editor，也没有进行 Player 构建验证。由于项目不是 Git 仓库，本次无法提供 git diff 范围判断；新增内容仅限 `docs` 目录。
