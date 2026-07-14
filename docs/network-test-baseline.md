# 网络测试自动化基线

记录日期：`2026-07-11`

本页记录本地优先网络测试阶段 0 的首个可重复基线。数据来自 Unity `2022.3.61f1c1`、Unity Test Framework `1.1.33` 和 Code Coverage `1.2.4`。

## 执行命令

```powershell
.\scripts\Run-UnityCoverage.ps1
```

该命令解析项目要求的 Unity 版本，运行 EditMode 测试，并将 NUnit XML、Editor 日志和 HTML Coverage 报告写入：

```text
artifacts/unity-coverage/
```

`artifacts/` 已被 Git 忽略。报告需要在每台开发机或 CI 中重新生成，不提交生成文件。

## 首次绿色基线

| 指标 | 结果 |
| --- | ---: |
| EditMode 测试 | `16` |
| 通过 | `16` |
| 失败 | `0` |
| 跳过 | `0` |
| 被统计程序集 | `5` |
| 可覆盖行 | `16,259` |
| 已覆盖行 | `263` |
| 总行覆盖率 | `1.6%` |
| 总方法覆盖率 | `4.3%` |

程序集基线：

| 程序集 | 行覆盖率 | 方法覆盖率 |
| --- | ---: | ---: |
| `Aoyi.BaseClasses` | `0%` | `0%` |
| `Aoyi.ErrorManager` | `0%` | `0%` |
| `Aoyi.FixMath` | `13.6%` | `20.7%` |
| `Aoyi.Messages` | `5.7%` | `24.2%` |
| `Assembly-CSharp` | `1.4%` | `3.0%` |

## 基线修正

第一次运行暴露 4 个过时测试，不是 Code Coverage 引入的回归：

- `GameModes`、`MsgMatchRequest`、`MsgPlayerOp` 和 `MsgBattleReady` 已属于 `Aoyi.Messages`，测试仍从 `Assembly-CSharp` 解析。
- 致死攻击测试使用固定伤害 `150`，但当前英雄配置生命值为 `1000`。

测试已改为使用正确程序集名称，并读取目标当前生命值生成确定性的致死伤害。启用和禁用 Coverage 的对照运行具有相同的原始失败集合；修正后完整 Coverage 运行通过 `16/16`。

## 门禁状态

当前不启用固定百分比门禁。`Assembly-CSharp` 同时包含正式逻辑、UI、旧玩法和示例代码，`1.4%` 不能直接用来判断核心网络质量。

下一步按以下顺序收紧：

1. 使用 `pathFilters` 排除 UI、历史玩法、TMP 示例和生成代码。
2. 为 `Aoyi.Messages` 的编解码和协议兼容增加测试。
3. 为 `Aoyi.FixMath`、战斗纯逻辑和在线匹配状态机分别建立模块门槛。
4. 连续收集至少三次绿色基线后，先启用“改动不得降低覆盖率”。
5. 核心模块测试成熟后，再逐步提升到规划中的 `70%`。

完整路线见 [本地优先网络测试规划](local-network-test-plan.md)。
