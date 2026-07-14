# 奥义项目开发工作流自动化规划

## 1. 文档目的

本文规划一套适用于当前项目的开发工作流，在不升级 Unity 6、也不依赖 Unity 官方 MCP 的前提下，提高代码修改、编译检查、自动测试、持续集成和 Unity 编辑器操作的可靠性。

规划覆盖三个代码域：

- Unity 客户端：项目根目录，版本为 `2022.3.61f1c1`。
- TCP 服务端：`Aoyi_TCPServer/`，目标框架为 .NET Framework 4.7.2。
- Web 服务：`Web/`，独立 Git 仓库，使用 Next.js、TypeScript 和 pnpm。

本方案的核心原则是：命令行验证是稳定基础，CI 复用相同命令，Unity MCP 或 Editor Bridge 只提高交互效率，不成为编译、测试或发布的必要条件。

## 2. 当前状态与约束

### 2.1 已具备的能力

- 本机已安装与项目一致的 Unity `2022.3.61f1c1`。
- Unity 项目已有 EditMode 测试程序集 `AoyiTeam2.EditModeTests`。
- Web 项目已提供 `lint`、`test` 和 `build` 命令。
- Unity 项目已经拆出部分叶子程序集，可进行基础 C# 编译检查。
- Git 可以用于识别修改范围和保护正在制作的场景、Prefab 与资源。

### 2.2 主要限制

- Unity 官方 MCP 要求 Unity 6，当前项目没有官方支持。
- `2022.3.61f1c1` 是特定发行版本，公共 Unity CI 容器不一定提供完全一致的镜像。
- Unity 同一个项目通常不能被编辑器和批处理测试进程同时打开。
- TCP 服务端的 `MySql.Data.dll` 当前引用本机绝对路径，不适合直接放入 CI。
- Web 目录是独立 Git 仓库，版本、CI 和提交边界需要与 Unity 根仓库分开处理。
- 项目路径 `D:\Desktop\game\aoyi team2` 含空格，所有脚本必须使用严格的路径引用；部分社区 Unity MCP 可能不兼容此路径。
- 场景、Prefab、材质和 `ProjectSettings` 等序列化文件容易产生大面积或难以审查的差异。

## 3. 目标与非目标

### 3.1 目标

1. 建立一个统一的本地验证入口，并提供快速、完整和按模块验证模式。
2. 让本地和 CI 使用相同的底层命令，减少“本地通过、CI 失败”。
3. 在 Pull Request 阶段快速发现 C# 编译、EditMode 测试、服务端构建和 Web 回归。
4. 提供受控的 Unity 编辑器自动化能力，支持读取状态、运行测试和有限的编辑器操作。
5. 为日志、测试报告和构建产物规定统一位置，便于人工或 AI 定位失败原因。
6. 保留 Unity 2022.3 的稳定开发线，不把升级 Unity 6 作为前置条件。

### 3.2 非目标

- 第一阶段不自动升级 Unity、Mirror、Edgegap 或其他核心包。
- 第一阶段不自动修改场景、Prefab、材质和 `ProjectSettings`。
- 不把社区 MCP 的可用性作为交付门槛。
- 不在没有独立测试项目的情况下宣称覆盖完整的网络联机、帧同步或真实客户端对战。
- 不在工作区存在无关修改时自动提交、清理或重置文件。

## 4. 推荐总体架构

工作流分为三层，上一层不依赖下一层：

```text
开发者 / AI
    |
    v
本地统一命令 scripts/verify.ps1
    |-- Unity 编译与 EditMode 测试
    |-- TCP 服务端还原与编译
    |-- Web lint / test / build
    |-- Git 修改范围检查
    `-- artifacts/ 日志与报告
    |
    v
GitHub Actions
    |-- GitHub 托管 Runner：Web 与通用静态检查
    `-- Windows 自托管 Runner：指定版本 Unity 与旧版服务端
    |
    v
Unity Editor Bridge / 社区 MCP
    |-- 查询编译、Play Mode 和 Console 状态
    |-- 运行测试、打开场景
    `-- 经确认后执行有写入风险的操作
```

各层职责：

- 本地命令层负责“可重复、可诊断、可独立运行”。
- CI 层负责“每次提交或定时重复执行同一套验证”。
- Editor Bridge 层负责“减少人工点击”，不能绕过本地命令和 CI。

## 5. 第一层：本地统一验证

### 5.1 计划目录

```text
scripts/
  verify.ps1                 # 统一入口
  modules/
    Verify-GitState.ps1      # 修改范围与敏感文件检查
    Verify-Unity.ps1         # Unity 编译、EditMode 测试和日志
    Verify-Server.ps1        # TCP 服务端还原与编译
    Verify-Web.ps1           # Web lint、测试和构建
    Write-Summary.ps1        # 汇总各步骤结果

artifacts/                   # 本地生成并加入 .gitignore
  summary.md
  unity/
    editor.log
    editmode-results.xml
  server/
    build.log
  web/
    lint.log
    test.log
    build.log
```

`artifacts/` 只保存运行产物，不进入版本控制。每次执行默认创建带时间戳的子目录或覆盖 `latest/`，同时保留最近若干次失败报告。

### 5.2 统一命令接口

建议入口：

```powershell
# 日常修改后，优先执行受影响模块的快速检查
.\scripts\verify.ps1 -Mode Fast

# 提交或交付前执行完整验证
.\scripts\verify.ps1 -Mode Full

# 只检查指定模块
.\scripts\verify.ps1 -Mode Full -Target Unity
.\scripts\verify.ps1 -Mode Full -Target Server
.\scripts\verify.ps1 -Mode Full -Target Web

# CI 使用，禁止交互并输出机器可读报告
.\scripts\verify.ps1 -Mode Full -CI
```

参数约定：

| 参数 | 值 | 行为 |
|---|---|---|
| `-Mode` | `Fast` | 运行 Git 状态检查、受影响模块的快速编译和单元测试 |
| `-Mode` | `Full` | 运行所有编译、测试和 Web 构建 |
| `-Target` | `All/Unity/Server/Web` | 限制验证范围，默认 `All` |
| `-CI` | 开关 | 禁止提示输入，输出 XML/日志，失败立即返回非零退出码 |
| `-ChangedOnly` | 开关 | 根据 Git diff 只运行受影响模块；交付前不能替代 `Full` |

### 5.3 Git 状态保护

脚本启动时记录工作区快照，但不自动 stash、reset、checkout 或 commit。检查规则如下：

- 显示已修改、未跟踪和暂存文件。
- 修改仅涉及 `.cs`、测试或脚本时，可以自动继续快速检查。
- 涉及 `.unity`、`.prefab`、`.asset`、`.mat`、`.controller`、`Packages/manifest.json` 或 `ProjectSettings/` 时，在本地给出醒目警告。
- 在 `-CI` 模式下发现运行验证后产生了新的受版本控制文件，直接失败。
- 自动化操作结束后再次比较 Git 状态；如果出现非预期修改，在总结中列出完整路径。
- 根仓库与 `Web/` 独立仓库分别检查，不能把两者当作一次原子提交。

### 5.4 Unity 验证

Unity 验证分为两种路径。

#### 编辑器未打开

使用 Unity 批处理模式运行 EditMode 测试：

```powershell
& $UnityExe `
  -batchmode `
  -nographics `
  -projectPath $ProjectRoot `
  -runTests `
  -testPlatform EditMode `
  -testResults $TestResults `
  -logFile $EditorLog
```

脚本必须：

- 从 `ProjectSettings/ProjectVersion.txt` 读取版本，而不是写死当前版本。
- 优先在 Unity Hub 标准目录中解析对应 `Unity.exe`，找不到时接受显式参数或环境变量。
- 在执行前确认当前项目没有被另一 Unity 进程锁定。
- 检查 Unity 进程退出码、XML 测试结果和日志中的编译错误，不能只依赖其中一项。
- 设置合理的超时，并在超时后保留完整日志。
- 第一阶段只跑 EditMode；PlayMode 和真实联机冒烟测试在稳定后单独增加。

#### 编辑器已打开

批处理进程不应抢占同一个项目。此时有三个选择，按优先级处理：

1. 如果 Editor Bridge 可用，通过已打开的编辑器触发测试并等待结果。
2. 如果只是快速 C# 检查，运行生成的解决方案编译并明确标注“未经过 Unity 导入和序列化验证”。
3. 完整验证要求用户关闭编辑器后重试。

生成的 `.sln/.csproj` 编译只能作为快速反馈，不能替代 Unity 自身编译，因为包解析、条件编译、资源导入和序列化问题仍需 Unity 验证。

### 5.5 TCP 服务端验证

当前服务端是旧式 `.csproj`，目标为 .NET Framework 4.7.2。正式接入自动化前需完成一个小型可移植性改造：

1. 将 `MySql.Data` 从 `C:\Program Files (x86)\...` 的绝对引用改为仓库可还原的 NuGet 包引用。
2. 固定 `Newtonsoft.Json` 与 `MySql.Data` 版本。
3. 明确使用 Visual Studio Build Tools/MSBuild，或者迁移到 SDK-style 项目后使用 `dotnet build`。
4. 为无数据库依赖的协议编解码和匹配逻辑建立测试项目。

第一阶段验证命令建议为：

```powershell
nuget restore .\Aoyi_TCPServer\Aoyi_TCPServer.sln
msbuild .\Aoyi_TCPServer\Aoyi_TCPServer.sln /m /p:Configuration=Release
```

验收时禁止依赖某台开发机的全局 DLL 或手工复制文件。

### 5.6 Web 验证

Web 使用自己的工作目录和 Git 仓库：

```powershell
pnpm --dir .\Web install --frozen-lockfile
pnpm --dir .\Web lint
pnpm --dir .\Web test
pnpm --dir .\Web build
```

本地 `Fast` 模式可以跳过 `install` 和 `build`，前提是依赖已安装且 lockfile 未变化；`Full` 与 `CI` 模式必须使用冻结 lockfile，并运行完整构建。

`.env` 不得上传。测试所需变量放在 `.env.example` 中说明，并在 CI 使用 GitHub Secrets 或测试专用非敏感值。

### 5.7 失败汇总

无论在哪一步失败，都生成 `artifacts/summary.md`，至少包含：

- 开始时间、结束时间和总耗时。
- Git commit、Unity 版本、Node/pnpm/MSBuild 版本。
- 每个模块的成功、失败、跳过状态。
- 失败命令、退出码和对应日志路径。
- Unity 编译错误与失败测试名称摘要。
- 验证前后 Git 状态差异。

脚本最终退出码必须可靠：全部通过为 `0`，任意必需步骤失败为非 `0`。

## 6. 第二层：GitHub CI

### 6.1 CI 分工

建议按运行环境拆分，不建立一个巨大的串行工作流。

| 工作流 | Runner | 触发条件 | 主要任务 |
|---|---|---|---|
| Web CI | GitHub 托管 Linux | Web 仓库 PR/push | pnpm install、lint、test、build |
| Unity 快速检查 | Windows 自托管 | Unity 根仓库 PR/push | Unity EditMode 测试、日志上传 |
| Server CI | Windows 自托管或托管 Windows | 服务端相关改动 | NuGet restore、MSBuild、服务端测试 |
| Nightly Full | Windows 自托管 | 每晚、手动 | Unity 全量导入、全部测试、可选构建 |

Web 是独立仓库，因此它应拥有自己的 `.github/workflows/`。Unity 根仓库的 CI 不应假定 Web 改动与 Unity 提交始终同步。

### 6.2 为什么 Unity 使用 Windows 自托管 Runner

- 能安装精确的 `2022.3.61f1c1`，避免公共镜像版本不一致。
- 可以复用已有 Unity 授权方式和 Windows Build Support。
- 能执行依赖 Windows/.NET Framework 的服务端构建。
- 对大型 `Library` 缓存更可控。

代价是需要维护一台构建机。构建机应使用专用账户和最小权限，不能存放个人开发环境的长期凭据。

### 6.3 PR、主分支与夜间策略

#### Pull Request

- 根据路径过滤启动 Unity、Server 或 Web 对应任务。
- 必跑快速编译、EditMode 测试、服务端测试、Web lint/test。
- 目标反馈时间控制在 10–15 分钟内。
- 上传失败日志和 NUnit XML，保留至少 7 天。

#### 主分支

- 运行模块完整验证。
- Web 额外运行生产构建。
- Unity 在关键包、场景或 `ProjectSettings` 变化时执行全量检查。

#### 夜间或手动

- 清理缓存后重新导入项目，发现被缓存掩盖的问题。
- 运行所有 EditMode 测试和未来的 PlayMode/联机冒烟测试。
- 可选生成开发版本，但不自动发布正式版本。

### 6.4 缓存与并发

- Unity `Library` 缓存键包含 Unity 版本、目标平台、`Packages/manifest.json` 和 `Packages/packages-lock.json` 哈希。
- pnpm 缓存键包含 `pnpm-lock.yaml` 哈希。
- NuGet 缓存键包含 `packages.config` 或项目文件哈希。
- 同一 Unity 项目目录同一时间只允许一个作业访问。
- PR 新提交应取消该 PR 的旧作业，避免浪费自托管 Runner。
- 缓存恢复失败不能阻止构建，只会增加耗时。

### 6.5 凭据与许可证

- Unity 许可证、数据库连接、Edgegap、Supabase 和部署密钥只能保存在 CI Secret 中。
- Fork PR 默认不能访问敏感 Secret，也不能执行可接触内网的自托管作业。
- 测试不得连接生产数据库、生产 Relay 或正式用户数据。
- 日志输出前应屏蔽 token、Authorization header、连接字符串和用户隐私数据。

## 7. 第三层：Unity 编辑器自动控制

### 7.1 推荐方式

建立一个最小化的 Unity Editor Bridge，再决定使用社区 MCP、命令行客户端或其他工具调用它。Bridge 只处理 Unity 编辑器内部必须完成的操作，不承载通用文件编辑和 Git 操作。

推荐顺序：

1. 先实现稳定的本地验证脚本。
2. 再做只读 Bridge：状态、Console、测试结果。
3. 再开放低风险操作：刷新资源、运行测试、Play/Stop、打开指定场景。
4. 最后评估是否接入社区 MCP 协议适配层。

这样即使社区 MCP 升级失败，验证和 CI 仍然可用。

### 7.2 Bridge 最小能力集

#### 只读能力

- `get_editor_state`：Unity 版本、项目路径、是否编译、是否 Play Mode。
- `get_console_logs`：按级别、数量和时间读取 Console。
- `get_compilation_status`：编译成功、失败和错误列表。
- `get_test_status`：最近一次测试运行状态和报告位置。
- `get_active_scene`：当前场景路径及是否存在未保存修改。

#### 低风险操作

- `refresh_assets`：调用资源刷新。
- `run_editmode_tests`：运行指定程序集、命名空间或测试名称。
- `enter_play_mode` / `exit_play_mode`。
- `open_scene`：仅允许打开 `Assets/` 下的已知场景；存在未保存修改时拒绝切换。
- `capture_game_view`：保存截图到 `artifacts/unity/`。

#### 高风险操作

以下能力默认关闭，只有用户显式确认后才能执行：

- 保存或批量修改场景、Prefab 和 ScriptableObject。
- 修改 `ProjectSettings`、Packages 或 Build Settings。
- 删除、移动或重命名资源。
- 发起正式构建、上传或发布。
- 执行任意 C#、Shell 或反射调用。

Bridge 不应提供“执行任意编辑器代码”接口，否则权限边界等同于完全控制开发机。

### 7.3 通信与状态模型

可选通信方式：

- 本机 HTTP，仅监听 `127.0.0.1`。
- Named Pipe，适合只支持 Windows 的环境。
- 文件队列，最容易实现，但交互延迟和并发处理较弱。

推荐本机 HTTP，并满足以下要求：

- 使用随机会话 token，启动时生成，退出 Unity 后失效。
- 仅绑定回环地址，不向局域网暴露。
- 每条命令包含唯一 ID，可查询 `queued/running/succeeded/failed/cancelled`。
- Unity 主线程执行 Editor API；耗时任务异步返回状态，避免请求超时。
- 同一时间只执行一个会改变编辑器状态的命令。
- 所有请求和结果写入不含敏感数据的审计日志。

### 7.4 社区 MCP 接入标准

评估社区 Unity MCP 时必须验证：

- 明确支持 Unity 2022.3。
- 支持带空格和非 ASCII 字符的 Windows 路径，或项目可安全迁移到短英文路径。
- 不要求安装未知二进制或执行任意远程脚本。
- 能限定工具权限，并关闭资源删除、任意代码执行等危险能力。
- 能稳定读取 Console、等待编译结束并运行 Test Runner。
- 停止或断线后不会让 Unity 留在 Play Mode、锁住资源或持续占用端口。

社区 MCP 如果不满足这些标准，只作为实验工具，不进入正式工作流。

## 8. 建议的日常工作流程

### 8.1 开始任务

1. 分别检查 Unity 根仓库和 Web 仓库状态。
2. 明确本次允许修改的模块和文件范围。
3. 已有无关修改时保留它们，不自动清理或覆盖。
4. 对高风险场景或 Prefab 修改，先保留可审查的基线。

### 8.2 开发循环

1. 修改范围尽量保持小且职责单一。
2. 为逻辑缺陷先增加或更新 EditMode/单元测试。
3. 运行对应模块的 `Fast` 验证。
4. 失败时优先读取统一摘要和对应日志，而不是盲目重试。
5. Unity 编辑器操作可由 Bridge 辅助，但结果仍由测试和 Git diff 验证。

### 8.3 提交前

1. 运行 `verify.ps1 -Mode Full`。
2. 检查 Unity Console 无新增错误。
3. 审查场景、Prefab、`Packages` 和 `ProjectSettings` 差异。
4. 确认验证过程没有产生非预期文件。
5. Unity 和 Web 分别提交到各自仓库，不混淆提交历史。

### 8.4 合并前

1. 所有必需 CI 任务通过。
2. 网络、登录、匹配或场景切换变化需要人工或自动冒烟测试。
3. 失败报告必须可从 CI artifact 复现和定位。

## 9. 分阶段实施计划

### 阶段 0：建立基线

工作内容：

- 记录 Unity、Visual Studio Build Tools、Node 和 pnpm 版本。
- 确认现有 EditMode 测试在 Unity Test Runner 中可重复通过。
- 明确 Unity 根仓库和 Web 独立仓库的提交边界。
- 将生成的报告目录加入 `.gitignore`。

验收标准：同一台开发机连续运行两次基线验证，结果一致且不产生非预期 Git 修改。

### 阶段 1：本地一键验证

工作内容：

- 创建 `scripts/verify.ps1` 和模块脚本。
- 接入 Unity EditMode、Web lint/test/build。
- 修复 TCP 服务端包引用后接入 MSBuild。
- 生成统一摘要、XML 和日志。

验收标准：全新 PowerShell 会话中执行一条命令即可完成完整验证；任意模块故意制造错误时，脚本返回非零退出码并指出日志位置。

### 阶段 2：GitHub CI

工作内容：

- Web 仓库先启用 GitHub 托管 Runner。
- 准备专用 Windows 自托管 Runner 和精确 Unity 版本。
- Unity 根仓库接入 EditMode 测试和服务端构建。
- 配置路径过滤、并发取消、缓存和 artifact。

验收标准：PR 自动运行受影响任务；失败测试名称和日志可在 GitHub 页面下载；Fork PR 无法读取敏感 Secret。

### 阶段 3：Editor Bridge

工作内容：

- 实现只读状态和 Console 接口。
- 接入 EditMode 测试、Play/Stop 和安全场景打开。
- 增加会话 token、审计日志、超时和取消。
- 评估社区 MCP 作为协议适配层。

验收标准：能够从外部查询编译状态、运行指定测试并获得结构化结果；Bridge 断开后 Unity 状态可恢复；未经确认不能写场景或设置。

### 阶段 4：本地优先的分层网络测试与构建

工作内容：

- 安装 Unity Code Coverage，建立核心战斗和网络模块覆盖率基线。
- 自动启动两个本地 Windows Development Build，覆盖 KCP 回环、RoomPlayer、场景切换和退出清理。
- 使用 Mirror `LatencySimulation` 固化延迟、抖动、丢包和乱序测试档位。
- 使用本地假匹配 API 覆盖等待、匹配、取消、超时和 Relay 参数校验，不自行模拟 Edgegap UDP 协议。
- 建立独立测试环境和测试账号，最后执行真实 LAN 与 Edgegap Relay 冒烟。
- 增加开发构建与构建产物校验。

验收标准：大多数回归不依赖云服务；测试不访问生产数据；失败能区分纯逻辑、回环、弱网、Web API、LAN 或 Relay；构建产物带版本和 commit 信息。详细门槛见 [本地优先网络测试规划](local-network-test-plan.md)。

## 10. 优先级与预估工作量

| 优先级 | 工作项 | 预估 | 价值 |
|---|---|---:|---|
| P0 | 本地验证入口与日志汇总 | 1–2 天 | 立即提升每次修改的反馈速度 |
| P0 | 修复服务端绝对 DLL 引用 | 0.5–1 天 | 让服务端可在其他机器和 CI 构建 |
| P0 | 稳定 Unity EditMode 批处理 | 0.5–1 天 | 建立 Unity 自动验证核心 |
| P1 | Web 独立 CI | 0.5 天 | 成本低、收益快 |
| P1 | Windows 自托管 Runner | 1–2 天 | 支持精确 Unity 和旧服务端环境 |
| P1 | Unity 根仓库 CI | 1 天 | 自动拦截客户端回归 |
| P2 | Editor Bridge 只读能力 | 1–2 天 | 减少查看 Console 和测试的人工操作 |
| P2 | Editor Bridge 操作能力 | 2–4 天 | 自动 Play Mode、场景和截图流程 |
| P1 | Unity Code Coverage 基线 | 0.5–1 天 | 量化核心逻辑和网络状态机的自动测试缺口 |
| P2 | 本机双进程回环协调器 | 2–4 天 | 让主要联机流程无需第二台设备即可回归 |
| P2 | Mirror 弱网矩阵 | 1–2 天 | 本地覆盖延迟、抖动、丢包和乱序风险 |
| P2 | 本地假匹配 API 契约 | 1–3 天 | 无云依赖覆盖匹配、取消和 Relay 参数错误 |
| P3 | 真实 LAN 与 Edgegap 冒烟 | 2–4 天 | 覆盖广播、防火墙、跨 NAT 和真实 Relay 协议 |

预估不包含 Unity 6 升级、核心包迁移或大规模重构。

## 11. 风险与应对

| 风险 | 应对措施 |
|---|---|
| Unity 批处理与已打开编辑器冲突 | 检查项目锁；优先走 Bridge；完整验证要求关闭编辑器 |
| 自托管 Runner 被污染 | 使用专用账户、固定工具版本、定期重建环境 |
| `Library` 缓存掩盖错误 | 夜间任务定期无缓存全量导入 |
| 服务端只能在开发机编译 | 改为可还原 NuGet 依赖，并在干净环境验证 |
| 社区 MCP 不支持当前版本或路径 | 核心流程不依赖 MCP；先做兼容性试验 |
| 自动化误写 Unity 资源 | 高风险接口默认关闭；操作前后比较 Git 状态 |
| 日志泄露 token 或用户信息 | 日志脱敏；测试环境使用短期、最小权限凭据 |
| CI 时间过长 | PR 路径过滤和 Fast 模式，完整任务放夜间执行 |

## 12. 最终验收标准

完成本规划后，应满足：

1. 开发者和 AI 使用同一条命令完成本地验证。
2. Unity、服务端和 Web 的失败都有明确退出码和可定位日志。
3. PR 能按改动范围自动运行对应验证。
4. CI 使用与项目一致的 Unity 版本，不依赖未声明的本机 DLL。
5. Unity 编辑器自动控制断开时，不影响命令行验证和 CI。
6. 自动化默认不修改或提交场景、Prefab、设置和无关工作区文件。
7. 网络功能的关键修复至少有 EditMode 测试和覆盖报告，并按风险补充回环、弱网、假 API、真实 LAN 与 Relay 测试。

## 13. 推荐的下一步

按以下顺序落地，避免一次引入过多变量：

1. 实现阶段 0 和阶段 1：本地统一验证。
2. 同时修复 TCP 服务端依赖的可移植性。
3. 先为 Web 启用 CI，再部署 Unity Windows 自托管 Runner。
4. 本地与 CI 稳定后，再实现最小 Editor Bridge。
5. 按 Coverage、回环双进程、弱网、假 API、真实 LAN/Relay 的顺序建设网络测试。

第一项具体实施任务应是创建 `scripts/verify.ps1` 的接口与日志规范，并先接入现有 Unity EditMode 测试和 Web 测试；它能最快形成可重复的验证闭环。
