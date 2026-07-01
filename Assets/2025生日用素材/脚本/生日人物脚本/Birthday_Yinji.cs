using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Birthday_Yinji : MonoBehaviour
{
    public GameObject AttackParticle;//测试在箱子上面能否实现
    public GameObject LongYanWenZi;
    public float ParticleMover;
    public int AttackAmount = 360;
    public Vector2 targetPos;

    void Start()
    {
        Destroy(gameObject, 0.45f);
        StartCoroutine(DestroyTheAttack());

    }
    IEnumerator DestroyTheAttack()
    {
        yield return new WaitForSeconds(0.1f);
        gameObject.GetComponent<CapsuleCollider2D>().enabled = false;
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Box"))
        {
            Attack(collision.gameObject);
        }
    }
    private void Attack(GameObject TargetGameobject)
    {
        if (TargetGameobject.tag == "Box")
        {
            BoxTrigger BoxScript = TargetGameobject.GetComponent<BoxTrigger>();
            int FormerHealth = BoxScript.HealthMax;
            Instantiate(LongYanWenZi, targetPos, Quaternion.identity);
            Vector2 ParticleRandom = Random.insideUnitCircle * ParticleMover;
            Vector2 Dimension2 = new Vector2(transform.position.x, transform.position.y);
            Instantiate(AttackParticle, Dimension2 + ParticleRandom, Quaternion.identity);
            BoxScript.HealthMax -= AttackAmount;
            int NowHealth = BoxScript.HealthMax;
            BoxScript.AnimaPlaySystem(FormerHealth, NowHealth);

        }
    }
}
