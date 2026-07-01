using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Field_Harm : MonoBehaviour
{
    [Header("Damage Settings")]
    public int damagePerSecond = 80; // √ø√Î…À∫¶
    public float damageInterval = 0.45f; // …À∫¶º‰∏Ù ±º‰£®√Î£©
    public GameObject QiXingLong_Wenzi;
    private List<Continuous_Harm> targets = new List<Continuous_Harm>();
    private bool isDamageCoroutineRunning = false;
    public float Destroy_Time = 2.6f;
    private void Start()
    {
        StartCoroutine(Destroy_Gameobj());
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.tag=="Haluda")
        {
            Continuous_Harm health = other.GetComponent<Continuous_Harm>();
            if (!targets.Contains(health))
            {
                targets.Add(health);
                if (!isDamageCoroutineRunning)
                {
                    StartCoroutine(ApplyDamage());
                }
            }
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        Continuous_Harm health = other.GetComponent<Continuous_Harm>();
        if (health != null && targets.Contains(health))
        {
            targets.Remove(health);
        }
    }

    private IEnumerator ApplyDamage()
    {
        isDamageCoroutineRunning = true;
        while (targets.Count > 0)
        {
            foreach (Continuous_Harm target in new List<Continuous_Harm>(targets))
            {
                if (target != null)
                {
                    target.TakeDamage(damagePerSecond, QiXingLong_Wenzi);
                }
                else
                {
                    targets.Remove(target);
                }
            }
            yield return new WaitForSeconds(damageInterval);
        }
        isDamageCoroutineRunning = false;
    }
    IEnumerator Destroy_Gameobj()
    {
        yield return new WaitForSeconds(Destroy_Time);
        targets.Clear();
        Destroy(gameObject);
    }

}
