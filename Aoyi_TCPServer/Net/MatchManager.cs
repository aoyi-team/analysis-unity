using MsgFramework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;

/// <summary>
/// 游戏模式排位=0，赏金=1
/// </summary>
public enum GameModes
{
    paiwei = 0, shengcun=1,dantiao=2
}
// 服务端新增匹配管理类
public class MatchManager
{
    // 新增房间类
    public class GameRoom
    {
        public class BattleTeam {
            public int num { get { return playerIds.Count; } }
            public int teamId { get; private set; }
            private int maxNum;
            public bool IsFull { get { return num >= maxNum; } }
            //剩余容量
            public int leftCapacity { get { return maxNum - num; } }
            public List<string> playerIds { get; private set; }
            //玩家选择        玩家id, 英雄id
            public List<Tuple<string,int>> _playerChooseList = new List<Tuple<string, int>>();
            public BattleTeam(int teamId,int maxNum,List<string> playerIds)//初始化塞入所有该队伍玩家//用于创建队伍调用
            {
                Console.WriteLine($"battleTeam:{teamId},maxnum:{maxNum},players:{playerIds}");
                this.teamId = teamId;
                this.maxNum = maxNum;
                this.playerIds = new List<string>();
                this.playerIds.AddRange(playerIds);
            }

            //用于简单初始化队伍
            public BattleTeam(int teamid,int maxNum) { 
                this.maxNum = maxNum;
                this.teamId = teamid;
                playerIds=new List<string>(maxNum);
            }
            public void Join(string id)
            {
                playerIds.Add(id);
            }
            public void Join(List<string> playerIds,List<PlayerChooseCache> playerChooseList)
            {
                this.playerIds.AddRange(playerIds);
                for(int i = 0;i<playerIds.Count;i++)
                {
                    _playerChooseList.Add(new Tuple<string, int>(playerIds[i], playerChooseList[i].selectedHeroId));
                }
            }
            public void Exit(string id)
            {
                if (playerIds.Remove(id)) _playerChooseList.RemoveAll(o => o.Item1 == id);
            }
        }

        
        public GameRoom(string RoomId,GameModes mode)
        {
            this.roomId=RoomId;
            switch (mode)
            {
                case GameModes.paiwei:
                    {
                        this.thisGameRoomConfig = ServerConfig.modesConfig[mode];
                        break;
                    }
                case GameModes.shengcun:
                    {
                        this.thisGameRoomConfig = ServerConfig.modesConfig[mode]; break;
                    }
                case GameModes.dantiao:
                    {
                        this.thisGameRoomConfig = ServerConfig.modesConfig[mode]; break;
                    }
            }
            Teams = new List<BattleTeam>(thisGameRoomConfig.MaxTeamNum);

            //初始化队伍配置
            for(int i=1; i<=thisGameRoomConfig.MaxTeamNum;i++)
            {
                Teams.Add(new BattleTeam(i,thisGameRoomConfig.EachTeamNum));
            }
        }
        #region 队伍和房间配置
        public List<BattleTeam> Teams { get; private set; }//队伍列表
        //房间锁
        public static readonly object RoomLock=new object();

        //当前模式配置
        public ServerConfig.modeConfig thisGameRoomConfig=new ServerConfig.modeConfig();
        public int currentTeamCount { get { return Teams.Count; }}//当前队伍数量
        public int currentRoomNumber { get {
                return Teams.Sum(o => o.num);
            } }//当前玩家数量
        #endregion
        //玩家退出放假
        public void Exit(string id)
        {
            foreach(var team in Teams)
            {
                foreach(var xid in team.playerIds)
                {
                    if(xid==id)
                    {
                        team.Exit(xid);
                        break;
                    }
                }
            }
        }

        //剩余房间容量
        public int leftPlayerCapacity { get { return thisGameRoomConfig.MaxPlayerNum - currentRoomNumber; } }

        //存储房间玩家所有对应的Socket


        public string roomId;                  // 房间ID
        public bool IsFull { get { return leftPlayerCapacity == 0; } }
        //
        public bool TryJoinTeam(List<string> playerList,List<PlayerChooseCache> playerChooseList)
        {
            var playerCounts= playerChooseList.Count;
            lock(RoomLock)
            {
                if(leftPlayerCapacity<playerCounts||IsFull) return false;
                if(Teams!=null)
                {
                    //查找符合条件的队伍
                    foreach (var team in Teams)
                    {
                        if (team.leftCapacity >= playerCounts)
                        {
                            team.Join(playerList, playerChooseList);
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        //检查满足对战需求
        public bool CheckCanFight()
        {
            if(IsFull) return true;
            return false;
        }
        public List<Tuple< int,string,int>> GetRoomAllPlayersInfo()
        {
            //                                  teamid,playerId,heroId
            var allPlayersInfo = new List<Tuple<int, string,int>>();

            foreach(var team in Teams)
            {
                var teamid = team.teamId;
                foreach (var playerCache in team._playerChooseList)
                {
                    allPlayersInfo.Add(new Tuple<int, string,int>(teamid, playerCache.Item1, playerCache.Item2));
                }
            }
            return allPlayersInfo;
        }
    }

    //匹配队列(对应模式)
    private  Dictionary<GameModes,Queue<ClientState>> _modesQueue= new Dictionary<GameModes, Queue<ClientState>>() {
        { GameModes.dantiao,new Queue<ClientState>() },{ GameModes.shengcun,new Queue<ClientState>()},{ GameModes.paiwei,new Queue<ClientState>()}
    };
    // 房间字典（key:房间模式，value:对应模式的所有房间列表）
    private  Dictionary<GameModes, List<GameRoom>> _roomDict = new Dictionary<GameModes, List<GameRoom>>() { {GameModes.shengcun,new List<GameRoom>() } ,
        { GameModes.paiwei,new List<GameRoom>()} ,{ GameModes.dantiao,new List<GameRoom>()} };

    //对应游戏模式锁
    private static readonly Dictionary<GameModes, object> _modeLocks = new Dictionary<GameModes, object>()
{
    { GameModes.dantiao, new object() },
    { GameModes.shengcun, new object() },
    { GameModes.paiwei, new object() }
};

    //玩家到房间的映射，便于直接退出该房间。
    private  Dictionary<string,GameRoom> playerIDRoomMap= new Dictionary<string,GameRoom>();

    //单例模式
    private static  MatchManager _Instance;
    private readonly static object _instanceLock = new object();

    public static MatchManager Instance
    {
        get
        {
            if( _Instance == null )
            {
                lock( _instanceLock )
                {
                    if(_Instance == null )
                    {  _Instance = new MatchManager();
                    }
                }
            }
            return _Instance;
        }
    }

    //匹配方法
    public GameRoom Join(List<string> playerList,List<PlayerChooseCache> playerChooseList,GameModes mode)
    {
        bool isOk = false;
        GameRoom battleRoom = null;
        foreach(var Room in _roomDict[mode])
        {
            if (Room == null) continue;
            if(Room.TryJoinTeam(playerList, playerChooseList))
            {
                battleRoom = Room;
                isOk = true;
                break;
            }
        }
        Console.WriteLine($"加入房间{isOk}");
        if(!isOk)
        {
            Console.WriteLine("创建房间");
            battleRoom = new GameRoom(TimeStamp._GetTimeStampInstance().GetId(),mode);
            _roomDict[mode].Add(battleRoom);
            battleRoom.TryJoinTeam(playerList, playerChooseList);
        }
        foreach(var id in playerList)
        {
            playerIDRoomMap[id] = battleRoom;
        }
        return battleRoom;
    }

    // 处理客户端匹配请求，加入到匹配队列
    public  void HandleMatchRequest(ClientState client, MsgMatchRequest req)
    {
        GameModes o = req.GameModes;
        var playerChooseList = req.playerPack;
        List<string> playerList=playerChooseList.Select(x => x.userId.ToString("D6")).ToList();
        try
        {
            GameRoom battleRoom = Join(playerList, playerChooseList, o);
            if(battleRoom.CheckCanFight())
            {
                var allPlayersInfo = battleRoom.GetRoomAllPlayersInfo();
                List<PlayerData> roomAllPlayersInfo = new List<PlayerData>();
                foreach(var playerinfo in allPlayersInfo)
                {
                    PlayerData player = new PlayerData();
                    player.teamId = playerinfo.Item1;
                    player.userId = playerinfo.Item2;
                    player.HeroId = playerinfo.Item3;
                    roomAllPlayersInfo.Add(player);
                    playerIDRoomMap.Remove(player.userId);
                }
                Console.WriteLine($"房间{battleRoom.roomId},当前人数:{battleRoom.currentRoomNumber},游戏模式:{o},开始游戏");
                MsgMatchSuccess msg = new  MsgMatchSuccess() {
                    roomId = battleRoom.roomId,
                    playerInfos = roomAllPlayersInfo
                };
                StartGame(battleRoom.roomId, roomAllPlayersInfo, o);
                foreach (var player in roomAllPlayersInfo)
                {
                    SendMatchSuccessToPlayer(NetManager.GetActiveClientById(player.userId), msg);
                    Console.WriteLine("发送给玩家"+player.userId+" 匹配成功");
                }
                _roomDict[o].Remove(battleRoom);
                return;
            }
            //todo:房间人数广播
            Console.WriteLine($"房间{battleRoom.roomId},当前人数:{battleRoom.currentRoomNumber}");

        }
        catch(Exception ex)
        {
            Console.WriteLine("加入匹配失败:"+ex);
        }


        /*if (!_modesQueue.ContainsKey(o) || !_modeLocks.ContainsKey(o))
        {
            // 给客户端返回“无效游戏模式”的错误响应
            //client.SendErrorResponse("无效的游戏模式");
            return;
        }*/

        // 2. 加入对应模式匹配队列
        /*lock (_modeLocks[o])
        {
            _modesQueue[o].Enqueue(client);

            switch (o)
            {
                case GameModes.dantiao:
                    if (_modesQueue[o].Count >= 2)
                    {
                        roomPlayers = DequeuePlayers(o, 2);
                    }
                    break;
                case GameModes.shengcun:
                    if (_modesQueue[o].Count >= 10)
                    {
                        roomPlayers = DequeuePlayers(o, 10);
                    }
                    break;
                case GameModes.paiwei:
                    if (_modesQueue[o].Count >= 15)
                    {
                        roomPlayers = DequeuePlayers(o, 15);
                    }
                    break;
            }

        }*/
    }

    //开启对战房间实例
    private void StartGame(string roomId,List<PlayerData> _matchUserData,GameModes mode)
    {
        BattleManager.Instance.BeginBattle(roomId,_matchUserData,mode);
    }


    //退出房间匹配请求(和退出组队做区分)
    public void HandleExitRequest(ClientState client,MsgExitRequest req)
    {
        try
        {
            GameModes mode = req.mode;
            List<string> playerList = req.PlayerList.Select(o => o.ToString("D6")).ToList()?? new List<string>();
            GameRoom room= Exit(playerList, mode);
            Console.WriteLine("退出匹配成功:"+client.userId);
        }
        catch (Exception ex)
        {
            Console.WriteLine("退出匹配失败" + ex);
        }
    }

    public GameRoom Exit(List<string> PlayerList,GameModes mode)
    {
        var TargetRoom = playerIDRoomMap[PlayerList[0]];
        foreach(var id in PlayerList)
        {
            playerIDRoomMap.Remove(id);
            TargetRoom.Exit(id);
        }
        return TargetRoom;
    }

    // 抽离出队逻辑，保证原子性
    /*private static List<ClientState> DequeuePlayers(GameModes mode, int count)
    {
        List<ClientState> players = new List<ClientState>();
        // 加双层校验：避免队列长度不足时 Dequeue 抛异常
        int actualCount = Math.Min(count, _modesQueue[mode].Count);
        for (int i = 0; i < actualCount; i++)
        {
            ClientState player = _modesQueue[mode].Dequeue();
            // 标记玩家已退出匹配（避免重复匹配）
            player.isInMatching = false;
            players.Add(player);
        }
        return players;
    }*/

    /*
    // 创建双人房间
    private static void CreateRoom(List<ClientState> roomPlayers)
    {
        // 生成唯一房间ID
        string roomId = Guid.NewGuid().ToString("N").Substring(0, 8);

        // 创建房间对象
        GameRoom room = new GameRoom(roomId,)
        {
            roomId = roomId,
            players = new List<ClientState> { player1, player2 },
            isGameStart = false
        };
        _roomDict.Add(roomId, room);

        // 赋值房间ID给玩家
        player1.roomId = roomId;
        player2.roomId = roomId;
        player1.isInMatching = false;
        player2.isInMatching = false;

        // 4. 向两个玩家发送匹配成功协议
        SendMatchSuccessToPlayer(player1, player2);
        SendMatchSuccessToPlayer(player2, player1);
    }*/

    // 发送匹配成功通知（包含对手信息）

    private static void SendMatchSuccessToPlayer(ClientState self, MsgMatchSuccess msg)
    {
        // 通过NetManager发送协议
        NetManager.Send(self, msg);
    }
}


/// <summary>
/// 雪花算法获取时间戳作为房间roomId
/// </summary>
public class TimeStamp
{
    private long _lastTimeStamp;
    private long _sequence;//计数序列(从0开始)
    private readonly DateTime? _initialDateTime;//初始日期时间(看构造的时候要不要赋值)
    private static TimeStamp _timeStamp;//单例模式
    private const int MAX_NUMBER = 9999;

    //构造函数用于赋值初始日期时间(用于做时间差值)
    private TimeStamp(DateTime? initialTime)
    {
        _initialDateTime = initialTime;
    }
    protected DateTime InitialDateTime
    {
        get
        {
            if(_initialDateTime == null||_initialDateTime == DateTime.MinValue) return new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc);
            return _initialDateTime.Value;
        }
    }

    //获取时间戳算法实例
    public static TimeStamp _GetTimeStampInstance(DateTime? initialTime=null)
    {
        if(_timeStamp == null)
        {
            System.Threading.Interlocked.CompareExchange(ref _timeStamp,new TimeStamp(initialTime),null) ;
        }
        return _timeStamp;
    }
    //获取时间戳
    public string GetId()
    {
        long temp;
        var timeStamp=GetUniqueTimeStamp(out temp);
        return $"{timeStamp:D10}{temp:D4}";
    }

    //简单时间戳
    private long GetTimeStamp()
    {
        if (InitialDateTime >= DateTime.Now) throw new Exception("时间初值比现在还大");
        var ts=DateTime.UtcNow-InitialDateTime;
        return (long)ts.TotalMilliseconds;
    }

    //获取独一无二时间戳，避免同一时间的冲突问题
    private long GetUniqueTimeStamp(out long temp)
    {
        lock (this)
        {
            temp = 1;
            var timeStamp=GetTimeStamp();
            if(timeStamp==_lastTimeStamp)
            {
                _sequence++;
                temp = _sequence;
                if(temp>MAX_NUMBER)
                {
                    timeStamp=GetTimeStamp() ;
                    _lastTimeStamp = timeStamp ;
                    temp = _sequence=1;
                }
            }
            else
            {
                _sequence = 1;
                _lastTimeStamp = timeStamp;
            }
            return timeStamp;
        }
    }
}