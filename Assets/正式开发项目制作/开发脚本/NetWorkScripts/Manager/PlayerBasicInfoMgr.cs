using System;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Aoyi.Mirror;
/// <summary>
/// 玩家基础信息管理器
/// 全局存储我的ID、角色等
/// (选角色ID(就进入游戏后默认角色就是获得的那个)
/// </summary>

public class PlayerBasicInfoMgr : MonoBehaviour
{
    private static PlayerBasicInfoMgr instance;
    public  static PlayerBasicInfoMgr Instance
    {
        get {
            if(instance == null)
            {
                GameObject InfoMgr = new GameObject("PlayerBasicInfoMgr");
                instance=InfoMgr.AddComponent<PlayerBasicInfoMgr>();
                DontDestroyOnLoad(InfoMgr);
            }
            return instance;
        }
    }
    private GameModes gameMode;
    private string ID;
    private string battleId;
    private bool _battleIdFallbackLogged; // 避免降级日志刷屏
    private string Name;
    private int Level;
    private string roomID;
    private int teamId;
    public GameModes GameMode { get { return gameMode; }}
    public string RoomId { get { return roomID; } }
    public int TeamId { get { return teamId; } }
    public HeroSelectCache HeroCache;

    /// <summary>
    /// 战斗场景中所有玩家数据（由 AoyiNetworkRoomManager 在场景切换后设置）
    /// </summary>
    private List<PlayerData> _battleAllPlayers;
    /// <summary>本地玩家在战斗中的 index（游戏内 ID）</summary>
    private int _localPlayerIndex = -1;

    #region 网络会话状态（Phase 1 新增）
    /// <summary>当前网络运行模式</summary>
    public NetworkMode CurrentNetworkMode { get; set; }
    /// <summary>当前目标服务端点（局域网房主或本地服务器）</summary>
    public NetworkEndpoint TargetEndpoint { get; set; }
    /// <summary>当前使用的后端服务提供者</summary>
    public IBackendProvider CurrentBackend { get; set; }
    /// <summary>当前登录玩家基础信息</summary>
    public PlayerBasicInfo CurrentPlayer { get; set; }
    #endregion
    /// <summary>
    /// ��ʼ����ֵ������Ϣ
    /// </summary>
    private void Awake()
    {
        // ��ʼ�����棨Ĭ��ֵ��ѡ��һ��Ӣ�ۡ���һ��Ƥ������ͨģʽ��
        HeroCache = new HeroSelectCache()
        {
            heroId = 101,
            skinId = 1
        };

        // ��ѡ���ӱ��ش浵�����ϴε�ѡ�񣨱���PlayerPrefs��
        LoadHeroCacheFromLocal();
    }
    public void SetCurrentGamemode(GameModes mode)
    {
        gameMode = mode;
    }
    public void UpdateTeamId(int id)
    {
        teamId = id;
    }
    public void UpdateRoomID(string id)
    {
        roomID = id;
    }
    //��ȡ��ǰ�ȼ���Ϣ
    public int GetLevel()
    {
        return Level;
    }
    //���µ�ǰ�ȼ���Ϣ
    public void UpdateLeveInfo(int level)
    {
        Level = level;
    }
    public void UpdatePlayerId(string id)
    {
        ID = id;
    }
    public void UpdatePlayerName(string name)
    {
        Name = name;
    }
    public int GetIntId()
    {
        int id;
        int.TryParse(ID, out id);
        return id;
    }
    public string GetID()
    {
        return ID;
    }
    public string GetName()
    {
        return Name;
    }
    public void SetBattleId(string id)
    {
        battleId = id;
        Debug.Log($"[PlayerBasicInfoMgr] SetBattleId='{id}' (账号ID='{ID}')");
    }
    public void ClearBattleId()
    {
        Debug.Log($"[PlayerBasicInfoMgr] ClearBattleId (旧值='{battleId}')");
        battleId = null;
        _localPlayerIndex = -1;
    }
    public string GetBattleId()
    {
        // 局域网模式：优先用 RoomPlayer 的 index 作为游戏内 ID
        if (MirrorNetBridge.IsMirrorActive && AoyiNetworkRoomManager.singleton != null)
        {
            int idx = AoyiNetworkRoomManager.singleton.GetLocalPlayerIndex();
            if (idx >= 0)
            {
                _localPlayerIndex = idx;
                string battleIdFromIndex = idx.ToString();
                if (battleId != battleIdFromIndex)
                {
                    battleId = battleIdFromIndex;
                    Debug.Log($"[PlayerBasicInfoMgr] GetBattleId 从 RoomPlayer.index 获取：'{battleId}'");
                }
                return battleId;
            }
        }

        if (battleId != null)
            return battleId;

        // 降级日志只打一次
        if (!_battleIdFallbackLogged)
        {
            Debug.Log($"[PlayerBasicInfoMgr] GetBattleId 降级中：battleId 为空, ID={ID}, IsMirrorActive={MirrorNetBridge.IsMirrorActive}");
            _battleIdFallbackLogged = true;
        }

        return ID;
    }

    /// <summary>
    /// 设置战斗场景中所有玩家数据（由 AoyiNetworkRoomManager 调用）
    /// </summary>
    public void SetBattleAllPlayers(List<PlayerData> players)
    {
        _battleAllPlayers = players;
        Debug.Log($"[PlayerBasicInfoMgr] SetBattleAllPlayers: 玩家数={players?.Count ?? 0}, 本地index={_localPlayerIndex}");
    }

    /// <summary>
    /// 获取战斗场景中所有玩家数据
    /// </summary>
    public List<PlayerData> GetBattleAllPlayers()
    {
        return _battleAllPlayers;
    }

    /// <summary>
    /// 获取本地玩家在战斗中的 index
    /// </summary>
    public int GetLocalPlayerIndex()
    {
        return _localPlayerIndex;
    }
    /// <summary>
    /// ����Ӣ��ѡ�񻺴棨ѡ��Ӣ��/�л�ģʽʱ���ã�
    /// </summary>
    public void UpdateHeroCache(int heroId, int skinId)
    {
        HeroCache.heroId = heroId;
        HeroCache.skinId = skinId;

        // ��ѡ�����浽���أ��´ε�¼���ܸ���
        SaveHeroCacheToLocal();
    }
    /// <summary>
    /// �ӱ��ؼ��ػ���(ʹ��prefabs�����ȡ)
    /// </summary>
    private void LoadHeroCacheFromLocal()
    {
        if (PlayerPrefs.HasKey("LastHeroId"))
        {
            HeroCache.heroId = PlayerPrefs.GetInt("LastHeroId");
            HeroCache.skinId = PlayerPrefs.GetInt("LastSkinId");
        }
    }
    /// <summary>
    /// �������ѡ�񻺴浽����
    /// </summary>
    public void SaveHeroCacheToLocal()
    {
        PlayerPrefs.SetInt("LastHeroId", HeroCache.heroId);
        PlayerPrefs.SetInt("LastSkinId", HeroCache.skinId);
        PlayerPrefs.Save();
    }
}
// �����ѡӢ��/Ƥ���Ļ�������
[Serializable]
public class HeroSelectCache
{
    public int heroId;    // 
    public int skinId;    // 
}