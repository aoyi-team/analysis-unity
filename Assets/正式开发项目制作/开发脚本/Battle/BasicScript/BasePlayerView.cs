
using Unity.Mathematics;
using UnityEngine;

// 渲染层基类
public class BasePlayerView:MonoBehaviour
{
    private Animator m_Animator;
    private SpriteRenderer m_SpriterRender;
    protected _playerInfo m_playerInfo;
    // 渲染层的速度（用于SmoothDamp）
    private float last_RenderTime;
    private Vector3 _prePos;
    private Vector3 _currPos;
    // 强制同步，期间锁住位置，保证能到达目标点
    private bool _forceSync;


    public virtual void InitView(_playerInfo info)
    {
        m_playerInfo = info;
        m_Animator = GetComponent<Animator>();
        m_SpriterRender=GetComponent<SpriteRenderer>();
        _prePos = _currPos=info._currLogicPos.ToVector2();
    }

    // 每渲染帧调用
    private void Update()
    {
        if (m_playerInfo.IsDead) return;
        // 计算在两帧逻辑之间的进度
        float timeSinceLastLogic = Time.time - last_RenderTime;
        float t = Mathf.Clamp01(timeSinceLastLogic / ServerConfig.frameTime);

        // 直接用 Lerp 做插值（60fps，丝滑不卡）
        if(_forceSync==false) transform.position = Vector3.Lerp(_prePos, _currPos, t);
        //float sinceLastRender = Time.time - last_RenderTime;
        //float t = Mathf.Clamp01(sinceLastRender / ServerConfig.frameTime);

        //Vector3 targetPos = CalculatePredictedPosition();
        //// 核心优化：当 t >= 0.95 时，强制直接设置位置，保证能到终点
        //if (t >= 0.95f)
        //{
        //    transform.position = targetPos;
        //    _renderVelocity = Vector3.zero;
        //}
        //else
        //{
        //    // 否则用 SmoothDamp 做丝滑过渡
        //    transform.position = Vector3.SmoothDamp(
        //        transform.position,
        //        targetPos,
        //        ref _renderVelocity,
        //        smoothTime
        //    );
        //}

    }

    public virtual void OnRenderFrameUpdate()
    {
        if(m_playerInfo.IsDead) return;
        _forceSync = true;
        transform.position = _currPos;
        _prePos = m_playerInfo._prevLogicPos.ToVector2();
        _currPos= m_playerInfo._currLogicPos.ToVector2();
        _forceSync = false; 
        last_RenderTime = Time.time;
        UpdatePlayerAnim();

    }
    // 每逻辑帧调用
    public virtual void UpdatePlayerAnim()
    {
        if (m_Animator == null || m_playerInfo == null) return;
        m_SpriterRender.flipX = m_playerInfo.FlipX <0;
        if(m_playerInfo.IsMoving) m_Animator.SetBool("isMove", true);
        else m_Animator.SetBool("isMove", false);
        if (m_playerInfo.A_State == AnimState.up) m_Animator.SetBool("isUp", true);
        else m_Animator.SetBool("isUp", false);
        PlayActionAnim(m_playerInfo.A_State, m_playerInfo.A_Code);
    }
    public virtual void PlayActionAnim(AnimState state,ActionCode acode)
    {
        if(state==AnimState.up)
        {
            switch (acode)
            {
                case ActionCode.Attack:
                    m_Animator.Play("Shangmian_Attack");
                    break;
                case ActionCode.Skill:
                    m_Animator.Play("Shangmian_Skill");
                    break;
            }
        }
        if(state==AnimState.side)
        {
            switch (acode)
            {
                case ActionCode.Attack:
                    m_Animator.Play("Cemian_Attack");
                    break;
                case ActionCode.Skill:
                    m_Animator.Play("Cemian_Skill");
                    break;
            }
        }
    }
    public virtual void UpdatePlayerUI()
    {

    }

    /// <summary>
    /// 预测算法
    /// </summary>
    //private Vector3 CalculatePredictedPosition()
    //{

    //    float sinceLastRender = Time.time - last_RenderTime;
    //    float t=Mathf.Clamp01(sinceLastRender / ServerConfig.frameTime);
    //    Vector3 targetPos=Vector3.Lerp(_prePos, _currPos, t);
    //    //如果大于1说明超时了，那么这时候开始预测了，简单线性外推
    //    if (t>=1f)
    //    {
    //        float externTime = sinceLastRender - last_RenderTime;
    //        Vector3 moveDir=_currPos-_prePos;
    //        float speed=moveDir.magnitude/ServerConfig.frameTime;// 长度/时间=速度
    //        Vector3 prediction=moveDir.normalized*speed*externTime* predictionStrength;// 方向*速度*时间=位移
    //        targetPos+=prediction;

    //    }
    //    return targetPos;
    //}
}