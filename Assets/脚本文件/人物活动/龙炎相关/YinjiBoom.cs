using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using UnityEngine;

public class YinjiBoom : MonoBehaviour
{
    private Animation ThisAnim;
    public GameObject AttackParticle;//测试在箱子上面能否实现
    public GameObject LongYanWenZi;
    public float ParticleMover;
    public int AttackAmount = 360;
    private AudioSource ThisAudioSource;
    public int WhoDash;
    private void Start()
    {
        ThisAnim = gameObject.GetComponent<Animation>();
        Destroy(gameObject, ThisAnim.clip.length);
        ThisAudioSource = gameObject.GetComponent<AudioSource>();
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        GameObject TargetGameobject = collision.gameObject;
        if (TargetGameobject.tag == "SmallMonster")
        {
            TargetGameobject.GetComponent<Animator>().SetTrigger("BeAttack");
            ThisAudioSource.PlayOneShot(ThisAudioSource.clip);
            Vector3 YinJiBoom = gameObject.transform.position;
            Instantiate(LongYanWenZi, YinJiBoom, Quaternion.identity);
            Vector2 ParticleRandom = Random.insideUnitCircle * ParticleMover;
            Vector2 Dimension2 = new Vector2(transform.position.x, transform.position.y);
            Instantiate(AttackParticle, Dimension2 + ParticleRandom, Quaternion.identity);
            TargetGameobject.GetComponent<SmallMonster>().HealthMax -= AttackAmount;
        }
        if (TargetGameobject.tag == "Box")
        {
            BoxTrigger BoxScript = TargetGameobject.GetComponent<BoxTrigger>();
            int FormerHealth = BoxScript.HealthMax;
            Vector3 YinJiBoom = gameObject.transform.position;
            Instantiate(LongYanWenZi, YinJiBoom, Quaternion.identity);
            Vector2 ParticleRandom = Random.insideUnitCircle * ParticleMover;
            Vector2 Dimension2 = new Vector2(transform.position.x, transform.position.y);
            Instantiate(AttackParticle, Dimension2 + ParticleRandom, Quaternion.identity);
            BoxScript.HealthMax -= AttackAmount;
            ThisAudioSource.PlayOneShot(ThisAudioSource.clip);
            int NowHealth = BoxScript.HealthMax;
            BoxScript.AnimaPlaySystem(FormerHealth, NowHealth);

        }
        if (TargetGameobject.tag == "SmallBox")
        {
            TargetGameobject.GetComponent<Animator>().SetTrigger("Break");
            TargetGameobject.GetComponent<SmallBox>().Healthbar.fillAmount = 0;
            TargetGameobject.GetComponent<CircleCollider2D>().enabled = false;
            ThisAudioSource.PlayOneShot(ThisAudioSource.clip);
            Vector3 YinJiBoom = gameObject.transform.position;
            Instantiate(LongYanWenZi, YinJiBoom, Quaternion.identity);
            Vector2 ParticleRandom = Random.insideUnitCircle * ParticleMover;
            Vector2 Dimension2D = new Vector2(transform.position.x, transform.position.y);
            Instantiate(AttackParticle, Dimension2D + ParticleRandom, Quaternion.identity);
        }
    }
}
