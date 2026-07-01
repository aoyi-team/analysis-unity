using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Nuoya_Move : MonoBehaviour
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
    private PlayerInfo NuoyaInfo;
    private float BianXiongTime = 0;
    void Start()
    {
        NuoyaInfo = gameObject.GetComponent<PlayerInfo>();
        myRigid = GetComponent<Rigidbody2D>();
        NuoyaAnimator = gameObject.GetComponent<Animator>();
        NuoyaInfo.Team = LayerMask.LayerToName(gameObject.layer);
    }
    void Update()
    {
        speed = NuoyaInfo.NowSpeed;
        StateChange();
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
        if (NowAttackCool >= AttackCool) { CanAttack = true; NowAttackCool = 0f; }
        if (Input.GetMouseButtonDown(0) && CanAttack == true)
        {
            CanAttack = false;
            NuoyaAnimator.SetTrigger("Attack");
            Vector3 WorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3 HeightPos = new Vector3(WorldPos.x, WorldPos.y, 1f);
            Instantiate(Misile, HeightPos, Quaternion.identity);
        }
    }
    private void AoyiK()//诺亚奥义
    {
        if (!CanSpace) NowSpaceCool += Time.deltaTime;
        if (NowSpaceCool >= SpaceCool) { CanSpace = true; NowSpaceCool = 0f; }
        if (Input.GetKeyDown(KeyCode.Space) && CanSpace == true)
        {
            CanSpace = false;
            if (NuoyaAnimator.GetBool("isAngle") == false) NuoyaAnimator.Play("NuoyaBackAoyi");
            else NuoyaAnimator.Play("NuoyaCemianAoyi");
            Vector3 WorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3 HeightPos = new Vector3(WorldPos.x, WorldPos.y, 1f);
            Instantiate(AoyiKuang, HeightPos, Quaternion.identity);
        }
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
                NuoyaInfo.NowSpeed = NuoyaInfo.OriginalSpeed;
                NuoyaInfo.NowState = CharacterState.Normal;
            }
        }
    }
}
