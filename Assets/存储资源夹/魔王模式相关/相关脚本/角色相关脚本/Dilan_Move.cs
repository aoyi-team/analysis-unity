using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Dilan_Move : MonoBehaviour
{
    private Animator Dilan_Animator;
    public GameObject Dilan_Bo;
    public float AttackFlySpeed;
    public float PianZhuan_Angle;
    public Transform Haluda_Trans;
    private void Start()
    {
        Dilan_Animator = GetComponent<Animator>();
        Attack();
    }
    IEnumerator BulletFlyPath(Vector3 Direction, float _angle)
    {
        yield return new WaitForSeconds(0.18f);
        float angle = Mathf.Atan2(Direction.y, Direction.x) * Mathf.Rad2Deg;
        float AngleUp = angle + PianZhuan_Angle;
        float AngleDown = angle - PianZhuan_Angle;
        Vector3 DirectionUp = new Vector3(Mathf.Cos(AngleUp * Mathf.Deg2Rad), Mathf.Sin(AngleUp * Mathf.Deg2Rad));
        Vector3 DirectionDown = new Vector3(Mathf.Cos(AngleDown * Mathf.Deg2Rad), Mathf.Sin(AngleDown * Mathf.Deg2Rad));
        GameObject DiLan_Bo_01 = Instantiate(Dilan_Bo, transform.position + DirectionUp.normalized * 1f, Quaternion.identity);
        DiLan_Bo_01.transform.eulerAngles = new Vector3(0, 0, _angle + PianZhuan_Angle);
        DiLan_Bo_01.GetComponent<Rigidbody2D>().velocity = DirectionUp.normalized * AttackFlySpeed;
        GameObject DiLan_Bo_02 = Instantiate(Dilan_Bo, transform.position + DirectionDown.normalized * 1f, Quaternion.identity);
        DiLan_Bo_02.transform.eulerAngles = new Vector3(0, 0, _angle - PianZhuan_Angle);
        DiLan_Bo_02.GetComponent<Rigidbody2D>().velocity = DirectionDown.normalized * AttackFlySpeed;
        GameObject DiLan_Bo_03= Instantiate(Dilan_Bo, transform.position + Direction.normalized * 1f, Quaternion.identity);
        DiLan_Bo_03.transform.eulerAngles = new Vector3(0, 0, _angle);
        DiLan_Bo_03.GetComponent<Rigidbody2D>().velocity = Direction.normalized * AttackFlySpeed;
    }
    private void Attack()
    {
        Dilan_Animator.Play("Dilan_Attack");
        Vector3 mouspos = Haluda_Trans.position;
        Vector3 direction = mouspos - transform.position;
        direction.z = 0;
        float angle = 360 - Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;
        StartCoroutine(BulletFlyPath(direction, angle));
    }
}
