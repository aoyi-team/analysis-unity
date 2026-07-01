using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 局域网房间列表项
/// 挂载到列表项 Prefab 的 RectTransform 上，配合 LanRoomListPanel 使用。
/// 需要在 Inspector 中绑定以下 UI 元素：
/// - roomNameText（房间名）
/// - modeText（游戏模式）
/// - playerCountText（人数）
/// - statusText（状态/兼容性提示）
/// - joinButton（加入按钮）
/// </summary>
public class LanRoomListItem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI roomNameText;
    [SerializeField] private TextMeshProUGUI modeText;
    [SerializeField] private TextMeshProUGUI playerCountText;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Button joinButton;

    private LanDiscoveredRoom _room;
    private Action<LanDiscoveredRoom> _onJoinClicked;

    /// <summary>初始化列表项显示</summary>
    public void Setup(LanDiscoveredRoom room, Action<LanDiscoveredRoom> onJoinClicked)
    {
        _room = room;
        _onJoinClicked = onJoinClicked;

        if (roomNameText != null)
            roomNameText.text = room.RoomInfo.RoomName;

        if (modeText != null)
            modeText.text = room.RoomInfo.Mode.ToString();

        if (playerCountText != null)
            playerCountText.text = $"{room.RoomInfo.CurrentPlayers}/{room.RoomInfo.MaxPlayers}";

        if (statusText != null)
        {
            if (room.IsCompatible)
            {
                statusText.text = room.RoomInfo.Status.ToString();
            }
            else
            {
                statusText.text = $"版本不匹配 (v{room.RoomInfo.ProtocolVersion})";
            }
        }

        if (joinButton != null)
        {
            joinButton.interactable = room.IsCompatible;
            joinButton.onClick.RemoveAllListeners();
            joinButton.onClick.AddListener(OnJoinButtonClicked);
        }
    }

    private void OnJoinButtonClicked()
    {
        if (_room == null || !_room.IsCompatible)
            return;

        _onJoinClicked?.Invoke(_room);
    }
}
