// 服务端发过来的数据就是按顺序的，所以不需要区分队伍出生点了，直接按顺序赋值即可
using FixMath;
using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class ModeConfig : ScriptableObject
{

    [Header("基础全局配置")]
    public GameModes gameMode;
    public int totalPlayerCount;       // 总玩家数
    public int teamCount;              // 队伍数量（2队 or 3队）
    public int GameTimeSeconds;          // 游戏时间,逻辑帧为单位，客户端根据这个值来显示倒计时

    [Header("出生点（按队伍分类）")]
    [SerializeField]
    public List<TeamSpawnPoints> spawnPoints;

    [Header("碰撞体数据")]
    [SerializeField] public List<ColliderData> _colliderDatas;

    [SerializeField, HideInInspector]
    private List<FixedVector2List> cachedSpawnPoints;
    public IReadOnlyList<IReadOnlyList<FixedVector2>> SpawnPoints => cachedSpawnPoints;

    protected void OnEnable()
    {
        // 只在运行时/非编辑模式执行
        if (Application.isPlaying)
        {
            cachedSpawnPoints.Clear();
            foreach (var team in spawnPoints)
            {
                var fixedList = new FixedVector2List();
                if (team?.points != null)
                {
                    foreach (var p in team.points)
                        fixedList.Add(new FixedVector2(p.x, p.y));
                }
                cachedSpawnPoints.Add(fixedList);
            }
        }
    }
    /// <summary>
    /// 编辑器中修改出生点后会调用这个方法来更新缓存的出生点列表，避免每次访问SpawnPoints属性时都进行转换，提高性能
    /// </summary>
#if UNITY_EDITOR
    protected void OnValidate()
    {
        cachedSpawnPoints.Clear();
        if (spawnPoints == null) return;
        foreach (var team in spawnPoints)
        {
            var fixedList = new FixedVector2List();
            if (team?.points != null)
            {
                foreach (var p in team.points)
                    fixedList.Add(new FixedVector2(p.x, p.y));
            }
            cachedSpawnPoints.Add(fixedList);
        }
    }

    [ContextMenu("Import from Selected Collider")]
    public void ImportFromCollider()
    {
        var obj = UnityEditor.Selection.activeGameObject;
        if (obj == null)
        {
            Debug.LogWarning("No gameobject selected!");
            return;
        }
        var col = obj.GetComponents<Collider2D>();
        if (col != null)
        {
            _colliderDatas.Clear();
            foreach (var c in col)
            {
                var data = ColliderTool.GetColliderDataFromUnityCollider2D(c);
                if (data.colliderType != Collider2DEnum.None)
                {
                    _colliderDatas.Add(data);
                }
                Debug.Log($"import collider data from {obj.name} : {data.colliderType}, center: {data.center}, size: {data.size}, points count: {data.points?.Count ?? 0}");
            }
        }
    }

#endif


    // todo:添加物资生成坐标和一些基本配置
}
[Serializable]
public class TeamSpawnPoints
{
    [Tooltip("该队伍所有成员的出生点")]
    public List<Vector2> points = new List<Vector2>();
}


// 预留一个事件系统，后续添加一些模式相关的事件（比如占领点事件，物资生成事件等），客户端根据事件类型和数据来显示一些特效或者UI提示
[Serializable]
public struct FrameEvent
{
    public int frameNumber;
    public FrameEventType eventType;
    public Dictionary<string, object> eventData;
}

public enum FrameEventType
{
    None,
    CapturePoint,
    SupplyDrop,
    // 后续添加更多事件类型
}

[Serializable]
public class FixedVector2List : List<FixedVector2>, IReadOnlyList<FixedVector2> { }