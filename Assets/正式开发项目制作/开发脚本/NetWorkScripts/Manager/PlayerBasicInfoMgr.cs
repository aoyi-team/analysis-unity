using System;
using UnityEngine;
using static Cinemachine.CinemachineTriggerAction.ActionSettings;
/// <summary>
/// ��һ�����Ϣ������
/// ȫ�ִ洢��ҵ�ID����ɫ��
/// ��ѡ��ɫID(�Ͼ��°���Ϸ��Ĭ�Ͻ�ɫ���ǻ��õ���)
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
    private GameModes gameMode;// ��ǰѡ�����Ϸģʽ
    private string ID;//���ID,����ʱת����int
    private string Name;//�������
    private int Level;//��ǰ�˺ŵȼ�
    private string roomID;//��ǰ���ڷ���ID
    private int teamId;//��ǰ���ڶ���ID
    public GameModes GameMode { get { return gameMode; }}
    public string RoomId { get { return roomID; } }
    public int TeamId { get { return teamId; } }
    public HeroSelectCache HeroCache;

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
    public void UpdatePlayerId(string id)//�ⲿ���ø���ID
    {
        ID = id;
    }
    public void UpdatePlayerName(string name)//��������
    {
        Name = name;
    }
    public int GetIntId()
    {
        return int.Parse(ID);
    }
    public string GetID()//��ȡ��ǰString����ID
    {
        return ID;
    }
    public string GetName()//��ȡ��ǰ��ɫ��
    {
        return Name;
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