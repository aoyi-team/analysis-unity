using FixMath;
using System.Collections.Generic;
using UnityEngine;

// 逻辑层基类
public class BasePlayerLogic
{
    private const float TargetPositionScale = 1000f;
    private static readonly Fixed64 DefaultAttackDamage = new Fixed64(100);
    private static readonly Fixed64 DefaultAttackRadius = new Fixed64(1.2f);
    private ISkillLogic[] _skills; // 0=普攻，1..n=技能
    public _playerInfo player_Info { get; private set; }

    public virtual void Init(_playerInfo info, int[] skillIds)
    {
        player_Info = info;
        AttachCollisionEvents();
    }

    //逻辑帧执行
    public virtual void OnFrameLogicUpdate(MsgPlayerOp op)
    {
        ExecuteMove(op.moveDirX, op.moveDirY,ServerConfig.frameTime);
        if (op.actionCode == ActionCode.Attack)
        {
            Vector2 attackTarget = new Vector2(op.targetX / TargetPositionScale, op.targetY / TargetPositionScale);
            player_Info.SetLastAttackTarget(attackTarget);
            CombatResolver.ApplyPointAttack(player_Info, PlayerManager.Instance.AllPlayerInfos, attackTarget, GetNormalAttackDamage(), DefaultAttackRadius);
        }
        UpdateAnimatonState(op.flipx, op.actionCode, op.animstate,op.isMoving);
    }
    //走路
    public virtual void ExecuteMove(int movex,int movey,float frameInterval)
    {
        if(player_Info.IsDead) return;
        if(movex==0&&movey==0)
        {
            player_Info.SetIsMoving(false); 
            player_Info.SetPosition(FixedVector2.Zero);
            return;
        }
        FixedVector2 input = new FixedVector2(movex, movey).Normalize;
        //Debug.Log($"x移动:{movex},y移动{movey}");
        FixedVector2 movedir= input*player_Info.Speed* frameInterval.ToFixed();
        player_Info.SetPosition(movedir);
        //Debug.Log($"玩家{player_Info.UserId}新坐标:{player_Info.PosX},{player_Info.PosY}");
    }

    // Animator_AnimationState 更新当前播放动画
    public virtual void UpdateAnimatonState(int flipx,ActionCode acode,AnimState state,bool isMove)
    {
        if (player_Info.IsDead) return;
        player_Info.SetFlipX(flipx);
        player_Info.SetACode(acode);
        player_Info.SetAState(state);
        player_Info.SetIsMoving(isMove);
    }

    private Fixed64 GetNormalAttackDamage()
    {
        if (player_Info?.characterConfig != null && player_Info.characterConfig.AttackBits > Fixed64.Zero)
        {
            return player_Info.characterConfig.AttackBits;
        }

        return DefaultAttackDamage;
    }

    public virtual void AttachCollisionEvents()
    {

    }
}