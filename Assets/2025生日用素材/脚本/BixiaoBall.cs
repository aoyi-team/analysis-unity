using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BixiaoBall :MonoBehaviour
{
    Animator animator;
    public GameObject AttackParticle;
    public GameObject BixiaoWenzi;
    public float ParticleMover;
    public int AttackAmount = 360;
    public float MaxDistance = 0;
    public float FlySpeed = 0;
    bool exploded = false;
    Vector3 direction;
    float DesDistance;
    public float destroytime;
    float NowFlyDis;
    public Vector2 targetPos;
    public virtual void Init( Vector3 dir)
    {
        direction = dir;
    }
    private void Awake()
    {
        animator = GetComponent<Animator>();
    }
    private void Update()
    {
        FlyToDestination(direction);
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        Explode();
        if (collision.CompareTag("Box"))
        {
            GameObject TargetGameobject = collision.gameObject;
            if (TargetGameobject.tag == "Box")
            {
                BoxTrigger BoxScript = TargetGameobject.GetComponent<BoxTrigger>();
                int FormerHealth = BoxScript.HealthMax;
                Instantiate(BixiaoWenzi, targetPos, Quaternion.identity);
                Vector2 ParticleRandom = Random.insideUnitCircle * ParticleMover;
                Vector2 Dimension2 = new Vector2(transform.position.x, transform.position.y);
                Instantiate(AttackParticle, Dimension2 + ParticleRandom, Quaternion.identity);
                BoxScript.HealthMax -= AttackAmount;
                int NowHealth = BoxScript.HealthMax;
                BoxScript.AnimaPlaySystem(FormerHealth, NowHealth);

            }
        }

    }
    //外界传参关于指定地点
    public void FlyToDestination(Vector2 direction)
    {
        if (exploded) return;

        float step = FlySpeed * Time.deltaTime;
        transform.Translate(direction * step, Space.World);
        NowFlyDis += step;

        // 条件 1：超过距离立即爆炸
        if (NowFlyDis >= MaxDistance) Explode();
    }
    public void Explode()
    {
        exploded = true;
        animator.Play("Bixiao_attackball");
        Destroy(gameObject, destroytime);
    }
}
