using System;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

public static class OnlineMatchApiClient
{
    private static string BaseUrl => (SupabaseConfig.Instance.OnlineMatchApiBaseUrl ?? string.Empty).TrimEnd('/');
    private static int Timeout => Mathf.RoundToInt(SupabaseConfig.Instance.RequestTimeout);

    public static Task<OnlineMatchApiResult<OnlineMatchResponse>> StartMatchAsync(
        string accessToken,
        GameModes mode,
        int heroId,
        int skinId)
    {
        var body = new OnlineMatchStartRequest
        {
            Mode = mode.ToString(),
            HeroId = heroId,
            SkinId = skinId,
            MatchType = "random",
            ProtocolVersion = ServerConfig.ProtocolVersion
        };

        return PostAsync<OnlineMatchResponse>("/api/match/start", body, accessToken);
    }

    public static Task<OnlineMatchApiResult<OnlineMatchResponse>> GetStatusAsync(string accessToken, string ticketId)
    {
        string path = $"/api/match/status?ticketId={UnityWebRequest.EscapeURL(ticketId)}";
        return SendAsync<OnlineMatchResponse>(UnityWebRequest.kHttpVerbGET, path, null, accessToken);
    }

    public static Task<OnlineMatchApiResult<OnlineMatchCancelResponse>> CancelMatchAsync(string accessToken, string ticketId)
    {
        return PostAsync<OnlineMatchCancelResponse>("/api/match/cancel", new OnlineMatchCancelRequest { TicketId = ticketId }, accessToken);
    }

    private static Task<OnlineMatchApiResult<T>> PostAsync<T>(string path, object body, string accessToken)
    {
        string json = JsonConvert.SerializeObject(body);
        return SendAsync<T>(UnityWebRequest.kHttpVerbPOST, path, json, accessToken);
    }

    private static async Task<OnlineMatchApiResult<T>> SendAsync<T>(string method, string path, string json, string accessToken)
    {
        string baseUrl = BaseUrl;
        if (string.IsNullOrWhiteSpace(baseUrl) || baseUrl.Contains("localhost") && !Application.isEditor)
        {
            return new OnlineMatchApiResult<T>
            {
                Success = false,
                ErrorMessage = $"OnlineMatchApiBaseUrl 未配置为线上地址: {baseUrl}"
            };
        }

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return new OnlineMatchApiResult<T>
            {
                Success = false,
                ErrorMessage = "缺少 Supabase access token，请先登录。"
            };
        }

        string url = baseUrl + path;
        using var request = new UnityWebRequest(url, method);
        request.downloadHandler = new DownloadHandlerBuffer();
        if (!string.IsNullOrEmpty(json))
        {
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        }

        request.timeout = Timeout;
        request.SetRequestHeader("Authorization", $"Bearer {accessToken}");
        request.SetRequestHeader("Content-Type", "application/json");

        try
        {
            await request.SendWebRequestAsync();

            int statusCode = (int)request.responseCode;
            string responseText = request.downloadHandler?.text ?? string.Empty;
            if (request.result == UnityWebRequest.Result.Success)
            {
                T data = string.IsNullOrWhiteSpace(responseText)
                    ? default
                    : JsonConvert.DeserializeObject<T>(responseText);

                return new OnlineMatchApiResult<T> { Success = true, Data = data, StatusCode = statusCode };
            }

            return new OnlineMatchApiResult<T>
            {
                Success = false,
                ErrorMessage = $"{request.error} (HTTP {statusCode}): {responseText}",
                StatusCode = statusCode
            };
        }
        catch (Exception ex)
        {
            return new OnlineMatchApiResult<T> { Success = false, ErrorMessage = ex.Message };
        }
    }
}
