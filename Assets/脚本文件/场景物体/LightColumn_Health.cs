using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightColumn_Health : MonoBehaviour
{
    private Coroutine damageCoroutine;
    public GameObject AttackParticle;
    public float ParticleMover;//粒子半径显示

    public void TakeDamage(int damage)//艾米光柱部分
    {
        if (gameObject.tag == "Box")
        {
            BoxTrigger BoxScript = gameObject.GetComponent<BoxTrigger>();
            int FormerHealth = BoxScript.HealthMax;
            Vector2 ParticleRandom = Random.insideUnitCircle * ParticleMover;
            Vector2 Dimension2 = new Vector2(gameObject.transform.position.x, gameObject.transform.position.y);
            Instantiate(AttackParticle, Dimension2 + ParticleRandom, Quaternion.identity);
            BoxScript.HealthMax -= damage;
            int NowHealth = BoxScript.HealthMax;
            BoxScript.AnimaPlaySystem(FormerHealth, NowHealth);
        }
        else if (gameObject.tag == "SmallBox")
        {
            gameObject.GetComponent<Animator>().SetTrigger("Break");
            gameObject .GetComponent<SmallBox>().Healthbar.fillAmount = 0;
            gameObject.GetComponent<CircleCollider2D>().enabled = false;
            Vector2 ParticleRandom = Random.insideUnitCircle * ParticleMover;
            Vector2 Dimension2D = new Vector2(transform.position.x, transform.position.y);
            Instantiate(AttackParticle, Dimension2D + ParticleRandom, Quaternion.identity);
        }
        else if(gameObject.tag== "SmallMonster")
        {
            gameObject .GetComponent<Animator>().SetTrigger("BeAttack");
            Vector2 ParticleRandom = Random.insideUnitCircle * ParticleMover;
            Vector2 Dimension2 = new Vector2(gameObject.transform.position.x, gameObject.transform.position.y);
            Instantiate(AttackParticle, Dimension2 + ParticleRandom, Quaternion.identity);
            gameObject .GetComponent<SmallMonster>().HealthMax -= damage ;
        }
    }

    public void StartTakingDamage(int damage, float interval)
    {
        if (damageCoroutine != null)
        {
            StopCoroutine(damageCoroutine); // 如果已经有协程在运行，则停止
        }
        damageCoroutine = StartCoroutine(DealDamage(damage, interval));
    }

    public void StopTakingDamage()
    {
        if (damageCoroutine != null)
        {
            StopCoroutine(damageCoroutine);
            damageCoroutine = null; // 清空协程引用
        }
    }

    private IEnumerator DealDamage(int damage, float interval)
    {
        while (true)
        {
            TakeDamage(damage);
            yield return new WaitForSeconds(interval);
        }
    }
}

