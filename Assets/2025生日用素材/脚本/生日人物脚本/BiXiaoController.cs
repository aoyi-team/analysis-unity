using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BiXiaoController :BaseCharController
{
    private Animator this_animator;
    public GameObject AttackFlyObj;
    public float AttackFlySpeed;
    private void Start()
    {
        this_animator = GetComponent<Animator>();
    }
    public override void Attack()
    {
        this_animator.Play("Bixiao_cemian_Attack");
        Vector3 boxPos = ClosetBox.transform.position;
        Vector3 direction = (boxPos - transform.position).normalized;
        direction.z = 0;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90;
        GameObject BixiaoBall = Instantiate(AttackFlyObj, transform.position, Quaternion.identity);
        BixiaoBall.transform.eulerAngles = new Vector3(0f, 0f, angle);
        BixiaoBall.GetComponent<BixiaoBall>().Init(direction);
    }
}
