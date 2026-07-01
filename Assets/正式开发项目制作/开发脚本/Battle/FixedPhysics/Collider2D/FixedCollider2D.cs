using FixMath;
using System;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// 碰撞器类型
/// </summary>
public enum Collider2DEnum
{
    None,
    Box,
    Circle,
    Sphere,
    Edge,
    Polygon,
    OBB // 新增可旋转矩形
}
/// <summary>
/// 碰撞器基类，所有具体的碰撞器类型（如圆形、矩形等）   (后续添加多边形碰撞器和edge Collider)
/// </summary>
public partial class FixedCollider2D
{

    #region 事件模块
    // 碰撞事件（对应Unity的OnCollisionXXX/OnTriggerXXX）
    public event Action<CollisionInfo> OnCollisionEnter;
    public event Action<CollisionInfo> OnCollisionStay;
    public event Action<CollisionInfo> OnCollisionExit;
    public event Action<CollisionInfo> OnTriggerEnter;
    public event Action<CollisionInfo> OnTriggerStay;
    public event Action<CollisionInfo> OnTriggerExit;

    // 触发事件（内部调用）
    internal void InvokeCollisionEnter(CollisionInfo info) => OnCollisionEnter?.Invoke(info);
    internal void InvokeCollisionStay(CollisionInfo info) => OnCollisionStay?.Invoke(info);
    internal void InvokeCollisionExit(CollisionInfo info) => OnCollisionExit?.Invoke(info);
    internal void InvokeTriggerEnter(CollisionInfo info) => OnTriggerEnter?.Invoke(info);
    internal void InvokeTriggerStay(CollisionInfo info) => OnTriggerStay?.Invoke(info);
    internal void InvokeTriggerExit(CollisionInfo info) => OnTriggerExit?.Invoke(info);
    #endregion
    public bool IsSameOwner(FixedCollider2D other)
    {
        if(renderInfo == null || other.renderInfo == null) return false;
        return ReferenceEquals(this.renderInfo, other.renderInfo);
    }

    private static int _nextColliderId = 1; // 用于生成唯一ID的静态计数器
    public int ColliderId { get; }= _nextColliderId++; // 每创建一个碰撞器，ID自增

    /// <summary>
    /// 碰撞器类型
    /// </summary>
    public Collider2DEnum ColliderType { get; protected set; }

    public int Layer { get; set; } // 碰撞层，默认为0，可以根据需要设置不同的层来控制碰撞检测
    public EntityInfo renderInfo { get; private set; }
    /// <summary>
    /// 激活状态,默认激活
    /// </summary>
    public bool Active { get; private set; }
    /// <summary>
    /// 所挂载的物体的逻辑位置
    /// </summary>
    public FixedVector2 LogicPos { get; private set; }
    
    public FixedVector2 Center { get; private set; }

    /// <summary>
    /// Angle,角度0~360°
    /// </summary>
    public Fixed64 Rotation { get; private set; }

    /// <summary>
    /// 世界逻辑位置，等于逻辑位置加上中心偏移(该地方进行偏移的角度旋转)
    /// </summary>
    public virtual FixedVector2 WorldLogicPos => LogicPos + Center.Rotate(Rotation); 
    public bool IsTrigger { get; set; } // 是否为触发器碰撞体，触发器碰撞体只会调用事件，不会产生物理反应,默认为false

    /// <summary>
    /// X和Y属性提供了访问逻辑位置的X和Y坐标的简便方式(偏移后)
    /// </summary>
    public Fixed64 X => WorldLogicPos.x;
    public Fixed64 Y => WorldLogicPos.y;

    /// <summary>
    /// 是否发生碰撞
    /// </summary>
    protected bool is_mCollider;

    public FixedCollider2D(FixedVector2 logicPos, FixedVector2 center, bool isTrigger = false)
    {
        this.Active = true;
        this.LogicPos = logicPos;
        this.Center = center;
        this.IsTrigger = isTrigger;
        Layer = (int)CollisionLayer.None;
    }

    /// <summary>
    /// 设置目标跟随信息
    /// </summary>
    /// <param name="obj"></param>
    public void SetFollowObj(EntityInfo obj)
    {
        this.renderInfo = obj;
    }
    /// <summary>
    /// 更新逻辑位置
    /// </summary>
    /// <param name="logicPos"></param>
    public virtual void SyncLogicPos(FixedVector2 logicPos)
    {
        this.LogicPos = logicPos;
    }

    public void SetActive(bool active)
    {
        this.Active = active;
    }

    public virtual void SyncLogicRotation(Fixed64 Rotation)
    {
        this.Rotation= Rotation;
    }
}