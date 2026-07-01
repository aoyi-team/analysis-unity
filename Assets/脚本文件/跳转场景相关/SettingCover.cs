using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SettingCover : MonoBehaviour
{
    public float NewAlpha = 0.5f;
    void Start()
    {
        Color CurrentColor = gameObject.GetComponent<Image>().color;
        CurrentColor = Color.black;
        CurrentColor.a = NewAlpha;
        gameObject.GetComponent<Image>().color = CurrentColor;


    }
}
