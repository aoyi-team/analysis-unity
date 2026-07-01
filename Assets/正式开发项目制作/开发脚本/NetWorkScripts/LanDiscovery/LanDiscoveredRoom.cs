using System;

/// <summary>
/// 发现到的局域网房间封装
/// 包含房间信息、最后收到 Beacon 的时间以及兼容性状态。
/// </summary>
[Serializable]
public class LanDiscoveredRoom
{
    public RoomInfo RoomInfo;
    public DateTime LastSeenTime;
    public bool IsCompatible;

    public LanDiscoveredRoom(RoomInfo info, bool isCompatible)
    {
        RoomInfo = info;
        LastSeenTime = DateTime.UtcNow;
        IsCompatible = isCompatible;
    }

    /// <summary>刷新房间信息与最后可见时间</summary>
    public void Refresh(RoomInfo info, bool isCompatible)
    {
        RoomInfo = info;
        LastSeenTime = DateTime.UtcNow;
        IsCompatible = isCompatible;
    }
}
