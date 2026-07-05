# Supabase 数据库 Schema

> 配套 Phase 3 的轻量级 REST 客户端使用。请在 Supabase Dashboard 的 SQL Editor 中顺序执行以下脚本。

## 1. profiles 表

存储玩家基础资料，与 `auth.users` 一对一关联。

```sql
CREATE TABLE profiles (
    id UUID PRIMARY KEY REFERENCES auth.users(id) ON DELETE CASCADE,
    username TEXT NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    last_login TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);
```

## 2. rooms 表

存储在线房间列表，供局域网外的玩家发现并加入。

```sql
CREATE TABLE rooms (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    room_code TEXT UNIQUE NOT NULL,
    mode TEXT NOT NULL,
    host_ip TEXT NOT NULL,
    host_tcp_port INTEGER NOT NULL,
    host_udp_port INTEGER NOT NULL,
    max_players INTEGER NOT NULL DEFAULT 2,
    current_players INTEGER NOT NULL DEFAULT 0,
    status TEXT NOT NULL DEFAULT 'Waiting',
    protocol_version INTEGER NOT NULL DEFAULT 1,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);
```

## 3. 行级安全策略（RLS）

```sql
ALTER TABLE profiles ENABLE ROW LEVEL SECURITY;
ALTER TABLE rooms ENABLE ROW LEVEL SECURITY;

-- profiles
CREATE POLICY "Public profiles are viewable by everyone"
    ON profiles FOR SELECT USING (true);

CREATE POLICY "Users can insert own profile"
    ON profiles FOR INSERT WITH CHECK (auth.uid() = id);

CREATE POLICY "Users can update own profile"
    ON profiles FOR UPDATE USING (auth.uid() = id);

-- rooms
CREATE POLICY "Rooms are viewable by everyone"
    ON rooms FOR SELECT USING (true);

CREATE POLICY "Users can insert rooms"
    ON rooms FOR INSERT WITH CHECK (auth.uid() IS NOT NULL);

CREATE POLICY "Users can update rooms"
    ON rooms FOR UPDATE USING (auth.uid() IS NOT NULL);
```

## 4. 字段约定

| 字段 | 说明 |
| --- | --- |
| `room_code` | 6 位大写字母数字，由客户端生成，作为房间显示名称 |
| `mode` | 游戏模式字符串，与 `GameModes` 枚举一致，如 `dantiao`、`paiwei` |
| `status` | 房间状态字符串，与 `RoomStatus` 枚举一致：`Waiting`、`Playing`、`Full`、`Closed` |
| `protocol_version` | 协议版本号，用于过滤不兼容房间，默认 `1` |
| `updated_at` | 心跳字段，客户端通过 PATCH 更新为当前时间，后端可定时清理过期房间 |

## 5. 客户端对应关系

| 服务端对象 | 客户端文件 |
| --- | --- |
| `profiles` 表 | `SupabaseModels.cs` 中的 `SupabaseProfileDto` |
| `rooms` 表 | `SupabaseModels.cs` 中的 `SupabaseRoomDto` |
| Auth API | `SupabaseAuthClient.cs` |
| REST CRUD | `SupabaseRestClient.cs` |
| 业务接口 | `SupabaseBackendProvider.cs` |
