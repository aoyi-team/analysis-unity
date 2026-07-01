using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Birthday_Bullet : MonoBehaviour
{
    public GameObject AttackParticle;//测试在箱子上面能否实现
    public GameObject TaiErWenZi;
    public float ParticleMover;
    public int AttackAmount = 360;
    public float BulletDestroyTime = 0.5f;
    public Vector2 targetPos;
    void Start()
    {
        Destroy(gameObject, BulletDestroyTime);
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Box"))
        {
            GameObject TargetGameobject = collision.gameObject;
            BoxTrigger BoxScript = TargetGameobject.GetComponent<BoxTrigger>();
            int FormerHealth = BoxScript.HealthMax;
            Instantiate(TaiErWenZi, targetPos, Quaternion.identity);
            Vector2 ParticleRandom = Random.insideUnitCircle * ParticleMover;
            Vector2 Dimension2 = new Vector2(transform.position.x, transform.position.y);
            Instantiate(AttackParticle, Dimension2 + ParticleRandom, Quaternion.identity);
            BoxScript.HealthMax -= AttackAmount;
            int NowHealth = BoxScript.HealthMax;
            BoxScript.AnimaPlaySystem(FormerHealth, NowHealth);
            Destroy(gameObject);
        }

    }
}
