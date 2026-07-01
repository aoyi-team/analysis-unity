using FixMath;
using System.Collections.Generic;
public class FixedEdgeCollider2D : FixedCollider2D
{
    public List<FixedVector2> LocalPoints { get; private set; }

    public List<FixedVector2> worldPoints;
    private List<(FixedVector2 start, FixedVector2 end)> worldSegments;

    public override FixedVector2 WorldLogicPos => LogicPos+Center.Rotate(Rotation);
    public FixedEdgeCollider2D(FixedVector2 logicPos, FixedVector2 center, List<FixedVector2> localPoints, Fixed64 rotation = default, bool isTrigger = true) : base(logicPos, center, isTrigger)
    {
        this.LocalPoints = new List<FixedVector2>(localPoints);
        base.SyncLogicRotation(rotation);
        worldPoints = new List<FixedVector2>();
        worldSegments = new List<(FixedVector2 start, FixedVector2 end)>();
        this.ColliderType = Collider2DEnum.Edge;
        UpdateWorldData();
    }

    public void UpdateWorldData()
    {
        worldPoints.Clear();
        worldSegments.Clear();
        foreach (var p in LocalPoints)
        {
            worldPoints.Add(p.Rotate(Rotation) + WorldLogicPos);
        }
        for (int i = 0; i < worldPoints.Count - 1; i++)
        {
            worldSegments.Add((worldPoints[i], worldPoints[i + 1]));
        }
    }
    public override void SyncLogicPos(FixedVector2 logicPos)
    {
        base.SyncLogicPos(logicPos);
        UpdateWorldData();
    }
    public IReadOnlyList<(FixedVector2 start, FixedVector2 end)> GetSegments => worldSegments;

    /*
    public override bool DetectCollider(FixedCollider2D other)
    {
        switch (other.ColliderType)
        {
            case Collider2DEnum.Box: is_mCollider = PhysicsWorld2D.Instance.DetectCollider(other as FixedBoxCollider2D, this); break;
            case Collider2DEnum.Sphere: is_mCollider = PhysicsWorld2D.Instance.DetectCollider(other as FixedSphereCollider2D, this); break;
            case Collider2DEnum.OBB: is_mCollider = PhysicsWorld2D.Instance.DetectCollider(other as FixedOBBCollider2D, this); break;
            case Collider2DEnum.Polygon: is_mCollider = PhysicsWorld2D.Instance.DetectCollider(other as FixedPolygonCollider2D, this); break;
            case Collider2DEnum.Edge: is_mCollider = PhysicsWorld2D.Instance.DetectCollider(this, other as FixedEdgeCollider2D); break;
        }
        return base.DetectCollider(other);
    }*/
}