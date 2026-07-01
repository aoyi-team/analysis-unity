using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HeadAndOutlineExchange : MonoBehaviour
{
    public GameObject ChooseHeadLight;
    public GameObject ChooseOutlineLight;
    public GameObject[] DifferentJihe; 
    public void OnclickHeadButton()
    {
        ChooseHeadLight.SetActive(true);
        ChooseOutlineLight.SetActive(false);
        DifferentJihe[0].SetActive(true);
        DifferentJihe[1].SetActive(false);
    }
    public void OnclickOutlineButton()
    {
        ChooseHeadLight.SetActive(false);
        ChooseOutlineLight.SetActive(true);
        DifferentJihe[0].SetActive(false);
        DifferentJihe[1].SetActive(true);
    }
}
