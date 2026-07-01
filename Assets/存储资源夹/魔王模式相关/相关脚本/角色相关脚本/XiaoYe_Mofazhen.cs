using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class XiaoYe_Mofazhen : MonoBehaviour
{
    public GameObject Show_Characters;
    public float WaitTime;

    private void Start()
    {
        StartCoroutine(ShowCharacters());
    }
    IEnumerator ShowCharacters()
    {
        yield return new WaitForSeconds(WaitTime);
        Show_Characters.SetActive(true);
    }
}
