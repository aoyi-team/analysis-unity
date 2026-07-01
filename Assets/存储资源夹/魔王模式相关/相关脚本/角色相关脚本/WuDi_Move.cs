using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class WuDi_Move : MonoBehaviour
{
    public float collisionTime = 1f;  // 冲撞的时间
    public float maxSpeed = 10f;          // 冲撞的最大速度 
    private Vector2 targetDirection;
    private Rigidbody2D rb;
    private float totalDistance;
    private float TargeDistance;
    private Animator Wudi_Animator;
    public Transform Haluda_TransForm;

    [Header("调整因子")]
    public float Front_Slow;
    public float Latter_Accelerate;
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        Wudi_Animator = GetComponent<Animator>();
        Aoyi();
    }
    private void Aoyi()
    {
        targetDirection = Haluda_TransForm.position - transform.position;
        TargeDistance = Vector2.Distance(transform.position, Haluda_TransForm.position);
        StartCoroutine(ExecuteCollisionSkill());
    }

    // 使用协程来实现冲撞的逐渐加速效果
    IEnumerator ExecuteCollisionSkill()
    {
        totalDistance = 0f;
        Vector2 startPosition = transform.position;  // 初始位置
        float OriginalTime = 0f;                  // 当前已行进的距离
        float speed = 0f;                             // 初始速度为0
        Wudi_Animator.Play("Wudi_Aoyi");
        bool IsTargetPos=false;
        while (OriginalTime < collisionTime/2)
        {
            // 计算加速速度：从0到最大速度
            speed = Mathf.Lerp(0, maxSpeed, (OriginalTime / collisionTime)/Front_Slow);

            // 计算移动的距离：speed * Time.deltaTime
            OriginalTime += Time.deltaTime;
            rb.velocity = targetDirection.normalized * speed;
            if (totalDistance > TargeDistance) break;
            totalDistance += speed * Time.deltaTime;
            // 根据计算出的目标方向移动玩家
            //rb.MovePosition(rb.position + targetDirection * moveDistance);

            // 每一帧等待
            yield return null;
        }
        if (!IsTargetPos)
        {
            while (OriginalTime < collisionTime)
            {
                speed = Mathf.Lerp(0, maxSpeed, (OriginalTime / collisionTime) * Latter_Accelerate);

                // 计算移动的距离：speed * Time.deltaTime
                OriginalTime += Time.deltaTime;
                rb.velocity = targetDirection.normalized * speed;
                if (totalDistance > TargeDistance) break;
                totalDistance += speed * Time.deltaTime;
                // 根据计算出的目标方向移动玩家
                //rb.MovePosition(rb.position + targetDirection * moveDistance);

                // 每一帧等待
                yield return null;
            }
        }
        rb.velocity = Vector2.zero;

    }

}
