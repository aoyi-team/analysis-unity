
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
/// <summary>
/// 管理所有游戏资源的载入
/// </summary>
public static class ResMgr
{
    //皮肤缓存
    static readonly Dictionary<string, Sprite> _skinCahe=new Dictionary<string, Sprite>();
    static readonly Dictionary<string, Sprite> _modesIncoCahe=new Dictionary<string, Sprite>();
    /// <summary>
    /// 用于加载面板预制体
    /// </summary>
    public static GameObject LoadPanelPrefabs  (string PanelName) 
    {
        return Resources.Load<GameObject>(PanelName);
    }
    // ================== 游戏模式图标加载 ==================
    /// <summary>
    /// 加载游戏模式图标（同步，图标体积小，直接加载）
    /// </summary>
    public static void PreLoadModesIcons()
    {
        Sprite[] modeSprites = Resources.LoadAll<Sprite>("UISprites/CharacterChoose/ModesIcon/ModesIcons");
        foreach (var modeSprite in modeSprites)
        {
            _modesIncoCahe[modeSprite.name]= modeSprite;
        }
    }
    public static Sprite LoadModeIcon(GameModes mode)
    {
        if (_modesIncoCahe.ContainsKey(mode.ToString()))
        {
            return _modesIncoCahe[mode.ToString()];
        }
        else return null;
    }
    // ================== 英雄皮肤海报加载 ==================
    /// <summary>
    /// 异步加载英雄皮肤海报（核心：大资源异步，避免卡顿）
    /// </summary>
    public static IEnumerator PreLoadHeroSkinDic(int heroId, int skinCounts)
    {
        for(int num=1; num<= skinCounts; num++)
        {
            string baseKey = $"{heroId}_{num:D2}";
            string numStr=num.ToString("D2") ;
            //加载皮肤海报
            yield return LoadSprite($"{baseKey}", $"UISprites/CharacterChoose/SkinPosters/{heroId}/{numStr}");
            yield return LoadSprite($"{baseKey}_bg",$"UISprites/CharacterChoose/SkinPosters/{heroId}/{numStr}_bg");
            //Debug.Log($"加载完成英雄ID:{heroId},皮肤ID：{heroId}_{num:D2}");
        }
    }
    /// <summary>
    /// 预加载所有英雄的默认皮肤海报（大厅加载时调用）
    /// </summary>
    /*public static IEnumerator PreloadAllHeroDefaultPoster(int heroCount)
    {
        // 逐个异步加载，避免一次性占满内存
        for (int heroId = 0; heroId < heroCount; heroId++)
        {
            string path = $"HeroPosters/{heroId}_0"; // 0为默认皮肤
            ResourceRequest req = Resources.LoadAsync<Sprite>(path);
            yield return req;

            if (req.asset == null)
            {
                Debug.LogWarning($"预加载英雄{heroId}默认海报失败：{path}");
            }
            // 无需赋值，Resources会自动缓存加载过的资源
        }
    }*/
    /// <summary>
    /// 释放未使用的海报资源（关闭面板时调用）
    /// </summary>
    public static void UnloadUnusedHeroPoster()
    {
        _skinCahe.Clear();
        Resources.UnloadUnusedAssets();
        System.GC.Collect();
    }
    static IEnumerator LoadSprite(string cacheKey,string path)
    {
        if (_skinCahe.ContainsKey(cacheKey)) yield break;
        ResourceRequest rq= Resources.LoadAsync<Sprite>(path);
        yield return rq;
        if(rq.asset != null)
        {
            _skinCahe.Add(cacheKey, rq.asset as Sprite);
        }
    }
    public static Sprite GetPoster(int heroId,int skinId)
    {
        _skinCahe.TryGetValue($"{heroId}_{skinId:D2}",out var spr);
        return spr;
    }
    public static Sprite GetPosterBg(int heroId, int skinId)
    {
        _skinCahe.TryGetValue($"{heroId}_{skinId:D2}_bg",out var spr);
        return spr;
    }

    public static T LoadResource<T>(string path)where T: Object
    {
        return Resources.Load<T>(path); 
    }
}