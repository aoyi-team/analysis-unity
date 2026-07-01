using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Birthday_Achu : BaseCharController
{
    private Animator Achu_animator;
    public PolygonCollider2D this_collider2d;
    public float frontTime;
    public float backTime;
    public GameObject Achu_Wenzi;
    public GameObject AttackParticle;
    public float ParticleMover;
    public float wenziMover;
    public int AttackAmount;
    public Vector2 targetPos;
    private void Start()
    {
        Achu_animator= GetComponent<Animator>();
    }
    public override void Attack()
    {
        Achu_animator.Play("Achu_cemian_attack");
        StartCoroutine(AttackSettle());
    }
    IEnumerator AttackSettle()
    {
        yield return new WaitForSeconds(frontTime);
        this_collider2d.enabled = true;
        yield return new WaitForSeconds(backTime);
        this_collider2d.enabled=false;
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
            Instantiate(Achu_Wenzi, targetPos, Quaternion.identity);
            Vector2 ParticleRandom = Random.insideUnitCircle * ParticleMover;
            Vector2 Dimension2 = new Vector2(TargetGameobject.transform.position.x, TargetGameobject.transform.position.y);
            Instantiate(AttackParticle, Dimension2 + ParticleRandom, Quaternion.identity);
            BoxScript.HealthMax -= AttackAmount;
            int NowHealth = BoxScript.HealthMax;
            BoxScript.AnimaPlaySystem(FormerHealth, NowHealth);

        }

    }
}
