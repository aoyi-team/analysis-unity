# 本机双进程网络回环基线

本文面向维护者，说明如何在 Windows 上运行 Unity 2022.3 + Mirror/KCP 的 L1 本机双进程回环，并用结构化结果定位失败。完整测试范围和最终门槛见[本地优先网络测试规划](local-network-test-plan.md)。

## 当前状态

基础回环子集已真实通过。它证明两个独立 Windows Player 进程可以通过本机 KCP 建立连接、创建两个 RoomPlayer、获得不同的 `playerIndex`/`teamId`、同步进入 `dantiao_map`，并在 Host 发出 `MsgBattleOver` 后由服务器统一把双方切回 `LobbyPanel`。

这不是 L1 阶段的完整验收，具体未覆盖项见[范围和限制](#范围和限制)。

## 前置条件

- 在 Windows PowerShell 中进入项目根目录。
- 安装并可启动 Unity `2022.3.61f1c1`，Unity 许可证可用于批处理构建，并安装 Windows 构建支持。
- 没有另一个 Unity Editor 实例占用同一项目。
- 允许本机 Player 使用回环 UDP；脚本默认自动选择可用端口。

脚本无法自动找到 Unity 时，通过 `-UnityExe` 指定 Editor：

```powershell
.\scripts\Run-NetworkLoopback.ps1 `
  -UnityExe "C:\Program Files\Unity\Hub\Editor\2022.3.61f1c1\Editor\Unity.exe" `
  -TimeoutSeconds 90
```

## 运行基线

首选命令会先构建 Development Player，再依次启动 Host 和 Client：

```powershell
.\scripts\Run-NetworkLoopback.ps1 -TimeoutSeconds 90
```

控制台会输出本轮 `Run ID`、UDP 端口和 artifact 目录。成功时末尾应包含：

```text
Host: success, player=0, team=1, scene=LobbyPanel
Client: success, player=1, team=2, scene=LobbyPanel
Loopback result: PASS
```

已有可复用构建时，可跳过 Unity 构建：

```powershell
.\scripts\Run-NetworkLoopback.ps1 -SkipBuild -TimeoutSeconds 90
```

该命令默认使用 `Builds\LanTest\NetworkLoopback\AoyiLoopback.exe`。若要复用某次版本化构建，显式传入它的路径：

```powershell
.\scripts\Run-NetworkLoopback.ps1 `
  -SkipBuild `
  -BuildPath ".\Builds\LanTest\NetworkLoopback\loopback-20260711-075502-3ec6aff8\AoyiLoopback.exe" `
  -TimeoutSeconds 90
```

`-SkipBuild` 只适用于代码、场景和 Player 构建内容未变化的重跑；否则应使用首选命令重新构建。

### 多轮工程检查

使用 `-Iterations` 可在同一个 Development Player 上连续执行多轮独立回环。参数范围是 `1..100`；多轮模式会为每轮分配新的 `runId`、UDP 端口和 artifact 子目录，并在批次结束时写出 `summary.json`。多轮模式不能同时传入非零的 `-Port`。

日常工程检查使用 3 轮即可：

```powershell
.\scripts\Run-NetworkLoopback.ps1 `
  -SkipBuild `
  -BuildPath ".\Builds\LanTest\NetworkLoopback\loopback-20260711-075502-3ec6aff8\AoyiLoopback.exe" `
  -Iterations 3 `
  -TimeoutSeconds 90
```

这条命令只构建/复用一次 Player；任一轮失败后立即停止后续轮次，仍会写出已完成轮次和失败轮次的摘要，并以非零退出码结束。3 轮是工程回归检查，不是发布验收；L1 发布门槛仍要求连续 20 局通过。

## 成功检查点

Host 和 Client 都必须按以下顺序写出检查点：

```text
bootstrap
-> room-scene-ready
-> transport-started
-> connected
-> room-player-ready
-> battle-scene
-> lobby-scene
```

Host 会在 `battle-scene` 与 `lobby-scene` 之间额外写出 `battle-over-sent`。

最终判定还要求：

- 双方 `success` 为 `true`，最终检查点为 `lobby-scene`。
- 双方最终场景均为 `LobbyPanel`。
- Host 与 Client 的 `playerIndex` 不同。
- Host 与 Client 的 `teamId` 不同。

## 已验证 PASS

`2026-07-11` 的已知通过基线如下：

| 项目 | 值 |
| --- | --- |
| Run ID | `loopback-20260711-075502-3ec6aff8` |
| UDP 端口 | `59539` |
| Host | `playerIndex=0`、`teamId=1`、`scene=dantiao_map` |
| Client | `playerIndex=1`、`teamId=2`、`scene=dantiao_map` |
| 证据目录 | `artifacts/network-loopback/loopback-20260711-075502-3ec6aff8/` |

双方的 checkpoint 顺序与上节一致。

本次 L1.5 多轮收尾验证（`2026-07-11`）如下：

| 项目 | 值 |
| --- | --- |
| Batch ID | `loopback-20260711-120224-cdb75a49` |
| 请求 / 完成 / 通过 / 失败 | `3 / 3 / 3 / 0` |
| UDP 端口 | `61591`, `61888`, `56428`（互异） |
| Host / Client 场景 | 三轮均为 `dantiao_map` |
| 摘要 | `artifacts/network-loopback/loopback-20260711-120224-cdb75a49/summary.json` |

这次 3 轮结果只证明多轮协调器、端口隔离、身份/场景门禁和摘要写入正常；不改变下方 20 轮发布门槛的状态。

`2026-07-12` 的战斗结束回大厅回归如下：

| 项目 | 值 |
| --- | --- |
| Run ID | `loopback-20260712-022527-bbfbd8d5` |
| UDP 端口 | `59404` |
| Host | `playerIndex=0`、`teamId=1`、`checkpoint=lobby-scene`、`scene=LobbyPanel` |
| Client | `playerIndex=1`、`teamId=2`、`checkpoint=lobby-scene`、`scene=LobbyPanel` |
| KCP 队列溢出 | 未发现 `Queue total ... >10000` 或拥塞断线 |
| 证据目录 | `%TEMP%\aoyi-team2-loopback-e2e-20260712\artifacts\` |

## Artifact 和失败定位

单轮默认写入 `artifacts/network-loopback/<run-id>/`。多轮模式使用批次根目录和每轮子目录：

```text
artifacts/network-loopback/<batch-id>/
├─ summary.json
└─ <run-id>/
   ├─ host.log / client.log
   ├─ host-checkpoints.ndjson / client-checkpoints.ndjson
   └─ host-result.json / client-result.json
```

`summary.json` 包含 `requestedIterations`、`completedIterations`、`passedIterations`、`failedIterations`、`success`，以及每轮的 `runId`、端口、耗时、artifact 路径、Host/Client 结果和错误信息。写入采用同目录临时文件再原子替换；即使首败，也会先写摘要再返回非零退出码。

每轮目录包含：

| 文件 | 用途 |
| --- | --- |
| `build.log` | Unity 构建日志；使用 `-SkipBuild` 时不会为本轮生成 |
| `host.log` / `client.log` | 两个 Player 的完整运行日志 |
| `host-checkpoints.ndjson` / `client-checkpoints.ndjson` | 按时间顺序记录双方检查点 |
| `host-result.json` / `client-result.json` | 最终 `success`、`checkpoint`、`message`、场景和玩家身份 |

失败时按以下顺序检查：

1. 从控制台输出确认本轮 artifact 目录，不要混用旧 `run-id`。
2. 先读两个 `*-result.json` 的 `checkpoint` 和 `message`；若结果文件不存在，检查对应 Player 是否提前退出。
3. 比较两个 `*-checkpoints.ndjson` 的最后一行，确定失败停在 Host 还是 Client、连接前还是场景切换后。
4. 构建失败查看 `build.log`；运行失败查看对应的 `host.log` 或 `client.log`。

可用以下命令快速查看结构化证据：

```powershell
Get-Content .\artifacts\network-loopback\<run-id>\*-result.json
Get-Content .\artifacts\network-loopback\<run-id>\*-checkpoints.ndjson
```

## 范围和限制

当前基线只验证：

- KCP 本机回环连接。
- Host 和 Client 各自创建 RoomPlayer。
- 两端获得不同的 `playerIndex` 和 `teamId`。
- Mirror 同步网络场景切换到 `dantiao_map`。
- Host 发送 `MsgBattleOver` 后，服务器权威切场景并让两端回到 `LobbyPanel`。

当前基线不验证：

- 确定性战斗动作或双方最终状态 hash。
- 通过正常伤害流程产生死亡；回环测试直接发送 `MsgBattleOver`。
- 长时间实时帧流量下的 KCP 队列稳定性。
- Client/Host 强制断线与恢复。
- 快速取消、退出或重新开局。
- 连续 `3` 轮工程检查已验证；连续 `20` 轮稳定性与资源清理仍是 L1 发布门槛，尚未由本基线声明通过。
- UDP 广播、Windows 防火墙、物理网卡、真实 LAN 或 Edgegap Relay。

Player 使用 `-nographics` 运行，日志中存在已知 UI、视频和 TextMesh Pro shader 噪声，其中包含 `WARNING` 和 `ERROR` 字样。因此不能把“日志无 warning/error”作为这条基线的通过标准；应以脚本退出结果、结构化 checkpoint 和双方 result JSON 为准，同时仍需单独调查与网络流程相关的新异常。
