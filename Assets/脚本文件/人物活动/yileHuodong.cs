using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class yileHuodong : MonoBehaviour
{
    // Start is called before the first frame update
    public int CharacterHealthFullAmount;
    private int CharacterHealthNowAmount;
    public float speed=3.5f;
    private Rigidbody2D myRigid;
    private float Timefeibiaoback;
    bool iscanfeibiao = true;
    public GameObject feibiao;
    public float attackspeed=1.8f;
    private Animator _animator;
    public float aoyileng=2.8f;//奥义冷却时间
    public float nowaoyileng=2.8f;
    public GameObject  feibiaod;
    public GameObject guangx;
    public bool Sbiao=false  ;//用来判断小飞镖能否发射出来
    public Image AttackCoolImage;
    public Image SpaceCoolImage;
    public GameObject AttackParticle;
    private CharacterLevelSystem TargetLeveupAndCoin;
    private bool isAccelerate=false;
    public Image HealthBar;
    private AudioSource ThisAudioSource;
    private CharacterAudio AudioEffects;
    private SFbiao ThisSbiao;



    void Start()
    {
        CharacterHealthNowAmount = CharacterHealthFullAmount;
        myRigid = GetComponent<Rigidbody2D>();
        _animator = gameObject.GetComponent<Animator>();
        TargetLeveupAndCoin = gameObject.GetComponent<CharacterLevelSystem>();
        AttackCoolImage = GameObject.Find("AttackHuan").GetComponent<Image>();
        SpaceCoolImage = GameObject.Find("SpaceHuan").GetComponent<Image>();
        ThisAudioSource = gameObject.GetComponent<AudioSource>();
        AudioEffects = gameObject.GetComponent<CharacterAudio>();
        ThisSbiao = GetComponent<SFbiao>();

    }

    // Update is called once per frame
    void Update()
    {
        HealthBar.fillAmount = CharacterHealthNowAmount / CharacterHealthFullAmount;
        keyAoyi();
        Aoyihuancun();
        if (iscanfeibiao == true)
        { AttackCoolImage.fillAmount = 0; };
        if (isAccelerate)
        {
            Speedup();
        }
        Run();
        if (!iscanfeibiao)
        {
            Timefeibiaoback += Time.deltaTime;
            AttackCoolImage.fillAmount = 1-Timefeibiaoback / attackspeed;
     
            if (Timefeibiaoback >= attackspeed) 
            {
                iscanfeibiao = true;
                
            }
        }
        if( Input.GetMouseButtonDown(0)&&iscanfeibiao)
        {
            iscanfeibiao = false;
            Timefeibiaoback = 0;
            Vector3 worldpos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3 dir = worldpos - transform.position;
            dir.z = 0;
            ThisAudioSource.PlayOneShot(AudioEffects.AttackAudio);
            GameObject go = Instantiate(feibiao, transform.position + dir.normalized * 1f, transform.rotation);
            go.GetComponent<xuanzhuan>().PlayerName = gameObject.name;
            go.GetComponent<Rigidbody2D>().velocity = dir.normalized * 10;
            _animator.SetTrigger("Attack");

            
            


        }
        
    }
    public void Run()//走路脚本
    {
        float movedirx = Input.GetAxis("Horizontal");
        float movediry = Input.GetAxis("Vertical");
        Vector2 playervel = new Vector2(movedirx * speed, movediry * speed);
        myRigid.velocity = playervel;

    }
    private void OnTriggerEnter2D(Collider2D collision)//碰撞本体回收飞镖
    {
        if (collision.CompareTag ( "feibiao")) 
        {
            string Objectname= collision.gameObject.GetComponent<xuanzhuan>().PlayerName;//判断丢出的飞镖是否为自己的，不是的话造成伤害，后续应该先进行标签的判断属于哪一队
            if (Objectname  == gameObject.name)
            {
                Destroy(collision.gameObject);
                if (Timefeibiaoback >= 1.2f)
                { iscanfeibiao = true; };
            }
            else 
            {
                Vector3 OccurPlace = transform.position;
                Instantiate(AttackParticle, OccurPlace, Quaternion.identity);//伤害的闪光粒子特效
            } 
        }
        if (collision.CompareTag("GoldenCoin"))//拾取金币效果
        {
            CoinPickUp(collision.gameObject.tag);
            Destroy(collision.gameObject);
        }
        else if (collision.CompareTag("TongCoin"))
        {
            CoinPickUp(collision.gameObject.tag);
            Destroy(collision.gameObject);
        }
        else if (collision.CompareTag("SliverCoin"))
        {
            CoinPickUp(collision.gameObject.tag);
            Destroy(collision.gameObject);
        }
        else if (collision.CompareTag("SpedUpPickUp"))
        {
            isAccelerate = true;
            Destroy(collision.gameObject);
        }

    }
    private void keyAoyi()//空格按下释放奥义
    {
        if (Input.GetKeyDown(KeyCode.Space) )
        {
            if (nowaoyileng >=aoyileng )
            {
                if (GameObject.FindGameObjectWithTag("feibiao"))
                {
                    feibiaod = GameObject.FindGameObjectWithTag("feibiao");
                    Vector3 Kon_ge = transform.position;
                    ThisAudioSource.PlayOneShot(AudioEffects.SpaceAudio);
                    gameObject . transform.position = feibiaod.transform.position;
                    Sbiao = true;
                    nowaoyileng = 0;
                    GameObject io = Instantiate(guangx, Kon_ge, Quaternion.identity);
                    Animation TX_ani = io .GetComponent<Animation>();
                    TX_ani.Play();
                    Vector3 nowp = transform.position;
                    GameObject ia = Instantiate(guangx, nowp, Quaternion.identity);
                    ThisSbiao.Smallbiao();
                    Animation TX_a = ia .GetComponent<Animation >();
                    TX_a.Play();




                }
                else Debug.Log("无飞镖");



            }
            else Debug.Log("技能正在冷却");
            
            


        }
        
        
    }
    private void Aoyihuancun() //缓存图标以及奥义的冷却
    {
        if (nowaoyileng < aoyileng)
        {
            nowaoyileng += Time.deltaTime;
            SpaceCoolImage.fillAmount = 1 - nowaoyileng/aoyileng ;
            
        }
        

    }
    private void CoinPickUp(string CoinTag) 
    {
        ThisAudioSource.PlayOneShot(AudioEffects.ScorePickAudio);
        if (CoinTag == "GoldenCoin")
        {
            TargetLeveupAndCoin.PlayerScore += 1000;
        }
        if (CoinTag == "TongCoin") TargetLeveupAndCoin.PlayerScore += 10;
        if (CoinTag == "SliverCoin") TargetLeveupAndCoin.PlayerScore += 100;
    }
    private void Speedup()
    {
        float time = 0;
        time += Time.deltaTime;
        speed = 4.0f;
        if (time >= 5.0f) isAccelerate = false;
    }
}   
