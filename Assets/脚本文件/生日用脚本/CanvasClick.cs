using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CanvasClick : MonoBehaviour
{
    public GameObject ThisCharacterPart;
    public GameObject XiaoyePart;
    public GameObject[] Image;
    public GameObject[] Context;
    public GameObject VirtualCamera1;
    public GameObject Cameras;
    public GameObject virualcameral2;
    public GameObject FirstChar;
    private int Times=0;
    public void CloseTheCanvas()
    {
        ThisCharacterPart.SetActive(false);
        GameObject.FindGameObjectWithTag("Characters").GetComponent<XiaoYeHuoDong>().enabled = true;Times++;
    }
    public void Nextcontext()
    {
        XiaoyePart.SetActive(true);
        Image[0].SetActive (false);
        Image[1].SetActive(true);
        Context[0].SetActive(false);
        Context[1].SetActive(true);
        GameObject.FindGameObjectWithTag("Characters").GetComponent<XiaoYeHuoDong>().enabled = false;
        GameObject.FindGameObjectWithTag("Characters").GetComponent<Rigidbody2D>().velocity = Vector2.zero;


    }
    public void StarShowFires()
    {
        if (Times > 1)
        {
            VirtualCamera1.SetActive(false);
            virualcameral2.SetActive(true);
            Cameras.GetComponent<Camera>().orthographic = false;
            GameObject.FindGameObjectWithTag("Characters").GetComponent<XiaoYeHuoDong>().enabled = false;
            FirstChar.SetActive(true);
        }
    }
}
