using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// 局域网房间列表面板
/// 挂载到包含 Scroll View 的房间列表 UI 根节点上。
/// 需要在 Inspector 中绑定：
/// - beaconReceiver（场景中的 LanBeaconReceiver）
/// - contentRoot（ScrollView 的 Content RectTransform）
/// - itemPrefab（LanRoomListItem 预制体）
/// - refreshButton（刷新按钮）
/// - manualJoinButton（手动加入按钮）
/// - manualIpInput（手动输入 IP 的 InputField）
/// </summary>
public class LanRoomListPanel : MonoBehaviour
{
    [Header("Discovery")]
    [SerializeField] private LanBeaconReceiver beaconReceiver;

    [Header("UI References")]
    [SerializeField] private RectTransform contentRoot;
    [SerializeField] private LanRoomListItem itemPrefab;
    [SerializeField] private Button refreshButton;
    [SerializeField] private Button manualJoinButton;
    [SerializeField] private InputField manualIpInput;

    [Header("Events")]
    public UnityEvent OnRoomSelected;

    private readonly List<LanRoomListItem> _spawnedItems = new List<LanRoomListItem>();

    private void OnEnable()
    {
        if (beaconReceiver != null)
        {
            beaconReceiver.OnRoomListUpdated += OnRoomListUpdated;
            beaconReceiver.StartListening();
        }

        if (refreshButton != null)
            refreshButton.onClick.AddListener(RefreshList);

        if (manualJoinButton != null)
            manualJoinButton.onClick.AddListener(OnManualJoinClicked);

        RefreshList();
    }

    private void OnDisable()
    {
        if (beaconReceiver != null)
        {
            beaconReceiver.OnRoomListUpdated -= OnRoomListUpdated;
            beaconReceiver.StopListening();
        }

        if (refreshButton != null)
            refreshButton.onClick.RemoveListener(RefreshList);

        if (manualJoinButton != null)
            manualJoinButton.onClick.RemoveListener(OnManualJoinClicked);

        ClearItems();
    }

    private void OnRoomListUpdated()
    {
        RefreshList();
    }

    /// <summary>重新生成房间列表项</summary>
    private void RefreshList()
    {
        ClearItems();

        if (beaconReceiver == null || contentRoot == null || itemPrefab == null)
        {
            Debug.LogWarning("[LAN] 房间列表面板缺少必要引用，无法刷新列表");
            return;
        }

        var rooms = beaconReceiver.DiscoveredRooms;
        foreach (var room in rooms)
        {
            var item = Instantiate(itemPrefab, contentRoot);
            item.Setup(room, OnItemJoinClicked);
            _spawnedItems.Add(item);
        }

        Debug.Log($"[LAN] 房间列表已刷新，共发现 {rooms.Count} 个房间");
    }

    private void ClearItems()
    {
        foreach (var item in _spawnedItems)
        {
            if (item != null)
                Destroy(item.gameObject);
        }
        _spawnedItems.Clear();
    }

    private void OnItemJoinClicked(LanDiscoveredRoom room)
    {
        if (!room.IsCompatible)
        {
            Debug.LogWarning($"[LAN] 无法加入版本不匹配的房间: {room.RoomInfo.RoomName}");
            return;
        }

        PlayerBasicInfoMgr.Instance.TargetEndpoint = room.RoomInfo.HostEndpoint;
        Debug.Log($"[LAN] 已选择房间: {room.RoomInfo.RoomName}，目标端点 {room.RoomInfo.HostEndpoint.TcpIp}:{room.RoomInfo.HostEndpoint.TcpPort}");
        OnRoomSelected?.Invoke();
    }

    private void OnManualJoinClicked()
    {
        if (manualIpInput == null)
            return;

        string ip = manualIpInput.text.Trim();
        if (string.IsNullOrEmpty(ip))
        {
            Debug.LogWarning("[LAN] 手动加入 IP 为空");
            return;
        }

        var endpoint = new NetworkEndpoint(
            ip,
            ServerConfig.TcpPort,
            ip,
            ServerConfig.UdpPort
        );

        PlayerBasicInfoMgr.Instance.TargetEndpoint = endpoint;
        Debug.Log($"[LAN] 手动加入目标已设置: {ip}:{ServerConfig.TcpPort}");
        OnRoomSelected?.Invoke();
    }
}
