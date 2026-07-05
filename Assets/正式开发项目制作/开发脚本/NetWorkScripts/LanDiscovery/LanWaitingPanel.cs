using System;
using Aoyi.Mirror;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 局域网等待对手面板。
/// 挂载到等待对手 UI 根节点，显示房间信息、当前玩家数，房主可点击开始战斗。
/// </summary>
public class LanWaitingPanel : MonoBehaviour
{
    [Header("UI 引用")]
    [SerializeField] private Text roomNameText;
    [SerializeField] private Text playerCountText;
    [SerializeField] private Button startBattleBtn;
    [SerializeField] private Button cancelBtn;

    private int _lastPlayerCount = -1;

    private void Awake()
    {
        if (roomNameText == null)
            roomNameText = transform.Find("RoomNameText")?.GetComponent<Text>();
        if (playerCountText == null)
            playerCountText = transform.Find("PlayerCountText")?.GetComponent<Text>();
        if (startBattleBtn == null)
            startBattleBtn = transform.Find("StartBattleBtn")?.GetComponent<Button>();
        if (startBattleBtn == null)
            startBattleBtn = transform.Find("ReadyBtn")?.GetComponent<Button>();
        if (cancelBtn == null)
            cancelBtn = transform.Find("CancelBtn")?.GetComponent<Button>();

        if (startBattleBtn != null)
        {
            startBattleBtn.onClick.AddListener(OnStartBattleClicked);
        }

        if (cancelBtn != null)
        {
            cancelBtn.onClick.AddListener(OnCancelClicked);
        }

        gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        _lastPlayerCount = -1;
        RefreshUI();
    }

    private void Update()
    {
        if (!gameObject.activeSelf) return;

        int currentCount = GetCurrentPlayerCount();
        if (currentCount != _lastPlayerCount)
        {
            _lastPlayerCount = currentCount;
            RefreshUI();
        }
    }

    /// <summary>
    /// 显示等待面板。
    /// </summary>
    public void Show()
    {
        gameObject.SetActive(true);
        RefreshUI();
    }

    /// <summary>
    /// 隐藏等待面板。
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void RefreshUI()
    {
        var mgr = PlayerBasicInfoMgr.Instance;
        if (mgr == null) return;

        if (roomNameText != null)
        {
            roomNameText.text = $"房间号：{mgr.RoomId}";
        }

        if (playerCountText != null)
        {
            bool isHost = mgr.CurrentNetworkMode == NetworkMode.LanHost;
            if (isHost)
            {
                int count = GetCurrentPlayerCount();
                int max = GetRequiredPlayerCount();
                playerCountText.text = $"玩家数：{count}/{max}";
            }
            else
            {
                playerCountText.text = "等待房主开始游戏...";
            }
        }

        // 自动开战，不需要手动按钮
        if (startBattleBtn != null)
        {
            startBattleBtn.gameObject.SetActive(false);
        }
    }

    private int GetCurrentPlayerCount()
    {
        var nm = AoyiNetworkRoomManager.singleton;
        if (nm != null)
        {
            return nm.roomSlots.Count;
        }

        return MirrorNetBridge.ServerPlayers.Count;
    }

    private int GetRequiredPlayerCount()
    {
        var nm = AoyiNetworkRoomManager.singleton;
        if (nm != null)
        {
            return Mathf.Max(1, nm.maxRoomPlayers);
        }

        return 2;
    }

    private void OnStartBattleClicked()
    {
        LanQuickMatchManager.Instance?.HostReadyToBegin();
    }

    private void OnCancelClicked()
    {
        LanQuickMatchManager.Instance?.CancelMatch();
        Hide();
    }

    private void OnDestroy()
    {
        if (startBattleBtn != null)
        {
            startBattleBtn.onClick.RemoveListener(OnStartBattleClicked);
        }

        if (cancelBtn != null)
        {
            cancelBtn.onClick.RemoveListener(OnCancelClicked);
        }
    }
}
