using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Continuous_Harm : MonoBehaviour
{
    public GameObject AttackParticle;
    public float ParticleMover;//Á£×Ó°ë¾¶ÏÔÊ¾

    public void TakeDamage(int damage,GameObject Wenzi)//·¶Î§ÐÔÉËº¦Ð§¹û
    {
        if (gameObject.tag == "Box")
        {
            BoxTrigger BoxScript = gameObject.GetComponent<BoxTrigger>();
            int FormerHealth = BoxScript.HealthMax;
            Vector2 ParticleRandom = Random.insideUnitCircle * ParticleMover;
            Vector2 Dimension2 = new Vector2(gameObject.transform.position.x, gameObject.transform.position.y);
            Instantiate(AttackParticle, Dimension2 + ParticleRandom, Quaternion.identity);
            Instantiate(Wenzi, Dimension2 + ParticleRandom, Quaternion.identity);
            BoxScript.HealthMax -= damage;
            int NowHealth = BoxScript.HealthMax;
            BoxScript.AnimaPlaySystem(FormerHealth, NowHealth);
        }
        else if (gameObject.tag == "SmallBox")
        {
            gameObject.GetComponent<Animator>().SetTrigger("Break");
            gameObject.GetComponent<SmallBox>().Healthbar.fillAmount = 0;
            gameObject.GetComponent<CircleCollider2D>().enabled = false;
            Vector2 ParticleRandom = Random.insideUnitCircle * ParticleMover;
            Vector2 Dimension2D = new Vector2(transform.position.x, transform.position.y);
            Instantiate(AttackParticle, Dimension2D + ParticleRandom, Quaternion.identity);
            Instantiate(Wenzi, Dimension2D + ParticleRandom, Quaternion.identity);
        }
        else if (gameObject.tag == "SmallMonster")
        {
            gameObject.GetComponent<Animator>().SetTrigger("BeAttack");
            Vector2 ParticleRandom = Random.insideUnitCircle * ParticleMover;
            Vector2 Dimension2 = new Vector2(gameObject.transform.position.x, gameObject.transform.position.y);
            Instantiate(AttackParticle, Dimension2 + ParticleRandom, Quaternion.identity);
            Instantiate(Wenzi, Dimension2 + ParticleRandom, Quaternion.identity);
            gameObject.GetComponent<SmallMonster>().HealthMax -= damage;
        }
        else if (gameObject.tag == "ZhanWuYanDiLei")
        {
            gameObject.GetComponent<PlayerInfo>().NowHealth -= damage;
            Vector2 ParticleRandom = Random.insideUnitCircle * ParticleMover;
            Vector2 Dimension2D = new Vector2(gameObject.transform.position.x, gameObject.transform.position.y);
            Instantiate(AttackParticle, Dimension2D + ParticleRandom, Quaternion.identity);
            Instantiate(Wenzi, Dimension2D + ParticleRandom, Quaternion.identity);
        }
        else if (gameObject.tag == "Haluda")
        {
            gameObject.GetComponent<Haluda_Health>().Take_Damage(damage);
            Vector2 ParticleRandom = Random.insideUnitCircle * ParticleMover;
            Vector2 Dimension2D = new Vector2(gameObject.transform.position.x, gameObject.transform.position.y);
            Instantiate(AttackParticle, Dimension2D + ParticleRandom, Quaternion.identity);
            Instantiate(Wenzi, Dimension2D + ParticleRandom, Quaternion.identity);
        }
    }
}
