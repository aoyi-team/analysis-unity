// 用于存储碰撞对的结构体
using FixMath;
using System;
using System.Collections.Generic;
using System.Diagnostics;

// 碰撞信息结构体
public struct CollisionInfo
{
    public FixedCollider2D colliderA;
    public FixedCollider2D colliderB;
    public FixedVector2 ContactPoint; // 碰撞接触点
    public FixedVector2 Normal; // 碰撞法线
    public Fixed64 PenetrationDepth; // 穿透深度
    public bool IsTrigger;// 是否为触发器碰撞

    public EntityInfo OwnerA => colliderA?.renderInfo;
    public EntityInfo OwnerB => colliderB?.renderInfo;
    public EntityInfo GetOtherOwner(FixedCollider2D self)
    {
        if (colliderA == self) return OwnerB;
        if (colliderB == self) return OwnerA;
        return null;
    }
}

public struct CollisionPair:IEquatable<CollisionPair>
{
    public readonly FixedCollider2D colA;
    public readonly FixedCollider2D colB;

    public CollisionPair(FixedCollider2D a, FixedCollider2D b)
    {
        // 采用小在前，大在后
        if(a.ColliderId < b.ColliderId)
        {
            colA = a;
            colB = b;
        }
        else
        {
            colA = b;
            colB = a;
        }
    }
    public bool Equals(CollisionPair other)
    {
        return colA == other.colA && colB == other.colB;
    }
    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + (colA != null ? colA.ColliderId : 0);
            hash = hash * 31 + (colB != null ? colB.ColliderId : 0);
            return hash;
        }
    }

    public override bool Equals(object obj)
    {
        return obj is CollisionPair other && Equals(other);
    }

}
public enum CollisionLayer
{
    None = 0,
    Player = 1,
    Enemy = 2,
    Wall = 3,
    Skill = 4,
    Trigger = 5,
    Max = 8
}

// 碰撞管理器
// CollisionManager.cs — 性能 + 初始化改进版
public class CollisionManager
{
    private static CollisionManager _instance;
    public static CollisionManager Instance => _instance ??= new CollisionManager();

    private FixedVector2 _mapMin = new FixedVector2(-1000, -1000);
    private FixedVector2 _mapMax = new FixedVector2(1000, 1000);

    private QuadTree _staticTree;
    private QuadTree _dynamicTree;

    private readonly HashSet<FixedCollider2D> _allColliders = new();
    private readonly List<FixedCollider2D> _staticColliders = new();
    private readonly List<FixedCollider2D> _dynamicColliders = new();
    private readonly List<FixedCollider2D> _queryResult = new();

    private readonly Dictionary<CollisionPair, CollisionInfo> _currentCollisions = new();
    private readonly HashSet<CollisionPair> _lastCollisions = new();

    public bool[,] LayerMatrix = new bool[(int)CollisionLayer.Max, (int)CollisionLayer.Max];

    private bool _dynamicTreeDirty = true;
    private bool _initialized = false;

    private CollisionManager() { }

    // ========== 战斗开始时调用 ==========
    public void Init(FixedVector2 mapMin, FixedVector2 mapMax)
    {
        _mapMin = mapMin;
        _mapMax = mapMax;

        _allColliders.Clear();
        _staticColliders.Clear();
        _dynamicColliders.Clear();
        _currentCollisions.Clear();
        _lastCollisions.Clear();
        _queryResult.Clear();

        _staticTree = new QuadTree(_mapMin, _mapMax);
        _dynamicTree = new QuadTree(_mapMin, _mapMax);
        _dynamicTreeDirty = true;

        InitLayerMatrix();
        _initialized = true;
    }

    public void Shutdown()
    {
        _allColliders.Clear();
        _staticColliders.Clear();
        _dynamicColliders.Clear();
        _currentCollisions.Clear();
        _lastCollisions.Clear();
        _staticTree?.Clear();
        _dynamicTree?.Clear();
        _initialized = false;
    }

    private void InitLayerMatrix()
    {
        // 先全部关闭，再按需打开
        for (int i = 0; i < (int)CollisionLayer.Max; i++)
            for (int j = 0; j < (int)CollisionLayer.Max; j++)
                LayerMatrix[i, j] = false;

        SetLayerCollision(CollisionLayer.Player, CollisionLayer.Enemy, true);
        SetLayerCollision(CollisionLayer.Player, CollisionLayer.Wall, true);
        SetLayerCollision(CollisionLayer.Player, CollisionLayer.Skill, true);
        SetLayerCollision(CollisionLayer.Player, CollisionLayer.Trigger, true);

        SetLayerCollision(CollisionLayer.Enemy, CollisionLayer.Wall, true);
        SetLayerCollision(CollisionLayer.Enemy, CollisionLayer.Skill, true);
        SetLayerCollision(CollisionLayer.Enemy, CollisionLayer.Trigger, true);

        // 如果你有 Neutral 层，在 enum 里加一项再打开
        // SetLayerCollision(CollisionLayer.Player, CollisionLayer.Neutral, true);
    }

    public void SetLayerCollision(CollisionLayer a, CollisionLayer b, bool canCollide)
    {
        LayerMatrix[(int)a, (int)b] = canCollide;
        LayerMatrix[(int)b, (int)a] = canCollide;
    }

    public bool IsLayerCollisionEnable(CollisionLayer a, CollisionLayer b)
    {
        return LayerMatrix[(int)a, (int)b];
    }

    // ========== 注册 ==========
    public void AddStaticCollider(FixedCollider2D collider)
    {
        if (collider == null || !_allColliders.Add(collider)) return;
        _staticColliders.Add(collider);
        _staticTree.Insert(collider);
    }

    public void AddDynamicCollider(FixedCollider2D collider)
    {
        if (collider == null || !_allColliders.Add(collider)) return;
        _dynamicColliders.Add(collider);
        _dynamicTreeDirty = true;
    }

    public void RemoveCollider(FixedCollider2D collider)
    {
        if (collider == null || !_allColliders.Remove(collider)) return;

        _staticColliders.Remove(collider);
        _dynamicColliders.Remove(collider);

        // 静态物体删除较少，直接重建静态树
        RebuildStaticTree();
        _dynamicTreeDirty = true;
    }

    /// <summary>
    /// 动态物体移动后调用，标记四叉树需要更新
    /// </summary>
    public void MarkDynamicDirty()
    {
        _dynamicTreeDirty = true;
    }

    private void RebuildStaticTree()
    {
        _staticTree.Clear();
        foreach (var c in _staticColliders)
            if (c.Active) _staticTree.Insert(c);
    }

    private void RebuildDynamicTreeIfNeeded()
    {
        if (!_dynamicTreeDirty) return;
        _dynamicTree.Clear();
        foreach (var c in _dynamicColliders)
            if (c.Active) _dynamicTree.Insert(c);
        _dynamicTreeDirty = false;
    }

    // ========== 每逻辑帧调用 ==========
    public void LogicUpdate()
    {
        if (!_initialized) return;
        RebuildDynamicTreeIfNeeded();
        _currentCollisions.Clear();
        foreach (var collider in _dynamicColliders)
        {
            if (!collider.Active) continue;
            _queryResult.Clear();
            _staticTree.Query(collider, _queryResult);
            _dynamicTree.Query(collider, _queryResult);
            foreach (var other in _queryResult)
            {
                if (!other.Active || other == collider)
                {
                    continue;
                }
                // ★ 同一 _playerInfo 上的 Event/Block 不互相检测
                if (collider.IsSameOwner(other))
                {
                    continue;
                }
                if (!IsLayerCollisionEnable((CollisionLayer)collider.Layer,(CollisionLayer)other.Layer))
                {
                    continue;
                }
                CollisionPair pair = new CollisionPair(collider, other);
                if (_currentCollisions.ContainsKey(pair))
                {
                    continue;
                }
                if (CollisionHelper.DetectCollision(collider, other, out CollisionInfo info))
                {
                    info.IsTrigger = collider.IsTrigger || other.IsTrigger;
                    _currentCollisions[pair] = info;
                }
            }
        }
        DispatchEvents();
        _lastCollisions.Clear();
        foreach (var pair in _currentCollisions.Keys)
            _lastCollisions.Add(pair);
    }
    private void DispatchEvents()
    {
        foreach (var kv in _currentCollisions)
        {
            var pair = kv.Key;
            var info = kv.Value;
            bool isNew = !_lastCollisions.Contains(pair);
            // ★ 按每个 collider 自己的 IsTrigger 分发，而不是统一用 info.IsTrigger
            DispatchToCollider(info.colliderA, info.colliderB, info, isNew, isStay: !isNew);
            DispatchToCollider(info.colliderB, info.colliderA, info, isNew, isStay: !isNew);
        }
        foreach (var pair in _lastCollisions)
        {
            if (_currentCollisions.ContainsKey(pair)) continue;
            var info = new CollisionInfo
            {
                colliderA = pair.colA,
                colliderB = pair.colB,
                IsTrigger = pair.colA.IsTrigger || pair.colB.IsTrigger
            };
            DispatchExit(pair.colA, pair.colB, info);
            DispatchExit(pair.colB, pair.colA, info);
        }
    }

    private static void DispatchToCollider(FixedCollider2D self, FixedCollider2D other, CollisionInfo info, bool isNew, bool isStay)
    {
        bool useTrigger = self.IsTrigger || other.IsTrigger;
        (info.colliderA, info.colliderB) = (self, other);
        if (useTrigger)
        {
            if (isNew) self.InvokeTriggerEnter(info);
            else self.InvokeTriggerStay(info);
        }
        else
        {
            if (isNew) self.InvokeCollisionEnter(info);
            else self.InvokeCollisionStay(info);
        }
    }
    private static void DispatchExit(FixedCollider2D self, FixedCollider2D other, CollisionInfo info)
    {
        (info.colliderA, info.colliderB) = (self, other);
        if (self.IsTrigger || other.IsTrigger)
            self.InvokeTriggerExit(info);
        else
            self.InvokeCollisionExit(info);
    }
}