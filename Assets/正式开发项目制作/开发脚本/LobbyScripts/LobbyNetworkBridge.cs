using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 大厅网络流程桥接器。
/// 根据当前 NetworkMode 将创建房间 / 加入房间请求分发到本地服务器、局域网或 Supabase。
/// </summary>
public class LobbyNetworkBridge : MonoBehaviour
{
    public static LobbyNetworkBridge Instance { get; private set; }

    [Header("局域网")]
    [SerializeField] private LanHostManager lanHostManager;
    [SerializeField] private LanRoomListPanel lanRoomListPanel;

    [Header("Supabase 加入房间")]
    [SerializeField] private InputField roomCodeInput;

    [Header("房间默认配置")]
    [SerializeField] private int maxPlayers = 2;

    private void Awake()
    {
        Instance = this;
    }

    /// <summary>点击“创建房间”时调用。</summary>
    public void OnCreateRoom()
    {
        NetworkMode mode = PlayerBasicInfoMgr.Instance.CurrentNetworkMode;
        switch (mode)
        {
            case NetworkMode.LocalServer:
                Debug.Log("[LobbyNetworkBridge] 本地服务器模式：沿用原匹配/建房流程");
                break;
            case NetworkMode.LanHost:
            case NetworkMode.LanClient:
                CreateLanRoom();
                break;
            case NetworkMode.SupabaseOnline:
                CreateSupabaseRoom();
                break;
        }
    }

    /// <summary>点击“加入房间”时调用。</summary>
    public void OnJoinRoom()
    {
        NetworkMode mode = PlayerBasicInfoMgr.Instance.CurrentNetworkMode;
        switch (mode)
        {
            case NetworkMode.LocalServer:
                Debug.Log("[LobbyNetworkBridge] 本地服务器模式：沿用原匹配/加入流程");
                break;
            case NetworkMode.LanHost:
            case NetworkMode.LanClient:
                OpenLanRoomList();
                break;
            case NetworkMode.SupabaseOnline:
                JoinSupabaseRoom();
                break;
        }
    }

    #region 局域网

    private void CreateLanRoom()
    {
        if (lanHostManager == null)
        {
            lanHostManager = FindObjectOfType<LanHostManager>();
            if (lanHostManager == null)
            {
                GameObject go = new GameObject("LanHostManager");
                lanHostManager = go.AddComponent<LanHostManager>();
            }
        }

        RoomInfo room = new RoomInfo
        {
            RoomId = GenerateShortCode(),
            RoomName = $"{PlayerBasicInfoMgr.Instance.GetName()}的房间",
            Mode = PlayerBasicInfoMgr.Instance.GameMode,
            MaxPlayers = Mathf.Max(2, maxPlayers),
            ProtocolVersion = ServerConfig.ProtocolVersion,
            Status = RoomStatus.Waiting
        };

        lanHostManager.StartHosting(room);

        PlayerBasicInfoMgr.Instance.CurrentNetworkMode = NetworkMode.LanHost;
        PlayerBasicInfoMgr.Instance.TargetEndpoint = new NetworkEndpoint(
            "127.0.0.1", ServerConfig.TcpPort, "127.0.0.1", ServerConfig.UdpPort);
        PlayerBasicInfoMgr.Instance.UpdateRoomID(room.RoomId);

        Debug.Log($"[LobbyNetworkBridge] 局域网房间已创建：{room.RoomId}");
    }

    private void OpenLanRoomList()
    {
        if (lanRoomListPanel == null)
        {
            lanRoomListPanel = FindObjectOfType<LanRoomListPanel>();
        }

        if (lanRoomListPanel != null)
        {
            lanRoomListPanel.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogWarning("[LobbyNetworkBridge] 场景中未找到 LanRoomListPanel");
        }
    }

    #endregion

    #region Supabase

    private async void CreateSupabaseRoom()
    {
        IBackendProvider backend = PlayerBasicInfoMgr.Instance.CurrentBackend;
        if (backend == null)
        {
            Debug.LogWarning("[LobbyNetworkBridge] 未设置 CurrentBackend");
            return;
        }

        try
        {
            CreateRoomRequest request = new CreateRoomRequest
            {
                RoomName = $"{PlayerBasicInfoMgr.Instance.GetName()}的房间",
                Mode = PlayerBasicInfoMgr.Instance.GameMode,
                MaxPlayers = Mathf.Max(2, maxPlayers),
                HostEndpoint = new NetworkEndpoint(
                    ServerConfig.ServerIp, ServerConfig.TcpPort,
                    ServerConfig.ServerIp, ServerConfig.UdpPort),
                ProtocolVersion = ServerConfig.ProtocolVersion
            };

            RoomInfo room = await backend.CreateRoomAsync(request);
            PlayerBasicInfoMgr.Instance.TargetEndpoint = room.HostEndpoint;
            PlayerBasicInfoMgr.Instance.UpdateRoomID(room.RoomId);

            Debug.Log($"[LobbyNetworkBridge] Supabase 房间已创建，房间号：{room.RoomId}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[LobbyNetworkBridge] 创建 Supabase 房间失败：{ex}");
        }
    }

    private async void JoinSupabaseRoom()
    {
        if (roomCodeInput == null)
        {
            Debug.LogWarning("[LobbyNetworkBridge] 未绑定 Supabase 房间号输入框");
            return;
        }

        string code = roomCodeInput.text.Trim();
        if (string.IsNullOrEmpty(code))
        {
            Debug.LogWarning("[LobbyNetworkBridge] 房间号为空");
            return;
        }

        IBackendProvider backend = PlayerBasicInfoMgr.Instance.CurrentBackend;
        if (backend == null)
        {
            Debug.LogWarning("[LobbyNetworkBridge] 未设置 CurrentBackend");
            return;
        }

        try
        {
            List<RoomInfo> rooms = await backend.GetRoomListAsync(new GetRoomListRequest
            {
                Mode = null,
                ProtocolVersion = ServerConfig.ProtocolVersion,
                MaxResults = 100
            });

            RoomInfo target = default;
            foreach (RoomInfo room in rooms)
            {
                if (room.RoomId == code || room.RoomName == code)
                {
                    target = room;
                    break;
                }
            }

            if (string.IsNullOrEmpty(target.RoomId))
            {
                Debug.LogWarning($"[LobbyNetworkBridge] 未找到房间：{code}");
                return;
            }

            bool ok = await backend.JoinRoomAsync(target.RoomId);
            if (ok)
            {
                PlayerBasicInfoMgr.Instance.TargetEndpoint = target.HostEndpoint;
                PlayerBasicInfoMgr.Instance.UpdateRoomID(target.RoomId);
                Debug.Log($"[LobbyNetworkBridge] 加入房间成功：{target.RoomId}");
            }
            else
            {
                Debug.LogWarning("[LobbyNetworkBridge] 加入房间失败");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[LobbyNetworkBridge] 加入 Supabase 房间异常：{ex}");
        }
    }

    #endregion

    private static string GenerateShortCode()
    {
        return Guid.NewGuid().ToString("N").Substring(0, 6);
    }
}
