
using FixMath;
using System;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// 用于存储对应玩家的角色的数值
/// 
/// 提供<根据角色信息更新信息>接口
/// 
/// </summary>
public class _playerInfo:EntityInfo
{
    // ==================== 基础标识 ====================
    public string UserId { get; private set; }
    public int HeroId { get; private set; }
    public int TeamId { get; private set; }
    public bool IsEnemy { get; private set; } // 相对于本地玩家

    // ==================== 配置引用 ====================
    public CharacterConfig characterConfig { get; private set; }

    // ==================== 动态数据（定点数） ====================
    private int _level;
    private Fixed64 _currentHp;
    private int _score;                 // 分数是整数，无需定点数
    private bool _isDead;
    private ActionCode actionCode;
    private AnimState animState;
    private int _skillCooldownRemaining;      // 帧数，保持 int
    private int _normalAttackCooldownRemaining;
    private Fixed64 _speed;                   // 移动速度（定点数）
    private int _flipX;                       // -1 或 1，用 int 足够
    private bool _isMoving;

    // ==================== 初始化 ====================
    public _playerInfo(string userId, int heroId, int teamId, FixedVector2 spawnPos)
    {
        _level = 0;
        UserId = userId;
        characterConfig = ResMgr.LoadResource<CharacterConfig>($"HeroConfigs/Hero_{heroId}");
        HeroId = heroId;
        TeamId = teamId;
        IsEnemy = teamId != PlayerBasicInfoMgr.Instance.TeamId ;

        // 从 Config 读取定点数属性
        _currentHp = characterConfig.MaxHp;
        _speed = characterConfig.MoveSpeedBits;


        // 初始化
        bornPoint = spawnPos;
        _currLogicPos = bornPoint;
        _prevLogicPos = bornPoint;
        _score = 0;
        _isDead = false;
        actionCode = ActionCode.None;
        animState = AnimState.side;
        _skillCooldownRemaining = 0;
        _normalAttackCooldownRemaining = 0;
        _flipX = 1;
        _isMoving = false;


    }

    public override void Init(object o =null)
    {
        // 创建碰撞器（根据角色配置）
        if (characterConfig._colliderDatas != null && characterConfig._colliderDatas.Count != 0)
        {
            foreach (var data in characterConfig._colliderDatas)
            {
                dynamic_col = ColliderTool.ColliderDataConvertToCollider(data, _currLogicPos, false);
                dynamic_col.SetFollowObj(this);
                CollisionManager.Instance.AddDynamicCollider(dynamic_col);
                CollisionManager.Instance.MarkDynamicDirty();
                dynamic_col.Layer = (int)CollisionLayer.Player;
                //static_col = ColliderTool.ColliderDataConvertToCollider(data, _currLogicPos, false);
                //static_col.SetFollowObj(this);
                //static_col.Layer = (int)CollisionLayer.Player;
                //CollisionManager.Instance.AddStaticCollider(static_col);

            }
            FixedColliderPosSync(); // 同步碰撞器位置
            Debug.Log("player初始化碰撞器成功");
        }
    }

    #region 外部访问接口（只读，逻辑层使用定点数）
    public int Level => _level;
    public Fixed64 Speed => _speed;
    public Fixed64 CurrentHp => _currentHp;
    public Fixed64 MaxHp => characterConfig.MaxHp;
    public Fixed64 HpPercent => _currentHp / characterConfig.MaxHp;   // 百分比，定点数
    public int Score => _score;
    public bool IsDead => _isDead;
    public ActionCode A_Code => actionCode;
    public AnimState A_State => animState;
    public bool CanAttack => !_isDead && _normalAttackCooldownRemaining <= 0;
    public bool CanSkill => !_isDead && _skillCooldownRemaining <= 0;
    public int FlipX => _flipX;          // 表现层方向
    public bool IsMoving => _isMoving;
    #endregion

    #region 逻辑层修改接口（只有逻辑层能调用）
    public void SetIsMoving(bool moving) => _isMoving = moving;
    public void SetFlipX(int flip) => _flipX = flip;

    public void SetSpeed(Fixed64 value) => _speed = value;

    public void SetACode(ActionCode acode) => actionCode = acode;
    public void SetAState(AnimState state) => animState = state;

    /// <summary>
    /// 扣血（伤害值为定点数）
    /// </summary>
    public void TakeDamage(Fixed64 damage)
    {
        if (_isDead) return;
        _currentHp = Fixed64.Max(Fixed64.Zero, _currentHp - damage);
        if (_currentHp <= Fixed64.Zero)
        {
            _isDead = true;
        }
    }

    public void Heal(Fixed64 amount)
    {
        if (_isDead) return;
        _currentHp = Fixed64.Min(characterConfig.MaxHp, _currentHp + amount);
    }

    public void LevelUp() => _level++;
    public void AddScore(int score) => _score += score;
    public void SetDead(bool isDead) => _isDead = isDead;

    public void ReduceCooldowns()
    {
        if (_skillCooldownRemaining > 0) _skillCooldownRemaining--;
        if (_normalAttackCooldownRemaining > 0) _normalAttackCooldownRemaining--;
    }
    #endregion

    #region 额外方法
    public override void FixedColliderPosSync()
    {
        if(dynamic_col != null)
        {
            dynamic_col.SyncLogicPos(_currLogicPos);
        }
        if (dynamic_col == null) Debug.Log("当前Dynamic为空！");
        //static_col.SyncLogicPos(_currLogicPos);
    }

    #endregion

    /// 测试用
#if UNITY_EDITOR
    public void SetDycCollider(FixedCollider2D col)
    {
        dynamic_col= col;
    }
    public void SetStaticCollider(FixedCollider2D col)
    {
        static_col = col;
    }
#endif
}