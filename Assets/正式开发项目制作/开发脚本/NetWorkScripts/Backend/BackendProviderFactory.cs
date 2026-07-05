/// <summary>
/// 后端提供者工厂。
/// 根据当前网络运行模式创建对应的 IBackendProvider 实现，
/// 并自动设置到 PlayerBasicInfoMgr.Instance.CurrentBackend。
/// </summary>
public static class BackendProviderFactory
{
    public static IBackendProvider Create(NetworkMode mode)
    {
        IBackendProvider provider;
        switch (mode)
        {
            case NetworkMode.LanHost:
            case NetworkMode.LanClient:
                provider = new LanBackendProvider();
                break;
            case NetworkMode.SupabaseOnline:
                provider = new SupabaseBackendProvider();
                break;
            case NetworkMode.LocalServer:
            default:
                provider = new LocalBackendProvider();
                break;
        }

        if (PlayerBasicInfoMgr.Instance != null)
        {
            PlayerBasicInfoMgr.Instance.CurrentBackend = provider;
        }

        return provider;
    }
}

/// <summary>
/// 本地服务器登录凭证。
/// </summary>
public class LocalLoginCredentials
{
    public int LoginWay; // 0-ID 登录，1-名称登录
    public string Id;
    public string Name;
    public string Password;
}

/// <summary>
/// 本地服务器注册凭证。
/// </summary>
public class LocalRegisterCredentials
{
    public string Password;
}

/// <summary>
/// 局域网临时登录凭证。
/// </summary>
public class LanLoginCredentials
{
    public string NickName;
}
