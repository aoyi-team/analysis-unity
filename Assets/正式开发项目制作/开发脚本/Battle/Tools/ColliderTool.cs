// ColliderRegistrationHelper.cs
using FixMath;
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct ColliderData
{ 
    public Collider2DEnum colliderType;
    public FixedVector2 center;    // 相对 _currLogicPos
    public FixedVector2 size;        // Box: 宽高; Circle: x=直径或半径（约定一种）
    public List<FixedVector2> points;    // Polygon/Edge 的顶点列表（相对 _currLogicPos）
    public bool isTrigger;
}

public static class ColliderTool
{
    /*
    /// <summary>
    /// 从 GameObject 上所有 Collider2D 创建 FixedCollider2D 并注册
    /// </summary>
    public static List<FixedCollider2D> RegisterFromGameObject(
        GameObject go,
        FixedVector2 entityLogicPos,
        CollisionLayer layer,
        bool isStatic)
    {
        var result = new List<FixedCollider2D>();
        var unityColliders = go.GetComponentsInChildren<Collider2D>(includeInactive: false);

        foreach (var uc in unityColliders)
        {
            var fc = ConvertCollider(uc, entityLogicPos);
            if (fc == null) continue;

            fc.Layer = (int)layer;
            fc.IsTrigger = uc.isTrigger;
            fc.SetRenderObj(go);

            if (isStatic)
                CollisionManager.Instance.AddStaticCollider(fc);
            else
                CollisionManager.Instance.AddDynamicCollider(fc);

            result.Add(fc);
        }

        return result;
    }
    */
    /// <summary>
    /// 从Unity Collider2D 创建对应的 ColliderData
    /// </summary>
    /// <param name="unityCollider"></param>
    /// <param name="entityLogicPos"></param>
    /// <returns></returns>
    public static ColliderData GetColliderDataFromUnityCollider2D(Collider2D unityCollider)
    {
        Debug.Log("Load Collider2D type: " + unityCollider.GetType().Name);
        if (unityCollider is BoxCollider2D box)
        {
            return new ColliderData
            {
                colliderType = Collider2DEnum.Box,
                center = new FixedVector2(box.offset.x, box.offset.y),
                size = new FixedVector2(box.size.x, box.size.y),
                isTrigger = box.isTrigger
            };
        }
        if(unityCollider is CircleCollider2D circle)
        {
            return new ColliderData
            {
                colliderType = Collider2DEnum.Circle,
                center = new FixedVector2(circle.offset.x, circle.offset.y),
                size = new FixedVector2(circle.radius, 0), // 约定 size.x 存储直径
                isTrigger = circle.isTrigger
            };
        }
        if(unityCollider is PolygonCollider2D poly)
        {
            List<FixedVector2> points = new List<FixedVector2>();
            foreach(var p in poly.points)
            {
                points.Add(new FixedVector2(p.x, p.y));
            }
            return new ColliderData
            {
                colliderType = Collider2DEnum.Polygon,
                center = new FixedVector2(poly.offset.x, poly.offset.y),
                points = points,
                isTrigger = poly.isTrigger
            };
        }
        if(unityCollider is EdgeCollider2D edge)
        {
            List<FixedVector2> points = new List<FixedVector2>();
            foreach(var p in edge.points)
            {
                points.Add(new FixedVector2(p.x, p.y));
            }
            return new ColliderData
            {
                colliderType = Collider2DEnum.Edge,
                center = new FixedVector2(edge.offset.x, edge.offset.y),
                points = points,
                isTrigger = edge.isTrigger
            };
        }
        return new ColliderData
        {
            colliderType = Collider2DEnum.None,
            size = FixedVector2.Zero,
            isTrigger = false
        };
    }
    /// <summary>
    /// 通过ColliderData 转成 FixedCollider2D。
    /// entityLogicPos = 该实体逻辑根位置（玩家是 _currLogicPos，静态物是出生点/锚点）
    /// </summary>
    public static FixedCollider2D ColliderDataConvertToCollider(ColliderData colliderData,FixedVector2 logicPos,bool isTrigger=false)
    {
        var _colType=colliderData.colliderType;
        if (_colType == Collider2DEnum.Box)
        {
            return new FixedOBBCollider2D(logicPos, colliderData.center, colliderData.size, Fixed64.Zero, isTrigger);
        }

        if (_colType == Collider2DEnum.Circle)
        {
            FixedVector2 center = colliderData.center;
            return new FixedSphereCollider2D(logicPos, center, colliderData.size.x,Fixed64.Zero, isTrigger);
        }
        if(_colType== Collider2DEnum.Polygon)
        {
            return new FixedPolygonCollider2D(logicPos, colliderData.center, colliderData.points, Fixed64.Zero, isTrigger);
        }
        if(_colType== Collider2DEnum.Edge)
        {
            return new FixedEdgeCollider2D(logicPos, colliderData.center, colliderData.points, Fixed64.Zero, isTrigger);
        }

        return null;
    }
    /// <summary>
    /// 每逻辑帧：同步动态 collider 的逻辑坐标
    /// </summary>
    public static void SyncCollidersPosition(List<FixedCollider2D> colliders, FixedVector2 entityLogicPos, GameObject go)
    {
        var unityColliders = go.GetComponentsInChildren<Collider2D>();
        int count = Mathf.Min(colliders.Count, unityColliders.Length);

        for (int i = 0; i < count; i++)
        {
            Vector2 localOffset = unityColliders[i].transform.position - go.transform.position;
            FixedVector2 logicPos = entityLogicPos + new FixedVector2(localOffset.x, localOffset.y);

            // 建议你把 FixedCollider2D.SyncLogicPos 改成：LogicPos = logicPos（不要 +Center）
            colliders[i].SyncLogicPos(logicPos);
        }

        CollisionManager.Instance.MarkDynamicDirty();
    }
}