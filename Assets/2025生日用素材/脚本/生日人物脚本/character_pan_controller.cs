using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class character_pan_controller : BaseCharController
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
        this_animator.Play("Pan_cemian_attack");
        Vector3 boxPos = ClosetBox.transform.position;
        Vector3 direction = (boxPos - transform.position).normalized;
        float dis = Vector2.Distance(transform.position, boxPos);
        direction.z = 0;
        GameObject panduoball = Instantiate(AttackFlyObj, transform.position, Quaternion.identity);
        panduoball.GetComponent<PandBall>().Init(dis, direction);
    }
}
