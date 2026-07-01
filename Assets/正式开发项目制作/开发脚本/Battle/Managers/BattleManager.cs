using MsgFramework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

//存放玩家临时数据
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
                instance.Init();
            }
            return instance;
        }
    }
    public void Init()
    {
        this.BattleId = PlayerBasicInfoMgr.Instance.RoomId;
        this.teamID= PlayerBasicInfoMgr.Instance.TeamId;
        this.MyId=PlayerBasicInfoMgr.Instance.GetIntId();
    }

    private readonly static object _lock = new object();

    //保存当前帧数
    public int FrameId { get; private set; }
    //Id信息
    public string BattleId { get; private set; }
    public int MyId { get; private set; }
    public int teamID { get; private set; }
    public void Logic_Update_FrameDic(MsgFramePack msg)
    {
        if (msg.frameId <= FrameId)
        {
            Debug.Log($"该帧已过时!:{msg.frameId}");
            return;
        }
        int len = msg.frames.Count;
        if (len < msg.frameId - FrameId)
        {
            Debug.Log($"帧不完整!当前帧:{FrameId},收到帧:{msg.frameId},帧长度:{len}");
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
    // 唯一保存模式配置的变量，供各个管理器访问
    private ModeConfig modeConfig;

    // 总进度，接收子管理器的进度更新，计算总进度并通知UI
    public float TotalLoadProgress { get; private set; }

    public event Action<float> OnTotalLoadProgressUpdated; // 进度更新事件，参数为当前总进度

    // 唯一标识当前是否战斗
    public static bool IsInBattle = false;
    //逻辑执行帧
    private int currentFrameId;
    private static BattleManager instance;
    // 全员加载完成，可以开始游戏了，通知UI关闭加载面板
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

    public void Init(List<PlayerData> playerInfos)
    {
        StartCoroutine(LoadAllManagers(playerInfos));
    }

    /// <summary>
    /// 加载完成后等待0.2秒，确保所有玩家都进入了战斗场景，之后开始发送准备就绪的UDP消息
    /// </summary>
    /// <returns></returns>
    IEnumerator WaitInitFinisih()
    {
        yield return new WaitForSeconds(0.2f);
        InvokeRepeating("Send_BattleReady", 0.2f, 0.2f);
    }

    public void LogicUpdateFrame(FrameData msg)
    {
        PlayerManager.Instance.OnLogicFrameUpdate(msg);
        // 这里只处理逻辑更新，渲染更新由PlayerManager在Update中处理
        //PlayerManager.Instance.OnRenderFrameUpdate();
        CollisionManager.Instance.LogicUpdate();
    }
    // MsgBattleReady 服务端不用填充，直接发送即可
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
    #region 私有处理方法
    //UDP发送
    private void Send_BattleReady()
    {
        MsgBattleReady msg = new MsgBattleReady();
        msg.roomId = PlayerBasicInfoMgr.Instance.RoomId;
        msg.userId = int.Parse(PlayerBasicInfoMgr.Instance.GetID());
        UDPSocketManager.Instance.Send(msg);
    }

    //发送帧操作
    private void Send_PlayerOp()
    {
        MsgPlayerOp op= new MsgPlayerOp();
        op.roomId = BattleData.Instance.BattleId;
        op.frameId=BattleData.Instance.FrameId+1;//我的下一帧操作
        op.playerId = BattleData.Instance.MyId;
        op.teamId = BattleData.Instance.teamID;
        InputCollect collect= InputManager.Instance.ReturnCurrentFrameInput();
        op.moveDirX = collect.moveDirx;
        op.moveDirY = collect.moveDiry;
        op.animstate = collect.state;
        op.actionCode = collect.code;
        op.flipx = collect.flipx;
        op.isMoving = collect.isMoving;
        UDPSocketManager.Instance.Send(op);
    }

    #endregion

    #region 消息处理方法
    private void HandleFrameData(MsgFramePack msg)
    {
        BattleData.Instance.Logic_Update_FrameDic(msg);
    }

    private void HandleBattleOver(MsgBattleOver msg)
    {
        // 游戏结束处理(暂时方便调试直接跳转场景)
        SceneManager.LoadScene("LobbyPanel");
    }
    //Tcp接收所有玩家准备完成的通知
    public void OnMsgBattleReady(MsgBase msg)
    {
        CancelInvoke("Send_BattleReady");
        // 全都加载完毕，可以关闭面板
        HanleGameReady?.Invoke();
        InvokeRepeating("Send_PlayerOp", 0, ServerConfig.frameTime);
    }


    #endregion

    #region 外部接口调用
    public void GameLoadFin() {
        StartCoroutine(WaitInitFinisih());
    }
    //请求重发帧数据（当发现丢帧时调用）
    public void RequestResendFrames(int frameId)
    {

    }

    // 子管理器调用，上报自己的加载情况
    public void UpdateLoadProgress(float progress)
    {
        TotalLoadProgress += progress; // 这里可以根据实际情况计算总进度
        OnTotalLoadProgressUpdated?.Invoke(TotalLoadProgress); // 通知UI更新进度
    }

    #endregion

    #region 协程加载
    IEnumerator LoadAllManagers(List<PlayerData> playerInfos)
    {
        modeConfig = ResMgr.LoadResource<ModeConfig>($"ModeConfigs/{PlayerBasicInfoMgr.Instance.GameMode}_ModeConfig");
        if (modeConfig == null)
        {
            Debug.LogError($"加载模式配置失败!路径:ModeConfigs/{PlayerBasicInfoMgr.Instance.GameMode}_ModeConfig");
        }
        UDPSocketManager.Instance.InitUDPSocket();
        UDPSocketManager.Instance.Handle = HandleMessage;
        yield return null; // 等待一帧，确保UDP管理器初始化完成
        CollisionManager.Instance.Init(new FixMath.FixedVector2(-11,-11),new FixMath.FixedVector2(11,11));
        yield return null;
        BattleEntityManager.Instance.Init(modeConfig);
        yield return null;// 等待加载
        NetWorkMgr.AddMsgListener("MsgBattleReady", OnMsgBattleReady);
        // 场景加载
        var asyop= SceneMgr.Instance.LoadSceneByName(PlayerBasicInfoMgr.Instance.GameMode);
        while(asyop.isDone==false)
        {
            yield return null;
        }
        asyop.allowSceneActivation = true;
        yield return null; // 等待一帧，确保场景加载完成)
        if(modeConfig!=null)Debug.Log($"成功加载模式配置!模式:{PlayerBasicInfoMgr.Instance.GameMode},地图:{modeConfig.gameMode}");
        PlayerManager.Instance.Init(playerInfos, modeConfig);
        yield return null; // 等待一帧，确保玩家管理器初始化完成
        InputManager.Instance.Init();
        yield return null;
        CameraManager.Instance.Init();
        GameLoadFin();
    }

    #endregion
}