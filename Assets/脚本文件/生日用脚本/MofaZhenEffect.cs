using Photon.Pun.Demo.Cockpit;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MofaZhenEffect : MonoBehaviour
{
    public RuntimeAnimatorController ZhanwuYanSkin;
    private GameObject Mangager;
    private void Start()
    {
        Mangager = GameObject.Find("Chararctermanger");
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (true)
        {
            if (collision.gameObject.tag == "TargetPlayrer") StartCoroutine(ChangeSkinController(collision.gameObject));
        }
    }
    IEnumerator ChangeSkinController(GameObject collision)
    {
        yield return new WaitForSeconds(0.6f);
        collision.gameObject.GetComponent<Animator>().runtimeAnimatorController = ZhanwuYanSkin;
        Mangager.GetComponent<SaveObj>().Objs[1].SetActive(false);
        Mangager.GetComponent<SaveObj>().Objs[0].SetActive(true);
        Mangager.GetComponent<SaveObj>().Objs[2].SetActive(true);
        GameObject.FindGameObjectWithTag("Characters").GetComponent<XiaoYeHuoDong>().enabled = false;
        GameObject.FindGameObjectWithTag("Characters").GetComponent<Rigidbody2D>().velocity = Vector2.zero;

    }
}
