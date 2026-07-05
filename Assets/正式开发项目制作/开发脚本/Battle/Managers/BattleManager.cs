using MsgFramework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    }

    private readonly static object _lock = new object();

    //���浱ǰ֡��//当前帧号
    public int FrameId { get; private set; }
    //Id信息
    public string BattleId { get; private set; }
    public int MyId { get; private set; }
    public int teamID { get; private set; }
    /// <summary>
    /// Mirror host 模式下本地推进帧号（单人模式使用）
    /// </summary>
    public void SetFrameId(int id) { FrameId = id; }
    public void Logic_Update_FrameDic(MsgFramePack msg)
    {
        if (msg.frameId <= FrameId)
        {
            Debug.Log($"��֡�ѹ�ʱ!:{msg.frameId}");
            return;
        }
        int len = msg.frames.Count;
        if (len < msg.frameId - FrameId)
        {
            Debug.Log($"֡������!��ǰ֡:{FrameId},�յ�֡:{msg.frameId},֡����:{len}");
            return;
        }
        List<FrameData> frameData = msg.frames;
        for (int i = 0; i < len; i++)
        {
            if (FrameId > frameData[i].frameId) continue;
            BattleManager.Instance.LogicUpdateFrame(frameData[i]);
        }
        FrameId = msg.frameId;
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
    private static BattleManager instance;
    // ȫԱ������ɣ����Կ�ʼ��Ϸ�ˣ�֪ͨUI�رռ������
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
        StartCoroutine(LoadAllManagers(ctx));
    }

    /// <summary>
    /// 加载完成后等待0.2秒，确保所有玩家都进入战斗场景，之后开始准备流程发送UDP消息。
    /// Mirror host 模式下无需等待 UDP 回包，直接触发 OnMsgBattleReady。
    /// </summary>
    IEnumerator WaitInitFinisih()
    {
        yield return new WaitForSeconds(0.2f);
        // Mirror host 模式：自己就是服务器，无需等待 MsgBattleReady 回包
        if (global::Mirror.NetworkServer.active && global::Mirror.NetworkClient.active)
        {
            Debug.Log("[BattleManager] Mirror host 模式，直接触发 OnMsgBattleReady");
            OnMsgBattleReady(null);
        }
        else
        {
            InvokeRepeating("Send_BattleReady", 0.2f, 0.2f);
        }
    }

    public void LogicUpdateFrame(FrameData msg)
    {
        PlayerManager.Instance.OnLogicFrameUpdate(msg);
        // ����ֻ�����߼����£���Ⱦ������PlayerManager��Update�д���
        //PlayerManager.Instance.OnRenderFrameUpdate();
        CollisionManager.Instance.LogicUpdate();
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
        UDPSocketManager.Instance.Send(msg);
    }

    //����֡����
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
        op.animstate = collect.state;
        op.actionCode = collect.code;
        op.flipx = collect.flipx;
        op.isMoving = collect.isMoving;

        // Mirror host 模式（含单人模式）：自己就是服务器，直接本地打包成 FrameData 应用，不走 UDP
        if (global::Mirror.NetworkServer.active && global::Mirror.NetworkClient.active)
        {
            FrameData frame = new FrameData();
            frame.frameId = op.frameId;
            frame.allPlayerOps = new List<MsgPlayerOp> { op };
            LogicUpdateFrame(frame);
            BattleData.Instance.SetFrameId(op.frameId);
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
        // ��Ϸ��������(��ʱ�������ֱ����ת����)
        SceneManager.LoadScene("LobbyPanel");
    }
    //Tcp�����������׼����ɵ�֪ͨ
    public void OnMsgBattleReady(MsgBase msg)
    {
        CancelInvoke("Send_BattleReady");
        // ȫ��������ϣ����Թر����
        HanleGameReady?.Invoke();
        InvokeRepeating("Send_PlayerOp", 0, ServerConfig.frameTime);
    }


    #endregion

    #region �ⲿ�ӿڵ���
    public void GameLoadFin() {
        StartCoroutine(WaitInitFinisih());
    }
    //�����ط�֡���ݣ������ֶ�֡ʱ���ã�
    public void RequestResendFrames(int frameId)
    {

    }

    // �ӹ��������ã��ϱ��Լ��ļ������
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
        loadCtx.OnGameReady = () => HanleGameReady?.Invoke();
        loadCtx.StartBattleReadyLoop = () =>
        {
            InvokeRepeating("Send_BattleReady", 0.2f, 0.2f);
            StartCoroutine(WaitInitFinisih());
        };

        var chain = new MacroCommand("BattleInit", commandsPerFrame: 2)
            .Add(new CmdLoadModeConfig())
            .Add(new CmdInitUDPSocket())
            .Add(new CmdInitCollisionManager())
            .Add(new CmdInitBattleEntity())
            .Add(new CmdRegisterBattleMsg())
            .Add(new CmdLoadBattleScene())
            .Add(new CmdInitPlayerManager())
            .Add(new CmdInitInputManager())
            .Add(new CmdInitCameraManager())
            .Add(new CmdGameLoadFinish());

        yield return chain.Execute(loadCtx);
    }

    #endregion
}
