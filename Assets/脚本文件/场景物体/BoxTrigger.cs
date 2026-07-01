using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
public class BoxTrigger : MonoBehaviour
{

    public GameObject[] Coins;
    public float MoveR;
    private Animator BoxAnimator;
    public int HealthMax = 900;
    public int Numbers;
    public GameObject AttackParticle;//测试在箱子上面能否实现
    public GameObject YileWenZi;
    public float ParticleMover;
    private bool Can = true;
    private Animation ThisAnim;
    public Image HealthBar;
    private float Fill;
    private AudioSource ThisAudioSource;
    // Start is called before the first frame update
    void Start()
    {
        ThisAnim = gameObject.GetComponent<Animation>();
        BoxAnimator = gameObject.GetComponent<Animator>();
        ThisAudioSource = gameObject.GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
        Fill = HealthMax / 900f;
        HealthBar.fillAmount = Fill;
        if (Can == true)
        {
            if (HealthMax <= 0)
            {
                Can = false;
                gameObject.GetComponent<CircleCollider2D>().enabled = false;
                for (int i = 0; i < Numbers; i++)
                {
                    GameObject GoldenCoin = Instantiate(Coins[Random.Range(0, Coins.Length)], transform.position, Quaternion.identity);
                    Vector2 RandomCircle = Random.insideUnitCircle * MoveR;
                    Vector2 Dimension2 = new Vector2(transform.position.x, transform.position.y);
                    GoldenCoin.GetComponent<Transform>().DOLocalMove(Dimension2 + RandomCircle, 0.5f);
                }
                BoxAnimator.SetTrigger("Break");
                Destroy(gameObject,ThisAnim.clip.length);
            }
        }
    }

    

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag  ( "feibiao"))
        {
            int FormerHealth = HealthMax;
            Vector3 FeibiaodPosition = collision.gameObject.transform.position;
            Instantiate(YileWenZi, FeibiaodPosition , Quaternion.identity);
            Vector2 ParticleRandom = Random.insideUnitCircle * ParticleMover ;
            Vector2 Dimension2 = new Vector2(transform.position.x, transform.position.y);
            Instantiate(AttackParticle, Dimension2 +ParticleRandom , Quaternion.identity);
            HealthMax -= 200;
            ThisAudioSource.PlayOneShot(ThisAudioSource.clip);
            int NowHealth = HealthMax;
            AnimaPlaySystem(FormerHealth,NowHealth);
        }
        if (collision.CompareTag("SmallBiao"))
        {
            int FormerHealth = HealthMax;
            HealthMax -= 50;
            ThisAudioSource.PlayOneShot(ThisAudioSource.clip);
            int NowHealth = HealthMax;
            Vector3 FeibiaodPosition = collision.gameObject.transform.position;
            Instantiate(YileWenZi, FeibiaodPosition, Quaternion.identity);
            Vector2 ParticleRandom = Random.insideUnitCircle * ParticleMover;
            Vector2 Dimension2 = new Vector2(transform.position.x, transform.position.y);
            Instantiate(AttackParticle, Dimension2 + ParticleRandom, Quaternion.identity);
            AnimaPlaySystem(FormerHealth, NowHealth);
            Destroy(collision.gameObject);

        }
    }
    public void AnimaPlaySystem(int FormerHealth,int NowHealth)
    {
        if (NowHealth >=450)
        {
            BoxAnimator.SetTrigger("FullBeAttack");
        }
        if (FormerHealth >= 450 && NowHealth < 450)
        {
            BoxAnimator.SetTrigger("FulltoHalf");
        }
        if (FormerHealth < 450&&NowHealth>0)
        {
            BoxAnimator.SetTrigger("HalfBeAttack");
        }
    }
}
