using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

public class ZhanWuYan_Activity : MonoBehaviour
{
    private Rigidbody2D myRigid;
    public float speed;
    private Animator ZhanWuYan_Animator;
    private float NowAttackCool = 0f;
    private float NowSpaceCool = 0f;
    public float AttackCool;
    public float SpaceCool;
    private bool CanAttack = true;
    private bool CanSpace = true;
    public Image AttackCoolImage;
    public Image SpaceCoolImage;
    private CharacterLevelSystem TargetLeveupAndCoin;
    private AudioSource ThisAudioSource;
    private CharacterAudio AudioEffects;
    private PlayerInfo ZhanWuYanInfo;
    private float BianXiongTime = 0;
    public GameObject ZhanWuYan_Bomb;
    public GameObject ZhanWuYan_FireField;
    public int BombNumer = 2;
    private bool IsAnimPlayOver = true;
    private float Animation_Huancun=0f;
    void Start()
    {
        TargetLeveupAndCoin = gameObject.GetComponent<CharacterLevelSystem>();
        ThisAudioSource = gameObject.GetComponent<AudioSource>();
        AudioEffects = gameObject.GetComponent<CharacterAudio>();
        ZhanWuYanInfo = gameObject.GetComponent<PlayerInfo>();
        myRigid = GetComponent<Rigidbody2D>();
        ZhanWuYan_Animator = gameObject.GetComponent<Animator>();
        AttackCoolImage = GameObject.Find("AttackHuan").GetComponent<Image>();
        SpaceCoolImage = GameObject.Find("SpaceHuan").GetComponent<Image>();//查找缓存图标（黑色的）
    }
    void Update()
    {
        StateChange();
        ImageHuanCun();
        SetBomb_HuanCun();
    }
    public void Run()//走路脚本
    {
        float movedirx = Input.GetAxis("Horizontal");
        float movediry = Input.GetAxis("Vertical");
        Vector2 playervel = new Vector2(movedirx * speed, movediry * speed);
        myRigid.velocity = playervel;

    }
    private void CoolHuanChong()//冷却缓冲
    {
        if (NowAttackCool >= AttackCool) { CanAttack = true; NowAttackCool = 0f; }
        if (!CanAttack) NowAttackCool += Time.deltaTime;
        if (!CanSpace) NowSpaceCool += Time.deltaTime;
        if (NowSpaceCool >= SpaceCool) { CanSpace = true; NowSpaceCool = 0f;BombNumer = 2; }
    }
    private void Attack()//战无炎普攻
    {
        if (Input.GetMouseButtonDown(0) && CanAttack == true)
        {
            ZhanWuYan_Animator.SetTrigger("Attack");
            CanAttack = false;
            Vector3 WorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3 HeightPos = new Vector3(WorldPos.x, WorldPos.y, 1f);
            StartCoroutine(Instantiate_FireField(HeightPos));
        }
    }
    IEnumerator Instantiate_FireField(Vector3 Fire_Pos)
    {
        yield return new WaitForSeconds(0.14f);
        Instantiate(ZhanWuYan_FireField, Fire_Pos, Quaternion.identity);
    }
    private void AoyiK()//战无炎奥义
    {
        if (Input.GetKeyDown(KeyCode.Space) && CanSpace == true && BombNumer != 0)
        {
            if (IsAnimPlayOver)
            {
                BombNumer -= 1;
                ZhanWuYan_Animator.Play("YuTuCemianAoYi");
                Vector3 WorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                Vector3 HeightPos = new Vector3(WorldPos.x, WorldPos.y, 1f);
                Instantiate(ZhanWuYan_Bomb, HeightPos, Quaternion.identity);
            }
            if(BombNumer==0) CanSpace = false;
        }

    }
    private void StateChange()
    {
        if (ZhanWuYanInfo.NowState == CharacterState.Normal)
        {
            CoolHuanChong();
            Run();
            Attack();
            AoyiK();
        }
        if (ZhanWuYanInfo.NowState == CharacterState.BianXiong)
        {
            CoolHuanChong();
            Run();
            BianXiongTime += Time.deltaTime;
            if (BianXiongTime >= 3.0f)
            {
                BianXiongTime = 0;
                gameObject.GetComponent<Animator>().runtimeAnimatorController = ZhanWuYanInfo.ThisPlayerRunAnimator;
                ZhanWuYanInfo.NowSpeed = ZhanWuYanInfo.OriginalSpeed;
                ZhanWuYanInfo.NowState = CharacterState.Normal;
            }
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
    private void CoinPickUp(string CoinTag)//加分逻辑
    {
        ThisAudioSource.PlayOneShot(AudioEffects.ScorePickAudio);
        if (CoinTag == "GoldenCoin")
        {
            TargetLeveupAndCoin.PlayerScore += 1000;
        }
        if (CoinTag == "TongCoin") TargetLeveupAndCoin.PlayerScore += 10;
        if (CoinTag == "SliverCoin") TargetLeveupAndCoin.PlayerScore += 100;
    }
    private void ImageHuanCun()//图标缓存
    {
        if (CanAttack == true) AttackCoolImage.fillAmount = 0f;
        else AttackCoolImage.fillAmount = (AttackCool - NowAttackCool) / AttackCool;
        if (CanSpace == true) SpaceCoolImage.fillAmount = 0f;
        else SpaceCoolImage.fillAmount = (SpaceCool - NowSpaceCool) / SpaceCool;
    }
    private void SetBomb_HuanCun()
    {
        if (IsAnimPlayOver == false) Animation_Huancun += Time.deltaTime;
        if (Animation_Huancun >= 0.35f) { IsAnimPlayOver = true;Animation_Huancun = 0f; }
    }
}
