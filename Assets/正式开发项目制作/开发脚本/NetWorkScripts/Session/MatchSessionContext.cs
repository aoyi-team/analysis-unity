public sealed class MatchSessionContext
{
    public MatchSessionPhase Phase { get; private set; } = MatchSessionPhase.Idle;
    public int Generation { get; private set; }
    public NetworkMode NetworkMode { get; private set; } = NetworkMode.LocalServer;
    public GameModes GameMode { get; private set; }
    public string RoomId { get; private set; }
    public string Role { get; private set; }

    public bool IsOnline => NetworkMode == NetworkMode.SupabaseOnline;

    public int BeginSearch(NetworkMode networkMode, GameModes gameMode)
    {
        Generation++;
        NetworkMode = networkMode;
        GameMode = gameMode;
        RoomId = null;
        Role = null;
        Phase = MatchSessionPhase.Searching;
        return Generation;
    }

    public bool TryMarkMatched(int generation, string roomId, string role)
    {
        if (generation != Generation
            || Phase != MatchSessionPhase.Searching
            || string.IsNullOrWhiteSpace(roomId))
        {
            return false;
        }

        RoomId = roomId;
        Role = role;
        Phase = MatchSessionPhase.Matched;
        return true;
    }

    public bool TryBeginConnecting()
    {
        if (Phase != MatchSessionPhase.Matched)
        {
            return false;
        }

        Phase = MatchSessionPhase.Connecting;
        return true;
    }

    public void MarkInRoom()
    {
        if (Phase == MatchSessionPhase.Connecting || Phase == MatchSessionPhase.Matched)
        {
            Phase = MatchSessionPhase.InRoom;
        }
    }

    public void MarkInBattle()
    {
        if (Phase != MatchSessionPhase.Idle && Phase != MatchSessionPhase.Ending)
        {
            Phase = MatchSessionPhase.InBattle;
        }
    }

    public bool TryBeginEnding()
    {
        if (Phase == MatchSessionPhase.Idle || Phase == MatchSessionPhase.Ending)
        {
            return false;
        }

        Phase = MatchSessionPhase.Ending;
        Generation++;
        return true;
    }

    public void Reset()
    {
        Generation++;
        Phase = MatchSessionPhase.Idle;
        RoomId = null;
        Role = null;
        GameMode = default;
    }
}
