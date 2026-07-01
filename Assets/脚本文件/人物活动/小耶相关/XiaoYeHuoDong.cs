using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class XiaoYeHuoDong : MonoBehaviour
{
    private float NowAttackCool = 0f;
    private float NowSpaceCool = 0f;
    public float AttackCool = 0.65f;
    public float SpaceCool = 3.5f;
    private Rigidbody2D myRigid;
    public float speed ;
    private bool CanAttack = true;
    private bool CanSpace = true;
    public Image AttackCoolImage;
    public Image SpaceCoolImage;
    private CharacterLevelSystem TargetLeveupAndCoin;
    private AudioSource ThisAudioSource;
    private CharacterAudio AudioEffects;
    private Animator XiaoYeAnimator;
    public GameObject Aixin;
    public int AttackFlySpeed = 12;
    public GameObject MofaZhen;
    private PlayerInfo XiaoyeInfo;
    private float BianXiongTime=0;
    void Start()
    {
        XiaoyeInfo = gameObject.GetComponent<PlayerInfo>();
        myRigid = GetComponent<Rigidbody2D>();
        XiaoYeAnimator = gameObject.GetComponent<Animator>();
        AttackCoolImage = GameObject.Find("AttackHuan").GetComponent<Image>();
        SpaceCoolImage = GameObject.Find("SpaceHuan").GetComponent<Image>();//查找缓存图标（黑色的）
        TargetLeveupAndCoin = gameObject.GetComponent<CharacterLevelSystem>();
        ThisAudioSource = gameObject.GetComponent<AudioSource>();
        AudioEffects = gameObject.GetComponent<CharacterAudio>();
        XiaoyeInfo.Team = LayerMask.LayerToName(gameObject.layer);
    }

    void Update()
    {
        speed = XiaoyeInfo.NowSpeed;
        StateChange();
        if (CanAttack == true) AttackCoolImage.fillAmount = 0f;
        else AttackCoolImage.fillAmount = (0.5f - NowAttackCool) / 0.5f;
        if (CanSpace == true) SpaceCoolImage.fillAmount = 0f;
        else SpaceCoolImage.fillAmount = (3.5f - NowSpaceCool) / 3.5f;
    }
    private void Attack()//小耶普攻
    {
        if (!CanAttack) NowAttackCool += Time.deltaTime;
        if (NowAttackCool >= AttackCool) { CanAttack = true; NowAttackCool = 0f; }
        if (Input.GetMouseButtonDown(0) && CanAttack == true)
        {
            CanAttack = false;
            XiaoYeAnimator.SetTrigger("Attack");
            ThisAudioSource.PlayOneShot(AudioEffects.AttackAudio);
            Vector3 mouspos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3 direction = mouspos - transform.position;
            direction.z = 0;
            float angle = 360 - Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;
            GameObject XiaoYeAixin = Instantiate(Aixin, transform.position + direction.normalized  * 1f, Quaternion.identity);
            XiaoYeAixin.transform.eulerAngles = new Vector3(0, 0, angle);
            XiaoYeAixin.GetComponent<Rigidbody2D>().velocity = direction.normalized * AttackFlySpeed;
        }
    }
    private void AoyiK()//小耶奥义
    {
        if (!CanSpace) NowSpaceCool += Time.deltaTime;
        if (NowSpaceCool >= SpaceCool) { CanSpace = true; NowSpaceCool = 0f; }
        if (Input.GetKeyDown(KeyCode.Space) && CanSpace == true)
        {
            CanSpace = false;
            if (XiaoYeAnimator.GetBool("isAngle") == false) XiaoYeAnimator.Play("XiaoYeBackAoyi");
            else XiaoYeAnimator.Play("XiaoYeCemianAoyi");
            Vector3 WorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3 HeightPos = new Vector3(WorldPos.x, WorldPos.y, 1f);
            ThisAudioSource.PlayOneShot(AudioEffects.SpaceAudio);
            GameObject NewMofazhen= Instantiate(MofaZhen, HeightPos, Quaternion.identity);
            NewMofazhen.GetComponent<XiaoYeMoFaZhen>().TeamLabel = XiaoyeInfo.Team;
        }
    }
    public void Run()//走路脚本
    {
        float movedirx = Input.GetAxis("Horizontal");
        float movediry = Input.GetAxis("Vertical");
        Vector2 playervel = new Vector2(movedirx * speed, movediry * speed);
        myRigid.velocity = playervel;

    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
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
            Destroy(collision.gameObject);
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
    private void StateChange()
    {
        if (XiaoyeInfo.NowState == CharacterState.Normal)
        {
            Run();
            Attack();
            AoyiK();
        }
        if (XiaoyeInfo.NowState == CharacterState.BianXiong)
        {
            Run();
            BianXiongTime += Time.deltaTime;
            if (BianXiongTime >= 3.0f)
            {
                BianXiongTime = 0;
                gameObject.GetComponent<Animator>().runtimeAnimatorController = XiaoyeInfo.ThisPlayerRunAnimator;
                XiaoyeInfo.NowState = CharacterState.Normal;
                XiaoyeInfo.NowSpeed= XiaoyeInfo.OriginalSpeed;
            }
        }
    }
}
