using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Dishitian_Move : MonoBehaviour
{
    private Rigidbody2D myRigid;
    public float speed;
    private Animator DiShiTianAnimator;
    private float NowAttackCool = 0f;
    private float NowSpaceCool = 0f;
    public float AttackCool;
    public float SpaceCool;
    private bool CanAttack = true;
    private bool CanSpace = true;
    private PlayerInfo DiShiTianInfo;
    private float BianXiongTime = 0;
    public GameObject[] DifferentBow;
    public GameObject Arrow;
    public int AttackFlySpeed = 12;
    public Transform RotationPivot;
    public float Distance;
    void Start()
    {
        DiShiTianInfo = gameObject.GetComponent<PlayerInfo>();
        myRigid = GetComponent<Rigidbody2D>();
        DiShiTianAnimator = gameObject.GetComponent<Animator>();
    }
    void Update()
    {
        StateChange();
    }
    public void Run()//走路脚本
    {
        float movedirx = Input.GetAxis("Horizontal");
        float movediry = Input.GetAxis("Vertical");
        Vector2 playervel = new Vector2(movedirx * speed, movediry * speed);
        myRigid.velocity = playervel;

    }
    private void Attack()//帝释天普攻
    {
        if (NowAttackCool >= AttackCool) { CanAttack = true; NowAttackCool = 0f; }
        if (!CanAttack) NowAttackCool += Time.deltaTime;
        if (Input.GetMouseButtonDown(0) && CanAttack == true)
        {
            CanAttack = false;
            DiShiTianAnimator.SetTrigger("Attack");
            Vector3 mouspos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3 direction = mouspos - transform.position;
            direction.z = 0;
            float angle = 360 - Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;
            BowShow(angle);
            StartCoroutine(FireBow(true, angle, direction));

        }
    }
    private void AoyiK()//帝释天奥义
    {
        if (!CanSpace) NowSpaceCool += Time.deltaTime;
        if (NowSpaceCool >= SpaceCool) { CanSpace = true; NowSpaceCool = 0f; }
        if (Input.GetKeyDown(KeyCode.Space) && CanSpace == true)
        {
            CanSpace = false;
            Vector3 mouspos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3 direction = mouspos - transform.position;
            direction.z = 0;
            float angle = 360 - Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;
            BowShow(angle);
            StartCoroutine(FireBow(false, angle, direction));
            if (DiShiTianAnimator.GetBool("isAngle") == false) DiShiTianAnimator.Play("DiShiTianBackAoyi");
            else DiShiTianAnimator.Play("DiShiTianCemianAoyi");

        }
    }
    private void StateChange()
    {
        if (DiShiTianInfo.NowState == CharacterState.Normal)
        {
            Run();
            AoyiK();
            Attack();
        }
        if (DiShiTianInfo.NowState == CharacterState.BianXiong)
        {
            Run();
            BianXiongTime += Time.deltaTime;
            if (BianXiongTime >= 3.0f)
            {
                BianXiongTime = 0;
                gameObject.GetComponent<Animator>().runtimeAnimatorController = DiShiTianInfo.ThisPlayerRunAnimator;
                DiShiTianInfo.NowSpeed = DiShiTianInfo.OriginalSpeed;
                DiShiTianInfo.NowState = CharacterState.Normal;
            }
        }
    }
    private void BowShow(float angle)
    {
        if (DiShiTianAnimator.GetBool("isAngle"))
        {
            BowDirection(0, angle);
            DifferentBow[0].SetActive(true);
            DifferentBow[0].GetComponent<Animator>().SetBool("Fire", true);
            StartCoroutine(HideTheBow(DifferentBow[0]));
        }
        else
        { BowDirection(1, angle); DifferentBow[1].SetActive(true); DifferentBow[1].GetComponent<Animator>().SetBool("Fire", true); StartCoroutine(HideTheBow(DifferentBow[1])); }
    }
    IEnumerator HideTheBow(GameObject Bow)
    {
        yield return new WaitForSeconds(0.35f);
        Bow.GetComponent<Animator>().SetBool("Fire", false);
        Bow.SetActive(false);

    }
    IEnumerator FireBow(bool Which, float angle, Vector3 direction)
    {
        yield return new WaitForSeconds(0.2f);
        if (Which == true)
        {
            GameObject BowArrow = Instantiate(Arrow, transform.position + direction.normalized * 1f, Quaternion.identity);
            BowArrow.transform.eulerAngles = new Vector3(0, 0, angle);
            BowArrow.GetComponent<Rigidbody2D>().velocity = direction.normalized * AttackFlySpeed;
        }
        else
        {
            BowFlyPath(direction, angle);
        }
    }
    private void BowDirection(int WhichBow, float angle)
    {
        DifferentBow[WhichBow].transform.eulerAngles = new Vector3(0, 0, angle);
        DifferentBow[WhichBow].transform.position = (Vector2)RotationPivot.position + new Vector2(Distance * Mathf.Cos(angle * Mathf.Deg2Rad), Distance * Mathf.Sin(angle * Mathf.Deg2Rad));
    }
    private void BowFlyPath(Vector3 Direction, float _angle)
    {
        float angle = Mathf.Atan2(Direction.y, Direction.x) * Mathf.Rad2Deg;
        float AngleUp = angle + 25f;
        float AngleDown = angle - 25f;
        Vector3 DirectionUp = new Vector3(Mathf.Cos(AngleUp * Mathf.Deg2Rad), Mathf.Sin(AngleUp * Mathf.Deg2Rad));
        Vector3 DirectionDown = new Vector3(Mathf.Cos(AngleDown * Mathf.Deg2Rad), Mathf.Sin(AngleDown * Mathf.Deg2Rad));
        GameObject BowArrow = Instantiate(Arrow, transform.position + Direction.normalized * 1f, Quaternion.identity);
        BowArrow.transform.eulerAngles = new Vector3(0, 0, _angle);
        BowArrow.GetComponent<Rigidbody2D>().velocity = Direction.normalized * AttackFlySpeed;
        GameObject BowArrow_01 = Instantiate(Arrow, transform.position + DirectionUp.normalized * 1f, Quaternion.identity);
        BowArrow_01.transform.eulerAngles = new Vector3(0, 0, _angle + 25f);
        BowArrow_01.GetComponent<Rigidbody2D>().velocity = DirectionUp.normalized * AttackFlySpeed;
        GameObject BowArrow_02 = Instantiate(Arrow, transform.position + DirectionDown.normalized * 1f, Quaternion.identity);
        BowArrow_02.transform.eulerAngles = new Vector3(0, 0, _angle - 25f);
        BowArrow_02.GetComponent<Rigidbody2D>().velocity = DirectionDown.normalized * AttackFlySpeed;

    }
}
