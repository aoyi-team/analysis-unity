using System;
using UnityEngine;

/// <summary>
/// 局域网房间 Beacon 数据包
/// 由房主发送，供局域网内其他客户端发现房间。
/// </summary>
[Serializable]
public class LanBeaconData
{
    public string RoomId;
    public string RoomName;
    public GameModes Mode;
    public int CurrentPlayers;
    public int MaxPlayers;
    public int ProtocolVersion;
    public string TcpIp;
    public int TcpPort;
    public string UdpIp;
    public int UdpPort;

    /// <summary>将 Beacon 序列化为 JSON 字符串</summary>
    public string ToJson()
    {
        return JsonUtility.ToJson(this);
    }

    /// <summary>从 JSON 字符串反序列化为 Beacon 数据</summary>
    public static LanBeaconData FromJson(string json)
    {
        if (string.IsNullOrEmpty(json))
            return null;

        try
        {
            return JsonUtility.FromJson<LanBeaconData>(json);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[LAN] 解析 Beacon JSON 失败: {ex.Message}");
            return null;
        }
    }
}
