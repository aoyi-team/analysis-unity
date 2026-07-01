using FixMath;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerManager:MonoBehaviour
{


    private static PlayerManager instance;
    public static PlayerManager Instance
    {
        get
        {
            if(instance == null)
            {
                GameObject o = new GameObject("PlayerManager");
                instance = o.AddComponent<PlayerManager>();
            }
            return instance;
        }
    }
    private Dictionary<string, _playerInfo> playerInfoDic;

    private ModeConfig modeConfig;

    // 玩家逻辑器
    private Dictionary<string, BasePlayerLogic> playerControllerSDic;
    //玩家渲染器
    private Dictionary<string, BasePlayerView> playerRendersDic;
    // 对外
    public BasePlayerView LocalPlayerView { get; private set; }

    // 后续考虑添加一个存放玩家GameObject的字典?

    // 角色预制体     heroId prefab
    // private Dictionary<int, GameObject> characterPrefabDic;

    //当前操作玩家的信息
    private _playerInfo MyplayerInfo;
    public _playerInfo PlayerInfo { get { return MyplayerInfo; } }
    public void Init(List<PlayerData> playerInfos,ModeConfig Config)
    {
        Debug.Log(playerInfos[0].teamId+","+playerInfos[1].teamId);
        // modeConfig = GameStaticValues.modeConfigDic[PlayerBasicInfoMgr.Instance.GameMode];
        // 通过资源加载导入模式配置文件，获取出生点等基本信息  TeamId是从1开始的
        if (Config!=null)
        {
            modeConfig = Config;
        }
        else
        {
            Debug.LogError("PlayerManager Init Error: ModeConfig is null!");
            return;
        }
        playerInfoDic =new Dictionary<string, _playerInfo>(playerInfos.Count);
        playerControllerSDic=new Dictionary<string, BasePlayerLogic>(playerInfos.Count);
        playerRendersDic=new Dictionary<string, BasePlayerView>(playerInfos.Count);
        string userId=PlayerBasicInfoMgr.Instance.GetID();
        IReadOnlyList<IReadOnlyList<FixedVector2>> spawnPoints = modeConfig.SpawnPoints;
        Dictionary<int, int> bornPointCount = new();
        foreach (var _playerInfoEach in playerInfos)
        {
            int teamId= _playerInfoEach.teamId-1;
            int teamIdx=bornPointCount.GetValueOrDefault(teamId, 0);
            _playerInfo player_info = new _playerInfo(_playerInfoEach.userId, _playerInfoEach.HeroId, _playerInfoEach.teamId, spawnPoints[teamId][teamIdx]);
            playerInfoDic[_playerInfoEach.userId] = player_info;
            // Logic注册
            BasePlayerLogic player_Logi = CharacterFactory.CreatePlayerLogic(_playerInfoEach.HeroId);
            if (player_Logi != null)
            {
                playerControllerSDic[_playerInfoEach.userId] = player_Logi;
                int[] skillIDs = player_info.characterConfig.Skillids;
                if(skillIDs!=null)
                {
                    player_Logi.Init(player_info, skillIDs);
                }
            }
            //根据英雄ID加载模型并且导入到字典当中
            //Assets/Resources/HeroPrefabs/101/诺亚.prefab
            GameObject o = BattleResourceManager.Instance.LoadCharacterPrefab(_playerInfoEach.HeroId);
            GameObject player = Instantiate(o, player_info.bornPoint.ToVector2(),Quaternion.identity);
            player.name = $"Player_{_playerInfoEach.userId}";
            Debug.Log("生成角色了!"+player.transform);

            // View注册
            BasePlayerView player_View =CharacterFactory.CreatePlayerView(_playerInfoEach.HeroId,ref player);
            if(player_View != null)
            {
                playerRendersDic[_playerInfoEach.userId]=player_View;
                player_View.InitView(player_info);
            }
            if (player_info.UserId == userId)
            {
                this.MyplayerInfo = player_info;
                this.LocalPlayerView = player_View;
            }
            bornPointCount[teamId] = teamIdx + 1;
            player_info.Init();
        }
    }

    /*****************帧更新接口******************/
    public void OnLogicFrameUpdate(FrameData data)
    {
        List<MsgPlayerOp> op = data.allPlayerOps;
        // todo:后续可能用到随机种子处理操作
        // 逻辑帧更新时，先根据服务器返回的操作数据来更新每个玩家的逻辑状态，然后再调用每个玩家的渲染器来更新渲染状态
        foreach (var eachOP in op)
        {
            string id = eachOP.playerId.ToString("D6");
            if (playerControllerSDic.TryGetValue(id, out BasePlayerLogic value))
            {
                value.OnFrameLogicUpdate(eachOP);
            }
        }
        foreach (var render in playerRendersDic.Values)
        {
            render.OnRenderFrameUpdate();
        }
    }

    #region 外部访问字典接口
    public bool GetPlayerViewById(string id,out BasePlayerView view)
    {
        if (playerRendersDic.TryGetValue(id, out BasePlayerView value))
        {
            view = value;
            return true;
        }
        view = null;
        return false;
    }

    #endregion


}