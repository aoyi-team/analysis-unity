# 联机系统 V1 计划：Supabase 账号/战绩 + 局域网房间发现 + Host/Client

## 1. 目标

V1 先实现一个低云端流量、可玩的局域网联机版本：

- Supabase 只负责账号、玩家资料、战绩和排行榜。
- 房间列表不走 Supabase，不做云端房间心跳，不轮询云端房间表。
- 局域网房间是否存在，靠 UDP Broadcast 或 Mirror Network Discovery。
- 局内实时同步使用 NGO 或 Mirror，由开房玩家作为 Host，其它玩家作为 Client 直连。
- V1 不解决公网 NAT 穿透，不做强防作弊，优先保证同一局域网内能发现房间、加入房间、开始游戏、结束结算。

## 2. 当前项目判断

项目已有一套 `Aoyi_TCPServer` 自定义服务端，包含 TCP 登录、匹配、房间、UDP 帧同步雏形。新的 V1 方案不是继续扩展这套自定义服务端，而是拆成三块：

- 云端慢数据：交给 Supabase，只保留账号、资料、战绩、排行榜。
- 局域网房间发现：交给 UDP Broadcast 或 Mirror Network Discovery。
- 局内实时同步：交给 NGO 或 Mirror。

这样可以避免 Supabase 房间轮询和心跳请求，降低云端请求量，也更符合局域网联机的实际判断方式。原 `Aoyi_TCPServer` 可以先保留，作为后续公网战斗服或服务端权威方案的参考。

## 3. 总体架构

```text
Unity 客户端
├─ AuthClient             -> Supabase Auth
├─ ProfileClient          -> Supabase profiles
├─ LanRoomDiscovery       -> UDP Broadcast 或 Mirror Network Discovery
├─ RoomSession            -> 当前局域网房间本地状态
├─ NetworkGameManager     -> NGO 或 Mirror Host/Client
└─ ResultReporter         -> Supabase match_results / player_stats

Supabase
├─ auth.users
├─ profiles
├─ match_results
└─ player_stats，可在 V1.5 或 V2 加

局域网发现
Host 玩家
└─ 每 1-2 秒广播 room_id、room_name、host_local_ip、port、player_count

Client 玩家
└─ 监听局域网广播，收到广播后显示房间

局内实时
Host 玩家
└─ NGO/Mirror Server + Client

Client 玩家
└─ NGO/Mirror Client -> host_local_ip:port
```

## 4. 核心原则

### Supabase 不做房间存活判断

Supabase 不能可靠判断玩家是否在同一个局域网，也不适合作为局域网房间心跳服务器。V1 不使用 Supabase `rooms` 和 `room_players` 表来维护房间列表。

房间是否存在按下面规则判断：

```text
最近几秒内收到局域网广播 = 房间看起来存在
点击加入后 NGO/Mirror 连接成功 = 房间真的可加入
```

### 房间列表不消耗 Supabase 请求量

V1 的房间发现完全在局域网内完成：

```text
Host 开房 -> 局域网广播
Client 找房 -> 局域网监听
房间消失 -> 几秒内收不到广播后从列表移除
```

Supabase 只在这些节点使用：

```text
登录/注册
读取或更新玩家资料
游戏结束上报战绩
读取排行榜
```

### 局内状态不走 Supabase

局内位置、血量、技能、聊天、拾取、死亡、胜负同步都走 NGO 或 Mirror，不写 Supabase。Supabase 只接收最终结果。

## 5. 技术选择

### 推荐选择：NGO + 自定义 UDP Broadcast

V1 推荐使用 NGO 做局内同步，并用一个很小的 UDP Broadcast 模块做局域网房间发现：

- NGO 是 Unity 官方方案，和 Unity Transport 配套。
- 适合直接 IP + 端口连接的 Host/Client 模式。
- 后续接 Unity Relay 时迁移路径更自然。
- 自定义 UDP Broadcast 只负责广播房间元数据，不碰局内同步。

### Mirror 备选：Mirror + Network Discovery

如果团队更看重现成房间发现和社区案例，可以选择 Mirror。Mirror Network Discovery 可以直接承担局域网发现，Mirror 自身负责局内 Host/Client 同步。

### 不推荐 V1 继续手写完整 UDP 同步

当前手写 TCP/UDP 框架已有基础，但要补完断线、重连、同步对象、场景切换、Host 权威、错误恢复，成本会比直接接 NGO/Mirror 更高。V1 目标是先把局域网联机跑通。

## 6. 局域网房间发现设计

### Host 广播内容

Host 创建房间后，每 1-2 秒向局域网广播一份轻量房间信息。这个广播只在局域网内传播，不消耗 Supabase 流量。

建议广播字段：

```json
{
  "protocol": "aoyi_lan_room_v1",
  "room_id": "host-user-id_1780000000_4932",
  "room_name": "玩家A的房间",
  "host_user_id": "supabase-user-id",
  "host_nickname": "玩家A",
  "host_local_ip": "192.168.1.20",
  "port": 7777,
  "game_mode": "dantiao",
  "current_players": 1,
  "max_players": 2,
  "is_playing": false,
  "timestamp": 1780000000
}
```

字段说明：

- `room_id` 是本地房间 id，用于局域网发现和战绩关联，不需要提前写入 Supabase。
- `room_id` 使用复合格式：`{host_user_id}_{unix_timestamp}_{random_4位}`。这样比纯随机 id 更方便排查，也能避免同一 Host 短时间内重开房间时误复用 id。
- `host_user_id` 来自 Supabase 登录用户，用于战绩上报时识别 Host。
- `host_local_ip` 是 Host 的局域网 IP。
- `port` 是 NGO/Mirror 监听端口。
- `is_playing = true` 时，Client 可以显示房间正在游戏中，不允许加入。

`room_id` 示例：

```text
room_id = "{host_user_id}_{unix_timestamp}_{random_4位}"
room_id = "7b7f4c91-1d4d-4f0a-9f6b-123456789abc_1780000000_4932"
```

### Client 房间列表

Client 不请求 Supabase 房间列表，而是监听局域网广播：

```text
1. 打开联机大厅。
2. LanRoomDiscovery 开始监听广播。
3. 收到 room_id 后加入本地房间列表。
4. 同一个 room_id 后续广播只更新 player_count、is_playing、last_seen_at。
5. 超过 3-5 秒没有再次收到某个 room_id 的广播，就从房间列表移除。
```

房间存在的判断：

```text
last_seen_at <= 5 秒：显示房间
last_seen_at > 5 秒：移除房间
点击加入失败：立即移除或标记为不可连接
```

### 加入房间

加入流程以实际连接为准：

```text
1. 玩家点击局域网发现到的房间。
2. RoomSession 记录 room_id、host_local_ip、port、host_user_id。
3. NetworkGameManager.StartClient(host_local_ip, port)。
4. 连接成功后进入等待房间或战斗准备界面。
5. 连接失败时提示“房间已失效或 Host 不可达”，并移除该房间。
```

## 7. Unity 模块规划

### AuthClient

职责：

- 登录、注册、登出。
- 保存 Supabase access token。
- 获取当前用户 id。

对外接口示例：

```text
Login(email, password)
Register(email, password, nickname)
Logout()
GetCurrentUser()
```

### ProfileClient

职责：

- 读取玩家昵称、等级、经验。
- 更新玩家昵称或基础资料。

对外接口示例：

```text
GetProfile(userId)
UpdateNickname(nickname)
```

### LanRoomDiscovery

职责：

- Host 广播房间。
- Client 监听房间。
- 维护本地房间列表。
- 房间超时移除。

对外接口示例：

```text
StartBroadcast(roomInfo)
UpdateBroadcast(roomInfo)
StopBroadcast()
StartDiscovery()
StopDiscovery()
GetVisibleRooms()
OnRoomFound(roomInfo)
OnRoomExpired(roomId)
```

### RoomSession

职责：

- 保存当前房间运行状态。
- 缓存 `room_id`、`host_user_id`、`host_local_ip`、`port`、`game_mode`、玩家列表、自己是否是 Host。
- 作为局域网发现和局内网络状态之间的桥。

状态示例：

```text
Idle
HostingRoom
DiscoveringRooms
ConnectingToHost
WaitingInRoom
Playing
Finished
Disconnected
```

### NetworkGameManager

职责：

- 封装 NGO 或 Mirror 的启动和关闭。
- Host 调用 StartHost。
- Client 调用 StartClient。
- 管理连接失败、断线、场景切换。
- 创建玩家对象和局内同步对象。

对外接口示例：

```text
StartHost(port)
StartClient(hostLocalIp, port)
StopNetwork()
LoadBattleScene()
ReturnToLobby()
```

### ResultReporter

职责：

- Host 在游戏结束后上报战绩。
- Client 可以上报自己的本地确认结果，但 V1 以 Host 上报为准。
- 上报失败时本地暂存，回到大厅后重试一次。

对外接口示例：

```text
ReportMatchResult(roomId, result)
RetryPendingResult()
GetLeaderboard()
```

## 8. Supabase 表结构草案

V1 不创建 Supabase 房间表。房间只存在于局域网广播和当前游戏会话中。

### profiles

玩家资料表。

```sql
create table profiles (
  id uuid primary key references auth.users(id) on delete cascade,
  nickname text not null,
  level int not null default 1,
  exp int not null default 0,
  created_at timestamptz not null default now(),
  updated_at timestamptz not null default now()
);
```

### match_results

对局结果表。`room_id` 使用局域网 Host 生成的本地房间 id，不引用 Supabase 房间表。

```sql
create table match_results (
  id uuid primary key default gen_random_uuid(),
  room_id text not null,
  reporter_user_id uuid not null references auth.users(id) on delete cascade,
  host_user_id uuid not null references auth.users(id) on delete cascade,
  winner_user_id uuid references auth.users(id),
  game_mode text not null,
  result_json jsonb not null,
  started_at timestamptz not null,
  ended_at timestamptz not null,
  created_at timestamptz not null default now()
);
```

`result_json` 可以包含：

```json
{
  "players": [
    {
      "user_id": "uuid",
      "nickname": "玩家A",
      "team_id": 1,
      "hero_id": 101,
      "kills": 3,
      "deaths": 1,
      "score": 1200
    }
  ],
  "duration_seconds": 420,
  "finish_reason": "normal"
}
```

### player_stats

V1 可以先不建，排行榜从 `match_results` 聚合。若聚合性能或排行榜功能变复杂，再加缓存表：

```sql
create table player_stats (
  user_id uuid primary key references auth.users(id) on delete cascade,
  total_matches int not null default 0,
  wins int not null default 0,
  losses int not null default 0,
  score int not null default 0,
  updated_at timestamptz not null default now()
);
```

## 9. 房间生命周期

### 创建房间

```text
1. 玩家登录 Supabase。
2. 点击创建房间。
3. 客户端生成本地 room_id，格式为 `{host_user_id}_{unix_timestamp}_{random_4位}`。
4. 客户端获取本机局域网 IP。
5. NetworkGameManager.StartHost(port)。
6. LanRoomDiscovery.StartBroadcast(roomInfo)。
7. RoomSession 进入 HostingRoom / WaitingInRoom。
```

这个流程不写 Supabase 房间表。

### 发现房间

```text
1. Client 打开联机大厅。
2. LanRoomDiscovery.StartDiscovery()。
3. 收到 Host 广播后显示房间。
4. 持续收到广播则刷新房间信息。
5. 超过 3-5 秒未收到广播则移除房间。
```

### 加入房间

```text
1. Client 点击房间。
2. 读取广播里的 host_local_ip 和 port。
3. NetworkGameManager.StartClient(host_local_ip, port)。
4. 连接成功后进入等待房间。
5. 连接失败则提示“房间已失效或 Host 不可达”。
```

### 开始游戏

```text
1. Host 检查人数和 ready 状态。
2. Host 将广播中的 is_playing 更新为 true。
3. Host 通过 NGO/Mirror 通知所有 Client 切换战斗场景。
4. NetworkGameManager 创建玩家对象。
5. 局内正式开始。
```

### 结束游戏

```text
1. Host 判定胜负。
2. ResultReporter.ReportMatchResult 写入 Supabase match_results。
3. Host 停止局域网广播。
4. 所有玩家 StopNetwork。
5. 返回大厅。
```

### 关闭房间

Host 在大厅退出：

```text
1. LanRoomDiscovery.StopBroadcast()。
2. StopNetwork。
3. Client 几秒内收不到广播后自动移除房间。
```

Client 在大厅退出：

```text
1. StopNetwork。
2. 返回局域网房间列表。
```

## 10. 局内同步设计

### FPS 与网络 Tick

V1 不同步渲染 FPS。每个客户端可以按自己的设备性能渲染 60 FPS、90 FPS 或 120 FPS。

局内网络同步使用固定网络 Tick：

```text
渲染 FPS：客户端本地画面刷新频率，不参与网络一致性。
网络 Tick：局内网络同步频率，由 Host 统一驱动。
```

推荐初始参数：

```text
网络 Tick：20 tick/s 或 30 tick/s
20 tick/s：每 50ms 同步一次，带宽更省，V1 推荐先用这个。
30 tick/s：每 33ms 同步一次，手感更好，流量略高。
```

局域网延迟较低，V1 先用 20 tick/s 足够。后续如果动作手感要求更高，再调到 30 tick/s。

### 同步边界

UDP Broadcast 只负责局域网房间发现，不负责游戏状态同步。

NGO/Mirror 负责局内同步，包括：

```text
玩家输入
玩家位置
朝向
动画状态
血量
技能状态
攻击事件
拾取事件
死亡和胜负
ready 状态
开始游戏
结束游戏
```

不要让 UDP Broadcast 和 NGO/Mirror 同时同步同一份局内状态，避免双写状态。

### Host 权威原则

V1 虽然是玩家 Host，但局内仍按 Host 权威设计：

```text
Client 发送输入或请求。
Host 校验是否合法。
Host 修改真实状态。
Host 广播状态或事件。
Client 播放表现。
```

不要让 Client 直接决定伤害、胜负、拾取结果。

### 输入上行

Client 每个网络 Tick 或输入变化时，把玩家输入发送给 Host：

```text
move_x
move_y
aim_x
aim_y
is_moving
attack_pressed
skill_id
input_seq
client_tick
```

NGO 对应：

```text
Client -> Host：ServerRpc
```

Mirror 对应：

```text
Client -> Host：Command
```

输入包只表达“我按了什么”，不表达“我造成了多少伤害”。

### Host 处理

Host 以固定网络 Tick 处理输入：

```text
1. 收集所有 Client 输入。
2. 校验输入是否合法。
3. 移动角色。
4. 判断攻击、技能、拾取、碰撞。
5. 更新血量、死亡、分数、胜负。
6. 生成权威状态快照。
7. 广播给所有 Client。
```

### 状态下行

Host 每个网络 Tick 或隔若干 Tick 广播权威状态：

```text
player_id
server_tick
position
velocity
facing
anim_state
血量
能量
分数
队伍
倒计时
玩家存活状态
```

NGO 对应：

```text
Host -> Clients：ClientRpc / NetworkVariable / NetworkTransform
```

Mirror 对应：

```text
Host -> Clients：ClientRpc / SyncVar / NetworkTransform
```

V1 可以先用 NetworkTransform 同步位置，用 NetworkVariable/SyncVar 同步血量和分数。等手感需要优化时，再替换为自定义 snapshot。

### 本地预测与远程插值

V1 简化版：

```text
本地玩家：可以先直接用 Host 回传状态驱动，局域网延迟低，能先跑通。
远程玩家：使用 NetworkTransform 或快照插值，避免移动抖动。
```

V1.5 增强版：

```text
本地玩家：
1. Client 本地立即根据输入移动。
2. 同时把输入发给 Host。
3. Host 返回权威位置。
4. 误差小时平滑修正。
5. 误差大时拉回权威位置。

远程玩家：
1. 保存 Host 快照队列。
2. 延迟约 100ms 播放。
3. 在两个快照之间插值。
```

### 高频状态

适合 NetworkTransform 或自定义 snapshot：

```text
位置
速度
朝向
移动状态
动画状态
```

### 可靠状态

适合 NetworkVariable / SyncVar 或可靠 RPC：

```text
血量
能量
分数
队伍
倒计时
玩家存活状态
```

### 事件同步

适合用 ServerRpc + ClientRpc：

```text
攻击请求
技能释放请求
拾取请求
受伤事件
死亡事件
聊天消息
```

示例流程：

```text
Client: RequestAttackServerRpc(direction, skillId)
Host: 检查冷却、距离、命中目标
Host: 修改目标血量
Host: PlayAttackClientRpc(attackerId, targetId, damage)
```

Mirror 中对应：

```text
Client: CmdRequestAttack(direction, skillId)
Host: 检查冷却、距离、命中目标
Host: 修改目标血量
Host: RpcPlayAttack(attackerId, targetId, damage)
```

## 11. 错误和异常处理

### 房间消失

如果 Client 超过 3-5 秒没有收到某个 `room_id` 的局域网广播：

- 从房间列表移除。
- 如果玩家正在查看该房间详情，提示“房间已关闭或 Host 不可达”。

### Client 连接失败

处理方式：

```text
1. 显示“房间已失效或 Host 不可达”。
2. StopNetwork。
3. 从本地房间列表移除或标记为不可连接。
4. RoomSession 回到 DiscoveringRooms。
```

### Host 退出

V1 直接解散房间：

```text
1. Host 停止广播。
2. Host StopNetwork。
3. 所有 Client 收到断线。
4. Client 返回大厅并提示房主已离开。
```

### 游戏中 Client 掉线

V1 简化处理：

```text
1. Host 标记该玩家 disconnected。
2. 角色原地不动或直接判负。
3. 对局可继续。
```

### 战绩上报失败

处理方式：

```text
1. 本地保存 pending result。
2. 返回大厅后自动重试一次。
3. 仍失败则提示玩家。
```

## 12. Supabase RLS 规则原则

必须开启 RLS。客户端只能使用 publishable/anon key，不能把 service role key 放进 Unity。

建议规则：

```text
profiles:
- 登录用户可读取公开资料。
- 用户只能更新自己的资料。

match_results:
- 登录用户可以插入自己作为 reporter_user_id 的结果。
- V1 限制只能由 Host 上报自己主持的局域网对局结果。
- 登录用户可以读取排行榜需要的结果数据。

player_stats:
- 登录用户可以读取排行榜数据。
- 若由客户端更新，必须限制只能更新自己的统计。
- 更推荐后续用数据库函数或服务端逻辑从 match_results 聚合更新。
```

V1 不依赖 Supabase 判断房间是否存在，也不把局域网 IP 当作权限依据。

### match_results RLS 示例

V1 先限制“只能上报自己当 Host 的对局”。这样可以避免普通 Client 任意伪造战绩。

```sql
alter table match_results enable row level security;

create policy "Host can report their own match"
  on match_results
  for insert
  to authenticated
  with check (
    (select auth.uid()) = reporter_user_id
    and (select auth.uid()) = host_user_id
  );

create policy "Authenticated users can read match results"
  on match_results
  for select
  to authenticated
  using (true);
```

这条规则只证明“当前登录用户就是上报者，并且上报者声明自己是 Host”。V1 的安全边界仍然是信任 Host；如果后续要防作弊，需要改成独立 Battle Server 或服务端校验战绩。

## 13. Supabase 请求预算

V1 的请求集中在低频节点：

```text
登录/注册：玩家主动操作时
读取 profiles：进入大厅或展示玩家信息时
上报 match_results：每局结束 1 次
读取排行榜：玩家打开排行榜时
```

不产生以下请求：

```text
不轮询 Supabase 房间列表
不写 Supabase 房间心跳
不频繁更新房间人数
不把 ready 状态写 Supabase
不把局内状态写 Supabase
```

## 14. 里程碑

### Milestone 1：Supabase 基础

- 创建 Supabase 项目。
- 建 `profiles` 和 `match_results` 表。
- 配置 RLS。
- Unity 可登录、注册、读取 profiles。

### Milestone 2：局域网房间发现

- 实现 `LanRoomDiscovery`。
- Host 可广播房间。
- Client 可发现房间。
- 超时未收到广播的房间可自动移除。

### Milestone 3：局域网连接

- 接入 NGO 或 Mirror。
- Host 创建房间后 StartHost。
- Client 从局域网广播读取 host_local_ip:port 后 StartClient。
- 两台局域网设备可以连上同一房间。

### Milestone 4：开始游戏

- 房间 ready 状态走 NGO/Mirror。
- Host 点击开始。
- 所有人切换到战斗场景。
- 玩家对象生成并能移动同步。

### Milestone 5：局内基础玩法同步

- 确定网络 Tick，V1 推荐 20 tick/s。
- Client 通过 ServerRpc/Command 上报输入。
- Host 按固定 Tick 处理输入并广播权威状态。
- 同步位置、动画、血量。
- 攻击/技能走 Host 校验。
- 死亡和胜负由 Host 判定。

### Milestone 6：战绩和排行榜

- Host 上报 match_results。
- 大厅展示个人历史和排行榜。
- 上报失败可重试。

## 15. 后续升级路线

### V1.5

- 更完整的断线重连。
- Host 迁移暂不建议做，复杂度较高。
- 增加玩家延迟显示。
- 增加房间密码、好友房、观战限制。
- 给局域网广播增加版本号、签名字段或简单校验码，减少误识别。

### V2

- 接入 Unity Relay，支持公网联机。
- Supabase 可以增加 relay_join_code 或 relay_alloc_id，用于公网房间入口。
- 房间连接方式从 lan 扩展为 lan/direct/relay。

### V3

- 独立 Battle Server。
- 服务端权威结算。
- 防作弊校验。
- 匹配队列、段位、赛季、回放。

## 16. 待确认问题

1. V1 最终选择 NGO 还是 Mirror。
2. UDP Broadcast 的广播端口和 NGO/Mirror 的游戏端口分别使用多少。
3. 房间最大人数和主要模式。
4. 战绩是否允许 V1 先信任 Host。
5. 是否保留旧 `Aoyi_TCPServer` 作为后续公网服务端方案。
6. 局域网发现是否需要显示 Host 昵称、玩家人数、模式、延迟等信息。

## 17. 推荐结论

V1 使用 Supabase Auth/Profile/Result + 局域网房间发现 + NGO Host/Client：

- Supabase 只管账号、资料、战绩和排行榜。
- Supabase 不保存局域网房间列表，不做房间心跳。
- 房间是否存在靠局域网广播。
- 房间能否加入靠 NGO/Mirror 实际连接。
- NGO 或 Mirror 负责局内实时同步。
- 局内不按渲染 FPS 同步，按固定网络 Tick 同步，V1 推荐 20 tick/s。
- Host 作为局内权威。
- 不做公网和强防作弊。
- 保留升级到 Unity Relay 或独立 Battle Server 的结构空间。
