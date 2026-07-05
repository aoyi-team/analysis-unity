using System.Net.Sockets;

/// <summary>
/// 内嵌主机的客户端连接状态
/// </summary>
public class EmbeddedClientState
{
    public Socket socket;
    public ByteArray readBuff = new ByteArray();
    public string tempUserId;
    public string userName;
    public bool isReady;
    public long lastPingTime;
    public int heroId;
    public int skinId;

    public void UpdateUserInfo(string userId, string userName)
    {
        this.tempUserId = userId;
        this.userName = userName;
    }
}
