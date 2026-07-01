using FixMath;

public class FixedOBBCollider2D : FixedCollider2D
{
    public FixedVector2 size { get; private set; }

    public Fixed64 HalfHeight => size.y / 2;
    public Fixed64 HalfWidth => size.x / 2;

    public FixedOBBCollider2D(FixedVector2 logicPos, FixedVector2 center, FixedVector2 size, Fixed64 Rotation, bool isTrigger = false) : base(logicPos, center, isTrigger)
    {
        base.SyncLogicRotation(Rotation);
        this.size = size;
        this.ColliderType = Collider2DEnum.OBB;
    }

    /// <summary>
    /// 旋转矩阵将世界坐标转换为局部坐标
    /// </summary>
    /// <param name="WorldPos"></param>
    /// <returns></returns>
    public FixedVector2 TransformPointToLocal(FixedVector2 WorldPos)
    {
        FixedVector2 localPoint = WorldPos - (WorldLogicPos);
        Fixed64 cos = FixedMath.Cos(-Rotation, true);
        Fixed64 sin = FixedMath.Sin(-Rotation, true);
        return new FixedVector2(localPoint.x * cos - localPoint.y * sin, localPoint.x * sin + localPoint.y * cos);
    }
}