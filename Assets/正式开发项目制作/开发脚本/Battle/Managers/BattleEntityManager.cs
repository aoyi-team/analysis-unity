using System.Collections.Generic;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;

public class BattleEntityManager: MonoBehaviour
{
    private ModeConfig modeConfig;
    // 场景物体管理器
    private Dictionary<int,EntityInfo> entityInfosDic=new Dictionary<int, EntityInfo>();

    private static BattleEntityManager instance;

    public static BattleEntityManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new GameObject("BattleEntityManager").AddComponent<BattleEntityManager>();
            }
            return instance;
        }

    }

    // 初始化，地图基本碰撞器
    public void Init(ModeConfig config)
    {
        if (config != null)
        {
            modeConfig = config;
        }
        else
        {
            Debug.LogError("PlayerManager Init Error: ModeConfig is null!");
            return;
        }

        // 碰撞器注册
        SceneObjInfo mapinfo=new SceneObjInfo();
        mapinfo.Init(config);
    }
}