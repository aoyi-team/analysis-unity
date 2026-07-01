using FixMath;

public class FixedSphereCollider2D : FixedCollider2D
{
    public Fixed64 radius { get; private set; }

    public FixedSphereCollider2D(FixedVector2 logicPos, FixedVector2 center, Fixed64 radius,Fixed64 Rotation, bool isTrigger = false) : base(logicPos, center, isTrigger)
    {
        base.SyncLogicRotation(Rotation);
        this.radius = radius;
        this.ColliderType = Collider2DEnum.Sphere;
    }


}