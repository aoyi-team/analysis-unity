using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NuoYaHuoDong: MonoBehaviour
{
    private Rigidbody2D myRigid;
    public float speed;
    public GameObject Misile;
    private Animator NuoyaAnimator;
    public GameObject AoyiKuang;
    private float NowAttackCool = 0f;
    private float NowSpaceCool = 0f;
    public float AttackCool = 0.5f;
    public float SpaceCool = 3.5f;
    private bool CanAttack = true;
    private bool CanSpace = true;
    public Image AttackCoolImage;
    public Image SpaceCoolImage;
    private CharacterLevelSystem TargetLeveupAndCoin;
    private AudioSource ThisAudioSource;
    private CharacterAudio AudioEffects;
    private PlayerInfo NuoyaInfo;
    private float BianXiongTime = 0;
    void Start()
    {
        NuoyaInfo = gameObject.GetComponent<PlayerInfo>();
        myRigid = GetComponent<Rigidbody2D>();
        NuoyaAnimator = gameObject.GetComponent<Animator>();
        AttackCoolImage = GameObject.Find("AttackHuan").GetComponent<Image>();
        SpaceCoolImage = GameObject.Find("SpaceHuan").GetComponent<Image>();//查找缓存图标（黑色的）
        TargetLeveupAndCoin = gameObject.GetComponent<CharacterLevelSystem>();
        ThisAudioSource = gameObject.GetComponent<AudioSource>();
        AudioEffects = gameObject.GetComponent<CharacterAudio>();
        NuoyaInfo.Team = LayerMask.LayerToName(gameObject.layer);
    }
    void Update()
    {
        speed = NuoyaInfo.NowSpeed;
        StateChange();
        if (CanAttack == true) AttackCoolImage.fillAmount = 0f;
        else AttackCoolImage.fillAmount = (0.5f - NowAttackCool) / 0.5f;
        if (CanSpace == true) SpaceCoolImage.fillAmount = 0f;
        else SpaceCoolImage.fillAmount = (3.5f - NowSpaceCool) / 3.5f;
    }
    public void Run()//走路脚本
    {
        float movedirx = Input.GetAxis("Horizontal");
        float movediry = Input.GetAxis("Vertical");
        Vector2 playervel = new Vector2(movedirx * speed, movediry * speed);
        myRigid.velocity = playervel;

    }
    private void Attack()//诺亚普攻
    {
        if (!CanAttack) NowAttackCool += Time.deltaTime;
        if (NowAttackCool >= AttackCool) { CanAttack = true;NowAttackCool = 0f; }
        if (Input.GetMouseButtonDown(0)&&CanAttack==true)
        {
            CanAttack = false;
            NuoyaAnimator.SetTrigger("Attack");
            Vector3 WorldPos= Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3 HeightPos = new Vector3(WorldPos.x, WorldPos.y, 1f);
            ThisAudioSource.PlayOneShot(AudioEffects.AttackAudio);
            Instantiate(Misile, HeightPos, Quaternion.identity);
        }
    }
    private void AoyiK()//诺亚奥义
    {
        if (!CanSpace) NowSpaceCool += Time.deltaTime;
        if (NowSpaceCool >= SpaceCool) { CanSpace = true;NowSpaceCool = 0f; }
        if (Input.GetKeyDown(KeyCode.Space) && CanSpace == true)
        {
            CanSpace = false;
            if (NuoyaAnimator.GetBool("isAngle") == false) NuoyaAnimator.Play("NuoyaBackAoyi");
            else NuoyaAnimator.Play("NuoyaCemianAoyi");
            Vector3 WorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3 HeightPos = new Vector3(WorldPos.x, WorldPos.y, 1f);
            ThisAudioSource.PlayOneShot(AudioEffects.SpaceAudio);
            Instantiate (AoyiKuang, HeightPos, Quaternion.identity);
        }
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
        if (NuoyaInfo.NowState == CharacterState.Normal)
        {
            Run();
            Attack();
            AoyiK();
        }
        if (NuoyaInfo.NowState == CharacterState.BianXiong)
        {
            Run();
            BianXiongTime += Time.deltaTime;
            if (BianXiongTime >= 3.0f)
            {
                BianXiongTime = 0;
                gameObject.GetComponent<Animator>().runtimeAnimatorController = NuoyaInfo.ThisPlayerRunAnimator;
                NuoyaInfo.NowSpeed=NuoyaInfo.OriginalSpeed;
                NuoyaInfo.NowState = CharacterState.Normal;
            }
        }
    }
}
