using System;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine.Networking;

/// <summary>
/// Supabase Auth REST API 轻量封装。
/// </summary>
public static class SupabaseAuthClient
{
    /// <summary>
    /// 邮箱密码注册。
    /// </summary>
    public static async Task<AuthSession> SignUpAsync(string email, string password)
    {
        string url = $"{SupabaseRestClient.BaseUrl}/auth/v1/signup";
        var body = new { email, password };
        return await PostAuthAsync(url, body);
    }

    /// <summary>
    /// 邮箱密码登录。
    /// </summary>
    public static async Task<AuthSession> SignInAsync(string email, string password)
    {
        string url = $"{SupabaseRestClient.BaseUrl}/auth/v1/token?grant_type=password";
        var body = new { email, password };
        return await PostAuthAsync(url, body);
    }

    /// <summary>
    /// 匿名登录：以空 body 调用 signup 接口获取临时用户。
    /// </summary>
    public static async Task<AuthSession> SignInAnonymouslyAsync()
    {
        string url = $"{SupabaseRestClient.BaseUrl}/auth/v1/signup";
        var body = new { };
        return await PostAuthAsync(url, body);
    }

    /// <summary>
    /// 刷新访问令牌。
    /// </summary>
    public static async Task<AuthSession> RefreshTokenAsync(string refreshToken)
    {
        string url = $"{SupabaseRestClient.BaseUrl}/auth/v1/token?grant_type=refresh_token";
        var body = new { refresh_token = refreshToken };
        return await PostAuthAsync(url, body);
    }

    private static async Task<AuthSession> PostAuthAsync(string url, object body)
    {
        using var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
        string json = JsonConvert.SerializeObject(body);
        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("apikey", SupabaseRestClient.AnonKey);
        request.SetRequestHeader("Content-Type", "application/json");

        try
        {
            await request.SendWebRequestAsync();
            int statusCode = (int)request.responseCode;
            string responseText = request.downloadHandler?.text ?? string.Empty;

            if (request.result == UnityWebRequest.Result.Success)
            {
                var response = JsonConvert.DeserializeObject<SupabaseAuthResponse>(responseText);
                return new AuthSession
                {
                    Success = true,
                    AccessToken = response.access_token,
                    RefreshToken = response.refresh_token,
                    UserId = response.user?.id,
                    Email = response.user?.email,
                    ExpiresIn = response.expires_in
                };
            }
            else
            {
                return new AuthSession
                {
                    Success = false,
                    ErrorMessage = $"{request.error} (HTTP {statusCode}): {responseText}"
                };
            }
        }
        catch (Exception ex)
        {
            return new AuthSession { Success = false, ErrorMessage = ex.Message };
        }
    }
}

/// <summary>
/// 统一认证会话结果。
/// </summary>
public class AuthSession
{
    public bool Success;
    public string ErrorMessage;
    public string AccessToken;
    public string RefreshToken;
    public string UserId;
    public string Email;
    public int ExpiresIn;
}

/// <summary>
/// Supabase Auth /token 响应 DTO。
/// </summary>
public class SupabaseAuthResponse
{
    [JsonProperty("access_token")] public string access_token;
    [JsonProperty("refresh_token")] public string refresh_token;
    [JsonProperty("expires_in")] public int expires_in;
    [JsonProperty("user")] public SupabaseUserResponse user;
}

/// <summary>
/// Supabase Auth user 对象 DTO。
/// </summary>
public class SupabaseUserResponse
{
    [JsonProperty("id")] public string id;
    [JsonProperty("email")] public string email;
}
