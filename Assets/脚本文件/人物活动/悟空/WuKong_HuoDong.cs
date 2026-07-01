using System.Collections;
using System.Collections.Generic;
using Unity.Burst.CompilerServices;
using UnityEngine;
using UnityEngine.UI;

public class WuKong_HuoDong : MonoBehaviour
{
    private Animator Wukong_Animator;
    private Rigidbody2D myRigid;
    public float Runspeed;
    public bool IsAnimation_Play_Over = true;
    public float attackDistance = 5f; // 攻击飞行的距离
    public float speed = 10f; // 飞行速度
    public float cooldown = 1f; // 普攻冷却时间
    private float cooldownTime = 0f;
    private float AnimationPlay_CoolTime=0f;
    public float AnimationPlay_TotakTime=0.38f;//用于外部修改时间,普攻动画时长
    public float Front_Time;
    public GameObject Wukong_Wenzi;
    public GameObject AttackParticle;
    public bool IsAttack = false;
    public int AttackAmount;
    public float ParticleMover;
    public CapsuleCollider2D Wukong_Trigger;
    public Image AttackCoolImage;
    public Image SpaceCoolImage;
    private AudioSource ThisAudioSource;
    private CharacterAudio AudioEffects;
    private List<GameObject> Enemies;
    private void Start()
    {
        Wukong_Animator = GetComponent<Animator>();
        myRigid = GetComponent<Rigidbody2D>();
        AttackCoolImage = GameObject.Find("AttackHuan").GetComponent<Image>();
        SpaceCoolImage = GameObject.Find("SpaceHuan").GetComponent<Image>();//查找缓存图标（黑色的）
        ThisAudioSource = gameObject.GetComponent<AudioSource>();
        AudioEffects = gameObject.GetComponent<CharacterAudio>();
        Enemies = new List<GameObject>();
    }
    private void Update()
    {
        Attack();
        Run();
        Attack_Timer();
        IsAnimation_Play_Timer();
        HuanCun();
    }
    private void Attack()
    {
        if (Input.GetMouseButtonDown(0) && cooldownTime <=0&& IsAnimation_Play_Over)
        {
            GetComponent<HideIn_Bush>().Show_Character(1f);
            AnimationPlay_CoolTime = AnimationPlay_TotakTime;
            IsAnimation_Play_Over = false;
            cooldownTime = cooldown;
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = transform.position.z;
            Vector2 direction = (mousePos - transform.position).normalized;
            StartCoroutine(AttackRoutine(direction));
        }
    }
    private void HuanCun()
    {
        if(cooldownTime <= 0) AttackCoolImage.fillAmount = 0f;
        else AttackCoolImage.fillAmount = cooldownTime / cooldown;
    }
    void IsAnimation_Play_Timer()
    {
        if (IsAnimation_Play_Over == false&&AnimationPlay_CoolTime>0)
        {
            AnimationPlay_CoolTime -= Time.deltaTime;
        }
        if (AnimationPlay_CoolTime <= 0 && IsAnimation_Play_Over == false)
        {
            AnimationPlay_CoolTime = 0f;
            IsAnimation_Play_Over = true;
        }
    }
    private void Attack_Timer()
    {
        if (cooldownTime > 0f)
        {
            cooldownTime -= Time.deltaTime;
        }
        if (cooldownTime <= 0)
        {
            cooldownTime = 0f;
        } 
    }
    public void Run()//走路脚本
    {

        float movedirx = Input.GetAxis("Horizontal");
        float movediry = Input.GetAxis("Vertical");
        Vector2 playervel = new Vector2(movedirx * Runspeed, movediry * Runspeed);
        myRigid.velocity = playervel;

    }
    IEnumerator AttackRoutine(Vector2 direction)
    {
        Wukong_Animator.Play("Wukong_Cemian_Attack");
        yield return new WaitForSeconds(Front_Time);
        IsAttack = true;
        Wukong_Trigger.enabled = true;
        Vector2 startPosition = transform.position;
        Vector2 targetPosition = startPosition + direction * attackDistance;
        float distanceTravelled = 0f;
        while (distanceTravelled < attackDistance)
        {
            float step = speed * Time.deltaTime;
            transform.position = Vector2.Lerp(startPosition, targetPosition, distanceTravelled / attackDistance);
            distanceTravelled += step;
            yield return null;
        }
        IsAttack = false;
        Enemies.Clear();
        Wukong_Trigger.enabled = false;
        // 停止在目标位置
        transform.position = targetPosition;
    }
    void ResetCooldown()
    {
        // 刷新普攻冷却时间
        cooldownTime = 0f; // 你可以设定冷却时间
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (IsAttack)
        {
            if ((collision.CompareTag("SmallMonster") || collision.CompareTag("Box") || collision.CompareTag("SmallBox"))&&!Enemies.Contains(collision.gameObject))
            {
                ResetCooldown();
                TakeDamage(collision.gameObject);
                Enemies.Add(collision.gameObject);
            }
        }
    }
    private void TakeDamage(GameObject TargetGameobject)
    {
        ThisAudioSource.PlayOneShot(AudioEffects.HitAudio);
        if (TargetGameobject.tag == "Box")
        {
            BoxTrigger BoxScript = TargetGameobject.GetComponent<BoxTrigger>();
            int FormerHealth = BoxScript.HealthMax;
            Vector3 MislePosition = gameObject.transform.position;
            Instantiate(Wukong_Wenzi, MislePosition, Quaternion.identity);
            Vector2 ParticleRandom = Random.insideUnitCircle * ParticleMover;
            Vector2 Dimension2 = new Vector2(transform.position.x, transform.position.y);
            Instantiate(AttackParticle, Dimension2 + ParticleRandom, Quaternion.identity);
            BoxScript.HealthMax -= AttackAmount;
            int NowHealth = BoxScript.HealthMax;
            BoxScript.AnimaPlaySystem(FormerHealth, NowHealth);

        }
        else if (TargetGameobject.tag == "SmallBox")
        {
            TargetGameobject.GetComponent<Animator>().SetTrigger("Break");
            TargetGameobject.GetComponent<SmallBox>().Healthbar.fillAmount = 0;
            TargetGameobject.GetComponent<CircleCollider2D>().enabled = false;
            Vector3 FeibiaodPosition = gameObject.transform.position;
            Instantiate(Wukong_Wenzi, FeibiaodPosition, Quaternion.identity);
            Vector2 ParticleRandom = Random.insideUnitCircle * ParticleMover;
            Vector2 Dimension2D = new Vector2(transform.position.x, transform.position.y);
            Instantiate(AttackParticle, Dimension2D + ParticleRandom, Quaternion.identity);
        }
        else if (TargetGameobject.tag == "SmallMonster")
        {
            TargetGameobject.GetComponent<Animator>().SetTrigger("BeAttack");
            Vector3 MislePosition = gameObject.transform.position;
            Instantiate(Wukong_Wenzi, MislePosition, Quaternion.identity);
            Vector2 ParticleRandom = Random.insideUnitCircle * ParticleMover;
            Vector2 Dimension2 = new Vector2(transform.position.x, transform.position.y);
            Instantiate(AttackParticle, Dimension2 + ParticleRandom, Quaternion.identity);
            TargetGameobject.GetComponent<SmallMonster>().HealthMax -= AttackAmount;
        }

    }
}
