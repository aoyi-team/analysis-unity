using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TaiEr_Move : MonoBehaviour
{
    private Rigidbody2D myRigid;
    public float speed;
    private Animator TaiErAnimator;
    private float NowAttackCool = 0f;
    private float NowSpaceCool = 0f;
    public float AttackCool;
    public float SpaceCool;
    private bool CanAttack = true;
    private bool CanSpace = true;
    private PlayerInfo TaiErInfo;
    private float BianXiongTime = 0;
    public GameObject[] DifferentGun;
    public GameObject Bulltet;
    public int AttackFlySpeed = 12;
    private Vector2 DisplacementDirection;
    public float DisplacementSpeed;
    public float DisplacementDurationTime;
    private float DisplacementTime = 0;
    private int BulletAmout = 7;
    public Transform RotationPivot;
    public GameObject[] Guns;
    public float Distance;
    void Start()
    {
        TaiErInfo = gameObject.GetComponent<PlayerInfo>();
        myRigid = GetComponent<Rigidbody2D>();
        TaiErAnimator = gameObject.GetComponent<Animator>();
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
    private void Attack()//太二普攻
    {
        if (NowAttackCool >= AttackCool) { CanAttack = true; NowAttackCool = 0f; BulletAmout = 7; }
        if (!CanAttack) NowAttackCool += Time.deltaTime;
        if (BulletAmout <= 0) CanAttack = false;
        if (Input.GetMouseButtonDown(0) && BulletAmout > 0)
        {

            TaiErAnimator.SetTrigger("Attack");
            Vector3 mouspos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3 direction = mouspos - transform.position;
            direction.z = 0;
            float angle = 360 - Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;
            GunShow(angle);
            BulletFlyPath(direction, angle);
            BulletAmout -= 1;

        }
    }
    private void AoyiK()//太二奥义
    {
        if (!CanSpace) NowSpaceCool += Time.deltaTime;
        if (NowSpaceCool >= SpaceCool) { CanSpace = true; NowSpaceCool = 0f; }
        if (Input.GetKeyDown(KeyCode.Space) && CanSpace == true)
        {
            BulletAmout = 7; CanAttack = true; NowAttackCool = 0f;
            float movedirx = Input.GetAxis("Horizontal");
            float movediry = Input.GetAxis("Vertical");
            CanSpace = false;
            if (TaiErAnimator.GetBool("isAngle") == false) TaiErAnimator.Play("TaiErBackAoyi");
            else TaiErAnimator.Play("TaiErCeMianAoyi");
            DisPlacement(movedirx, movediry);
            StartCoroutine(StopDisplacement());

        }
    }
    private void StateChange()
    {
        if (TaiErInfo.NowState == CharacterState.Normal)
        {
            Run();
            AoyiK();
            Attack();
        }
        if (TaiErInfo.NowState == CharacterState.BianXiong)
        {
            Run();
            BianXiongTime += Time.deltaTime;
            if (BianXiongTime >= 3.0f)
            {
                BianXiongTime = 0;
                gameObject.GetComponent<Animator>().runtimeAnimatorController = TaiErInfo.ThisPlayerRunAnimator;
                TaiErInfo.NowSpeed = TaiErInfo.OriginalSpeed;
                TaiErInfo.NowState = CharacterState.Normal;
            }
        }
        if (TaiErInfo.NowState == CharacterState.Displacement)
        {
            DisplacementTime += Time.deltaTime;
        }
    }
    private void DisPlacement(float x, float y)//位移方法
    {
        TaiErInfo.NowState = CharacterState.Displacement;
        DisplacementDirection.x = x;
        DisplacementDirection.y = y;
        myRigid.velocity = DisplacementDirection.normalized * DisplacementSpeed;
    }
    IEnumerator StopDisplacement()
    {
        yield return new WaitForSeconds(DisplacementDurationTime);
        DisplacementTime = 0;
        myRigid.velocity = Vector2.zero;
        TaiErInfo.NowState = CharacterState.Normal;
    }
    private void BulletFlyPath(Vector3 Direction, float _angle)
    {
        float angle = Mathf.Atan2(Direction.y, Direction.x) * Mathf.Rad2Deg;
        float AngleUp = angle + 10f;
        float AngleDown = angle - 10f;
        Vector3 DirectionUp = new Vector3(Mathf.Cos(AngleUp * Mathf.Deg2Rad), Mathf.Sin(AngleUp * Mathf.Deg2Rad));
        Vector3 DirectionDown = new Vector3(Mathf.Cos(AngleDown * Mathf.Deg2Rad), Mathf.Sin(AngleDown * Mathf.Deg2Rad));
        GameObject TaierBullet_01 = Instantiate(Bulltet, transform.position + DirectionUp.normalized * 1f, Quaternion.identity);
        TaierBullet_01.transform.eulerAngles = new Vector3(0, 0, _angle + 10f);
        TaierBullet_01.GetComponent<Rigidbody2D>().velocity = DirectionUp.normalized * AttackFlySpeed;
        GameObject TaierBullet_02 = Instantiate(Bulltet, transform.position + DirectionDown.normalized * 1f, Quaternion.identity);
        TaierBullet_02.transform.eulerAngles = new Vector3(0, 0, _angle - 10f);
        TaierBullet_02.GetComponent<Rigidbody2D>().velocity = DirectionDown.normalized * AttackFlySpeed;

    }
    private void GunShow(float angle)
    {
        if (TaiErAnimator.GetBool("isAngle"))
        {
            GunDirection(0, angle);
            for (int i = 0; i <= 1; i++)
            {
                DifferentGun[i].SetActive(true);
                DifferentGun[i].GetComponent<Animator>().SetBool("Fire", true);
                StartCoroutine(HindTheGun(DifferentGun[i]));
            }
        }
        else for (int i = 2; i <= 3; i++)
            { GunDirection(1, angle); DifferentGun[i].SetActive(true); DifferentGun[i].GetComponent<Animator>().SetBool("Fire", true); StartCoroutine(HindTheGun(DifferentGun[i])); }
    }
    IEnumerator HindTheGun(GameObject Gun)
    {
        yield return new WaitForSeconds(0.14f);
        Gun.GetComponent<Animator>().SetBool("Fire", false);
        Gun.SetActive(false);

    }
    private void GunDirection(int WhichGun, float angle)
    {
        Guns[WhichGun].transform.eulerAngles = new Vector3(0, 0, angle);
        Guns[WhichGun].transform.position = (Vector2)RotationPivot.position + new Vector2(Distance * Mathf.Cos(angle * Mathf.Deg2Rad), Distance * Mathf.Sin(angle * Mathf.Deg2Rad));
    }
}
