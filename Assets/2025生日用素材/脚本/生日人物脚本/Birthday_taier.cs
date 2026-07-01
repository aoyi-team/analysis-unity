using Photon.Pun.Demo.Asteroids;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Birthday_taier : BaseCharController
{
    private Animator TaiErAnimator;
    public GameObject[] DifferentGun;
    public GameObject Bulltet;
    public int AttackFlySpeed = 12;
    public Transform RotationPivot;
    public GameObject[] Guns;
    public float Distance=0f;
    public float FireStepTime=0.5f;
    void Start()
    {
        TaiErAnimator = gameObject.GetComponent<Animator>();
    }
    public override void Attack()
    {
        TaiErAnimator.SetTrigger("Attack");
        Vector3 boxPos = ClosetBox.transform.position;
        Vector3 direction = boxPos - transform.position;
        direction.z = 0;
        float angle = 360 - Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;
        GunShow(angle);
        BulletFlyPath(direction, angle);
    }
    private void BulletFlyPath(Vector3 Direction, float _angle)
    {
        float angle = Mathf.Atan2(Direction.y, Direction.x) * Mathf.Rad2Deg;
        float AngleUp = angle + 10f;
        Vector3 DirectionUp = new Vector3(Mathf.Cos(AngleUp * Mathf.Deg2Rad), Mathf.Sin(AngleUp * Mathf.Deg2Rad));
        GameObject TaierBullet_01 = Instantiate(Bulltet, transform.position + DirectionUp.normalized * 1f, Quaternion.identity);
        TaierBullet_01.transform.eulerAngles = new Vector3(0, 0, _angle + 10f);
        TaierBullet_01.GetComponent<Rigidbody2D>().velocity = DirectionUp.normalized * AttackFlySpeed;

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
