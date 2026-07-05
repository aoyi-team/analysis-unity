using System;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// 轻量级 Supabase REST 客户端。
/// 基于 UnityWebRequest，零外部依赖（除 Unity 自带 Newtonsoft.Json）。
/// </summary>
public static class SupabaseRestClient
{
    public static string BaseUrl => SupabaseConfig.Instance.SupabaseUrl.TrimEnd('/');
    public static string AnonKey => SupabaseConfig.Instance.AnonKey;
    public static float Timeout => SupabaseConfig.Instance.RequestTimeout;

    /// <summary>
    /// GET 查询表数据。
    /// </summary>
    public static async Task<SupabaseRestResult<T>> GetAsync<T>(string table, string query = "", string accessToken = "")
    {
        string url = $"{BaseUrl}/rest/v1/{table}";
        if (!string.IsNullOrEmpty(query)) url += "?" + query;

        using var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET);
        request.downloadHandler = new DownloadHandlerBuffer();
        SetCommonHeaders(request, accessToken);
        return await SendAsync<T>(request);
    }

    /// <summary>
    /// POST 插入数据。
    /// </summary>
    public static async Task<SupabaseRestResult<T>> PostAsync<T>(string table, object data, string accessToken = "")
    {
        string url = $"{BaseUrl}/rest/v1/{table}";

        using var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data)));
        request.downloadHandler = new DownloadHandlerBuffer();
        SetCommonHeaders(request, accessToken);
        return await SendAsync<T>(request);
    }

    /// <summary>
    /// PATCH 更新数据。
    /// </summary>
    public static async Task<SupabaseRestResult<T>> PatchAsync<T>(string table, string query, object data, string accessToken = "")
    {
        string url = $"{BaseUrl}/rest/v1/{table}?{query}";

        using var request = UnityWebRequest.Put(url, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data)));
        request.method = "PATCH";
        request.downloadHandler = new DownloadHandlerBuffer();
        SetCommonHeaders(request, accessToken);
        return await SendAsync<T>(request);
    }

    /// <summary>
    /// DELETE 删除数据。
    /// </summary>
    public static async Task<SupabaseRestResult<T>> DeleteAsync<T>(string table, string query, string accessToken = "")
    {
        string url = $"{BaseUrl}/rest/v1/{table}?{query}";

        using var request = UnityWebRequest.Delete(url);
        request.downloadHandler = new DownloadHandlerBuffer();
        SetCommonHeaders(request, accessToken);
        return await SendAsync<T>(request);
    }

    private static void SetCommonHeaders(UnityWebRequest request, string accessToken)
    {
        request.SetRequestHeader("apikey", AnonKey);
        string bearerToken = string.IsNullOrWhiteSpace(accessToken) ? AnonKey : accessToken;
        request.SetRequestHeader("Authorization", $"Bearer {bearerToken}");
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Prefer", "return=representation");
        request.timeout = Mathf.RoundToInt(Timeout);
    }

    private static async Task<SupabaseRestResult<T>> SendAsync<T>(UnityWebRequest request)
    {
        try
        {
            await request.SendWebRequestAsync();

            int statusCode = (int)request.responseCode;
            string responseText = request.downloadHandler?.text ?? string.Empty;

            if (request.result == UnityWebRequest.Result.Success)
            {
                T data = default;
                if (!string.IsNullOrWhiteSpace(responseText))
                {
                    data = JsonConvert.DeserializeObject<T>(responseText);
                }
                return new SupabaseRestResult<T> { Success = true, Data = data, StatusCode = statusCode };
            }
            else
            {
                return new SupabaseRestResult<T>
                {
                    Success = false,
                    ErrorMessage = $"{request.error} (HTTP {statusCode}): {responseText}",
                    StatusCode = statusCode
                };
            }
        }
        catch (Exception ex)
        {
            return new SupabaseRestResult<T> { Success = false, ErrorMessage = ex.Message };
        }
    }
}

/// <summary>
/// REST 调用结果包装。
/// </summary>
public class SupabaseRestResult<T>
{
    public bool Success;
    public T Data;
    public string ErrorMessage;
    public int StatusCode;
}

/// <summary>
/// UnityWebRequest 异步扩展。
/// </summary>
public static class UnityWebRequestAsyncExtensions
{
    public static Task<UnityWebRequest> SendWebRequestAsync(this UnityWebRequest request)
    {
        var tcs = new TaskCompletionSource<UnityWebRequest>();
        request.SendWebRequest().completed += op => tcs.SetResult(request);
        return tcs.Task;
    }
}
