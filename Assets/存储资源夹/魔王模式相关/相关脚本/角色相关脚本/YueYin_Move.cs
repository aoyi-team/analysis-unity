using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;

public class YueYin_Move : MonoBehaviour
{
    public GameObject YueYin_Attack_Obj;
    public float ShowAttackTime;
    private Animator YueYin_Animator;
    public Transform Haluda_Trans;
    private void Start()
    {
        YueYin_Animator = GetComponent<Animator>();
        Attack();
    }
    IEnumerator InstantiateAttack()
    {
        yield return new WaitForSeconds(ShowAttackTime);
        Vector3 mouspos = Haluda_Trans.position;
        Vector3 direction = mouspos - transform.position;
        direction.z = 0;
        float angle = 360 - Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;
        GameObject LongYanAttack = Instantiate(YueYin_Attack_Obj, transform.position + direction.normalized * 1f, Quaternion.identity);
        LongYanAttack.transform.eulerAngles = new Vector3(0, 0, angle);
    }
    private void Attack()
    {
        YueYin_Animator.Play("YueYin_Attack");
        StartCoroutine(InstantiateAttack());
    }
}
