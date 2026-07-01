using UnityEngine;

// 游戏生命周期管理器，负责管理游戏的整体流程，如游戏开始、游戏结束、重置等
public class GameLifeCycle:MonoBehaviour
{
    private static GameLifeCycle instance;
    public static GameLifeCycle Instance { get; private set; }

    public FrameEvent _newFrameEvent; // 当前帧事件，供客户端监听和处理

    public void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void UpdateFrameEvent(FrameEvent newEvent)
    {
        _newFrameEvent = newEvent;
        FrameEventType eventType = newEvent.eventType;
    }

    // 逻辑帧更新，处理游戏生命周期相关的逻辑，如检查游戏结束条件、处理重置等
    public void LogicUpdate()
    {
        if (_newFrameEvent.eventType == FrameEventType.None) return;
        if(BattleData.Instance.FrameId== _newFrameEvent.frameNumber)
        {
            // 处理逻辑

            // 处理完成后推进新事件

        }
    }

}