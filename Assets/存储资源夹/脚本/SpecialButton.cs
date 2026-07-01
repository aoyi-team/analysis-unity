using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpecialButton : MonoBehaviour
{
    public GameObject CloseButotn;
    public void SendBool()
    {
        if (gameObject.tag == "Special1") CloseButotn.GetComponent<BackGroundAppear>().IsSpecial1 = true;
        if (gameObject.tag=="Special2") CloseButotn.GetComponent<BackGroundAppear>().IsSpecial2 = true;
        CloseButotn.GetComponent<BackGroundAppear>().IsNormal = false;
    }
}
