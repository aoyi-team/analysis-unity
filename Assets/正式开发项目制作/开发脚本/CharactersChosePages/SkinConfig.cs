using Panels;
using System.Collections;
using System.Collections.Generic;
/// <summary>
/// 皮肤配置表
/// </summary>

public static class SkinConfig
{

    public static Dictionary<int, List<SkinData>> SkinsConfigDic = new Dictionary<int, List<SkinData>>()
        {
            { 101,new List<SkinData>()
            {
            new SkinData() { skinId = "01", skinName = "诺亚", isUnlocked = true },
            new SkinData() {skinId = "02", skinName = "铁甲·诺亚", isUnlocked = false},
            new SkinData() {skinId = "03", skinName = "白执事·诺亚", isUnlocked = false},
            new SkinData() {skinId = "04", skinName = "毁灭者·诺亚", isUnlocked = false}
            }
            }
        };//Heroid.HeroSkinConfig
    /// <summary>
    /// 获取皮肤ID和数量的对应
    /// </summary>
    /// <returns></returns>
    public static Dictionary<int,int> GetHeroSkinCountMap()
    {
        var map = new Dictionary<int, int>(GetHeroCount());
        foreach(var o in SkinsConfigDic)
        {
            map[o.Key] = o.Value.Count;
        }
        return map;
    }
    public static List<SkinData> GetSkins(int heroId)
    {
        SkinsConfigDic.TryGetValue(heroId, out var skins);
        return skins;
    }
    public static int GetHeroCount()
    {
        return SkinsConfigDic.Count;
    }
    public static int GetHeroSkinCount(int heroId) => GetSkins(heroId)?.Count ?? 0;
}
public class SkinData
{
    public string skinId;          // 皮肤ID（比如10101=英雄101的第1个皮肤，10102=第2个）
    public string skinName;     // 皮肤名称（用于UI显示）
    public bool isUnlocked;     // 是否解锁（可选，控制皮肤是否可切换）
}