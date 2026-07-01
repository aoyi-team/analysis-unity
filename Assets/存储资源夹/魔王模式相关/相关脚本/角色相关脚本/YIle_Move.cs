using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class YIle_Move : MonoBehaviour
{
    public float speed = 3.5f;
    private Rigidbody2D myRigid;
    private float Timefeibiaoback;
    bool iscanfeibiao = true;
    public GameObject feibiao;
    private Animator _animator;
    public float aoyileng = 2.8f;//奥义冷却时间
    public float nowaoyileng = 2.8f;
    public GameObject feibiaod;
    public GameObject guangx;
    public GameObject AttackParticle;
    public float attackspeed = 1.8f;
    private SFbiao ThisSbiao;
    void Start()
    {
        myRigid = GetComponent<Rigidbody2D>();
        _animator = gameObject.GetComponent<Animator>();
        ThisSbiao = GetComponent<SFbiao>();
    }
    public void Run()//走路脚本
    {
        float movedirx = Input.GetAxis("Horizontal");
        float movediry = Input.GetAxis("Vertical");
        Vector2 playervel = new Vector2(movedirx * speed, movediry * speed);
        myRigid.velocity = playervel;

    }
    private void Update()
    {
        Run();
        Aoyihuancun();
        Feibiao_Huan();
        keyAoyi();
        Attack();
        
    }
    private void OnTriggerEnter2D(Collider2D collision)//碰撞本体回收飞镖
    {
        if (collision.CompareTag("feibiao"))
        {
            string Objectname = collision.gameObject.GetComponent<xuanzhuan>().PlayerName;//判断丢出的飞镖是否为自己的，不是的话造成伤害，后续应该先进行标签的判断属于哪一队
            if (Objectname == gameObject.name)
            {
                if (collision.gameObject.GetComponent<xuanzhuan>().first == false)
                {
                    Destroy(collision.gameObject);
                    if (Timefeibiaoback >= 1.2f)
                    { iscanfeibiao = true; };
                }
            }
            else
            {
                Vector3 OccurPlace = transform.position;
                Instantiate(AttackParticle, OccurPlace, Quaternion.identity);//伤害的闪光粒子特效
            }
        }

    }
    private void keyAoyi()//空格按下释放奥义
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (nowaoyileng >= aoyileng)
            {
                if (GameObject.FindGameObjectWithTag("feibiao"))
                {
                    feibiaod = GameObject.FindGameObjectWithTag("feibiao");
                    Vector3 Kon_ge = transform.position;
                    gameObject.transform.position = feibiaod.transform.position;
                    nowaoyileng = 0;
                    GameObject io = Instantiate(guangx, Kon_ge, Quaternion.identity);
                    Animation TX_ani = io.GetComponent<Animation>();
                    TX_ani.Play();
                    Vector3 nowp = transform.position;
                    GameObject ia = Instantiate(guangx, nowp, Quaternion.identity);
                    ThisSbiao.Smallbiao();
                    Animation TX_a = ia.GetComponent<Animation>();
                    TX_a.Play();
                }
            }
        }
    }
    private void Aoyihuancun() //缓存图标以及奥义的冷却
    {
        if (nowaoyileng < aoyileng)
        {
            nowaoyileng += Time.deltaTime;
        }
    }
    private void Attack()
    {
        if (Input.GetMouseButtonDown(0) && iscanfeibiao)
        {
            iscanfeibiao = false;
            Timefeibiaoback = 0;
            Vector3 worldpos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3 dir = worldpos - transform.position;
            dir.z = 0;
            GameObject go = Instantiate(feibiao, transform.position + dir.normalized * 1f, transform.rotation);
            go.GetComponent<xuanzhuan>().PlayerName = gameObject.name;
            go.GetComponent<Rigidbody2D>().velocity = dir.normalized * 10;
            _animator.SetTrigger("Attack");
        }
    }
    private void Feibiao_Huan()
    {
        if (!iscanfeibiao)
        {
            Timefeibiaoback += Time.deltaTime;

            if (Timefeibiaoback >= attackspeed)
            {
                iscanfeibiao = true;
            }
        }
    }
}
