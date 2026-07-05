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

    // ����߼���
    private Dictionary<string, BasePlayerLogic> playerControllerSDic;
    //�����Ⱦ��
    private Dictionary<string, BasePlayerView> playerRendersDic;
    // ����
    public BasePlayerView LocalPlayerView { get; private set; }

    // ������������һ��������GameObject���ֵ�?

    // ��ɫԤ����     heroId prefab
    // private Dictionary<int, GameObject> characterPrefabDic;

    //��ǰ������ҵ���Ϣ
    private _playerInfo MyplayerInfo;
    public _playerInfo PlayerInfo { get { return MyplayerInfo; } }
    public void Init(BattleContext ctx,ModeConfig Config)
    {
        if (ctx.AllPlayers == null || ctx.AllPlayers.Count == 0)
        {
            Debug.LogError("[PlayerManager] Init Error: playerInfos is null or empty!");
            return;
        }
        Debug.Log($"[PlayerManager] Init 开始，playerInfos[0].teamId={ctx.AllPlayers[0].teamId}, playerInfos[1]?.teamId={(ctx.AllPlayers.Count > 1 ? ctx.AllPlayers[1].teamId : -1)}");

        if (Config!=null)
        {
            modeConfig = Config;
        }
        else
        {
            Debug.LogError("PlayerManager Init Error: ModeConfig is null!");
            return;
        }
        playerInfoDic =new Dictionary<string, _playerInfo>(ctx.AllPlayers.Count);
        playerControllerSDic=new Dictionary<string, BasePlayerLogic>(ctx.AllPlayers.Count);
        playerRendersDic=new Dictionary<string, BasePlayerView>(ctx.AllPlayers.Count);
        string userId=ctx.LocalPlayerId;
        Debug.Log($"[PlayerManager] 本地玩家ID={userId}, 服务器返回玩家数={ctx.AllPlayers.Count}: {string.Join(", ", System.Linq.Enumerable.Select(ctx.AllPlayers, p => $"{p.userId}(hero={p.HeroId})"))}");
        IReadOnlyList<IReadOnlyList<FixedVector2>> spawnPoints = modeConfig.SpawnPoints;
        Debug.Log($"[PlayerManager] spawnPoints 队伍数={spawnPoints?.Count ?? 0}, 玩家数={ctx.AllPlayers.Count}");
        Dictionary<int, int> bornPointCount = new();
        foreach (var _playerInfoEach in ctx.AllPlayers)
        {
            int teamId= _playerInfoEach.teamId-1;
            int teamIdx=bornPointCount.GetValueOrDefault(teamId, 0);
            // 边界检查：spawnPoints 越界时使用默认出生点 (0,0)
            FixMath.FixedVector2 spawnPos = new FixMath.FixedVector2(0, 0);
            if (spawnPoints != null && teamId >= 0 && teamId < spawnPoints.Count)
            {
                var teamPoints = spawnPoints[teamId];
                if (teamPoints != null && teamIdx >= 0 && teamIdx < teamPoints.Count)
                {
                    spawnPos = teamPoints[teamIdx];
                }
                else
                {
                    Debug.LogWarning($"[PlayerManager] teamIdx={teamIdx} 越界（队伍{teamId}出生点数={teamPoints?.Count ?? 0}），使用默认出生点(0,0)");
                }
            }
            else
            {
                Debug.LogWarning($"[PlayerManager] teamId={teamId} 越界（spawnPoints队伍数={spawnPoints?.Count ?? 0}），使用默认出生点(0,0)");
            }
            _playerInfo player_info = new _playerInfo(_playerInfoEach.userId, _playerInfoEach.HeroId, _playerInfoEach.teamId, spawnPos);
            player_info.IsEnemy = _playerInfoEach.teamId != ctx.LocalTeamId;
            playerInfoDic[_playerInfoEach.userId] = player_info;
            // Logic注册
            BasePlayerLogic player_Logi = CharacterFactory.CreatePlayerLogic(_playerInfoEach.HeroId);
            if (player_Logi != null)
            {
                playerControllerSDic[_playerInfoEach.userId] = player_Logi;
                int[] skillIDs = player_info.characterConfig?.Skillids;
                if(skillIDs!=null)
                {
                    player_Logi.Init(player_info, skillIDs);
                }
            }
            GameObject o = BattleResourceManager.Instance.LoadCharacterPrefab(_playerInfoEach.HeroId);
            if (o == null)
            {
                Debug.LogError($"[PlayerManager] 无法加载英雄预制体 HeroPrefabs/{_playerInfoEach.HeroId}/{_playerInfoEach.HeroId}，跳过玩家 {_playerInfoEach.userId}");
                bornPointCount[teamId] = teamIdx + 1;
                continue;
            }
            GameObject player = Instantiate(o, player_info.bornPoint.ToVector2(),Quaternion.identity);
            player.name = $"Player_{_playerInfoEach.userId}";
            Debug.Log("���ɽ�ɫ��!"+player.transform);

            // Viewע��
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
                Debug.Log($"[PlayerManager] 找到本地玩家：userId={userId}, LocalPlayerView={(player_View != null ? "不为空" : "为空")}");
            }
            else
            {
                Debug.Log($"[PlayerManager] 非本地玩家：player_info.UserId={player_info.UserId}, 本地userId={userId}");
            }
            bornPointCount[teamId] = teamIdx + 1;
            player_info.Init();
        }

        if (this.LocalPlayerView == null)
        {
            Debug.LogError($"[PlayerManager] Init 结束仍未找到本地玩家视图！本地userId={userId}，总玩家数={ctx.AllPlayers.Count}");
        }
    }

    /*****************֡���½ӿ�******************/
    public void OnLogicFrameUpdate(FrameData data)
    {
        List<MsgPlayerOp> op = data.allPlayerOps;
        // todo:���������õ�������Ӵ�������
        // �߼�֡����ʱ���ȸ��ݷ��������صĲ�������������ÿ����ҵ��߼�״̬��Ȼ���ٵ���ÿ����ҵ���Ⱦ����������Ⱦ״̬
        foreach (var eachOP in op)
        {
            string id = eachOP.playerId.ToString();
            if (playerControllerSDic.TryGetValue(id, out BasePlayerLogic value))
            {
                value.OnFrameLogicUpdate(eachOP);
            }
            else
            {
                Debug.LogWarning($"[PlayerManager] OnLogicFrameUpdate 找不到玩家 id={id}, 已注册玩家: {string.Join(",", playerControllerSDic.Keys)}");
            }
        }
        foreach (var render in playerRendersDic.Values)
        {
            render.OnRenderFrameUpdate();
        }
    }

    #region �ⲿ�����ֵ�ӿ�
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