using Photon.Pun.Demo.Asteroids;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HouYi_Move : MonoBehaviour
{
    private Animator Houyi_Animator;
    public GameObject Houyi_Bullet;
    public float Bullet_Fly_Speed;
    public float Shoot_Bullet_Time;
    public Transform Haluda_Trans;
    private void Start()
    {
        Houyi_Animator = GetComponent<Animator>();
        Attack();
    }
    private void Attack()
    {
        Houyi_Animator.Play("HouYi_Attack");
        Vector3 mouspos = Haluda_Trans.position;
        Vector3 direction = mouspos - transform.position;
        direction.z = 0;
        float angle = 360 - Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;
        StartCoroutine(BulletFlyPath(direction, angle));
    }
    IEnumerator BulletFlyPath(Vector3 Direction, float _angle)
    {
        yield return new WaitForSeconds(Shoot_Bullet_Time);
        GameObject DiLan_Bo_03 = Instantiate(Houyi_Bullet, transform.position + Direction.normalized * 1f, Quaternion.identity);
        DiLan_Bo_03.transform.eulerAngles = new Vector3(0, 0, _angle);
        DiLan_Bo_03.GetComponent<Rigidbody2D>().velocity = Direction.normalized * Bullet_Fly_Speed;
    }
}
