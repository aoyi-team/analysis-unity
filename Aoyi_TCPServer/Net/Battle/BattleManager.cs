using MsgFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static ServerConfig;


class BattleManager
{
    private static BattleManager instance;
    private static bool isUDPSocketStart;
    //对应对局的游戏模式
    public Dictionary<string,GameModes> _roomModeDic=new Dictionary<string,GameModes>();

    //对应对局的对战实例
    public Dictionary<string,BattleInstance> _BattleInstanceDic= new Dictionary<string,BattleInstance>();

    //对应对局的玩家信息
    public Dictionary<string, List<PlayerData>> _roomPlayerInfosDic= new Dictionary<string, List<PlayerData>>();

    //房间锁
    private readonly object _roomLock=new object();

    public static BattleManager Instance
    {
        get
        {
            if(instance == null)
            {
                instance = new BattleManager();
                return instance;
            }
            return instance;
        }
    }

    //开始对局
    public void BeginBattle(string roomId,List<PlayerData> _battleUsers,GameModes mode)
    {
        lock(_roomLock)
        {
            if(!isUDPSocketStart)
            {
                ClientUDP.Instance.InitClientUDP();
                isUDPSocketStart = true;
            }
            _roomPlayerInfosDic[roomId] = _battleUsers;
            _roomModeDic[roomId] = mode;
            BattleInstance _battle = new BattleInstance(roomId, _battleUsers, mode);
            _BattleInstanceDic[roomId] = _battle;
        }
        Console.WriteLine($"房间:{roomId}对局开始!");

    }
    public BattleInstance TryGetBattleInstance(string roomId)
    {
        lock(_roomLock)
        {
            return _BattleInstanceDic[roomId];
        }
    }

    //结束对局,发送对局结束消息
    public void FinishBattle(string roomId)
    {
        lock(_roomLock)
        {
            if (_BattleInstanceDic.TryGetValue(roomId, out BattleInstance battle))
            {
                battle.Dispose();
                _BattleInstanceDic.Remove(roomId);
                _roomPlayerInfosDic.Remove(roomId);
                _roomModeDic.Remove(roomId);
                Console.WriteLine($"销毁房间{roomId}成功");
            }
        }
    }
}
//对战实例
class BattleInstance
{
    public string battleId { get; private set; }

    #region 房间信息数据存储
    //存储对应队伍的玩家 teamid,playerlist
    private Dictionary<string, int> _playerToTeamId;
    //private Dictionary<int, List<string>> _playerListDic;

    //存储玩家的ClientState映射(TCP发送，误差校准，游戏结束等该通道发送)
    private Dictionary<string, ClientState> _playerClientStateDic;

    //玩家的UdpEndPoint id     remoteEndPoint
    private Dictionary<string, IPEndPoint> _playersUdpEndpointDic;

    //玩家选择和队伍信息
    private List<PlayerData> _battleUsers;

    //玩家准备情况     id     ready
    private Dictionary<string, bool> dic_battleReady;

    //玩家数量
    private int playerCount;

    #endregion

    #region 帧同步相关(校验，帧序,历史帧，下一帧)
    //房间全局随机种子
    private readonly int _randSeed;

    //下一帧序号
    private int oldestFrameId;// 历史帧中最旧的帧Id，后续考虑定期清除历史帧数据
    private int frameId;

    //历史帧字典
    private Dictionary<int, FrameData> _allHistoryFrameDic = new Dictionary<int, FrameData>();

    //下一帧存放帧字典(目前先使用userID映射，后续考虑转换成局内ID顺序方便数据的更新)
    private Dictionary<string,MsgPlayerOp> _nextFrameDic= new Dictionary<string, MsgPlayerOp>();

    //玩家的帧Id
    private Dictionary<string,int> _player_opt_frameIdDic= new Dictionary<string,int>();

    //帧数据锁
    private readonly static object _frameLock=new object();

    private System.Timers.Timer _timer;

    #endregion

    #region 状态标识，常态数据
    //战斗是否开始
    private bool isBeginBattle = false;

    //资源控制相关
    private bool _isDisposed = false;//资源释放

    //模式配置
    private ServerConfig.modeConfig _modeConfig;

    //帧同步
    private bool isFrameRunning = false;

    //玩家结束状态
    private Dictionary<string,bool> _isPlayerGameOverDic=new Dictionary<string,bool>();

    private bool OneGameOver;
    private bool AllGameOver;


    #endregion

    #region 模式条件
    private GameModes mode;

    //队伍分数
    private Dictionary<int, int> _teamScoresTotal;

    #endregion


    //private int playerCount;//玩家数量

    //初始化
    public BattleInstance(string battleId,List<PlayerData> playerInfos,GameModes mode)
    {
        this.battleId = battleId;
        this.mode = mode;
        _modeConfig = ServerConfig.modesConfig[mode];
        dic_battleReady = new Dictionary<string, bool>();
        _playerClientStateDic = new Dictionary<string, ClientState>();
        _playerToTeamId = new Dictionary<string, int>();
        _playersUdpEndpointDic= new Dictionary<string,IPEndPoint>();
        _battleUsers=new List<PlayerData>();

        _randSeed = new Random().Next(10000, 99999); // 生成全局随机种子

        _battleUsers = playerInfos;

        playerCount=playerInfos.Count;

        _timer=new System.Timers.Timer(_frameTime);
        _timer.Elapsed += OnFrameTick;
        _timer.AutoReset = true;

        //初始化完成所有字典数据的存储
        foreach (var playerInfo in playerInfos)
        {
            dic_battleReady[playerInfo.userId] = false;
            _playerClientStateDic[playerInfo.userId] = NetManager.GetActiveClientById(playerInfo.userId);
            _playerToTeamId[playerInfo.userId]=playerInfo.teamId;
        }
    }



    #region 外部接口调用

    //添加玩家的UdpIpEndpoint
    public void RecordPlayerUdpEndPoint(string playerId, IPEndPoint ipendPoint)
    {
        lock (_playersUdpEndpointDic)
        {
            _playersUdpEndpointDic[playerId] = ipendPoint;
        }
    }
    public bool IsPlayerContain(string playerid)
    {
        return _playerToTeamId.ContainsKey(playerid);
    }

    //资源释放
    public void Dispose()
    {
        if(_isDisposed) return;
        _isDisposed = true;

        _timer.Stop();
        _timer.Elapsed -= OnFrameTick;
        _timer.Dispose();

        lock (_frameLock)
        {
            _nextFrameDic.Clear();
            _allHistoryFrameDic.Clear();
            _player_opt_frameIdDic.Clear();
        }
        Console.WriteLine($"{battleId}帧同步已停止，最终帧为{frameId}");
    }

    public void HandleMsg(MsgBase msg)
    {
        string protoName = msg.protoName;
        switch (protoName)
        {
            case "MsgPlayerOp":
                {
                    MsgPlayerOp playerOp =(MsgPlayerOp) msg;
                    HandleMsgPlayerOp(playerOp);
                    break;
                }
            case "MsgBattleReady":
                {
                    MsgBattleReady playerReady=(MsgBattleReady) msg;
                    HandleMsgBattleReady(playerReady);
                    break;
                }
            case "MsgBattleOver":
                {
                    MsgBattleOver playerGameOver = (MsgBattleOver)msg;
                    HandleMsgBattleOver(playerGameOver);
                    break;
                }
            case "MsgPlayerExit":
                {
                    //后续处理玩家异常退出的逻辑，暂时先当作游戏结束
                    MsgPlayerExit playerExit = (MsgPlayerExit)msg;
                    HandleMsgBattleOver(new MsgBattleOver() { roomId=playerExit.roomId,userId=playerExit.userId});
                    break;
                }
            default:
                {
                    Console.WriteLine("未找到对应方法");
                    break;
                }
        }
    }

    #endregion

    #region 业务逻辑处理handleMsg
    private void HandleMsgPlayerOp(MsgPlayerOp msg)
    {
        lock(_frameLock)
        {
            //只收本帧
            if (msg.frameId == frameId)
            {
                //操作去重，以最后一次接收为准
                _nextFrameDic[msg.playerId.ToString("D6")] = msg;
                _player_opt_frameIdDic[msg.playerId.ToString("D6")] = frameId;
                return;
            }
            Console.WriteLine($"房间{battleId}玩家{msg.playerId}帧号{msg.frameId}无效，需要帧号为：{frameId}");
            return;
        }
    }

    private void HandleMsgBattleReady(MsgBattleReady msg) 
    {
        if (isBeginBattle) return;
        var playerId = msg.userId.ToString("D6");
        dic_battleReady[playerId] = true;
        isBeginBattle = true;
        foreach(var playerState in dic_battleReady.Values)
        {
            isBeginBattle = isBeginBattle && playerState;
        }
        if(isBeginBattle)
        {
            MsgBattleReady msgReady= (MsgBattleReady) msg;
            msgReady.roomId=msg.roomId;
            foreach(var name in  dic_battleReady.Keys)
            {
                ClientState state = NetManager.GetActiveClientById(name);
                NetManager.Send(state, msgReady);
            }
            BeginBattle();
        }
    }
    private void HandleMsgBattleOver(MsgBattleOver msg)
    {
        OneGameOver = true;
        _isPlayerGameOverDic[msg.userId.ToString("D6")]=true;
        BattleManager.Instance.FinishBattle(battleId);
        //AllGameOver=true;
        //foreach(var value in  dic_battleReady.Values)
        //{
        //    if (value == false)
        //    {
        //        AllGameOver = false;
        //        break;
        //    }
        //}
        //if(AllGameOver)
        //{
        //    var over = new MsgBattleOver();
        //    //后续处理调用battlerManager获取battleInfo然后发回胜负
        //    foreach(var playerState in _playerClientStateDic.Values)
        //    {
        //        NetManager.Send(playerState, over);
        //    }
        //}
    }

    #endregion


    #region 战斗部分
    //开始战斗
    private void BeginBattle()
    {
        frameId = 1;
        oldestFrameId = 1;
        isFrameRunning = false;
        OneGameOver = false;
        AllGameOver = false;

        foreach(var playerId in  dic_battleReady.Keys)
        {
            _player_opt_frameIdDic[playerId] = 0;

        }
        _nextFrameDic.Clear();
        _timer.Start();

    }

    //timeTick间隔时间发布一次操作消息  分发完之后才frameId增加(可能会滞后操作)
    private void OnFrameTick(object sender,System.Timers.ElapsedEventArgs arg)
    {
        if(isFrameRunning||_isDisposed) return;
        isFrameRunning=true;
        try
        {
            lock(_frameLock)//锁住不让访问
            {
                //1.1补发断线客户端的帧,填充要发送的帧
                foreach(var playerId in _playerToTeamId.Keys)
                {
                    if (!_nextFrameDic.ContainsKey(playerId))
                    {
                        MsgPlayerOp playerOp = new MsgPlayerOp()
                        {
                            roomId = battleId,
                            teamId = _playerToTeamId[playerId],
                            playerId=int.Parse(playerId),
                            frameId = this.frameId,
                        };
                        _nextFrameDic[playerId]= playerOp;
                    }
                }
                FrameData frameData = new FrameData();
                frameData.frameId = frameId;
                frameData.randSeed = _randSeed+frameId;
                frameData.allPlayerOps = new List<MsgPlayerOp>();
                ClearOldFrames();
                foreach (var playerOp in _nextFrameDic.Values)
                {
                    frameData.allPlayerOps.Add(playerOp);
                }
                //后续考虑存储历史帧数据
                _allHistoryFrameDic[frameId] = frameData;
                foreach (var playerKeyPair in _playersUdpEndpointDic)
                {
                    //if (_isPlayerGameOverDic[playerKeyPair.Key]) continue;
                    Send_unSync_frame(playerKeyPair.Value, playerKeyPair.Key);
                }
                _nextFrameDic.Clear();
                Console.WriteLine($"发送了第{frameId}帧");
                frameId++;
            }
            //处理获胜逻辑
        }
        catch(Exception ex)
        {
            Console.WriteLine($"房间{battleId}帧{frameId}处理异常：{ex.Message}");
        }
        finally
        {
            isFrameRunning = false;
        }
    }

    private void Send_unSync_frame(IPEndPoint ipPort,string playerId)
    {
        MsgFramePack framePack=new MsgFramePack();
        framePack.roomId=battleId;
        framePack.frameId=frameId;
        for (int i= _player_opt_frameIdDic[playerId];i<=frameId;i++)
        {
            if (_allHistoryFrameDic.ContainsKey(i))
            {
                framePack.frames.Add(_allHistoryFrameDic[i]);
            }
        }
        ClientUDP.Instance.Send(framePack, ipPort);

    }
    // 清除历史帧数据，后续考虑定期清除或者在玩家退出时清除
    private void ClearOldFrames()
    {
        if(frameId - oldestFrameId >= 99)// 只保留最近50帧的历史数据
        {
            lock(_frameLock)
            {
                for(int i=oldestFrameId;i<=frameId-50;i++)
                {
                    _allHistoryFrameDic.Remove(i);
                }
                oldestFrameId = frameId - 49;
            }
        }
    }
    #endregion

    #region 模式胜负判定
    private void InitModesWinCondition(GameModes Gamemode)
    {
        switch(Gamemode)
        {
            case GameModes.paiwei:
                {
                    _teamScoresTotal = new Dictionary<int, int>();
                    break;
                }
            case GameModes.shengcun:
                {
                    break;
                }
            case GameModes.dantiao: { 

                    break;
                }
        }
    }
    #endregion
}
