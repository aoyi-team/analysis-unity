using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

[Serializable]
public class OnlineMatchStartRequest
{
    [JsonProperty("mode")] public string Mode;
    [JsonProperty("heroId")] public int HeroId;
    [JsonProperty("skinId")] public int SkinId;
    [JsonProperty("matchType")] public string MatchType = "random";
    [JsonProperty("protocolVersion")] public int ProtocolVersion = ServerConfig.ProtocolVersion;
    [JsonProperty("roomCode", NullValueHandling = NullValueHandling.Ignore)] public string RoomCode;
}

[Serializable]
public class OnlineMatchCancelRequest
{
    [JsonProperty("ticketId")] public string TicketId;
}

[Serializable]
public class OnlineMatchResponse
{
    [JsonProperty("status")] public string Status;
    [JsonProperty("ticketId")] public string TicketId;
    [JsonProperty("roomId")] public string RoomId;
    [JsonProperty("role")] public string Role;
    [JsonProperty("room")] public JObject Room;
}

[Serializable]
public class OnlineMatchCancelResponse
{
    [JsonProperty("canceled")] public bool Canceled;
    [JsonProperty("ticketId")] public string TicketId;
}

public class OnlineMatchApiResult<T>
{
    public bool Success;
    public T Data;
    public string ErrorMessage;
    public int StatusCode;
}
