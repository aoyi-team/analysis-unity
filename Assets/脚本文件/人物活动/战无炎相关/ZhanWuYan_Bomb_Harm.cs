using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZhanWuYan_Bomb_Harm : MonoBehaviour
{
    public GameObject FatherGamObj;
    private void OnTriggerEnter2D(Collider2D collision)
    {
        FatherGamObj.GetComponent<ZhanWuYan_DiLeiBomb>().TakeDamage(collision.gameObject);
    }
}
