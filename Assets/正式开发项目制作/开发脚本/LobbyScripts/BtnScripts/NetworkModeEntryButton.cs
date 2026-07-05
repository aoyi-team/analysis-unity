using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 大厅模式入口按钮。
/// 根据当前 NetworkMode 将点击事件分发到 LobbyNetworkBridge 的创建房间或加入房间逻辑。
/// 本地服务器模式下不破坏原有按钮流程，仅作为可选入口。
/// </summary>
public class NetworkModeEntryButton : BaseButtonBehaviour
{
    [Header("入口类型")]
    [Tooltip("勾选为创建房间，否则为加入房间")]
    public bool isCreateRoomButton = true;

    [Tooltip("本地服务器模式下是否跳过（让原按钮逻辑处理）")]
    public bool skipInLocalServer = false;

    public override void OnPointerClick(PointerEventData eventData)
    {
        base.OnPointerClick(eventData);

        NetworkMode mode = PlayerBasicInfoMgr.Instance.CurrentNetworkMode;
        if (mode == NetworkMode.LocalServer && skipInLocalServer)
        {
            Debug.Log("[NetworkModeEntryButton] 本地服务器模式，沿用原流程");
            return;
        }

        if (LobbyNetworkBridge.Instance == null)
        {
            Debug.LogWarning("[NetworkModeEntryButton] 场景中缺少 LobbyNetworkBridge");
            return;
        }

        if (isCreateRoomButton)
            LobbyNetworkBridge.Instance.OnCreateRoom();
        else
            LobbyNetworkBridge.Instance.OnJoinRoom();
    }
}
