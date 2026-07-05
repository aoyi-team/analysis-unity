using System;
using Newtonsoft.Json;

/// <summary>
/// Supabase profiles 表 DTO。
/// </summary>
[Serializable]
public class SupabaseProfileDto
{
    [JsonProperty("id")] public string Id;
    [JsonProperty("username")] public string Username;
    [JsonProperty("email")] public string Email;
    [JsonProperty("created_at")] public DateTime CreatedAt;
    [JsonProperty("last_login")] public DateTime LastLogin;
}

/// <summary>
/// Supabase rooms 表 DTO。
/// </summary>
[Serializable]
public class SupabaseRoomDto
{
    [JsonProperty("id")] public string Id;
    [JsonProperty("room_code")] public string RoomCode;
    [JsonProperty("mode")] public string Mode;
    [JsonProperty("host_ip")] public string HostIp;
    [JsonProperty("host_tcp_port")] public int HostTcpPort;
    [JsonProperty("host_udp_port")] public int HostUdpPort;
    [JsonProperty("max_players")] public int MaxPlayers;
    [JsonProperty("current_players")] public int CurrentPlayers;
    [JsonProperty("status")] public string Status;
    [JsonProperty("protocol_version")] public int ProtocolVersion;
    [JsonProperty("created_at")] public DateTime CreatedAt;
    [JsonProperty("updated_at")] public DateTime UpdatedAt;
}
