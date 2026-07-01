using FixMath;
public class FixedBoxCollider2D : FixedCollider2D
{
    /// <summary>
    /// BoxCollider的尺寸，表示宽度和高度，单位为逻辑单位（如像素），在碰撞检测中会用到这个尺寸信息
    /// </summary>
    public FixedVector2 Size { get; private set; }

    /// <summary>
    /// 宽，高，单位为逻辑单位（如像素）
    /// </summary>
    public Fixed64 width => Size.x / 2;// 这里除以2是因为在碰撞检测中通常使用半宽和半高来进行计算
    public Fixed64 height => Size.y / 2;// 同上，使用半高进行碰撞检测计算
    public FixedBoxCollider2D(FixedVector2 logicPos, FixedVector2 center, FixedVector2 size, bool isTrigger = false) : base(logicPos, center, isTrigger)
    {
        Size = size;
        this.ColliderType = Collider2DEnum.Box;
    }
}