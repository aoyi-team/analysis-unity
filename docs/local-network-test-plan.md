# 本地优先网络测试规划

本文规划 `aoyi team2` 的分层网络测试：多数逻辑、双客户端流程和弱网回归在本地完成，真实局域网与 Edgegap Relay 只承担无法被本地替代的集成验证。当前重点是 Mirror 1v1，旧 `Aoyi_TCPServer`、5v5v5 容量测试和自建 Relay 不在本轮范围。

## 1. 测试目标

确认两个客户端在本机回环、真实局域网和真实 Relay 三种环境下能够稳定完成以下闭环：

```text
进入大厅
-> 选择 LAN 1v1 和英雄
-> 自动发现或创建房间
-> Mirror Host/Client 建立连接
-> 两名 RoomPlayer 就绪
-> 切换到 dantiao_map
-> 完成基础移动、技能、血量和胜负同步
-> 正常退出并释放端口
-> 再次匹配成功
```

发布前必须同时证明：正常路径可重复、失败时有明确日志、断开后不会留下错误网络状态。

日常开发目标是让不依赖真实云端的测试覆盖大多数回归；真实 Edgegap 测试用于验证 Relay 会话、鉴权和公网数据路径，不能被假服务完全替代。

## 2. 当前实现基线

| 项目 | 当前值或行为 |
| --- | --- |
| Unity | `2022.3.61f1c1` |
| 网络框架 | Mirror `NetworkRoomManager` |
| 游戏传输 | KCP，UDP |
| LAN 搜索超时 | `2.5` 秒；未发现房间的一端创建 Host |
| Beacon 周期 | `1.0` 秒 |
| Beacon 地址 | `255.255.255.255` 广播 |
| Beacon 端口 | UDP `889` |
| 游戏端口候选 | UDP `888-908`，从首个可用端口开始 |
| Beacon 携带的遗留 UDP 端点 | UDP `887-907`，不得与游戏端口重复；当前 Mirror 快速匹配不使用它建立连接 |
| 协议版本 | `1` |
| 1v1 战斗场景 | `dantiao_map` |
| 1v1 玩家数 | `2` |
| Unity Test Framework | 已安装 `1.1.33`，现有 EditMode 测试程序集为 `AoyiTeam2.EditModeTests` |
| Code Coverage | 尚未安装；Unity 2022.3 对应 released 版本为 `1.2.4` |
| Mirror 弱网模拟 | 已包含 `LatencySimulation`，但当前运行时创建的 NetworkManager 尚未接入测试配置 |
| Edgegap Transport | 已包含 `EdgegapKcpTransport`，在线模式已配置 Relay IP、双端口和 token |
| Fly.io | 当前无 `fly.toml` 或部署路径，不属于现行 Relay 测试架构 |

注意：代码中的 `TcpPort` 字段名称属于历史命名；当前 `LanQuickMatchManager` 将该端口交给 KCP，并按 UDP 检查和监听。Windows 防火墙的最低放行范围是 Beacon UDP `889` 与 Mirror KCP UDP `888-908`。

## 3. 范围

### 本轮必须覆盖

- EditMode 自动测试与 HTML 覆盖率报告。
- 本机两个独立进程的回环连接和重复对局。
- Mirror `LatencySimulation` 的延迟、抖动、丢包和乱序矩阵。
- 本地假匹配 API 的成功、取消、超时、非法 Relay 数据和 token 脱敏。
- 同机 Editor + Windows Player 冒烟测试。
- 同机两个 Windows Player 的端口选择和连接。
- 同一 Wi-Fi/有线局域网内两台 Windows 设备。
- 自动发现、Host 创建、Client 加入、RoomPlayer 就绪和场景切换。
- 1v1 基础状态同步和胜负结果一致性。
- Client 断线、Host 退出、取消匹配和重新匹配。
- Windows 防火墙拦截、端口占用和协议版本不一致。
- 连续多局和资源清理。
- 少量真实 Edgegap Relay 冒烟，验证 Host/Client 均连接 Relay 而非彼此直连。

### 本轮不实施

- 5v5v5 的容量、负载和长时间稳定性。
- Host Migration、断线续局和反作弊。
- Android、iOS、WebGL 和主机平台。
- 自己实现 Edgegap UDP 协议的“假 Relay”。
- 使用 Fly.io 自建 Relay 或替换 Edgegap。
- 将真实生产 Supabase、生产 Relay 或正式用户数据用于自动测试。

## 4. 分层测试架构

测试按反馈速度和真实性分成六层。下层失败时不继续运行成本更高的上层。

| 层 | 环境 | 验证对象 | 触发时机 | 云依赖 |
| --- | --- | --- | --- | --- |
| L0 | EditMode | 战斗纯逻辑、序列化、状态机、Relay 参数校验 | 每次相关修改 | 无 |
| L1 | 本机双进程 | KCP 回环、RoomPlayer、场景、退出清理 | 每次网络修改 | 无 |
| L2 | 本机双进程 + `LatencySimulation` | 延迟、抖动、丢包、乱序下的同步与恢复 | PR、每日 | 无 |
| L3 | 本地假匹配 API | Unity 与 Web API 契约、取消和错误处理 | 每次在线匹配修改 | 无 |
| L4 | 两台物理设备 | UDP 广播、防火墙、网卡和真实 LAN | 发布候选 | 无 |
| L5 | 真实 Edgegap | 会话创建、鉴权、跨 NAT 和 Relay 数据路径 | 发布候选、定时冒烟 | 测试环境 |

```text
L0 纯逻辑
-> L1 本机 KCP 回环
-> L2 本机弱网
-> L3 假匹配控制面
-> L4 真实 LAN
-> L5 真实 Edgegap Relay
```

“假匹配 API”只模拟控制面，不伪造 Edgegap UDP 数据面。本地双客户端使用 KCP 回环验证游戏通信；真实 Edgegap 用例验证 Transport 和 Relay 协议。

## 5. 测试环境与角色

### 5.1 最小设备矩阵

| 环境 | Host | Client | 目的 | 优先级 |
| --- | --- | --- | --- | --- |
| A | Unity Editor | Windows Player | 最快验证功能与日志 | P0 |
| B | Windows Player A | Windows Player B，同一台电脑 | 验证动态端口和进程隔离 | P0 |
| C | Windows 设备 A | Windows 设备 B，同一 Wi-Fi | 验证真实广播和防火墙 | P0 |
| D | Windows 设备 A，有线 | Windows 设备 B，Wi-Fi，同一路由器 | 验证跨接入介质 | P1 |
| E | Windows 设备 A | Windows 设备 B，访客 Wi-Fi/AP 隔离 | 验证不可发现时的错误表现 | P1 |
| F | Windows Player A | Windows Player B，`127.0.0.1` | 自动回环和弱网回归 | P0 |
| G | Windows 设备 A | Windows 设备 B，不同 NAT，经 Edgegap | 真实 Relay 冒烟 | P0（发布候选） |

同机测试只能作为快速反馈，不能替代两台物理设备测试；操作系统对本机广播、回环和防火墙的处理与真实 LAN 不完全相同。

### 5.2 测试前提

- 两端使用同一 Git commit、同一 Windows 构建和协议版本。
- `LoadScene`、`RegiserScene`、`LobbyPanel`、`dantiao_map` 已加入 Build Settings。
- 两台设备处于同一 IPv4 子网，访客网络和 AP/Client Isolation 已关闭。
- Windows 网络配置文件设为“专用网络”。
- 测试账号、角色选择和本地配置可正常进入大厅。
- 每端系统时间正确，磁盘有足够空间保存 Player 日志和录屏。
- 测试期间不要运行会改变路由的 VPN、代理或虚拟网卡；若必须保留，记录接口和路由优先级。
- 基线和稳定性测试期间，同一局域网一次只运行一种游戏模式；跨模式房间隔离使用独立异常用例验证。
- 环境 F 必须使用两个独立 OS 进程；Mirror 的全局静态状态决定了同一 Unity 进程不能同时充当两个相互独立的测试客户端。
- 环境 G 只允许连接测试 Web、测试 Supabase 和测试 Edgegap 会话，不使用生产凭据。

### 5.3 构建与证据命名

建议使用独立的 Windows Development Build，并开启 `Script Debugging`。构建目录和证据目录采用以下格式：

```text
Builds/LanTest/<commit>/
Logs/LanTest/<yyyyMMdd-HHmm>/<case-id>/host/
Logs/LanTest/<yyyyMMdd-HHmm>/<case-id>/client/
artifacts/coverage/<commit>/
artifacts/network/<yyyyMMdd-HHmm>/<case-id>/
```

每轮记录：commit、构建时间、设备名、IPv4、连接方式、Windows 版本、防火墙状态、用例结果和缺陷编号。不要把访问令牌、密码或完整用户隐私数据写入日志附件。

## 6. 测试前检查

在两台设备上分别执行：

```powershell
ipconfig
Get-NetConnectionProfile
Get-NetIPAddress -AddressFamily IPv4 |
  Where-Object { $_.IPAddress -notlike '127.*' -and $_.IPAddress -notlike '169.254*' }
```

确认双方 IPv4 和子网掩码可构成同一子网。`ping` 仅用于辅助检查；禁用 ICMP 不代表 UDP 游戏连接一定失败。

启动客户端前检查目标端口：

```powershell
Get-NetUDPEndpoint |
  Where-Object { $_.LocalPort -ge 887 -and $_.LocalPort -le 908 } |
  Sort-Object LocalPort
```

测试开始后再次执行，预期 Host 至少占用一个 KCP 游戏端口，监听端占用 Beacon UDP `889`。若防火墙首次弹窗出现，允许 Development Build 在“专用网络”通信，并记录本次选择。

## 7. L0：自动测试与覆盖率

### 7.1 安装与报告

第一阶段在 `Packages/manifest.json` 固定 Unity 2022.3 released 版本：

```json
"com.unity.testtools.codecoverage": "1.2.4"
```

安装后使用 Unity Test Framework 运行 EditMode 和后续 PlayMode 测试，并输出 HTML、测试 XML 和原始覆盖数据。若 CI 后续需要 Cobertura/LCOV，再在验证脚本中启用对应报告格式。建议批处理入口：

```powershell
& $UnityExe `
  -batchmode `
  -nographics `
  -projectPath $ProjectRoot `
  -runTests `
  -testPlatform EditMode `
  -testResults "$Artifacts\editmode-results.xml" `
  -debugCodeOptimization `
  -enableCodeCoverage `
  -coverageResultsPath "$Artifacts\coverage" `
  -coverageOptions "generateHtmlReport;generateAdditionalMetrics;assemblyFilters:+Aoyi.*,+Assembly-CSharp" `
  -logFile "$Artifacts\unity-editmode.log"
```

正式落地脚本需要按实际生成的程序集名称收紧 `assemblyFilters`，并用 `pathFilters` 排除测试、示例和生成代码；Unity 进程退出码、测试 XML 和覆盖报告缺一不可。报告目录必须加入 `.gitignore`，CI 将报告作为 artifact 上传。

### 7.2 覆盖率门槛

覆盖率只表示代码被执行，不代表断言充分。门槛按程序集或明确路径统计，不使用整个历史 `Assembly-CSharp` 的总百分比掩盖核心模块。

| 模块 | 第一阶段 | 稳定后门槛 | 说明 |
| --- | ---: | ---: | --- |
| 战斗纯逻辑、定点数、伤害和胜负判定 | 建立基线 | 行覆盖率 `>= 70%` | 分支覆盖率单独观察 |
| 网络消息编解码、匹配状态机、取消/重试 | 建立基线 | 行覆盖率 `>= 70%` | 高风险函数目标 `>= 80%` |
| Relay 数据规范化、角色与端口/token 校验 | `>= 70%` | 行覆盖率 `>= 80%` | 失败路径必须有断言 |
| UI、场景绑定和简单 MonoBehaviour 胶水 | 只报告 | 不设硬门槛 | 由 PlayMode/端到端覆盖 |
| 第三方 Mirror、Edgegap、Unity 包 | 排除 | 排除 | 不把第三方代码纳入项目门槛 |

门禁分两步启用：

1. 先连续收集三次基线，找出未被测试的核心代码。
2. 新增或修改的核心代码不得降低对应模块覆盖率。
3. 核心模块达到目标后，CI 才启用固定百分比阻断。
4. 任何覆盖率豁免必须写明原因、负责人和到期时间。

### 7.3 L0 必测行为

- Beacon JSON 的往返序列化、缺字段和非法字段。
- `ProtocolVersion` 兼容与不兼容判断。
- 房间模式、容量、状态过滤；包括当前尚未实现的跨模式拒绝。
- 可用 UDP 端口选择、端口耗尽和边界值。
- 在线匹配开始、轮询、取消、过期、重复结果和旧 generation 拒绝。
- Relay Host/Guest 角色解析、非房间成员拒绝。
- Relay 地址、server/client UDP 端口、session token 和 user token 校验。
- 预取消 token 不应创建 Mirror 状态。
- 战斗输入、伤害、死亡和胜负判定的确定性。

## 8. L1：本机回环双客户端

### 8.1 自动化运行模型

构建一个带测试启动参数的 Windows Development Build，启动两个独立进程：

```text
Aoyi.exe -networkTestRole host   -networkTestCase loopback-basic -logFile <host-log>
Aoyi.exe -networkTestRole client -networkTestCase loopback-basic -logFile <client-log>
```

这些启动参数和 PowerShell 协调器已实现基础回环子集，运行方式与证据结构见[本机双进程网络回环基线](network-loopback-baseline.md)。协调器支持 `-Iterations 1..100`：构建只执行一次，每轮使用独立 `runId`、动态 UDP 端口和 artifact 子目录，多轮批次根目录写入 `summary.json`。测试启动器的完整目标仍包括：

- 使用唯一测试运行 ID 隔离日志和结果。
- Host 显式绑定 `127.0.0.1` 与测试分配的 UDP 端口。
- Client 等待 Host 写出 ready 标记后连接，避免依赖 LAN Broadcast。
- 双方写出结构化 checkpoint：启动、连接、RoomPlayer、场景、战斗动作、退出。
- 总超时后终止本轮进程并保留日志，不遗留后台客户端。
- 每轮分配新端口，支持并行 CI 作业时避免冲突。

日常工程回归建议先运行 3 轮：

```powershell
.\scripts\Run-NetworkLoopback.ps1 `
  -SkipBuild `
  -BuildPath ".\Builds\LanTest\NetworkLoopback\<build-id>\AoyiLoopback.exe" `
  -Iterations 3 `
  -TimeoutSeconds 90
```

批次写入 `artifacts/network-loopback/<batch-id>/summary.json`，并在任一轮失败后停止后续轮次、保留已生成的 Host/Client 结果与错误信息、返回非零退出码。3 轮是工程检查，不替代 `LOOP-007` 要求的连续 20 局 L1 发布门槛。

本机回环验证 Mirror 游戏数据路径，不验证 UDP 广播、防火墙和物理网卡；这些由 L4 覆盖。

### 8.2 L1 用例

| ID | 场景 | 通过标准 |
| --- | --- | --- |
| LOOP-001 | Host/Client 回环连接 | 8 秒内双方连接并生成两个 RoomPlayer |
| LOOP-002 | 进入 `dantiao_map` | 双方场景一致，Host 只切换一次 |
| LOOP-003 | 确定性战斗脚本 | 双方最终血量、死亡与胜负摘要一致 |
| LOOP-004 | Client 正常退出 | Host 检测断开，双方进程退出码为预期值 |
| LOOP-005 | Client 强制终止 | Host 不永久等待，可清理并开始下一轮 |
| LOOP-006 | Host 强制终止 | Client 明确断线，不黑屏或无限等待 |
| LOOP-007 | 连续 20 局 | `20/20` 成功，无旧玩家、旧端口或旧 NetworkManager |
| LOOP-008 | 两轮快速取消/重开 | 旧异步结果不能影响新一轮匹配 |

## 9. L2：Mirror 弱网模拟

### 9.1 Transport 组合

项目已经包含 Mirror `LatencySimulation`。测试配置需支持以下组合：

```text
LAN/回环弱网：LatencySimulation -> KcpTransport
Relay 客户端实验：LatencySimulation -> EdgegapKcpTransport
```

`LatencySimulation` 必须成为 `NetworkManager.transport`，其 `wrap` 指向真实 Transport。仅在 Development Build、Editor 或显式测试参数下启用，正式发行配置默认关闭。

### 9.2 弱网档位

| 档位 | 单向基础延迟 | Jitter | Unreliable Loss | Unreliable Scramble | 用途 |
| --- | ---: | ---: | ---: | ---: | --- |
| NET-BASE | `0 ms` | `0` | `0%` | `0%` | 无模拟基线 |
| NET-LAN | `10 ms` | `0.01` | `0%` | `0%` | 本地轻微抖动 |
| NET-WAN | `80 ms` | `0.05` | `1%` | `1%` | 日常公网回归 |
| NET-POOR | `150 ms` | `0.10` | `3%` | `2%` | 发布门槛 |
| NET-EXTREME | `300 ms` | `0.20` | `5%` | `5%` | 恢复能力，不作为体验承诺 |

Mirror 的 `jitter` 是附加延迟幅度，不是百分比。Reliable channel 的丢包影响表现为额外延迟；`unreliableLoss` 和 `unreliableScramble` 只作用于 Unreliable channel。

### 9.3 弱网通过标准

- `NET-BASE`、`NET-LAN`、`NET-WAN` 和 `NET-POOR` 必须完成连接、场景切换、规定战斗动作和退出。
- `NET-POOR` 下不得出现双方胜负不一致、重复伤害、重复结算或永久卡住。
- `NET-EXTREME` 可以判定连接失败或主动超时，但必须可取消、可退出、可再次匹配。
- 每档至少运行 10 轮；发布候选对 `NET-WAN` 和 `NET-POOR` 各运行 20 轮。
- 报告记录配置、连接耗时、RTT、断线原因、checkpoint 和双方最终状态 hash。

## 10. L3：本地假匹配 API

### 10.1 模拟范围

本地假服务实现与当前 Web API 相同的控制面端点：

```text
POST /api/match/start
GET  /api/match/status?ticketId=<id>
POST /api/match/cancel
```

建议作为 `Web/` 测试辅助服务或 Node 测试进程实现，不在 Unity 中硬编码假响应。Unity 通过可注入的 `OnlineMatchApiBaseUrl` 指向 `127.0.0.1`。

假服务只返回匹配与 Relay 连接数据，不转发 KCP 数据包。成功匹配后，本地端到端测试选择以下一种数据面：

- 使用普通 `KcpTransport` 回环，验证匹配后的游戏流程。
- 只验证 `EdgegapKcpTransport` 参数被正确配置后停止，不声称真实 Relay 已连接。

### 10.2 契约场景

| ID | API 行为 | Unity 预期 |
| --- | --- | --- |
| API-001 | `start` 返回 waiting，随后 status 返回 matched | 只启动一次匹配房间 |
| API-002 | `start` 立即返回 matched | 正确解析本地角色和连接信息 |
| API-003 | 用户取消后旧 status 返回 matched | 拒绝旧 generation，不启动 Mirror |
| API-004 | 401/403 | 停止轮询并提示登录或权限错误 |
| API-005 | 429/5xx | 有上限地重试，不形成请求风暴 |
| API-006 | 超时、断网和无效 JSON | 可取消，可再次开始匹配 |
| API-007 | 缺 Relay IP/端口/token | fail closed，不启动 Transport |
| API-008 | local user 不属于 Host/Guest | fail closed，并记录成员不匹配 |
| API-009 | server/client 端口互换 | 契约测试失败，指出字段映射错误 |
| API-010 | cancel 幂等调用 | 重复取消不产生异常状态 |

日志不得输出 Authorization header、完整 session token、完整 user token 或服务端密钥。测试夹具使用固定虚假 token，并检查日志中不存在原文。

## 11. L4：真实 LAN P0 冒烟测试

### LAN-001 自动创建与发现

前置条件：两端均未匹配，网络状态已清理。

步骤：

1. Host 端先选择 LAN 1v1 并开始匹配。
2. 等待至少 3 秒，确认其搜索超时后创建房间。
3. Client 端选择相同模式并开始匹配。
4. 观察 Client 是否在一个 Beacon 周期内发现兼容房间并加入。

通过标准：

- Host 日志出现搜索超时、创建 Mirror 房间、开始 Beacon 广播。
- Client 日志出现发现房间、目标 IPv4/端口、`NetworkClient.isConnected=True`。
- 两端只形成一个房间，Host 为 `LanHost`，另一端为 `LanClient`。
- 从 Client 开始搜索到建立连接不超过 8 秒。

### LAN-002 同时开始匹配

步骤：两端在 500 ms 内同时开始 LAN 1v1，连续执行 10 次。

通过标准：每轮最终只形成一个可玩的房间；不得出现双方长期各自成为 Host、重复场景切换或卡在等待页。偶发失败必须能从日志区分“未发现广播”和“游戏端口连接失败”。

### LAN-003 房间玩家与场景切换

步骤：连接后等待两名 RoomPlayer 创建并就绪，进入战斗。

通过标准：

- 两端玩家索引和 teamId 有效且不冲突。
- Host 只触发一次 `ServerChangeScene`。
- 两端最终场景均为 `dantiao_map`。
- 不出现重复玩家、空本地玩家、未注册消息或 prefab spawn 错误。

### LAN-004 基础战斗同步

步骤：双方依次移动、普攻、释放一个技能、受到伤害并完成一局。

通过标准：

- 对端能看到移动、朝向、动画和技能事件。
- 双方关键血量和死亡状态最终一致。
- 胜负只结算一次，双方看到互补且一致的结果。
- 观察期间无持续增长的网络异常、反序列化异常或 NullReferenceException。

### LAN-005 正常退出与再次匹配

步骤：完成一局后双方返回大厅，再连续开始 3 局 LAN 1v1。

通过标准：每局均能重新创建/加入；前一局 Beacon 停止，Mirror Host/Client 被关闭，UDP 端口可复用；不得出现 `Client already started`、旧房间被重新发现或旧玩家残留。

## 12. L4 异常与恢复测试

| ID | 场景与操作 | 预期结果 | 优先级 |
| --- | --- | --- | --- |
| LAN-101 | Client 在等待房间时取消 | 搜索停止、监听释放、返回可再次匹配状态 | P0 |
| LAN-102 | Client 战斗中关闭进程 | Host 在合理时间内检测断开，不继续等待不存在的 Client；可退出并重开 | P0 |
| LAN-103 | Host 战斗中关闭进程 | Client 收到断线并回到可恢复界面，不永久黑屏或卡场景 | P0 |
| LAN-104 | Host 等待时退出 | Beacon 停止；Client 不应持续加入失效房间 | P0 |
| LAN-105 | Client 加入瞬间取消 | 异步等待终止，Mirror 状态清理，不得随后自动进入战斗 | P0 |
| LAN-106 | 占用 UDP `887-889` 后开房 | 自动尝试后续可用端口，或明确失败；不得无限等待 | P1 |
| LAN-107 | 阻止 UDP `889` 入站 | Client 无法发现；日志明确停留在发现阶段，解除规则后可恢复 | P1 |
| LAN-108 | 允许 Beacon、阻止 Host 的 KCP 游戏端口 | 能发现但连接失败；UI 可退出并重试，日志包含目标 IP/端口 | P0 |
| LAN-109 | 双方协议版本不同 | 不加入不兼容房间，并显示或记录版本不匹配 | P0 |
| LAN-110 | Wi-Fi 暂时断开 10 秒后恢复 | 当前局明确失败或断开；恢复后新一轮匹配成功 | P1 |
| LAN-111 | 开启 VPN/虚拟网卡导致多 IPv4 | 要么选择正确 LAN IPv4，要么明确暴露错误地址；记录为路由选择缺陷 | P1 |
| LAN-112 | 访客 Wi-Fi/AP 隔离 | 不应误报连接成功；文档化为网络环境限制 | P1 |
| LAN-113 | 一端搜索单挑，另一端广播排位等待房 | 不得跨模式加入；当前实现未比较 `room.Mode`，若复现误连应记录为已知 S1 缺陷 | P0 |

端口占用可用以下命令临时复现，测试结束后关闭对应 PowerShell 进程：

```powershell
$udp887 = [System.Net.Sockets.UdpClient]::new(887)
$udp888 = [System.Net.Sockets.UdpClient]::new(888)
$udp889 = [System.Net.Sockets.UdpClient]::new(889)
```

防火墙测试必须使用带明确名称和程序路径的临时规则。测试后只删除本轮创建的规则，不要关闭整个系统防火墙。

## 13. L5：真实 Edgegap Relay 冒烟

### 13.1 真实 Relay 数据路径

项目已经集成 `EdgegapKcpTransport`，无需再次下载 Mirror 或替换整个网络框架。匹配服务创建 Relay Session 后，两端从同一份会话中取得不同连接参数：

```text
Host
-> Edgegap Relay IP + server port
-> session token + Host user token
-> StartHost()

Client
-> Edgegap Relay IP + client port
-> 同一 session token + Client user token
-> StartClient()
```

Host 和 Client 都连接 Relay，不直接把对方公网 IP 设置为目标。每名玩家的 user token 必须不同，session token 在同一局内共享。测试日志只保留 token hash 或末尾少量字符。

### 13.2 Relay 用例

| ID | 场景 | 通过标准 |
| --- | --- | --- |
| RELAY-001 | 两台设备位于不同 NAT，正常匹配 | 两端经 Relay 连接并进入 `dantiao_map` |
| RELAY-002 | 核对 Host/Client 端口 | Host 使用 server port，Client 使用 client port |
| RELAY-003 | 用户 token 不同、session token 相同 | 鉴权成功，日志不泄露完整 token |
| RELAY-004 | 过期或非法 token | 连接失败且可恢复，不回退为玩家直连 |
| RELAY-005 | Relay API 创建失败或长时间未 ready | 有界重试、可取消、无遗留匹配票据 |
| RELAY-006 | Client 连接中取消 | 停止重试并清理 `EdgegapKcpTransport` 和 Mirror 状态 |
| RELAY-007 | 战斗中 Client 断网 | Host 检测断开；双方可结束本轮并重新匹配 |
| RELAY-008 | 连续完成 10 局 | `10/10` 成功，无旧 session 或旧 token 复用 |

### 13.3 Relay 执行约束

- 使用独立测试项目、测试账号和短期最小权限密钥。
- Relay 管理密钥只存在于 Web 服务端/CI Secret，不进入 Unity 构建。
- 每轮测试后关闭 match ticket、room 和 Relay session。
- 记录 Edgegap session ID、区域、创建耗时、ready 耗时和双方连接耗时；session ID 不视为密钥，但仍避免公开上传。
- 自动测试设置预算和每日运行上限，API 异常时停止创建新 session。
- L0-L3 未通过时禁止运行真实 Relay 测试，避免用云端重试掩盖本地缺陷。

## 14. 稳定性与质量指标

### 14.1 重复与长时间测试

- 连续匹配、进战斗、退出 20 轮，成功率要求 `20/20`。
- 同一房间保持 30 分钟，每 5 分钟执行移动、攻击和技能同步检查。
- 30 分钟内无非预期断线、场景漂移或重复结算。
- 退出后等待 10 秒，确认无旧 Beacon、无残留 NetworkManager 状态。
- L1 本机回环连续 20 局，L2 `NET-WAN`/`NET-POOR` 各 20 局，L5 真实 Relay 连续 10 局。

### 14.2 性能观察

记录 Unity Profiler 或 Development Build 指标：

- Host 和 Client 平均帧率、主线程尖峰。
- Mirror/KCP 发送与接收带宽。
- RTT、丢包或重传趋势。
- 进入战斗前后内存，以及 20 轮后的内存变化。

本轮建议门槛：同一稳定 LAN 下，空闲 RTT 中位数小于 30 ms，基础战斗不因网络处理产生持续超过 50 ms 的主线程尖峰。该门槛用于发现明显回归，不等同于公网性能承诺。

## 15. 日志与证据

Windows Player 日志通常位于：

```text
%USERPROFILE%\AppData\LocalLow\个人制作娱乐\奥义Demo\Player.log
%LOCALAPPDATA%\Unity\Editor\Editor.log
```

每个失败用例至少保存：

- Host 和 Client 的完整 `Player.log`。
- 双方开始操作的时间点和设备 IPv4。
- 失败界面截图或短录屏。
- `Get-NetUDPEndpoint` 输出。
- 防火墙、VPN、路由器隔离等环境变化。
- 预期结果、实际结果、首次异常日志和复现概率。

重点检索以下日志前缀和错误：

```text
[LanQuickMatchManager]
[LAN]
[AoyiNetworkRoomManager]
[MirrorNetBridge]
[OnlineMatchManager]
[OnlineConnectionLauncher]
Client already started
connection failed
timeout
SocketException
NullReferenceException
```

## 16. 缺陷分级

| 等级 | 判定示例 | 发布处理 |
| --- | --- | --- |
| S0 阻断 | 两台设备无法进入同一局；崩溃；数据破坏 | 必须修复并全量回归 |
| S1 严重 | 高概率双 Host、场景不一致、胜负不一致、退出后无法再匹配 | 必须修复并回归相关 P0 |
| S2 一般 | 特定防火墙/VPN 环境失败但可恢复；错误提示不清楚 | 评估后修复，必须记录限制 |
| S3 轻微 | 非阻断日志噪声、显示延迟、文案问题 | 可进入后续迭代 |

## 17. 分级门禁与发布门槛

### 17.1 日常提交

- L0 受影响测试全部通过。
- 生成覆盖报告；新增或修改的核心代码不降低模块基线。
- 网络或场景生命周期修改必须运行 L1 `LOOP-001` 至 `LOOP-006`。
- 在线匹配/API 修改必须运行 L3 契约测试。

### 17.2 Pull Request

- L0 全量通过，覆盖报告作为 artifact。
- L1 基本回环和取消/断线用例通过。
- L2 `NET-BASE`、`NET-LAN`、`NET-WAN` 各 10 轮通过。
- 仅当 PR 涉及 Edgegap Transport、Relay 模型或 Web Relay 创建逻辑时，运行受控的 L5 单局冒烟；来自 Fork 的 PR 不接触 Relay Secret。

### 17.3 发布候选

只有同时满足以下条件，当前 1v1 网络功能才可判定通过：

- 核心模块覆盖率达到已启用门槛，无无期限豁免。
- L1 本机回环连续 20 局通过。
- L2 `NET-WAN` 与 `NET-POOR` 各连续 20 局通过。
- L3 全部 API 契约和日志脱敏检查通过。
- 环境 A、B、C 的全部 P0 用例通过。
- LAN-002 同时匹配连续 10 次通过。
- 完整对局和退出重进连续 20 轮通过。
- L5 `RELAY-001` 至 `RELAY-007` 通过，连续 10 局成功。
- 无未关闭的 S0/S1 缺陷。
- Client 断线和 Host 断线均有可恢复路径。
- 协议版本不一致不会误加入。
- Host/Client 日志、构建 commit 和设备信息已归档。
- 端口与防火墙规则在退出后正确清理。

## 18. 执行记录模板

```markdown
### <case-id> <case-name>

- 构建 commit：
- 测试时间：
- Host 设备 / IPv4：
- Client 设备 / IPv4：
- 网络拓扑：
- 测试层 / 弱网档位：
- 防火墙 / VPN：
- Relay session / 区域（如适用）：
- 结果：通过 / 失败 / 阻塞
- 耗时：
- 实际表现：
- 关键日志时间点：
- 证据目录：
- 缺陷编号：
- 复测结果：
```

## 19. 分阶段实施顺序

### 阶段 0：基线

实施状态：`2026-07-11` 已完成首个绿色基线，详见 [网络测试自动化基线](network-test-baseline.md)。

- 安装并固定 `com.unity.testtools.codecoverage@1.2.4`。
- 连续三次运行现有 EditMode 测试并归档 HTML 报告。
- 确定核心模块 assembly/path filter 和初始覆盖率。
- 将测试报告、临时端口文件和本地 token fixture 加入忽略规则。

验收：三次测试结果稳定；报告不包含第三方程序集和秘密；未对历史低覆盖率直接启用全仓阻断。

### 阶段 1：本机回环

实施状态：`2026-07-11` 已完成基础回环子集和 L1.5 多轮协调器，并取得 3/3 的真实三轮 PASS，详见[本机双进程网络回环基线](network-loopback-baseline.md)。阶段 1 尚未全部完成。

已完成基础子集：

- Development Build 网络测试启动参数和结构化 checkpoint。
- PowerShell 协调器启动两个独立 Player，并处理动态 UDP 端口、超时和进程清理。
- 多轮协调器复用一次构建，写出批次 `summary.json`，并在首败时停止后续轮次、返回非零退出码。
- KCP 连接、两个 RoomPlayer、不同 `playerIndex`/`teamId` 以及同步进入 `dantiao_map` 的单轮验证。
- 使用最新构建完成 3 轮连续回环工程检查；三轮端口互异，Host/Client 均进入 `dantiao_map`。

待完成：

- 确定性战斗动作与双方状态 hash 比较。
- Client/Host 断线、强制终止和退出后的清理验证。
- 快速取消/重开以及旧异步结果隔离。
- 连续 `20` 局回归，并完成 `LOOP-001` 至 `LOOP-008` 的全部验收。

验收：一条本地命令可连续完成 20 局并生成摘要；失败能定位 Host 或 Client checkpoint。当前 3 轮工程检查已通过，但 20 局发布门槛仍待执行。

### 阶段 2：弱网模拟

- 为运行时 NetworkManager 增加仅测试环境可用的 Transport 装配配置。
- 将 `LatencySimulation` 包装在 KCP 外层。
- 固化五个弱网档位并写入测试报告。
- 对 `NET-WAN` 和 `NET-POOR` 运行重复回归。

验收：弱网参数可由测试参数选择，正式发行配置无法意外启用；极端档位失败后仍能重试。

### 阶段 3：假匹配 API

- 在 `Web/` 增加可编程的本地匹配 fixture。
- 让 Unity 测试构建通过配置切换 API base URL。
- 实现 `API-001` 至 `API-010`，并检查 token 脱敏。
- 将 Web 单元测试和 Unity 客户端契约测试连接到同一组 fixture。

验收：不访问 Supabase 或 Edgegap即可覆盖匹配、取消、错误响应和 Relay 参数映射。

### 阶段 4：真实网络与 CI

- 保留并执行 L4 双机 LAN 用例。
- 建立隔离的 Supabase/Edgegap 测试环境和预算限制。
- 自动执行 L5 Relay 冒烟并回收 session。
- PR 跑 L0-L3，发布候选跑 L0-L5。

验收：报告能区分逻辑、回环、弱网、API、LAN 和 Relay 六类失败；生产数据和生产密钥不参与测试。

## 20. Fly.io 决策

Fly.io 能承载 UDP 服务，但它不会自动实现 Edgegap 的 session、双端口路由、用户鉴权和 Mirror Transport 协议。本规划不把 Fly.io 当作“模拟 Edgegap Relay”。

只有在团队明确决定自建 Relay 或部署独立 Mirror Server 时，才另开技术验证，至少包含：

- UDP 服务模型和固定/Anycast IP 行为。
- 会话配对、鉴权、防滥用、超时回收和区域路由。
- 自建 Mirror Transport 或兼容协议。
- 成本、监控、DDoS 风险、故障转移和运维责任。

在该独立验证完成前，现行选择保持：本地 KCP + Mirror 弱网模拟用于大多数回归，Edgegap 用于真实 Relay 数据面。

## 21. 后续自动化建议

首轮保持真实双机人工测试，同时逐步增加自动化：

1. 优先完成 Coverage 基线与本机双进程协调器。
2. 再接入 `LatencySimulation` 和固定弱网档位。
3. 然后建立假匹配 API 契约与日志脱敏检查。
4. 最后将真实 LAN 和 Edgegap 冒烟接入发布候选流程。

相关设计与工程背景参见 [联机系统 V1 计划](networking-lan-supabase-plan.md)、[1v1 公网 Relay / P2P 方案对比](online-relay-comparison.md) 和 [开发工作流自动化规划](workflow-automation-plan.md)。

官方资料：

- [Unity 2022.3 Code Coverage 包](https://docs.unity3d.com/cn/2022.3/Manual/com.unity.testtools.codecoverage.html)
- [Unity Code Coverage 批处理参数](https://docs.unity3d.com/Packages/com.unity.testtools.codecoverage@1.2/manual/CoverageBatchmode.html)
- [Mirror Latency Simulation Transport](https://mirror-networking.gitbook.io/docs/manual/transports/latency-simulaton-transport)
- [Edgegap Distributed Relay Transport Samples](https://docs.edgegap.com/zh/learn/distributed-relay/relay-transport-samples)
- [Fly.io App Services 与 UDP 说明](https://fly.io/docs/networking/app-services/)
