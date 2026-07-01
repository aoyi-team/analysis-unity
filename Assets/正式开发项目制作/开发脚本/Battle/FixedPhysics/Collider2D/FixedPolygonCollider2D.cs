using FixMath;
using System.Collections.Generic;
public class FixedPolygonCollider2D : FixedCollider2D
{
    // 默认是实心的
    public bool isFill = true;

    // 多边形的局部顶点列表，表示相对于Collider中心的顶点位置，单位为逻辑单位（如像素），在碰撞检测中会用到这些顶点信息
    public List<FixedVector2> localVertices { get; private set; }

    private List<FixedVector2> worldVertices;

    public FixedPolygonCollider2D(FixedVector2 logicPos, FixedVector2 center, List<FixedVector2> localVertices, Fixed64 rotataion = default, bool isTrigger = false,bool fill=true) : base(logicPos, center, isTrigger)
    {
        isFill=fill;
        this.localVertices = new List<FixedVector2>(localVertices);
        SyncLogicRotation(rotataion);
        worldVertices = new List<FixedVector2>();
        this.ColliderType = Collider2DEnum.Polygon;
        UpdateWorldData();
    }
    // 更新世界顶点(逻辑坐标变化时调用)
    public void UpdateWorldData()
    {
        worldVertices.Clear();
        foreach (var p in localVertices)
        {
            worldVertices.Add(p + WorldLogicPos);
        }
    }
    // 同步逻辑坐标时更新世界顶点
    public override void SyncLogicPos(FixedVector2 logicPos)
    {
        base.SyncLogicPos(logicPos);
        UpdateWorldData();
    }
    public IReadOnlyList<FixedVector2> GetWorldVertices => worldVertices;
    /*
    public override bool DetectCollider(FixedCollider2D other)
    {
        switch (other.ColliderType)
        {
            case Collider2DEnum.Box: is_mCollider = PhysicsWorld2D.Instance.DetectCollider(other as FixedBoxCollider2D, this); break;
            case Collider2DEnum.Sphere: is_mCollider = PhysicsWorld2D.Instance.DetectCollider(other as FixedSphereCollider2D, this); break;
            case Collider2DEnum.OBB: is_mCollider = PhysicsWorld2D.Instance.DetectCollider(other as FixedOBBCollider2D, this); break;
            case Collider2DEnum.Polygon: is_mCollider = PhysicsWorld2D.Instance.DetectCollider(this, other as FixedPolygonCollider2D); break;
            case Collider2DEnum.Edge: is_mCollider = PhysicsWorld2D.Instance.DetectCollider(this, other as FixedEdgeCollider2D); break;
        }
        return base.DetectCollider(other);
    }*/
}