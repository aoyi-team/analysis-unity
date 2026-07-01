using System.Net.Sockets;
/// <summary>
/// 当前连接客户
/// </summary>
// 服务端 ClientState 类扩展
public class ClientState
{
    public Socket socket;          // 原有Socket连接
    public ByteArray readBuff = new ByteArray();   // 原有读取缓冲区
    public long lastPingTime;      // 原有心跳时间

    // 新增用户身份信息
    public string userId;          // 用户唯一ID（登录后赋值）
    public string userName;        // 用户名
    public int selectedHeroId;     // 选中的英雄ID
    public bool isInMatching;      // 是否在匹配队列中
    public string roomId;          // 所属房间ID（匹配成功后赋值）
    public bool isLoadComplete;    // 游戏资源是否加载完成

    public void UpdateUserInfo(string userId,string userName)
    {
        this.userId = userId;
        this.userName = userName;
    }

    public void UpdateUserId(string userId)
    {
        this.userId = userId;
    }
    public void UpdateUserName(string userName)
    {
        this.userName = userName;
    }

    public int GetIdToInt()
    {
        return int.Parse(userId);
    }

}
