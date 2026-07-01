using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AiMiAttackFlyObj : MonoBehaviour
{
    public GameObject AttackParticle;//测试在箱子上面能否实现
    public GameObject ChangEwenZi;
    public float ParticleMover;
    public int AttackAmount = 360;
    public float BulletDestroyTime = 0.5f;
    public GameObject AudioSave;
    void Start()
    {
        Destroy(gameObject, BulletDestroyTime);
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Box") || collision.CompareTag("SmallMonster") || collision.CompareTag("SmallBox"))
        {
            Instantiate(AudioSave, transform.position, Quaternion.identity);
            GameObject TargetGameobject = collision.gameObject;
            if (TargetGameobject.tag == "Box")
            {
                BoxTrigger BoxScript = TargetGameobject.GetComponent<BoxTrigger>();
                int FormerHealth = BoxScript.HealthMax;
                Vector3 MislePosition = gameObject.transform.position;
                Instantiate(ChangEwenZi, MislePosition, Quaternion.identity);
                Vector2 ParticleRandom = Random.insideUnitCircle * ParticleMover;
                Vector2 Dimension2 = new Vector2(transform.position.x, transform.position.y);
                Instantiate(AttackParticle, Dimension2 + ParticleRandom, Quaternion.identity);
                BoxScript.HealthMax -= AttackAmount;
                int NowHealth = BoxScript.HealthMax;
                BoxScript.AnimaPlaySystem(FormerHealth, NowHealth);

            }
            else if (TargetGameobject.tag == "SmallBox")
            {
                TargetGameobject.GetComponent<Animator>().SetTrigger("Break");
                TargetGameobject.GetComponent<SmallBox>().Healthbar.fillAmount = 0;
                TargetGameobject.GetComponent<CircleCollider2D>().enabled = false;
                Vector3 FeibiaodPosition = gameObject.transform.position;
                Instantiate(ChangEwenZi, FeibiaodPosition, Quaternion.identity);
                Vector2 ParticleRandom = Random.insideUnitCircle * ParticleMover;
                Vector2 Dimension2D = new Vector2(transform.position.x, transform.position.y);
                Instantiate(AttackParticle, Dimension2D + ParticleRandom, Quaternion.identity);
            }
            else
            {
                TargetGameobject.GetComponent<Animator>().SetTrigger("BeAttack");
                Vector3 MislePosition = gameObject.transform.position;
                Instantiate(ChangEwenZi, MislePosition, Quaternion.identity);
                Vector2 ParticleRandom = Random.insideUnitCircle * ParticleMover;
                Vector2 Dimension2 = new Vector2(transform.position.x, transform.position.y);
                Instantiate(AttackParticle, Dimension2 + ParticleRandom, Quaternion.identity);
                TargetGameobject.GetComponent<SmallMonster>().HealthMax -= AttackAmount;
            }
            Destroy(gameObject,0.05f);
        }

    }
}
