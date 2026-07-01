using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NuoYaAttackMisile : MonoBehaviour
{
    private Animation ThisAnim;
    public GameObject AttackParticle;//测试在箱子上面能否实现
    public GameObject NuoYaWenZi;
    public float ParticleMover;
    public int AttackAmount = 360;
    private AudioSource ThisAudioSource;
    void Start()
    {
        ThisAnim = gameObject.GetComponent<Animation>();
        Destroy(gameObject, ThisAnim.clip.length-0.025f);
        ThisAudioSource = gameObject.GetComponent<AudioSource>();
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Box") || collision.CompareTag("SmallMonster")|| collision.CompareTag("SmallBox"))
        {
            StartCoroutine(Attack(collision.gameObject));
        }
        
    }
    IEnumerator Attack(GameObject TargetGameobject)
    {
        yield return new WaitForSeconds(0.08f);
        gameObject.GetComponent<SpriteRenderer>().sortingOrder = 1;
        if (TargetGameobject.tag == "Box")
        {
            BoxTrigger BoxScript = TargetGameobject.GetComponent<BoxTrigger>();
            int FormerHealth = BoxScript.HealthMax;
            Vector3 MislePosition = gameObject.transform.position;
            Instantiate(NuoYaWenZi, MislePosition, Quaternion.identity);
            Vector2 ParticleRandom = Random.insideUnitCircle * ParticleMover;
            Vector2 Dimension2 = new Vector2(transform.position.x, transform.position.y);
            Instantiate(AttackParticle, Dimension2 + ParticleRandom, Quaternion.identity);
            BoxScript.HealthMax -= AttackAmount;
            ThisAudioSource.PlayOneShot(ThisAudioSource.clip);
            int NowHealth = BoxScript.HealthMax;
            BoxScript.AnimaPlaySystem(FormerHealth, NowHealth);

        }
        else if (TargetGameobject.tag == "SmallBox")
        {
            TargetGameobject.GetComponent<Animator>().SetTrigger("Break");
            TargetGameobject.GetComponent<SmallBox>().Healthbar.fillAmount = 0;
            TargetGameobject.GetComponent<CircleCollider2D>().enabled = false;
            ThisAudioSource.PlayOneShot(ThisAudioSource.clip);
            Vector3 FeibiaodPosition =gameObject.transform.position;
            Instantiate(NuoYaWenZi, FeibiaodPosition, Quaternion.identity);
            Vector2 ParticleRandom = Random.insideUnitCircle * ParticleMover;
            Vector2 Dimension2D =new Vector2(transform.position.x, transform.position.y);
            Instantiate(AttackParticle, Dimension2D + ParticleRandom, Quaternion.identity);
        }
        else if (TargetGameobject.tag == "SmallMonster")
        {
            TargetGameobject.GetComponent<Animator>().SetTrigger("BeAttack");
            ThisAudioSource.PlayOneShot(ThisAudioSource.clip);
            Vector3 MislePosition = gameObject.transform.position;
            Instantiate(NuoYaWenZi, MislePosition, Quaternion.identity);
            Vector2 ParticleRandom = Random.insideUnitCircle * ParticleMover;
            Vector2 Dimension2 = new Vector2(transform.position.x, transform.position.y);
            Instantiate(AttackParticle, Dimension2 + ParticleRandom, Quaternion.identity);
            TargetGameobject.GetComponent<SmallMonster>().HealthMax -= AttackAmount;
        }

    }
}
