# 1v1 公网 Relay / P2P 方案对比

本文记录当前 `aoyi team2` 项目在 1v1 公网联机上的技术选型判断。当前目标不是重做完整 5v5 在线架构，而是在现有 `Mirror + Supabase + 直接匹配` 基础上，解决玩家之间公网连接问题，并为后续“玩家和朋友玩”的好友房保留扩展空间。

## 1. 当前项目前提

当前 1v1 仍然以直接匹配为主：

```text
玩家点击开始
-> 进入匹配/等待
-> 找到另一个玩家
-> Mirror 房间同步选人/准备
-> 自动进入 dantiao_map
```

未来可能增加好友房，但好友房本质上只是匹配来源不同：

```text
直接匹配：系统分配对手
好友房：房间码/邀请分配对手
```

两者都需要解决同一个问题：

```text
两个普通玩家处在不同公网/路由器/NAT 后面，不能依赖局域网广播或直接 IP 连接。
```

因此，1v1 公网方案应优先满足：

- 不推翻当前 Mirror 房间和战斗流程。
- 不要求普通玩家端口映射。
- 能支持自动匹配，也能扩展到房间码/好友邀请。
- 成本适合轻量 1v1。
- 5v5 后续可单独设计，不强行和 1v1 共用同一套实现。

## 2. 备选方案

本文比较三个轻量公网连接方案：

| 方案 | 类型 | 解决什么 |
| --- | --- | --- |
| Unity Relay | Unity 官方 Relay | 帮客户端通过 Unity 中继连接 |
| Edgegap Relay | P2P Relay / Distributed Relay | 帮玩家托管游戏穿透 NAT/防火墙 |
| EOS P2P | Epic Online Services P2P | 提供 P2P、NAT 穿透和必要时的 relay |

它们都不是专服权威服务器。它们主要解决“玩家互相连不上”的问题，而不是帮项目运行完整战斗服务器。

## 3. 快速结论

当前推荐顺序：

```text
1. Edgegap Relay
2. Unity Relay
3. EOS P2P
```

理由：

- `Edgegap Relay` 和当前 `Mirror + KCP + 玩家托管 1v1` 的方向最接近。
- `Unity Relay` 免费额度不错，但更偏 Unity Gaming Services / Unity Transport 生态，Mirror 接入风险更高。
- `EOS P2P` 长期成本吸引人，但接入层更底，需要额外封装 Mirror transport 或桥接层。

## 4. 方案对比表

| 维度 | Edgegap Relay | Unity Relay | EOS P2P |
| --- | --- | --- | --- |
| 当前 Mirror 适配度 | 高 | 中 | 中低 |
| 是否需要换掉 Mirror | 不需要 | 不应需要，但可能要换/适配 transport | 不需要，但要写 transport/桥接 |
| 对现有 `LanQuickMatchManager` 的改动 | 中 | 中高 | 高 |
| 自动匹配支持 | 需要 Supabase/后端管理匹配状态 | 需要 UGS Lobby 或 Supabase 管理匹配状态 | 需要 EOS Sessions/Lobbies 或 Supabase 管理匹配状态 |
| 好友房支持 | 可通过 relay session + room code 实现 | 可通过 join code/room code 实现 | 可通过 EOS lobby/session 或自建房间码实现 |
| 普通玩家是否需要端口映射 | 不需要 | 不需要 | 不需要 |
| 服务端权威 | 否 | 否 | 否 |
| 房主断线影响 | 会影响本局 | 会影响本局 | 会影响本局 |
| 防作弊能力 | 弱于专服 | 弱于专服 | 弱于专服 |
| 免费/低价体验 | 免费试用较适合小规模 | 免费层适合小规模 | EOS 服务通常免费，但开发成本高 |
| 最大风险 | 需要后端安全创建 relay session/token | Mirror/UTP/Relay 适配复杂度 | EOS SDK + Mirror transport 工作量 |

## 5. Edgegap Relay

### 适合点

Edgegap Relay 更贴近当前项目想要的模式：

```text
玩家 A 匹配成为 host
-> 后端创建 relay session
-> 玩家 A 通过 relay 开 Mirror host/server

玩家 B 匹配加入
-> 后端分配 token/连接信息
-> 玩家 B 通过 relay 连接玩家 A
```

这和当前 LAN 1v1 的思想接近，只是把局域网直连替换成公网 relay。

### 项目中的实现方向

新增 `OnlineRelayMatchManager`，不要让 `SupabaseOnline` 继续复用 `LanQuickMatchManager`：

```text
ChooseHeroPanel
-> HeroSelectionMatchController
-> SupabaseOnline
-> OnlineRelayMatchManager.StartQuickMatch()
```

Supabase 负责慢状态：

```text
rooms
├─ room_id
├─ mode = dantiao
├─ match_type = random 或 friend
├─ host_user_id
├─ guest_user_id
├─ relay_session_id
├─ status = waiting / connecting / playing / closed
└─ created_at / updated_at
```

Edgegap 负责连接：

```text
host relay token
client relay token
relay connection endpoint
```

Mirror 继续负责：

```text
RoomPlayer
选人同步
ready
ServerChangeScene
dantiao_map 战斗同步
```

### 需要注意

Relay token/profile 不能直接在客户端随便创建和暴露。更合理的是：

```text
Unity 客户端
-> Supabase Edge Function / 自己的小后端
-> Edgegap API
-> 返回本局所需的最小连接信息
```

这样可以避免把服务端 API key 放进游戏包里。

## 6. Unity Relay

### 适合点

Unity Relay 的优势是官方生态完整，和 Unity Authentication、Lobby、Relay allocation / join code 配合清晰。对于“玩家创建房间，朋友输入码加入”的体验，它天然适配。

典型流程：

```text
Host 创建 Relay Allocation
-> 得到 join code
-> 写入 Supabase 房间或 Unity Lobby

Client 输入/匹配到 code
-> Join Allocation
-> 连接 host
```

### 当前项目的主要问题

当前项目已经用 Mirror 跑通了 LAN 房间和战斗。Unity Relay 更常见的接入路径是 Unity Transport / UGS 生态。即使存在 Mirror sample，也需要谨慎验证：

- Unity 版本是否匹配。
- Mirror 版本是否匹配。
- 当前 KCP transport 是否需要替换。
- RoomManager 场景切换是否受影响。
- 打包后连接是否稳定。

因此 Unity Relay 不是不能用，而是比 Edgegap Relay 更可能牵动 transport 层。

### 适用判断

如果项目后面决定深度接入 Unity Gaming Services，例如：

```text
Unity Authentication
Unity Lobby
Unity Relay
Cloud Save / Economy
```

则 Unity Relay 可以重新提升优先级。否则现阶段不作为第一选择。

## 7. EOS P2P

### 适合点

EOS P2P 的最大吸引力是长期成本。它适合想使用 Epic Online Services 生态，并愿意投入底层封装成本的项目。

它可以提供：

- P2P 连接。
- NAT 穿透。
- 必要时 relay。
- 跨平台账号/好友/会话生态。

### 当前项目的主要问题

EOS P2P 不等于 Unity/Mirror 的开箱即用 transport。要接到当前项目，大概率需要：

```text
Mirror Transport
-> EOS P2P SendPacket
-> EOS P2P ReceivePacket
-> socket/channel 管理
-> 连接请求/断开/错误处理
```

这会变成一项独立网络底层工程。对于当前已经在修 1v1 主流程的阶段，风险偏高。

### 适用判断

EOS P2P 可以作为后续长期成本优化方向，但不建议作为当前第一个公网 1v1 实现。

## 8. Web 后端职责

根目录下已经存在独立的 `Web/` Next.js 项目，它更适合承接公网 1v1 的匹配和 Relay 管理层。Unity 客户端不应该直接创建 Relay session，也不应该直接用客户端逻辑抢占匹配队列。

推荐职责划分：

```text
Unity 客户端
-> 只负责点击匹配、取消匹配、显示等待状态、拿到连接结果后启动 Mirror

Web/Next.js
-> 暴露匹配 API
-> 校验 Supabase 用户 token
-> 调用 Supabase RPC 做原子匹配
-> 调用 Edgegap / Unity Relay / EOS 创建或加入 relay session
-> 返回本局最小连接信息

Supabase Postgres
-> 存储 match_queue、match_rooms、relay_sessions
-> 通过 RPC 保证匹配操作原子性

Relay 服务
-> 解决公网/NAT 连接

Mirror
-> 负责进入房间后的 RoomPlayer、ready、场景切换和战斗同步
```

### Web API 建议

MVP 阶段优先做随机 1v1 匹配：

```text
POST /api/match/start
GET  /api/match/status?ticketId=xxx
POST /api/match/cancel
```

后续好友房复用同一套连接层：

```text
POST /api/rooms/create
POST /api/rooms/join
POST /api/rooms/close
```

Unity 侧只和 Web API 通信：

```text
OnlineRelayMatchManager.StartQuickMatch()
-> POST /api/match/start
-> 返回 waiting 或 matched

waiting
-> 每 1 秒 GET /api/match/status

matched
-> 使用 relay connection info
-> Mirror StartHost / StartClient
```

### Supabase 表建议

当前 `rooms` 表仍是旧直连模型，字段以 `host_ip / host_tcp_port / host_udp_port` 为主。Relay 方案下应新增或替换为匹配模型：

```text
match_queue
├─ id
├─ user_id
├─ mode = dantiao
├─ hero_id
├─ skin_id
├─ match_type = random / friend
├─ room_code nullable
├─ status = waiting / matched / canceled / expired
├─ created_at
└─ updated_at

match_rooms
├─ id
├─ mode = dantiao
├─ host_user_id
├─ guest_user_id
├─ host_hero_id
├─ guest_hero_id
├─ host_skin_id
├─ guest_skin_id
├─ status = connecting / playing / closed
├─ relay_provider = edgegap / unity / eos
├─ relay_session_id
├─ room_code nullable
├─ protocol_version
├─ created_at
└─ updated_at

relay_sessions
├─ id
├─ provider
├─ provider_session_id
├─ host_connection_info
├─ guest_connection_info
├─ expires_at
└─ created_at
```

### 原子匹配要求

随机匹配必须放到 Supabase RPC 或等价的服务端事务里做，不能由 Unity 客户端或普通前端流程执行“先查 waiting，再 update”。正确流程：

```text
join_random_match()
1. 开启事务
2. 查找一个 waiting 对手，并锁住该行
3. 如果找到：创建 match_room，更新双方 queue 状态
4. 如果没找到：插入自己的 waiting queue
5. 提交事务
```

这样可以避免两个玩家同时抢到同一个 waiting 玩家。

### 密钥边界

Relay 服务密钥只能放在 `Web/` 服务端环境变量里：

```env
SUPABASE_URL=
SUPABASE_SECRET_KEY=
EDGEGAP_API_TOKEN=
UNITY_RELAY_SERVICE_KEY=
EOS_CLIENT_SECRET=
```

不要放进：

```env
NEXT_PUBLIC_*
Unity Resources
Unity ScriptableObject
客户端配置文件
```

`NEXT_PUBLIC_*` 会进入浏览器包，Unity 包也可以被反编译，因此都不能保存 Relay 管理密钥。

### 当前 Web 项目落点

当前 `Web/` 已经具备基础：

```text
Web/package.json
Web/app
Web/lib/supabase
Web/supabase/migrations
Web/vercel.json
```

建议新增：

```text
Web/app/api/match/start/route.ts
Web/app/api/match/status/route.ts
Web/app/api/match/cancel/route.ts
Web/supabase/migrations/<timestamp>_add_matchmaking.sql
```

如果后续选择 Edgegap Relay，再新增：

```text
Web/lib/relay/edgegap.ts
```

这样 Unity 主项目、Web 后端、Supabase 数据库可以独立维护。

## 9. 推荐架构

推荐先做一个抽象层，把公网连接和匹配状态从 LAN 逻辑中拆出来：

```text
IOnlineMatchConnector
├─ StartMatch(GameModes mode, int heroId, int skinId)
├─ CancelMatch()
└─ JoinMatchedRoom(RoomInfo room)

LanQuickMatchManager
└─ 只负责 LAN broadcast / LAN Mirror host-client

OnlineRelayMatchManager
└─ 负责 SupabaseOnline 的 relay 匹配/连接
```

`ChooseHeroPanel` 不关心底层是哪种网络：

```text
LocalServer -> MsgMatchRequest
LanHost/LanClient -> LanQuickMatchManager
SupabaseOnline -> OnlineRelayMatchManager
```

这样以后可以替换：

```text
Edgegap Relay
-> Unity Relay
-> EOS P2P
-> Dedicated Server
```

而不需要重写选人 UI 和战斗入口。

## 10. 分阶段计划

### Phase 1：抽象在线匹配入口

- 从 `LanQuickMatchManager` 中拆出在线入口。
- 新增 `OnlineRelayMatchManager` 空实现/模拟实现。
- `SupabaseOnline` 不再调用 LAN broadcast 搜索。
- 保持当前 LAN 逻辑不变。
- Unity 侧只预留调用 Web API 的入口，不直接操作 Relay 管理密钥。

验收：

```text
LanHost/LanClient 仍然能局域网 1v1
SupabaseOnline 进入独立在线匹配流程
```

### Phase 2：Edgegap Relay 原型

- Web API 先只负责 Supabase 原子匹配，返回 `roomId` 和 `role`。
- Unity 使用 Mirror 自带 `EdgegapLobbyKcpTransport`。
- Host 用 `roomId` 作为 Edgegap lobby name 创建 lobby。
- Guest 用 `roomId` 查询 Edgegap lobby list，找到 `lobby_id` 后加入。
- Mirror 通过 Edgegap lobby/relay transport 连接。
- 成功进入 `dantiao_map`。

验收：

```text
两台不同网络下的机器可以直接匹配进入 1v1
玩家不需要端口映射
```

当前代码落点：

```text
ChooseHeroPanel
-> OnlineRelayMatchManager
-> OnlineMatchApiClient
-> https://aoyi-web.vercel.app/api/match/*
-> OnlineRelayConnector
-> EdgegapLobbyKcpTransport
-> AoyiNetworkRoomManager.StartHost / StartClient
```

运行前必须在 `Resources/SupabaseConfig.asset` 填入：

```text
OnlineMatchApiBaseUrl = https://aoyi-web.vercel.app
EdgegapLobbyUrl = Edgegap Lobby Service URL
```

`EdgegapLobbyUrl` 不是 Edgegap API key，也不是 Web 地址。它是通过 Edgegap Lobby Service 部署后得到的公开 lobby service URL。API key 仍然不能放进 Unity 客户端。

### Phase 3：好友房复用同一连接层

- 增加 `match_type = friend`。
- 创建房间生成 room code。
- 朋友输入 room code 加入。
- 连接层仍复用 Phase 2 的 Relay。

验收：

```text
随机匹配和好友房都能进入同一套 dantiao_map 战斗
```

### Phase 4：评估是否迁移或增加方案

如果 Edgegap Relay 成本或体验不合适，再评估：

- Unity Relay：更深接 UGS。
- EOS P2P：降低长期服务成本。
- Dedicated Server：提高公平性和稳定性。

## 11. 当前决策

当前 1v1 公网方案建议：

```text
首选：Edgegap Relay
备选：Unity Relay
长期研究：EOS P2P
```

当前不建议：

- 为了 1v1 立刻换 Photon。
- 把 1v1 和未来 5v5 强行绑定到同一套公网实现。
- 继续让 `SupabaseOnline` 复用 LAN broadcast 匹配逻辑。

下一步代码层面优先做：

```text
SupabaseOnline -> OnlineRelayMatchManager
OnlineRelayMatchManager -> Web API
Web API -> Supabase RPC + Relay API
LAN -> LanQuickMatchManager
选人 UI -> 只依赖抽象匹配入口
```

这样项目可以先稳定 1v1 玩家公网匹配，再单独规划 5v5。

## 12. 参考链接

- Unity Relay pricing: https://unity.com/products/gaming-services/pricing
- Unity Relay get started: https://docs.unity.com/en-us/relay/get-started
- Unity Relay Mirror sample: https://docs.unity.com/en-us/relay/mirror
- Edgegap pricing: https://edgegap.com/resources/pricing
- Edgegap Distributed Relay: https://docs.edgegap.com/learn/distributed-relay
- EOS P2P: https://dev.epicgames.com/docs/game-services/p-2-p
