# L1.5 多轮本地回环门禁设计

## 目标

在保持现有单轮命令兼容的前提下，让本地回环协调器一次执行多轮独立 Host/Client 测试，生成机器可读的批次摘要，并在任一轮失败时返回非零退出码。

## 设计

- `Run-NetworkLoopback.ps1` 新增 `-Iterations`，默认值为 `1`，合法范围为 `1..100`。
- 非 `-SkipBuild` 模式只构建一次，随后所有轮次复用同一 Development Player。
- 每轮生成独立 `runId`、动态 UDP 端口和 artifact 子目录。显式 `-Port` 仅允许单轮执行，避免多轮复用同一端口产生歧义。
- 单轮执行逻辑提取为脚本内函数，保持现有连接、身份、场景和进程清理门禁不变。
- 多轮 artifact 根目录写入 `summary.json`。摘要包含批次 ID、请求/完成/通过/失败轮数、整体成功状态，以及每轮 runId、端口、耗时、artifact 路径、Host/Client 结果和错误信息。
- 摘要通过临时文件加同目录移动原子写入。任一轮失败后记录该轮、停止后续轮次、写出摘要并返回非零退出码。
- 单轮默认命令仍使用原有 artifact 布局；多轮命令使用 `artifacts/network-loopback/<batch-id>/<run-id>/`，摘要位于批次根目录。

## 验证

- Pester 先验证摘要写入/读取和计数结构，并观察缺少实现时的预期失败。
- Pester 绿灯后，使用最新已有构建执行 `-SkipBuild -Iterations 3`。
- 检查三轮使用不同端口、身份互异、均进入 `dantiao_map`、`summary.json` 计数正确，且无残留 Unity/Player 进程。

## 非目标

- 不增加战斗动作或状态 hash。
- 不增加断线、快速重开或 20 轮发布门槛执行。
- 不接入 Mirror 弱网模拟、Edgegap Relay 或 Fly.io。
