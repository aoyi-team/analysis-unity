using MsgFramework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Aoyi.Mirror;
using Mirror;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

//��������ʱ����
public class BattleData
{
    private static BattleData instance;
    public static BattleData Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new BattleData();
            }
            return instance;
        }
    }
    public void Init(BattleContext ctx)
    {
        this.BattleId = ctx.RoomId;
        this.teamID = ctx.LocalTeamId;
        this.MyId = ctx.LocalPlayerIntId;
        FrameId = 0;
        _lastGapWarningFrame = -1;
        _lastGapWarningTime = -999f;
    }

    private readonly static object _lock = new object();

    //���浱ǰ֡��
//当前帧号
    public int FrameId { get; private set; }
    //Id信息
    public string BattleId { get; private set; }
    public int MyId { get; private set; }
    public int teamID { get; private set; }
    private int _lastGapWarningFrame = -1;
    private float _lastGapWarningTime = -999f;
    /// <summary>
    /// Mirror host 模式下本地推进帧号（单人模式使用�?    /// </summary>
    public void SetFrameId(int id) { FrameId = id; }
    public void Logic_Update_FrameDic(MsgFramePack msg)
    {
        if (msg == null || msg.frames == null || msg.frames.Count == 0)
        {
            return;
        }

        if (msg.frameId <= FrameId)
        {
            if (msg.frameId == _lastGapWarningFrame) return;
            _lastGapWarningFrame = msg.frameId;
            Debug.Log($"[BattleData] 跳过过期帧包: received={msg.frameId}, current={FrameId}");
            return;
        }

        List<FrameData> frameData = msg.frames
            .Where(frame => frame != null && frame.frameId > FrameId)
            .OrderBy(frame => frame.frameId)
            .ToList();

        if (frameData.Count == 0)
        {
            return;
        }

        if (frameData[0].frameId > FrameId + 1)
        {
            if (Time.time - _lastGapWarningTime > 1f || msg.frameId != _lastGapWarningFrame)
            {
                _lastGapWarningTime = Time.time;
                _lastGapWarningFrame = msg.frameId;
                Debug.LogWarning($"[BattleData] 等待补帧: current={FrameId}, received={msg.frameId}, firstFrame={frameData[0].frameId}, count={frameData.Count}");
            }
            return;
        }

        foreach (FrameData frame in frameData)
        {
            if (frame.frameId != FrameId + 1)
            {
                Debug.LogWarning($"[BattleData] 帧序列中�? current={FrameId}, next={frame.frameId}, packFrame={msg.frameId}, count={frameData.Count}");
                break;
            }

            BattleManager.Instance.LogicUpdateFrame(frame);
            FrameId = frame.frameId;
        }
    }
    //public void TryGetFrame(int frameId,out MsgFrameData framData)
    //{
    //    lock(_lock)
    //    {

    //    }
    //}
}

public class BattleManager : MonoBehaviour
{
    // 总加载进度，汇总各管理器的进度更新，得到总进度并通知UI
    public float TotalLoadProgress { get; private set; }

    public event Action<float> OnTotalLoadProgressUpdated; // ���ȸ����¼�������Ϊ��ǰ�ܽ���

    // Ψһ��ʶ��ǰ�Ƿ�ս��
    public static bool IsInBattle = false;
    //�߼�ִ��֡
    private int currentFrameId;
    private bool _battleLoopStarted;
    private bool _battleOverLocalSent;
    private bool _battleOverHandled;
    private static BattleManager instance;
    // ȫԱ������ɣ����Կ�ʼ��Ϸ�ˣ�֪ͨUI�رռ������?
    public event Action HanleGameReady;
    public static BattleManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject o = new GameObject("BattleManager");
                instance = o.AddComponent<BattleManager>();
                DontDestroyOnLoad(o);
                IsInBattle = true;
            }
            return instance;
        }
    }
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    public void Init(BattleContext ctx)
    {
        IsInBattle = true;
        _battleLoopStarted = false;
        _battleOverLocalSent = false;
        _battleOverHandled = false;
        CancelInvoke("Send_BattleReady");
        CancelInvoke("Send_PlayerOp");
        UnregisterBattleMessageListeners();
        StartCoroutine(LoadAllManagers(ctx));
    }

    public static bool ShouldEndBattleAfterDeath(IEnumerable<_playerInfo> players)
    {
        return players != null && players.Any(player => player != null && player.IsDead);
    }

    public static string[] GetBattleInitCommandNames()
    {
        return CreateBattleInitCommands().Select(cmd => cmd.Name).ToArray();
    }

    private static ILoadCommand[] CreateBattleInitCommands()
    {
        return new ILoadCommand[]
        {
            new CmdLoadModeConfig(),
            new CmdInitUDPSocket(),
            new CmdInitCollisionManager(),
            new CmdInitBattleEntity(),
            new CmdLoadBattleScene(),
            new CmdInitPlayerManager(),
            new CmdRegisterBattleMsg(),
            new CmdInitInputManager(),
            new CmdInitCameraManager(),
            new CmdGameLoadFinish()
        };
    }

    /// <summary>
    /// 加载完成后等�?.2秒，确保所有玩家都进入战斗场景，之后开始准备流程发送UDP消息�?
    /// Mirror host 模式下无需等待 UDP 回包，直接触�?OnMsgBattleReady�?
    /// </summary>
    IEnumerator WaitInitFinisih()
    {
        yield return new WaitForSeconds(0.2f);
        InvokeRepeating("Send_BattleReady", 0.2f, 0.2f);
    }

    public void LogicUpdateFrame(FrameData msg)
    {
        if (!IsInBattle || _battleOverHandled)
        {
            return;
        }

        PlayerManager.Instance.OnLogicFrameUpdate(msg);
        // ����ֻ�����߼����£���Ⱦ������PlayerManager��Update�д���
        //PlayerManager.Instance.OnRenderFrameUpdate();
        CollisionManager.Instance.LogicUpdate();
        TrySendBattleOverAfterDeath();
    }
    // MsgBattleReady ����˲�����䣬ֱ�ӷ��ͼ���
    public void HandleMessage(MsgBase msg)
    {
        switch (msg.protoName)
        {
            case "MsgFramePack":
                {

                    HandleFrameData((MsgFramePack)msg);
                    break;
                }
            case "MsgBattleOver":
                {
                    HandleBattleOver((MsgBattleOver)msg);
                    break;
                }
        }
    }
    #region ˽�д�������
    //UDP����
    private void Send_BattleReady()
    {
        MsgBattleReady msg = new MsgBattleReady();
        msg.roomId = BattleData.Instance.BattleId;
        msg.userId = BattleData.Instance.MyId;
        if (MirrorNetBridge.IsMirrorActive)
        {
            NetWorkMgr.Send(msg);
        }
        else
        {
            UDPSocketManager.Instance.Send(msg);
        }
    }

    //����֡����
private void Send_BattleOver(_playerInfo deadPlayer)
    {
        MsgBattleOver msg = new MsgBattleOver();
        msg.roomId = BattleData.Instance.BattleId;
        msg.userId = GetBattleOverUserId(deadPlayer);

        if (MirrorNetBridge.IsMirrorActive)
        {
            NetWorkMgr.Send(msg);
        }
        else
        {
            UDPSocketManager.Instance.Send(msg);
        }
    }

    private void Send_PlayerOp()
    {
        MsgPlayerOp op= new MsgPlayerOp();
        op.roomId = BattleData.Instance.BattleId;
        op.frameId=BattleData.Instance.FrameId+1;//�ҵ���һ֡����
        op.playerId = BattleData.Instance.MyId;
        op.teamId = BattleData.Instance.teamID;
        InputCollect collect= InputManager.Instance.ReturnCurrentFrameInput();
        op.moveDirX = collect.moveDirx;
        op.moveDirY = collect.moveDiry;
        op.targetX = collect.targetX;
        op.targetY = collect.targetY;
        op.animstate = collect.state;
        op.actionCode = collect.code;
        op.flipx = collect.flipx;
        op.isMoving = collect.isMoving;

        if (MirrorNetBridge.IsMirrorActive)
        {
            NetWorkMgr.Send(op);
        }
        else
        {
            UDPSocketManager.Instance.Send(op);
        }
    }

    #endregion

    #region ��Ϣ��������
    private void HandleFrameData(MsgFramePack msg)
    {
        BattleData.Instance.Logic_Update_FrameDic(msg);
    }

    private void HandleBattleOver(MsgBattleOver msg)
    {
        if (_battleOverHandled)
        {
            return;
        }

        Debug.Log($"[BattleManager] 收到战斗结束消息，deadUserId={msg?.userId}, roomId={msg?.roomId}");
        _battleOverHandled = true;
        StopBattleLoop(true);
        StartCoroutine(ReturnToLobbyAfterBattleOver());
    }

    private IEnumerator ReturnToLobbyAfterBattleOver()
    {
        // 避免在 Mirror 网络消息回调栈内直接切场景，防止同一帧后续逻辑访问已卸载对象。
        yield return null;
        yield return null;

        try
        {
            if (MirrorNetBridge.IsMirrorActive && AoyiNetworkRoomManager.singleton != null && NetworkClient.active)
            {
                AoyiNetworkRoomManager.singleton.StopClient();
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[BattleManager] 停止 Mirror Client 时发生异常，继续返回大厅: {ex.Message}");
        }

        if (SceneManager.GetActiveScene().name != "LobbyPanel")
        {
            SceneManager.LoadScene("LobbyPanel");
        }
    }
    //Tcp�����������׼����ɵ�֪ͨ
    public void OnMsgBattleReady(MsgBase msg)
    {
        if (_battleLoopStarted)
        {
            return;
        }

        _battleLoopStarted = true;
        CancelInvoke("Send_BattleReady");
        // ȫ��������ϣ����Թر����
        HanleGameReady?.Invoke();
        InvokeRepeating("Send_PlayerOp", 0, ServerConfig.frameTime);
    }


    #endregion
    private void TrySendBattleOverAfterDeath()
    {
        if (_battleOverLocalSent || _battleOverHandled) return;

        IEnumerable<_playerInfo> players = PlayerManager.Instance.AllPlayerInfos;
        _playerInfo deadPlayer = players?.FirstOrDefault(player => player != null && player.IsDead);
        if (deadPlayer == null) return;

        _battleOverLocalSent = true;
        CancelInvoke("Send_BattleReady");
        CancelInvoke("Send_PlayerOp");
        Send_BattleOver(deadPlayer);
    }

    private static int GetBattleOverUserId(_playerInfo deadPlayer)
    {
        if (deadPlayer != null && int.TryParse(deadPlayer.UserId, out int userId))
        {
            return userId;
        }

        return BattleData.Instance.MyId;
    }

    private void StopBattleLoop(bool unregisterListeners)
    {
        IsInBattle = false;
        CancelInvoke("Send_BattleReady");
        CancelInvoke("Send_PlayerOp");

        if (unregisterListeners)
        {
            UnregisterBattleMessageListeners();
        }
    }

    private void UnregisterBattleMessageListeners()
    {
        NetWorkMgr.RemoveMsgListener("MsgBattleReady", OnMsgBattleReady);
        NetWorkMgr.RemoveMsgListener("MsgFramePack", HandleMessage);
        NetWorkMgr.RemoveMsgListener("MsgBattleOver", HandleMessage);
    }

    private void OnDestroy()
    {
        UnregisterBattleMessageListeners();
    }


    #region �ⲿ�ӿڵ���
    public void GameLoadFin() {
        StartCoroutine(WaitInitFinisih());
    }
    //�����ط�֡���ݣ������ֶ�֡ʱ���ã�
    public void RequestResendFrames(int frameId)
    {

    }

    // �ӹ��������ã��ϱ��Լ��ļ������?
    public void UpdateLoadProgress(float progress)
    {
        TotalLoadProgress += progress; // ������Ը���ʵ����������ܽ���
        OnTotalLoadProgressUpdated?.Invoke(TotalLoadProgress); // ֪ͨUI���½���
    }

    #endregion

    #region Э�̼���
    IEnumerator LoadAllManagers(BattleContext ctx)
    {
        var loadCtx = new LoadContext(ctx);

        loadCtx.UdpMessageHandler = HandleMessage;
        loadCtx.OnBattleReadyMsgHandler = OnMsgBattleReady;
        loadCtx.OnFramePackMsgHandler = HandleMessage;
        loadCtx.OnGameReady = () => HanleGameReady?.Invoke();
        loadCtx.StartBattleReadyLoop = () =>
        {
            StartCoroutine(WaitInitFinisih());
        };

        var chain = new MacroCommand("BattleInit", commandsPerFrame: 2)
            .AddRange(CreateBattleInitCommands());

        yield return chain.Execute(loadCtx);
    }

    #endregion
}
