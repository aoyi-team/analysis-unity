using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 内嵌主机的简化房间管理
/// </summary>
public class EmbeddedRoom
{
    private readonly List<EmbeddedClientState> _players = new List<EmbeddedClientState>();
    private RoomInfo _roomInfo;
    private readonly object _lock = new object();

    public IReadOnlyList<EmbeddedClientState> Players
    {
        get { lock (_lock) { return _players.ToList(); } }
    }

    public RoomInfo RoomInfo
    {
        get { lock (_lock) { return _roomInfo; } }
    }

    public int CurrentPlayers
    {
        get { lock (_lock) { return _players.Count; } }
    }

    public int MaxPlayers
    {
        get { lock (_lock) { return _roomInfo.MaxPlayers; } }
    }

    public bool IsFull
    {
        get { lock (_lock) { return _players.Count >= _roomInfo.MaxPlayers; } }
    }

    public event Action<EmbeddedRoom> OnPlayerListChanged;

    public EmbeddedRoom(RoomInfo roomInfo)
    {
        _roomInfo = roomInfo;
        _roomInfo.CurrentPlayers = 0;
        if (_roomInfo.Status == RoomStatus.Closed)
            _roomInfo.Status = RoomStatus.Waiting;
    }

    public bool AddPlayer(EmbeddedClientState player)
    {
        if (player == null) return false;
        lock (_lock)
        {
            if (IsFull) return false;
            if (_players.Contains(player)) return false;
            _players.Add(player);
            _roomInfo.CurrentPlayers = _players.Count;
            UpdateStatusLocked();
        }
        OnPlayerListChanged?.Invoke(this);
        return true;
    }

    public bool RemovePlayer(EmbeddedClientState player)
    {
        if (player == null) return false;
        bool removed;
        lock (_lock)
        {
            removed = _players.Remove(player);
            if (!removed) return false;
            _roomInfo.CurrentPlayers = _players.Count;
            UpdateStatusLocked();
        }
        OnPlayerListChanged?.Invoke(this);
        return true;
    }

    public EmbeddedClientState FindPlayerByUserId(string userId)
    {
        lock (_lock)
        {
            return _players.FirstOrDefault(p => p.tempUserId == userId);
        }
    }

    public void SetMaxPlayers(int maxPlayers)
    {
        lock (_lock)
        {
            _roomInfo.MaxPlayers = Math.Max(1, maxPlayers);
            UpdateStatusLocked();
        }
        OnPlayerListChanged?.Invoke(this);
    }

    public void SetMode(GameModes mode)
    {
        lock (_lock)
        {
            _roomInfo.Mode = mode;
        }
        OnPlayerListChanged?.Invoke(this);
    }

    public void StartGame()
    {
        lock (_lock)
        {
            _roomInfo.Status = RoomStatus.Playing;
        }
        OnPlayerListChanged?.Invoke(this);
    }

    public void StopGame()
    {
        lock (_lock)
        {
            UpdateStatusLocked();
        }
        OnPlayerListChanged?.Invoke(this);
    }

    public RoomInfo ToRoomInfo()
    {
        lock (_lock)
        {
            return _roomInfo;
        }
    }

    private void UpdateStatusLocked()
    {
        if (_roomInfo.Status == RoomStatus.Playing) return;
        _roomInfo.Status = _players.Count >= _roomInfo.MaxPlayers ? RoomStatus.Full : RoomStatus.Waiting;
    }
}
