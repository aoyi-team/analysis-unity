using FixMath;

public  class EntityInfo
{
    private static int _next = 1;




    // ==================== 双帧缓存 + 速度预测 ====================
    // 缓存最近两帧的逻辑位置（用于插值）
    public FixedVector2 _prevLogicPos;
    public FixedVector2 _currLogicPos;
    public FixedVector2 bornPoint;

    // 场景物体唯一标识
    public int EntityNumber { get; } =_next++;
    protected FixedCollider2D dynamic_col;          // 碰撞器引用
    protected FixedCollider2D static_col;           // 有阻挡效果的碰撞器

    public FixedCollider2D Dyc_Col => dynamic_col;
    public FixedCollider2D Sta_Col => static_col;

    public virtual void Init(object o=null) { }
    public virtual void FixedColliderPosSync() {
        if (dynamic_col != null)
        {
            dynamic_col.SyncLogicPos(_currLogicPos);

        }
        if (static_col != null)
        {
            static_col.SyncLogicPos(_currLogicPos);
        }
    }

    /// <summary>
    /// 增量移动（逻辑层每帧调用）
    /// </summary>
    public virtual void SetPosition(FixedVector2 delta)
    {
        _prevLogicPos = _currLogicPos;
        _currLogicPos += delta;
        FixedColliderPosSync();
    }

    /// <summary>
    /// 直接设置位置（用于碰撞修正）
    /// </summary>
    public virtual void SetPositionDirect(FixedVector2 newPos)
    {
        _prevLogicPos = _currLogicPos;
        _currLogicPos = newPos;
    }
}