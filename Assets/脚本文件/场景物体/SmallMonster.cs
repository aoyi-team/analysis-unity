using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmallMonster : MonoBehaviour
{
    public GameObject YileWenZi;
    public float ParticleMover;
    public GameObject AttackParticle;
    public int HealthMax;
    public GameObject[] PickUps;
    public float MoveR;
    public GameObject SmallBiaoLeft;
    private float NowTickTime=0;//固定播放时间经过7秒;
    private Animator SmallMonsterAnimator;
    private Animation DiedAnim;
    private bool Can =true;
    private AudioSource ThisAudioSource;
    private void Start()
    {
        SmallMonsterAnimator = gameObject.GetComponent<Animator>();
        DiedAnim = gameObject.GetComponent<Animation>();
        ThisAudioSource = gameObject.GetComponent<AudioSource>();
    }

    private void Update()
    {
        FixedPlayAnimation();
        if (Can == true)
        {
            if (HealthMax <= 0)
            {
                Can = false;
                gameObject.GetComponent<CapsuleCollider2D>().enabled = false;
                SmallMonsterAnimator.SetBool("IsDead", true);
                GameObject SpedUp = Instantiate(PickUps[0], transform.position, Quaternion.identity);
                Vector2 RandomCircle = Random.insideUnitCircle * MoveR;
                Vector2 Dimension2 = new Vector2(transform.position.x, transform.position.y);
                SpedUp.GetComponent<Transform>().DOLocalMove(Dimension2 + RandomCircle, 0.5f);
                for (int i = 0; i <= 3; i++)
                {
                    GameObject Coin = Instantiate(PickUps[Random.Range(1, PickUps.Length)], transform.position, Quaternion.identity);
                    Vector2 RandomCirclet = Random.insideUnitCircle * MoveR;
                    Vector2 Dimension = new Vector2(transform.position.x, transform.position.y);
                    Coin.GetComponent<Transform>().DOLocalMove(Dimension + RandomCirclet, 0.5f);

                }
                Destroy(gameObject, DiedAnim.clip.length);
            }
        }

    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("feibiao"))
        {
            SmallMonsterAnimator.SetTrigger("BeAttack");
            Vector3 FeibiaodPosition = collision.gameObject.transform.position;
            Instantiate(YileWenZi, FeibiaodPosition, Quaternion.identity);
            Vector2 ParticleRandom = Random.insideUnitCircle * ParticleMover;
            Vector2 Dimension2 = new Vector2(transform.position.x, transform.position.y);
            Instantiate(AttackParticle, Dimension2 + ParticleRandom, Quaternion.identity);
            HealthMax -= 200;
            ThisAudioSource.PlayOneShot(ThisAudioSource.clip);
        }
        if (collision.CompareTag("SmallBiao"))
        {
            SmallMonsterAnimator.SetTrigger("BeAttack");
            HealthMax -= 50;
            ThisAudioSource.PlayOneShot(ThisAudioSource.clip);
            Destroy(collision.gameObject);
            Vector3 FeibiaodPosition = collision.gameObject.transform.position;
            Instantiate(YileWenZi, FeibiaodPosition, Quaternion.identity);
            Vector2 ParticleRandom = Random.insideUnitCircle * ParticleMover;
            Vector2 Dimension2 = new Vector2(transform.position.x, transform.position.y);
            Instantiate(AttackParticle, Dimension2 + ParticleRandom, Quaternion.identity);
            GameObject NewSmallBiao = Instantiate(SmallBiaoLeft, transform.position, Quaternion.identity);
            NewSmallBiao.GetComponent<Transform>().SetParent(gameObject.transform);
            Destroy(NewSmallBiao, 4f);

        }
    }
    private void FixedPlayAnimation()
    {
        NowTickTime += Time.deltaTime;
        if (NowTickTime >= 7f)
        {
            NowTickTime = 0;//播放动画;
            SmallMonsterAnimator.SetTrigger("PlayRandom");
        }
    }
}
