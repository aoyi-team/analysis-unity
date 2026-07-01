using System.Collections.Generic;
using UnityEngine;

public class BattleResourceManager
{
    private static BattleResourceManager _instance;

    public static BattleResourceManager Instance
    {
        get
        {
            if( _instance == null )
            {
                _instance = new BattleResourceManager();
            }
            return _instance;
        }
    }

    // 技能预制体存储<skillId,prefabResource>
    public Dictionary<int, GameObject> skillPrefabs = new Dictionary<int, GameObject>();

    // 场景物体和生物
    public Dictionary<int,GameObject> EntityPrefabs=new Dictionary<int, GameObject>();

    // 角色预制体资源<HeroId>
    public Dictionary<int, GameObject> CharacterPrefabs = new Dictionary<int, GameObject>();

    public  GameObject LoadCharacterPrefab(int HeroId)
    {
        if(!CharacterPrefabs.ContainsKey(HeroId))
        {
            GameObject o = ResMgr.LoadResource<GameObject>($"HeroPrefabs/{HeroId}/{HeroId}");
            CharacterPrefabs[HeroId] = o;
        }
        return CharacterPrefabs[HeroId];
    }

    public GameObject LoadSkillPrefab(int skillId)
    {
        if (skillPrefabs.TryGetValue(skillId, out GameObject o)) return o;

        o = ResMgr.LoadResource<GameObject>("");
        if (o != null)
        {
            skillPrefabs[skillId] = o;
        }
        return o;
    }

    public GameObject LoadScenePrefab(int entityId)
    {
        if (EntityPrefabs.TryGetValue(entityId, out GameObject o)) return o;

        o = ResMgr.LoadResource<GameObject>("");
        if (o != null)
        {
            EntityPrefabs[entityId] = o;
        }
        return o;
    }
}