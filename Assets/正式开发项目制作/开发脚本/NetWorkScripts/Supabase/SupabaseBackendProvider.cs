using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 基于 Supabase REST API 的 IBackendProvider 实现。
/// 负责登录、注册、玩家资料、房间生命周期管理。
/// </summary>
public class SupabaseBackendProvider : IBackendProvider
{
    private AuthSession _session;

    private const string PREFS_ACCESS_TOKEN = "SupabaseAccessToken";
    private const string PREFS_REFRESH_TOKEN = "SupabaseRefreshToken";
    private const string PREFS_USER_ID = "SupabaseUserId";
    private const string PREFS_USER_NAME = "SupabaseUserName";

    private string _sessionUserName;

    public SupabaseBackendProvider()
    {
        TryLoadSession();
    }

    #region 会话管理

    private void TryLoadSession()
    {
        if (PlayerPrefs.HasKey(PREFS_ACCESS_TOKEN))
        {
            _session = new AuthSession
            {
                AccessToken = PlayerPrefs.GetString(PREFS_ACCESS_TOKEN),
                RefreshToken = PlayerPrefs.GetString(PREFS_REFRESH_TOKEN),
                UserId = PlayerPrefs.GetString(PREFS_USER_ID)
            };
            _sessionUserName = PlayerPrefs.GetString(PREFS_USER_NAME);
        }
    }

    private void SaveSession(AuthSession session, string userName = null)
    {
        _session = session;
        _sessionUserName = userName;
        PlayerPrefs.SetString(PREFS_ACCESS_TOKEN, session.AccessToken ?? "");
        PlayerPrefs.SetString(PREFS_REFRESH_TOKEN, session.RefreshToken ?? "");
        PlayerPrefs.SetString(PREFS_USER_ID, session.UserId ?? "");
        PlayerPrefs.SetString(PREFS_USER_NAME, userName ?? "");
        PlayerPrefs.Save();
    }

    #endregion

    #region IBackendProvider

    public async Task<LoginResult> LoginAsync(object credentials)
    {
        AuthSession session;
        string userNameForSession = null;
        if (credentials is SupabaseLoginCredentials login)
        {
            string email = login.Email;
            string userName = null;

            if (login.UseUsernameLogin)
            {
                userName = login.Username?.Trim();
                var resolveResult = await ResolveEmailByUserNameAsync(userName);
                if (!resolveResult.Success)
                {
                    return new LoginResult { Success = false, ErrorMessage = resolveResult.ErrorMessage };
                }
                email = resolveResult.Data;
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                return new LoginResult { Success = false, ErrorMessage = "请输入账号或用户名" };
            }

            session = await SupabaseAuthClient.SignInAsync(email.Trim(), login.Password);
            if (session.Success && !string.IsNullOrWhiteSpace(userName))
            {
                userNameForSession = userName;
            }
        }
        else if (credentials is SupabaseAnonymousCredentials)
        {
            session = await SupabaseAuthClient.SignInAnonymouslyAsync();
        }
        else
        {
            return new LoginResult { Success = false, ErrorMessage = "不支持的凭证类型" };
        }

        if (!session.Success)
        {
            return new LoginResult { Success = false, ErrorMessage = session.ErrorMessage };
        }

        SaveSession(session, userNameForSession);
        return new LoginResult
        {
            Success = true,
            UserId = session.UserId,
            AccessToken = session.AccessToken,
            RefreshToken = session.RefreshToken
        };
    }

    public async Task<RegisterResult> RegisterAsync(object credentials)
    {
        if (!(credentials is SupabaseLoginCredentials login))
        {
            return new RegisterResult { Success = false, ErrorMessage = "需要邮箱和密码" };
        }

        var session = await SupabaseAuthClient.SignUpAsync(login.Email, login.Password);
        if (!session.Success)
        {
            return new RegisterResult { Success = false, ErrorMessage = session.ErrorMessage };
        }

        SaveSession(session);
        return new RegisterResult { Success = true, UserId = session.UserId };
    }

    public Task<PlayerBasicInfo> GetPlayerInfoAsync(string userId)
    {
        string userName = GetDefaultUserName(userId);
        return Task.FromResult(new PlayerBasicInfo
        {
            UserId = userId,
            UserName = userName,
            LastLoginAt = DateTime.UtcNow
        });
    }

    private async Task<SupabaseRestResult<string>> ResolveEmailByUserNameAsync(string userName)
    {
        if (string.IsNullOrWhiteSpace(userName))
        {
            return new SupabaseRestResult<string> { Success = false, ErrorMessage = "请输入用户名" };
        }

        string table = SupabaseConfig.Instance.ProfilesTable;
        string query = $"username=eq.{Uri.EscapeDataString(userName.Trim())}&select=id,username,email&limit=1";
        var result = await SupabaseRestClient.GetAsync<List<SupabaseProfileDto>>(table, query);

        if (!result.Success)
        {
            string errorMessage;
            if (result.StatusCode == 404)
            {
                errorMessage = $"用户名登录需要 Supabase 数据库中存在 public.{table} 表，请先执行 docs/supabase-schema.md 中的初始化 SQL，并包含 username、email 字段。";
            }
            else
            {
                errorMessage = "用户名登录需要资料表允许按 username 查询 email：" + result.ErrorMessage;
            }
            return new SupabaseRestResult<string>
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }

        if (result.Data == null || result.Data.Count == 0)
        {
            return new SupabaseRestResult<string> { Success = false, ErrorMessage = "用户名不存在" };
        }

        string email = result.Data[0].Email;
        if (string.IsNullOrWhiteSpace(email))
        {
            return new SupabaseRestResult<string>
            {
                Success = false,
                ErrorMessage = "用户名登录需要资料表保存 email 字段"
            };
        }

        return new SupabaseRestResult<string> { Success = true, Data = email.Trim() };
    }

    public async Task<RoomInfo> CreateRoomAsync(CreateRoomRequest request)
    {
        string roomCode = GenerateRoomCode();
        var dto = new
        {
            room_code = roomCode,
            mode = request.Mode.ToString().ToLowerInvariant(),
            host_ip = request.HostEndpoint.TcpIp,
            host_tcp_port = request.HostEndpoint.TcpPort,
            host_udp_port = request.HostEndpoint.UdpPort,
            max_players = request.MaxPlayers,
            current_players = 1,
            status = RoomStatus.Waiting.ToString(),
            protocol_version = request.ProtocolVersion
        };

        var result = await SupabaseRestClient.PostAsync<List<SupabaseRoomDto>>(SupabaseConfig.Instance.RoomsTable, dto, _session?.AccessToken);
        if (!result.Success || result.Data == null || result.Data.Count == 0)
        {
            throw new Exception(result.ErrorMessage ?? "创建房间失败");
        }

        return ConvertToRoomInfo(result.Data[0]);
    }

    public async Task<List<RoomInfo>> GetRoomListAsync(GetRoomListRequest request)
    {
        var queryBuilder = new StringBuilder();
        queryBuilder.Append($"status=eq.{RoomStatus.Waiting}");
        queryBuilder.Append($"&protocol_version=eq.{request.ProtocolVersion}");
        if (request.Mode.HasValue)
        {
            queryBuilder.Append($"&mode=eq.{request.Mode.Value.ToString().ToLowerInvariant()}");
        }
        queryBuilder.Append("&order=created_at.desc");
        queryBuilder.Append($"&limit={request.MaxResults}");

        var result = await SupabaseRestClient.GetAsync<List<SupabaseRoomDto>>(
            SupabaseConfig.Instance.RoomsTable, queryBuilder.ToString(), _session?.AccessToken);

        if (!result.Success || result.Data == null)
        {
            return new List<RoomInfo>();
        }

        var list = new List<RoomInfo>(result.Data.Count);
        foreach (var dto in result.Data)
        {
            list.Add(ConvertToRoomInfo(dto));
        }
        return list;
    }

    public async Task<bool> JoinRoomAsync(string roomId)
    {
        string table = SupabaseConfig.Instance.RoomsTable;
        string query = $"id=eq.{Uri.EscapeDataString(roomId)}";

        var getResult = await SupabaseRestClient.GetAsync<List<SupabaseRoomDto>>(table, query, _session?.AccessToken);
        if (!getResult.Success || getResult.Data == null || getResult.Data.Count == 0)
        {
            return false;
        }

        var room = getResult.Data[0];
        if (room.CurrentPlayers >= room.MaxPlayers)
        {
            return false;
        }

        var patch = new { current_players = room.CurrentPlayers + 1 };
        var patchResult = await SupabaseRestClient.PatchAsync<List<SupabaseRoomDto>>(table, query, patch, _session?.AccessToken);
        return patchResult.Success && patchResult.Data != null && patchResult.Data.Count > 0;
    }

    public async Task HeartbeatRoomAsync(string roomId)
    {
        string table = SupabaseConfig.Instance.RoomsTable;
        string query = $"id=eq.{Uri.EscapeDataString(roomId)}";
        var patch = new { updated_at = DateTime.UtcNow.ToString("O") };
        await SupabaseRestClient.PatchAsync<object>(table, query, patch, _session?.AccessToken);
    }

    #endregion

    private static string GenerateRoomCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        StringBuilder sb = new StringBuilder(6);
        for (int i = 0; i < 6; i++)
        {
            sb.Append(chars[UnityEngine.Random.Range(0, chars.Length)]);
        }
        return sb.ToString();
    }

    private string GetDefaultUserName(string userId)
    {
        if (!string.IsNullOrWhiteSpace(_sessionUserName))
        {
            return _sessionUserName;
        }

        if (_session != null && _session.UserId == userId && !string.IsNullOrWhiteSpace(_session.Email))
        {
            int atIndex = _session.Email.IndexOf('@');
            if (atIndex > 0)
            {
                return _session.Email.Substring(0, atIndex);
            }
            return _session.Email;
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            return "玩家";
        }

        return "玩家" + userId.Substring(0, Math.Min(6, userId.Length));
    }

    private static RoomInfo ConvertToRoomInfo(SupabaseRoomDto dto)
    {
        Enum.TryParse(dto.Status, out RoomStatus status);
        Enum.TryParse(dto.Mode, true, out GameModes mode);

        return new RoomInfo
        {
            RoomId = dto.Id,
            RoomName = dto.RoomCode,
            Mode = mode,
            HostEndpoint = new NetworkEndpoint(dto.HostIp, dto.HostTcpPort, dto.HostIp, dto.HostUdpPort),
            CurrentPlayers = dto.CurrentPlayers,
            MaxPlayers = dto.MaxPlayers,
            ProtocolVersion = dto.ProtocolVersion,
            Status = status
        };
    }
}
