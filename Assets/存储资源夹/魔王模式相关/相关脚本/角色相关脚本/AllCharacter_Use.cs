using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class AllCharacter_Use : MonoBehaviour
{
    [Header("资源添加")]
    public GameObject HurtWenzi;
    public GameObject Shine_Obj;
    public int Hurt_Num;

    [Header("光效粒子设置")]
    private float ParticleMover;

    [Header("哈鲁达HealthBar")]
    public GameObject[] Haluda_HealthBar;

    [Header("额外效果")]
    public GameObject Added_Effect;

    [Header("条件限制")]
    public bool IsDestroy;
    public bool IsLongFly_Obj;
    public bool IsContinuousHarm;

    [Header("条件参数")]
    public float Fly_Time;

    private void Start()
    {
        if (IsLongFly_Obj) StartCoroutine(Destroy_LongFly_Obj());
    }
    IEnumerator Destroy_LongFly_Obj()
    {
        yield return new WaitForSeconds(Fly_Time);
        Destroy(gameObject);
    }
    private void OnTriggerEnter2D(Collider2D TargetObj)
    {
        if (TargetObj.tag == "Haluda")
        {
            if (gameObject.tag == "Misle_Small")
            {
                StartCoroutine(Yanchi_Attack(TargetObj.gameObject));
                TargetObj.GetComponent<Haluda_Health>().Take_Damage(Hurt_Num);

            }
            else
            {
                TakeHurt_Haluda(TargetObj.gameObject);
                TargetObj.GetComponent<Haluda_Health>().Take_Damage(Hurt_Num);
            }
        }
    }

    public void TakeHurt_Haluda(GameObject Target)
    {
        Vector3 HurtPosition = Target.transform.position;
        Instantiate(HurtWenzi, HurtPosition, Quaternion.identity);
        Vector2 ParticleRandom = Random.insideUnitCircle * ParticleMover;
        Vector2 Dimension2D = new Vector2(transform.position.x, transform.position.y);
        Instantiate(Shine_Obj, Dimension2D + ParticleRandom, Quaternion.identity);
        if(Added_Effect!=null) Instantiate(Added_Effect, Dimension2D + ParticleRandom, Quaternion.identity);
        if (IsDestroy) Destroy(gameObject, 0.1f);
    }
    IEnumerator Yanchi_Attack(GameObject Target)
    {
        yield return new WaitForSeconds(0.08f);
        Vector3 HurtPosition = Target.transform.position;
        Instantiate(HurtWenzi, HurtPosition, Quaternion.identity);
        Vector2 ParticleRandom = Random.insideUnitCircle * ParticleMover;
        Vector2 Dimension2D = new Vector2(transform.position.x, transform.position.y);
        Instantiate(Shine_Obj, Dimension2D + ParticleRandom, Quaternion.identity);
        if (IsDestroy) Destroy(gameObject, 0.1f);
    }

}
