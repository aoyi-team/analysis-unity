using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class SmallBox : MonoBehaviour
{
    public float MoveR;
    public GameObject[] PickUps;
    public Image Healthbar;
    private Animation BreakAnim;
    public int PickUpNumbers;
    public GameObject AttackParticle;
    public GameObject YileWenZi;
    public float ParticleMover;
    private Animator ThisAnimator;
    private bool isfirtBreak = false;
    private AudioSource ThisAudioSource;
    void Start()
    {
        BreakAnim = gameObject.GetComponent<Animation>();
        ThisAnimator = gameObject.GetComponent<Animator>();
        ThisAudioSource = gameObject.GetComponent<AudioSource>();
    }
    private void Update()
    {
        BreakBOX();


    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("feibiao") || collision.CompareTag("SmallBiao"))
        {
            ThisAudioSource.PlayOneShot(ThisAudioSource.clip);
            ThisAnimator.SetTrigger("Break");
            Healthbar.fillAmount = 0;
            gameObject.GetComponent<CircleCollider2D>().enabled = false;
            Vector3 FeibiaodPosition = collision.gameObject.transform.position;
            Instantiate(YileWenZi, FeibiaodPosition, Quaternion.identity);
            Vector2 ParticleRandom = Random.insideUnitCircle * ParticleMover;
            Vector2 Dimension2D = new Vector2(transform.position.x, transform.position.y);
            Instantiate(AttackParticle, Dimension2D + ParticleRandom, Quaternion.identity);
            if (collision.CompareTag("SmallBiao")) Destroy(collision.gameObject);
        }
    }
    private void BreakBOX()
    {
        if (isfirtBreak == false)
        {
            if (Healthbar.fillAmount == 0)
            {
                isfirtBreak = true;
                for (int i = 0; i < PickUpNumbers; i++)
                {
                    GameObject GoldenCoin = Instantiate(PickUps[Random.Range(0, PickUps.Length)], transform.position, Quaternion.identity);
                    Vector2 RandomCircle = Random.insideUnitCircle * MoveR;
                    Vector2 Dimension2 = new Vector2(transform.position.x, transform.position.y);
                    GoldenCoin.GetComponent<Transform>().DOLocalMove(Dimension2 + RandomCircle, 0.5f);
                    Destroy(gameObject, BreakAnim.clip.length);
                }
            }
        }
    }
}
