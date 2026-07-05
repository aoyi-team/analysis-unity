using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 局域网模式的后端实现。
/// 局域网不依赖外部账号系统，登录时直接生成临时玩家信息。
/// 房间的发现/创建由 LanHostManager 与 LanBeaconReceiver 负责，Provider 仅作占位。
/// </summary>
public class LanBackendProvider : IBackendProvider
{
    private string _tempUserId;
    private string _tempName;

    public async Task<LoginResult> LoginAsync(object credentials)
    {
        string nickName = credentials is LanLoginCredentials lan ? lan.NickName : null;
        _tempName = string.IsNullOrWhiteSpace(nickName) ? null : nickName.Trim();

        // 局域网模式需要连接内嵌服务器并通过 MsgLoginProf 获取服务器分配的 tempUserId，
        // 这样后续 MsgMatchSuccess 中的 userId 才能与本地 ID 匹配。
        bool connected = await NetWorkMgr.WaitConnectAsync();
        if (!connected)
        {
            return new LoginResult
            {
                Success = false,
                ErrorMessage = "连接服务器失败"
            };
        }

        var tcs = new TaskCompletionSource<LoginResult>();
        NetWorkMgr.MsgListener listener = null;
        listener = (msgBase) =>
        {
            var msg = (MsgLoginProf)msgBase;
            if (msg.result == 0)
            {
                _tempUserId = msg.Id;
                if (string.IsNullOrWhiteSpace(_tempName))
                    _tempName = msg.Name;
                if (string.IsNullOrWhiteSpace(_tempName))
                    _tempName = $"玩家{_tempUserId}";
                tcs.TrySetResult(new LoginResult
                {
                    Success = true,
                    UserId = _tempUserId
                });
            }
            else
            {
                tcs.TrySetResult(new LoginResult
                {
                    Success = false,
                    ErrorMessage = "登录失败"
                });
            }
        };

        NetWorkMgr.AddMsgListener("MsgLoginProf", listener);

        var loginMsg = new MsgLoginProf
        {
            LoginMehod = 0,
            Id = string.Empty,
            Name = _tempName ?? string.Empty,
            pw = string.Empty
        };
        NetWorkMgr.Send(loginMsg);

        try
        {
            return await tcs.Task;
        }
        finally
        {
            NetWorkMgr.RemoveMsgListener("MsgLoginProf", listener);
        }
    }

    public Task<RegisterResult> RegisterAsync(object credentials)
    {
        // 局域网模式无需注册，直接复用当前临时账号。
        if (string.IsNullOrEmpty(_tempUserId))
        {
            _tempUserId = GenerateTempId();
            _tempName = $"玩家{_tempUserId}";
        }

        return Task.FromResult(new RegisterResult
        {
            Success = true,
            UserId = _tempUserId
        });
    }

    public Task<PlayerBasicInfo> GetPlayerInfoAsync(string userId)
    {
        return Task.FromResult(new PlayerBasicInfo
        {
            UserId = _tempUserId,
            UserName = _tempName,
            LastLoginAt = DateTime.UtcNow
        });
    }

    public Task<RoomInfo> CreateRoomAsync(CreateRoomRequest request)
    {
        // 局域网房间的创建由 LanHostManager.StartHosting 处理，
        // Provider 层不直接创建。如需调用，请使用 LobbyNetworkBridge.OnCreateRoom。
        throw new NotImplementedException("局域网房间创建请通过 LanHostManager 完成");
    }

    public Task<List<RoomInfo>> GetRoomListAsync(GetRoomListRequest request)
    {
        // 局域网房间通过 UDP Beacon 发现，不走 Provider 列表接口。
        return Task.FromResult(new List<RoomInfo>());
    }

    public Task<bool> JoinRoomAsync(string roomId)
    {
        return Task.FromResult(true);
    }

    public Task HeartbeatRoomAsync(string roomId)
    {
        return Task.CompletedTask;
    }

    private static string GenerateTempId()
    {
        return Guid.NewGuid().ToString("N").Substring(0, 6);
    }
}
