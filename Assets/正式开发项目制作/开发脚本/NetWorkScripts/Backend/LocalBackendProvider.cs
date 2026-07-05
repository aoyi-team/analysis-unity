using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 本地服务器模式的后端实现。
/// 直接复用现有 NetWorkMgr 的 MsgLoginProf / MsgRegisterProf 消息流程，
/// 通过 TaskCompletionSource 将回调式登录/注册转换为 async/await 接口。
/// </summary>
public class LocalBackendProvider : IBackendProvider
{
    public async Task<LoginResult> LoginAsync(object credentials)
    {
        if (!(credentials is LocalLoginCredentials cred))
        {
            return new LoginResult
            {
                Success = false,
                ErrorMessage = "需要本地登录凭证（LocalLoginCredentials）"
            };
        }

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
                tcs.TrySetResult(new LoginResult
                {
                    Success = true,
                    UserId = msg.Id
                });
            }
            else
            {
                string error = msg.ErrType == 0 ? "密码错误" : "账号不存在";
                tcs.TrySetResult(new LoginResult
                {
                    Success = false,
                    ErrorMessage = error
                });
            }
        };

        NetWorkMgr.AddMsgListener("MsgLoginProf", listener);

        var loginMsg = new MsgLoginProf
        {
            LoginMehod = cred.LoginWay,
            Id = cred.Id ?? string.Empty,
            Name = cred.Name ?? string.Empty,
            pw = cred.Password ?? string.Empty
        };
        NetWorkMgr.Send(loginMsg);

        return await AwaitAndCleanup(tcs, "MsgLoginProf", listener);
    }

    public async Task<RegisterResult> RegisterAsync(object credentials)
    {
        if (!(credentials is LocalRegisterCredentials cred))
        {
            return new RegisterResult
            {
                Success = false,
                ErrorMessage = "需要本地注册凭证（LocalRegisterCredentials）"
            };
        }

        bool connected = await NetWorkMgr.WaitConnectAsync();
        if (!connected)
        {
            return new RegisterResult
            {
                Success = false,
                ErrorMessage = "连接服务器失败"
            };
        }

        var tcs = new TaskCompletionSource<RegisterResult>();
        NetWorkMgr.MsgListener listener = null;
        listener = (msgBase) =>
        {
            var msg = (MsgRegisterProf)msgBase;
            if (msg.result == 0)
            {
                tcs.TrySetResult(new RegisterResult
                {
                    Success = true,
                    UserId = msg.Id
                });
            }
            else
            {
                tcs.TrySetResult(new RegisterResult
                {
                    Success = false,
                    ErrorMessage = "注册失败"
                });
            }
        };

        NetWorkMgr.AddMsgListener("MsgRegisterProf", listener);

        var registerMsg = new MsgRegisterProf
        {
            pw = cred.Password ?? string.Empty
        };
        NetWorkMgr.Send(registerMsg);

        return await AwaitAndCleanup(tcs, "MsgRegisterProf", listener);
    }

    public Task<PlayerBasicInfo> GetPlayerInfoAsync(string userId)
    {
        var mgr = PlayerBasicInfoMgr.Instance;
        // 本地服务器模式下优先使用服务器分配的 userId（如临时连接ID），确保与战斗消息中的玩家ID一致
        string id = userId;
        string name = mgr != null ? mgr.GetName() : string.Empty;

        if (string.IsNullOrEmpty(id))
            id = mgr != null ? mgr.GetID() : string.Empty;
        if (string.IsNullOrEmpty(name))
            name = "玩家" + id;

        return Task.FromResult(new PlayerBasicInfo
        {
            UserId = id,
            UserName = name,
            LastLoginAt = DateTime.UtcNow
        });
    }

    public Task<RoomInfo> CreateRoomAsync(CreateRoomRequest request)
    {
        // 本地服务器模式沿用原有的匹配/建房流程，不通过 Provider 创建房间。
        Debug.Log("[LocalBackendProvider] 本地服务器模式暂不支持通过 Provider 创建房间");
        return Task.FromResult(new RoomInfo());
    }

    public Task<List<RoomInfo>> GetRoomListAsync(GetRoomListRequest request)
    {
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

    private async Task<T> AwaitAndCleanup<T>(TaskCompletionSource<T> tcs, string msgName, NetWorkMgr.MsgListener listener)
    {
        try
        {
            return await tcs.Task;
        }
        finally
        {
            NetWorkMgr.RemoveMsgListener(msgName, listener);
        }
    }
}
