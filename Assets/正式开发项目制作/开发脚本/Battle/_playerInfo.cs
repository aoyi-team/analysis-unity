
using FixMath;
using System;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// ���ڴ洢��Ӧ��ҵĽ�ɫ����ֵ
/// 
/// �ṩ<���ݽ�ɫ��Ϣ������Ϣ>�ӿ�
/// 
/// </summary>
public class _playerInfo:EntityInfo
{
    // ==================== ������ʶ ====================
    public string UserId { get; private set; }
    public int HeroId { get; private set; }
    public int TeamId { get; private set; }
    public bool IsEnemy { get; set; }

    // ==================== �������� ====================
    public CharacterConfig characterConfig { get; private set; }

    // ==================== ��̬���ݣ��������� ====================
    private int _level;
    private Fixed64 _currentHp;
    private int _score;                 // ���������������趨����
    private bool _isDead;
    private ActionCode actionCode;
    private AnimState animState;
    private int _skillCooldownRemaining;      // ֡�������� int
    private int _normalAttackCooldownRemaining;
    private Fixed64 _speed;                   // �ƶ��ٶȣ���������
    private int _flipX;                       // -1 �� 1���� int �㹻
    private bool _isMoving;

    // ==================== ��ʼ�� ====================
    public _playerInfo(string userId, int heroId, int teamId, FixedVector2 spawnPos)
    {
        _level = 0;
        UserId = userId;
        characterConfig = ResMgr.LoadResource<CharacterConfig>($"HeroConfigs/Hero_{heroId}");
        HeroId = heroId;
        TeamId = teamId;

        // IsEnemy 由 PlayerManager 根据 BattleContext.LocalTeamId 统一设置
        // 避免在构造函数内依赖全局单例 PlayerBasicInfoMgr.Instance

        // �� Config ��ȡ����������
        // null check
        if (characterConfig == null)
        {
            Debug.LogError($"_playerInfo: failed to load HeroConfigs/Hero_{heroId}, using defaults");
            _currentHp = new Fixed64(100);
            _speed = new Fixed64(5);
        }
        else
        {
            _currentHp = characterConfig.MaxHp;
            _speed = characterConfig.MoveSpeedBits;
        }


        // ��ʼ��
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
        if (characterConfig == null) return;
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
            FixedColliderPosSync(); // ͬ����ײ��λ��
            Debug.Log("player��ʼ����ײ���ɹ�");
        }
    }

    #region �ⲿ���ʽӿڣ�ֻ�����߼���ʹ�ö�������
    public int Level => _level;
    public Fixed64 Speed => _speed;
    public Fixed64 CurrentHp => _currentHp;
    public Fixed64 MaxHp => characterConfig?.MaxHp ?? new Fixed64(100);
    public Fixed64 HpPercent => MaxHp > Fixed64.Zero ? _currentHp / MaxHp : Fixed64.Zero;
    public int Score => _score;
    public bool IsDead => _isDead;
    public ActionCode A_Code => actionCode;
    public AnimState A_State => animState;
    public bool CanAttack => !_isDead && _normalAttackCooldownRemaining <= 0;
    public bool CanSkill => !_isDead && _skillCooldownRemaining <= 0;
    public int FlipX => _flipX;          // ���ֲ㷽��
    public bool IsMoving => _isMoving;
    #endregion

    #region �߼����޸Ľӿڣ�ֻ���߼����ܵ��ã�
    public void SetIsMoving(bool moving) => _isMoving = moving;
    public void SetFlipX(int flip) => _flipX = flip;

    public void SetSpeed(Fixed64 value) => _speed = value;

    public void SetACode(ActionCode acode) => actionCode = acode;
    public void SetAState(AnimState state) => animState = state;

    /// <summary>
    /// ��Ѫ���˺�ֵΪ��������
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
        _currentHp = Fixed64.Min(MaxHp, _currentHp + amount);
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

    #region ���ⷽ��
    public override void FixedColliderPosSync()
    {
        if(dynamic_col != null)
        {
            dynamic_col.SyncLogicPos(_currLogicPos);
        }
        if (dynamic_col == null) Debug.Log("��ǰDynamicΪ�գ�");
        //static_col.SyncLogicPos(_currLogicPos);
    }

    #endregion

    /// ������
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