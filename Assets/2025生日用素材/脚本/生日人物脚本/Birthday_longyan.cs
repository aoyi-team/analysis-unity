using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Birthday_longyan : BaseCharController
{
    public GameObject LongYanAttackYinji;
    private Animator LongYanAnimator;
    void Start()
    {
        LongYanAnimator = gameObject.GetComponent<Animator>();
    }
    public  override void Attack()//ÁúÑ×ÆÕ¹¥
    {
        LongYanAnimator.SetTrigger("Attack");
        StartCoroutine(InstantiateAttack());
    }
    IEnumerator InstantiateAttack()
    {
        yield return new WaitForSeconds(0.43f);
        Vector3 boxPos = ClosetBox.transform.position;
        Vector3 direction = boxPos - transform.position;
        direction.z = 0;
        float angle = 360 - Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;
        GameObject LongYanAttack = Instantiate(LongYanAttackYinji, transform.position + direction.normalized * 1f, Quaternion.identity);
        LongYanAttack.transform.eulerAngles = new Vector3(0, 0, angle);
    }
}
