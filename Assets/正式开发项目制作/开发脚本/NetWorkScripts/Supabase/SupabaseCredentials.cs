/// <summary>
/// Supabase 邮箱密码登录凭证。
/// </summary>
public class SupabaseLoginCredentials
{
    public string Email;
    public string Username;
    public string Password;
    public bool UseUsernameLogin;
}

/// <summary>
/// Supabase 匿名登录凭证（空标记类）。
/// </summary>
public class SupabaseAnonymousCredentials
{
}
