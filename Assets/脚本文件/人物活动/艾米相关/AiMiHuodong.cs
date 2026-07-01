using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class AiMiHuodong : MonoBehaviour
{
    private Rigidbody2D myRigid;
    public float speed;
    private Animator AimiAnimator;
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
    private PlayerInfo AimiInfo;
    private float BianXiongTime = 0;
    public GameObject LightRing;
    public GameObject AttackFlyObj;
    public int AttackFlySpeed = 12;
    public GameObject Light_Column;
    public float Rotate_factor;
    void Start()
    {
        TargetLeveupAndCoin = gameObject.GetComponent<CharacterLevelSystem>();
        ThisAudioSource = gameObject.GetComponent<AudioSource>();
        AudioEffects = gameObject.GetComponent<CharacterAudio>();
        AimiInfo = gameObject.GetComponent<PlayerInfo>();
        myRigid = GetComponent<Rigidbody2D>();
        AimiAnimator = gameObject.GetComponent<Animator>();
        AttackCoolImage = GameObject.Find("AttackHuan").GetComponent<Image>();
        SpaceCoolImage = GameObject.Find("SpaceHuan").GetComponent<Image>();//查找缓存图标（黑色的）
    }
    void Update()
    {
        StateChange();
        ImageHuanCun();
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
        if (NowSpaceCool >= SpaceCool) { CanSpace = true; NowSpaceCool = 0f; AimiAnimator.SetBool("IsFinished", false); }
    }
    private void Attack()//艾米普攻
    {
        if (Input.GetMouseButtonDown(0) && CanAttack == true)
        {
            CanAttack = false;
            AimiAnimator.SetTrigger("Attack");
            Vector3 mouspos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3 direction = mouspos - transform.position;
            direction.z = 0;
            float angle = 360 - Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;
            StartCoroutine(ShowLightRing(angle, direction));

        }
    }
    private void AoyiK()//艾米奥义
    {
        if (Input.GetKeyDown(KeyCode.Space) && CanSpace == true)
        {
            AimiInfo.NowState = CharacterState.AoYing;
            CanSpace = false;
            AimiAnimator.Play("ChangErCemianJinruAoyi");
            Light_Column.SetActive(true);
            Vector3 mouspos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3 direction = mouspos - transform.position;
            direction.z = 0;
            float angle = 360 - Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;
            Light_Column.transform.eulerAngles = new Vector3(0, 0, angle);
            Light_Column.GetComponent<Animator>().Play("AiMi_Lightcolumn_Stand");
            StartCoroutine(CloseLightColumn());
            ThisAudioSource.PlayOneShot(AudioEffects.SpaceAudio);
        }
        
    }

    private void StateChange()
    {
        if (AimiInfo.NowState == CharacterState.Normal)
        {
            CoolHuanChong();
            Run();
            Attack();
            AoyiK();
        }
        if (AimiInfo.NowState == CharacterState.BianXiong)
        {
            CoolHuanChong();
            Run();
            BianXiongTime += Time.deltaTime;
            if (BianXiongTime >= 3.0f)
            {
                BianXiongTime = 0;
                gameObject.GetComponent<Animator>().runtimeAnimatorController = AimiInfo.ThisPlayerRunAnimator;
                AimiInfo.NowSpeed = AimiInfo.OriginalSpeed;
                AimiInfo.NowState = CharacterState.Normal;
            }
        }
        if (AimiInfo.NowState == CharacterState.AoYing)
        {
            CoolHuanChong();
            Run();
            if (Light_Column.activeSelf) 
            {
                Vector3 mouspos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                Vector3 direction = mouspos - transform.position;
                direction.z = 0;
                float angle = 360 - Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;
                Light_Column.transform.position = gameObject.transform.position + direction.normalized * 0.3f;
                Rotate_LightColumn(angle);
            }
            if (NowSpaceCool >= 4f) {AimiAnimator.SetBool("IsFinished",true); AimiInfo.NowState = CharacterState.Normal; }
        }
    }
    private void Rotate_LightColumn(float angle)
    {
        // 当前旋转角度
        float currentAngle = Light_Column.transform.eulerAngles.z;

        // 目标旋转角度
        float targetAngle = angle;
        // 插值计算，逐步旋转
        float newAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, Time.deltaTime*Rotate_factor);

        // 设置新的旋转角度
        Light_Column.transform.eulerAngles = new Vector3(0, 0, newAngle);
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
    IEnumerator ShowLightRing(float angle,Vector3 direction)
    {
        yield return new WaitForSeconds(0.05f);
        LightRing.transform.position = gameObject.transform.position + direction.normalized * 0.75f;
        LightRing.transform.eulerAngles = new Vector3(0, 0, angle);
        LightRing.SetActive(true);
        StartCoroutine(HideLightRing());
        StartCoroutine(InstantiateAttackFly(angle,direction));
        LightRing.GetComponent<Animator>().Play("AttackRingAnim");
    }
    IEnumerator HideLightRing()
    {
        yield return new WaitForSeconds(0.52f);
        LightRing.GetComponent<Animator>().Play("AttackRingStand");
        LightRing.SetActive(false);

    }
    IEnumerator InstantiateAttackFly(float angle,Vector3 direciton)
    {
        yield return new WaitForSeconds(0.05f);
        GameObject AimiAttack = Instantiate(AttackFlyObj, transform.position + direciton.normalized * 1f, Quaternion.identity);
        AimiAttack.transform.eulerAngles = new Vector3(0, 0, angle);
        AimiAttack.GetComponent<Rigidbody2D>().velocity = direciton.normalized * AttackFlySpeed;
    }
    IEnumerator CloseLightColumn()
    {
        yield return new WaitForSeconds(3.9f);
        Light_Column.SetActive(false);
    }

}
