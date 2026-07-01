using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BirthdayAbout : MonoBehaviour
{
    public GameObject WenziCanvas;
    bool First=true;
    void Start()
    {
        
    }

    void Update()
    {
        
    }
    private void OnTriggerEnter2D(Collider2D xiaoyecollison)
    {
        if (First)
        {
            
            if (xiaoyecollison.gameObject.tag == "Characters")
            {
                First = false;
                WenziCanvas.SetActive(true);
                xiaoyecollison.gameObject.GetComponent<XiaoYeHuoDong>().enabled = false;
                xiaoyecollison.gameObject.GetComponent<Rigidbody2D>().velocity = Vector2.zero;
            }
        }
    }
}
