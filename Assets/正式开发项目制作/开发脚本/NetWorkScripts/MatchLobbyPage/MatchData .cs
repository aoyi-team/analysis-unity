// 全局匹配数据类

public class MatchData : Singleton<MatchData>
{
    public string roomId;
    public string opponentUserId;
    public string opponentUserName;
    public int opponentHeroId;
    public bool isSelfLoadComplete; // 自己是否加载完成
    public bool isOpponentLoadComplete; // 对手是否加载完成

    public void InitMatchData(string roomId,
                             string opponentUserId, string opponentUserName, int opponentHeroId)
    {
        this.roomId = roomId;
        this.opponentUserId = opponentUserId;
        this.opponentUserName = opponentUserName;
        this.opponentHeroId = opponentHeroId;
        isSelfLoadComplete = false;
        isOpponentLoadComplete = false;
    }
}
// Singleton.cs
public class Singleton<T> where T : class, new()
{
    private static T _instance;
    private static readonly object _lock = new object();

    public static T Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new T();
                    }
                }
            }
            return _instance;
        }
    }
}