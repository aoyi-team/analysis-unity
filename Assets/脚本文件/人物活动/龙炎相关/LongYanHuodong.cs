using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class LongYanHuodong : MonoBehaviour
{
    public GameObject LongYanAttackYinji;
    private Rigidbody2D myRigid;
    public float speed;
    private Animator LongYanAnimator;
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
    private PlayerInfo LongYanInfo;
    private float BianXiongTime = 0;
    public float DashSpeed;
    public float DashDurationTime;
    private float DashTime = 0;
    private Vector3 DashDirection;
    void Start()
    {
        LongYanInfo = gameObject.GetComponent<PlayerInfo>();
        myRigid = GetComponent<Rigidbody2D>();
        LongYanAnimator = gameObject.GetComponent<Animator>();
        AttackCoolImage = GameObject.Find("AttackHuan").GetComponent<Image>();
        SpaceCoolImage = GameObject.Find("SpaceHuan").GetComponent<Image>();//查找缓存图标（黑色的）
        TargetLeveupAndCoin = gameObject.GetComponent<CharacterLevelSystem>();
        ThisAudioSource = gameObject.GetComponent<AudioSource>();
        AudioEffects = gameObject.GetComponent<CharacterAudio>();
        LongYanInfo.Team = LayerMask.LayerToName(gameObject.layer);
    }
    void Update()
    {
        if (!CanAttack) NowAttackCool += Time.deltaTime;
        if (NowAttackCool >= AttackCool) { CanAttack = true; NowAttackCool = 0f; }
        speed = LongYanInfo.NowSpeed;
        StateChange();
        if (CanAttack == true) AttackCoolImage.fillAmount = 0f;
        else AttackCoolImage.fillAmount = (AttackCool - NowAttackCool) / AttackCool;
        if (CanSpace == true) SpaceCoolImage.fillAmount = 0f;
        else SpaceCoolImage.fillAmount = (SpaceCool - NowSpaceCool) / SpaceCool;
    }
    public void Run()//走路脚本
    {
        float movedirx = Input.GetAxis("Horizontal");
        float movediry = Input.GetAxis("Vertical");
        Vector2 playervel = new Vector2(movedirx * speed, movediry * speed);
        myRigid.velocity = playervel;

    }
    private void Aoyi()//龙炎奥义
    {
        if (!CanSpace) NowSpaceCool += Time.deltaTime;
        if (NowSpaceCool >= SpaceCool) { CanSpace = true; NowSpaceCool = 0f; }
        if (Input.GetKeyDown(KeyCode.Space) && CanSpace == true)
        {
            gameObject.GetComponent<CircleCollider2D>().enabled = false;
            CanSpace = false;
            gameObject.GetComponent<CircleCollider2D>().enabled = true;
            if (LongYanAnimator.GetBool("isAngle") == false) LongYanAnimator.Play("LongYanBackAoyi");
            else LongYanAnimator.Play("LongYanCemianAoyi");
            Dash();
            StartCoroutine(StopDash());
        }
    }
    private void Attack()//龙炎普攻
    {
        if (Input.GetMouseButtonDown(0) && CanAttack == true)
        {
            CanAttack = false;
            LongYanAnimator.SetTrigger("Attack");
            ThisAudioSource.PlayOneShot(AudioEffects.AttackAudio);
            StartCoroutine(InstantiateAttack());
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
        if (LongYanInfo.NowState == CharacterState.Dashing)
        {
            if (collision.CompareTag("SmallMonster") || collision.CompareTag("Characters")) 
            {
                collision.gameObject.GetComponent<PlayerInfo>().NowState = CharacterState.BePushed;
                collision.gameObject.GetComponent<Rigidbody2D>().velocity= DashDirection.normalized * DashSpeed;
                collision.gameObject.GetComponent<PlayerInfo>().DashLeftTime = DashDurationTime - DashTime;
                collision.gameObject.GetComponent<PlayerInfo>().StartStopBePushed = true;
                collision.gameObject.GetComponent<PlayerInfo>().WhoDashLayer = gameObject.layer;
            }
             
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
    private void StateChange()//龙炎状态切换,通过不同状态实现某种状态可以控制某个部分
    {
        if (LongYanInfo.NowState == CharacterState.Normal)
        {
            Run();
            Attack();
            Aoyi();
        }
        if (LongYanInfo.NowState == CharacterState.BianXiong)
        {
            Run();
            BianXiongTime += Time.deltaTime;
            if (BianXiongTime >= 3.0f)
            {
                BianXiongTime = 0;
                gameObject.GetComponent<Animator>().runtimeAnimatorController = LongYanInfo.ThisPlayerRunAnimator;
                LongYanInfo.NowSpeed = LongYanInfo.OriginalSpeed;
                LongYanInfo.NowState = CharacterState.Normal;
            }
        }
        if (LongYanInfo.NowState == CharacterState.Dashing)
        {
            DashTime += Time.deltaTime;
        }
    }
    IEnumerator InstantiateAttack()
    {
        yield return new WaitForSeconds(0.43f);
        Vector3 mouspos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3 direction = mouspos - transform.position;
        direction.z = 0;
        float angle = 360 - Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;
        GameObject LongYanAttack = Instantiate(LongYanAttackYinji, transform.position + direction.normalized * 1f, Quaternion.identity);
        LongYanAttack.transform.eulerAngles = new Vector3(0, 0, angle); 
    }
    IEnumerator StopDash()
    {
        yield return new WaitForSeconds(DashDurationTime);
        DashTime = 0;
        myRigid.velocity = Vector2.zero;
        LongYanInfo.NowState = CharacterState.Normal;
    }
    private void Dash()
    {
        LongYanInfo.NowState = CharacterState.Dashing;
        ThisAudioSource.PlayOneShot(AudioEffects.SpaceAudio);
        Vector3 MousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        MousePosition.z = 0;
        DashDirection = MousePosition - transform.position;
        myRigid.velocity = DashDirection.normalized * DashSpeed;
    }
}
