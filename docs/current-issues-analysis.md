# 当前问题分析（2026-07-05）

本文基于当前工作区的实际文件状态、构建设置、包配置和脚本静态扫描整理。结论偏向“接下来最容易影响构建、联机、发布和维护”的问题。

## 快速结论

当前项目最大的问题不是单个脚本，而是工程状态和运行链路仍在迁移中：

- Git 工作区极不干净：当前约有 `1181` 个删除、`58` 个修改、`68` 个未跟踪条目，且不少核心网络/Mirror/Supabase 文件仍未跟踪。
- `opt/` 构建输出约 `668 MB`，当前未被 `.gitignore` 忽略，发布前有误上传风险。
- 联机链路同时存在旧 TCP/UDP、Mirror、Supabase、LAN beacon，多套状态源并存，容易出现“房主/客机状态不一致”。
- 构建场景和脚本加载方式还有脆弱点：部分脚本按场景下标加载，Build Settings 顺序一变就可能跳错。
- 安全侧仍有风险：账号密码、Supabase access/refresh token 写入 `PlayerPrefs`。
- 自动化验证不足：Unity Test Framework 已在包里，但当前命令行测试没有生成结果 XML，项目还缺稳定的构建/联机回归检查。

## P0：必须先处理

### 1. Git 工作区不可发布

证据：

- `git status --porcelain` 统计：`D 1181`、`M 58`、`?? 68`，总计约 `1307` 条状态。
- 未跟踪项包括：
  - `Assets/Mirror/`
  - `Assets/Resources/MirrorPrefabs/`
  - `Assets/Resources/SupabaseConfig.asset`
  - `Assets/正式开发项目制作/开发脚本/Mirror/`
  - `Assets/正式开发项目制作/开发脚本/NetWorkScripts/Supabase/`
  - `Assets/正式开发项目制作/开发脚本/NetWorkScripts/LanDiscovery/LanQuickMatchManager.cs`
  - `opt/`

影响：

- 很难判断哪些是本次要发布的代码，哪些是本地试验。
- 回滚、合并、构建复现都会变得危险。
- CI 或其他机器拉代码后可能缺少关键网络文件。

建议：

1. 先把 `opt/`、本地网页原型、临时转换文件明确移动到不上传目录或加入 `.gitignore`。
2. 把 Mirror/Supabase/LAN 相关核心代码纳入一次明确提交。
3. 对 1181 个删除项做一次“保留/废弃/误删”确认，避免误删可运行资源。

### 2. `opt/` 构建产物未忽略

证据：

- 当前 `opt/` 约 `668.22 MB / 288 files`。
- `.gitignore` 已忽略 `/Build/`、`/Builds/`，但没有忽略 `/opt/`。

影响：

- 极容易把本地构建包上传到仓库。
- 仓库体积会暴涨，也会污染代码审查。

建议：

- 如果 `opt/` 只是本地构建目录，加入 `.gitignore`：

```gitignore
/opt/
```

- 如果要保留构建产物，也建议移动到 `_AbandonedLocal/`、`Builds/` 或专门的 release 输出目录。

### 3. 联机状态源太多，仍容易分裂

证据：

- 旧链路仍存在：`NetWorkMgr`、`UDPSocketManager`、TCP login、UDP battle。
- 新链路存在：`AoyiNetworkRoomManager`、`AoyiRoomPlayer`、`LanQuickMatchManager`、Mirror room flow。
- Supabase 链路存在：`SupabaseBackendProvider`、`SupabaseConfig.asset`。
- 等待页之前读 `MirrorNetBridge.ServerPlayers`，而 Mirror 房间实际人数在 `roomSlots`。

影响：

- 容易出现房主、客机、UI、服务器各自认为的“玩家数/ready/房间状态”不一致。
- 构建版问题比编辑器更难复现，因为时序和进程环境不同。

建议：

1. 明确一个权威房间状态源：LAN/Mirror 模式下以 `AoyiNetworkRoomManager.roomSlots` 为准。
2. 旧 TCP/UDP 只保留战斗同步必要部分，登录/房间状态不要再双写。
3. 给开战流程加日志流水号：创建房间、加入房间、RoomPlayer 创建、ready、ServerChangeScene、ClientSceneChanged。

## P1：近期应处理

### 4. 场景加载依赖下标

证据：

- `Assets/正式开发项目制作/开发脚本/NetWorkScripts/LoginPage/LoginPanel.cs`：`SceneNumber = 2`
- `Assets/正式开发项目制作/开发脚本/NetWorkScripts/LoginPage/UploadNamePanel.cs`：`SceneNumber = 2`
- 旧脚本中还有 `SceneManager.LoadScene(0)`。
- Build Settings 当前顺序为：
  - `LoadScene`
  - `RegiserScene`
  - `LobbyPanel`
  - `paiwei_map`
  - `dantiao_map`

影响：

- 一旦 Build Settings 改顺序，登录后可能跳错场景。
- 禁用场景会让编辑器和构建版行为不同。

建议：

- 用场景名常量替代下标，例如 `SceneNames.LobbyPanel`。
- 把 `RegiserScene` 的拼写问题纳入后续整理，至少文档里标明这是现有场景名，不要误改。

### 5. 构建场景列表和玩法入口不完全一致

证据：

- `CharacterChoose.unity` 当前在 Build Settings 中 disabled。
- `Monster_Map.unity` 当前 disabled。
- `paiwei_map` 和 `dantiao_map` enabled。

影响：

- 编辑器里能打开/跳转的流程，构建版可能加载失败。
- 后续玩法入口增加后容易遗漏 Build Settings。

建议：

- 建一份 `SceneNames` + Build Settings 校验脚本。
- 构建前校验所有被代码加载的场景都已启用。

### 6. 敏感登录信息写入 `PlayerPrefs`

证据：

- `LoginPanel.cs` 保存 `LoginAccount`、`LoginPassword`。
- `SupabaseBackendProvider.cs` 保存 access token、refresh token、user id、user name。

影响：

- `PlayerPrefs` 不是安全存储，容易被本机读取或篡改。
- refresh token 泄露后风险更高。

建议：

- 不保存明文密码；“记住密码”改为记住账号或短期 session。
- token 至少要加过期校验、登出清理和异常刷新处理。
- 后续可按平台接入系统安全存储。

### 7. 异步入口多为 `async void`

证据：

- `LoginPanel.OnLoginBtnClick`
- `RegisterPanel.OnClickFinRegBtn`
- `LanQuickMatchManager.JoinRoom`
- `LanQuickMatchManager.CreateHostRoom`
- `LobbyNetworkBridge.CreateSupabaseRoom`
- `LobbyNetworkBridge.JoinSupabaseRoom`

影响：

- 异常容易变成未观察异常。
- 调用方无法取消、等待或测试。
- 用户快速重复点击时容易进入双重状态。

建议：

- UI 事件层保留 `async void`，内部立即转 `Task` 方法。
- 增加按钮防重复点击、CancellationToken、超时和统一错误面板。

## P2：中期优化

### 8. `DontDestroyOnLoad` 单例过多

证据：

- 项目脚本中 `DontDestroyOnLoad` 出现较多，涉及 UI、网络、战斗、错误面板、生命周期管理。

影响：

- 场景返回/重进时容易残留旧对象。
- host/client 重启匹配后容易出现旧连接、旧 UI、旧回调。

建议：

- 梳理一个启动根对象，其他管理器由根对象管理生命周期。
- 网络匹配结束、取消、断线、进入战斗时做统一清理。

### 9. `Resources` 仍是核心资源系统

证据：

- `Assets/Resources` 当前约 `8.94 MB / 146 files`。
- 代码中 `Resources.Load`/`Resources.LoadAll` 仍然存在。

影响：

- 构建包容易包含不需要的资源。
- 静态 GUID 扫描不能可靠判断资源是否未使用。

建议：

- 短期保留 Resources，但建立路径清单。
- 中期把大图、皮肤、模式图标迁移到 Addressables 或显式引用表。

### 10. 自动化验证不足

证据：

- `com.unity.test-framework` 已在 `Packages/manifest.json`。
- 当前命令行 `-runTests -testPlatform editmode` 没有生成 `TestResults-EditMode.xml`。
- 刚新增的联机人数门槛测试文件可编译，但命令行 Test Runner 未吐结果。

影响：

- 联机修复依赖人工双开测试。
- 构建版问题容易反复出现。

建议：

- 先修通 EditMode 测试发现机制。
- 加三类回归：
  - 房间未满不能开战。
  - 满员后 host/client 都收到场景切换。
  - 取消匹配后没有残留 NetworkManager/等待面板。

## 建议处理顺序

1. 先清 Git 状态：明确哪些文件要提交，哪些本地废弃。
2. 把 `opt/` 加入忽略或移走。
3. 验证刚修的 LAN/Mirror 双端进战斗流程。
4. 把场景下标加载改成场景名常量。
5. 处理 PlayerPrefs 明文密码和 token 生命周期。
6. 梳理网络状态源，减少旧 TCP/UDP 和 Mirror/Supabase 之间的双写。
7. 修通 Unity Test Runner，补联机回归测试。

## 本次扫描依据

- `ProjectSettings/EditorBuildSettings.asset`
- `Packages/manifest.json`
- `Packages/packages-lock.json`
- `.gitignore`
- `Assets/Resources/SupabaseConfig.asset`
- `Assets/正式开发项目制作/开发脚本`
- `Assets/脚本文件`
- `Assets/存储资源夹`
- 当前 `git status --porcelain`
