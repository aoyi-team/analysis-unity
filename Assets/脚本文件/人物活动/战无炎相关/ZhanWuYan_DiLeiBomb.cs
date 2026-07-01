using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ZhanWuYan_DiLeiBomb : MonoBehaviour
{
    private Animator Bomb_Animator;
    public Image HealthBar;
    public GameObject ZhanWuYan_Wenzi;
    public GameObject AttackParticle;
    public float ParticleMover;
    public int AttackAmount;
    private PlayerInfo BombInfo;
    public int perDuration_Time;
    private Coroutine BombCorutine;
    private float Filled;
    public GameObject Bomb_Field_Trigger;
    private bool First = true;
    private void Start()
    {
        BombInfo = GetComponent<PlayerInfo>();
        Bomb_Animator = GetComponent<Animator>();
        BombCorutine= StartCoroutine(StartCountBomb());
    }
    private void Update()
    {
        HealthBar_Sync();
    }
    IEnumerator StartCountBomb()
    {
        yield return new WaitForSeconds(3f);
        Bomb_Animator.Play("DiLeiBoomAnim");
        HealthBar.fillAmount = 0;
        Bomb_Field_Trigger.SetActive(true);
        Destroy(gameObject, 0.38f);
    }
    public  void TakeDamage(GameObject TargetGameobject)
    {
        if (TargetGameobject.tag == "Box")
        {
            BoxTrigger BoxScript = TargetGameobject.GetComponent<BoxTrigger>();
            int FormerHealth = BoxScript.HealthMax;
            Vector3 FirePosition = TargetGameobject.transform.position;
            Instantiate(ZhanWuYan_Wenzi, FirePosition, Quaternion.identity);
            Vector2 ParticleRandom = UnityEngine.Random.insideUnitCircle * ParticleMover;
            Vector2 Dimension2 = new Vector2(TargetGameobject.transform.position.x, TargetGameobject.transform.position.y);
            Instantiate(AttackParticle, Dimension2 + ParticleRandom, Quaternion.identity);
            BoxScript.HealthMax -= AttackAmount;
            int NowHealth = BoxScript.HealthMax;
            BoxScript.AnimaPlaySystem(FormerHealth, NowHealth);

        }
        else if (TargetGameobject.tag == "SmallBox")
        {
            TargetGameobject.GetComponent<Animator>().SetTrigger("Break");
            TargetGameobject.GetComponent<SmallBox>().Healthbar.fillAmount = 0;
            TargetGameobject.GetComponent<CircleCollider2D>().enabled = false;
            Vector3 FirePosition = TargetGameobject.transform.position;
            Instantiate(ZhanWuYan_Wenzi, FirePosition, Quaternion.identity);
            Vector2 ParticleRandom = UnityEngine. Random.insideUnitCircle * ParticleMover;
            Vector2 Dimension2D = new Vector2(TargetGameobject.transform.position.x, TargetGameobject.transform.position.y);
            Instantiate(AttackParticle, Dimension2D + ParticleRandom, Quaternion.identity);
        }
        else if(TargetGameobject.tag=="SmallMonster")
        {
            TargetGameobject.GetComponent<Animator>().SetTrigger("BeAttack");
            Vector3 MislePosition = TargetGameobject.transform.position;
            Instantiate(ZhanWuYan_Wenzi, MislePosition, Quaternion.identity);
            Vector2 ParticleRandom = UnityEngine. Random.insideUnitCircle * ParticleMover;
            Vector2 Dimension2 = new Vector2(TargetGameobject.transform.position.x, TargetGameobject.transform.position.y);
            Instantiate(AttackParticle, Dimension2 + ParticleRandom, Quaternion.identity);
            TargetGameobject.GetComponent<SmallMonster>().HealthMax -= AttackAmount;
        }
    }
    private void HealthBar_Sync()
    {
        if (BombInfo.NowHealth <= 0) 
        { 
            StopCoroutine(BombCorutine);
            if (First)
            {
                First = false;
                Bomb_Animator.Play("DiLeiBoomAnim");
                Bomb_Field_Trigger.SetActive(true);
                Destroy(gameObject, 0.38f);
            }
        }
        float NowHealth = BombInfo.NowHealth;
        Filled = NowHealth / BombInfo.FullHealth;
        HealthBar.fillAmount = Filled;
    }
}

