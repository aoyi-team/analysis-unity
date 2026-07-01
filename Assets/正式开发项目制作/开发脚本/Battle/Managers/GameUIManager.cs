
using UnityEngine;

/// <summary>
/// UI管理器，仅对玩家自身的UI进行管理，如分数面板、以及小地图等。对于其他玩家的UI，如血条等，由PlayerUIManager进行管理。
/// </summary>
public class GameUIManager:MonoBehaviour
{
    private static GameUIManager _instance;
    public static GameUIManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject o= new GameObject("GameUIManager");
                _instance = o.AddComponent<GameUIManager>();
                DontDestroyOnLoad(o);
            }
            return _instance;
        }
    }



    // 初始化UI元素，分数面板等
    public void Init()
    {

    }


}