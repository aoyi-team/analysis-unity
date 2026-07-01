using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Birthday_wukong : BaseCharController
{
    private Animator Wukong_Animator;
    public bool IsAnimation_Play_Over = true;
    public GameObject Wukong_Wenzi;
    public GameObject AttackParticle;
    public bool IsAttack = false;
    public int AttackAmount;
    public float ParticleMover;
    public PolygonCollider2D Wukong_Trigger;
    public float FireStepTime = 0.8f;
    public Vector2 targetPos;
    private void Start()
    {
        Wukong_Animator =GetComponent<Animator>();
    }
    public override void Attack()
    {
        IsAnimation_Play_Over = false;
        StartCoroutine(AttackRoutine());
    }
    IEnumerator AttackRoutine()
    {
        Wukong_Animator.Play("Wukong_Cemian_Attack");
        Wukong_Trigger.enabled = true;
        yield return new WaitForSeconds(FireStepTime-0.3f);
        Wukong_Trigger.enabled = false;
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Box"))
        {
            TakeDamage(collision.gameObject);
        }
    }
    private void TakeDamage(GameObject TargetGameobject)
    {
        if (TargetGameobject.tag == "Box")
        {
            BoxTrigger BoxScript = TargetGameobject.GetComponent<BoxTrigger>();
            int FormerHealth = BoxScript.HealthMax;
            Instantiate(Wukong_Wenzi, targetPos, Quaternion.identity);
            Vector2 ParticleRandom = Random.insideUnitCircle * ParticleMover;
            Vector2 Dimension2 = new Vector2(TargetGameobject.transform.position.x, TargetGameobject.transform.position.y);
            Instantiate(AttackParticle, Dimension2 + ParticleRandom, Quaternion.identity);
            BoxScript.HealthMax -= AttackAmount;
            int NowHealth = BoxScript.HealthMax;
            BoxScript.AnimaPlaySystem(FormerHealth, NowHealth);

        }

    }
}
