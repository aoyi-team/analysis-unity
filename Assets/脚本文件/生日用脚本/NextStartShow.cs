using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using UnityEngine;

public class NextStartShow : MonoBehaviour
{
    public GameObject NextName;
    void Start()
    {
        if (gameObject.tag == "LastBomb") StartCoroutine(waitForSomeSeconds());
        else StartCoroutine(waitawhile());
    }
    IEnumerator waitawhile()
    {
        yield return new WaitForSeconds(0.8f);
        NextName.SetActive(true);
    }
    IEnumerator waitForSomeSeconds()
    {
        yield return new WaitForSeconds(2.5f);
        GameObject.FindGameObjectWithTag("BombManager").GetComponent<BombClear>().TheFourLetters = true;
    }

}
