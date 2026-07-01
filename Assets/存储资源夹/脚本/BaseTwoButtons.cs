using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class BaseTwoButtons : MonoBehaviour
{
    public int Index = 1;
    public Text PageNumber;
    public BaseTwoButtons TheOtherButton;
    private Button TheOtherButtonIN;
    public int maxpagenumber;
    public GameObject ZhongZhuan;
    private void Start()
    {
        TheOtherButtonIN = TheOtherButton.gameObject.GetComponent<Button>();
    }
    public void LeftButtonOnclick()
    {
        if (Index == 1) gameObject.GetComponent<Button>().interactable = false;
        else { Index -= 1; TheOtherButtonIN.interactable = true; }
        PageNumber.text = Index.ToString();
        TheOtherButton.Index = Index;
        ZhongZhuan.GetComponent<CunChu>().HeadPages[Index-1].SetActive(true);

    }
    public void RightButtonOnclick()
    {
        if (Index == maxpagenumber) gameObject.GetComponent<Button>().interactable = false;
        else { Index += 1; TheOtherButtonIN.interactable = true; }
        PageNumber.text = Index.ToString();
        TheOtherButton.Index = Index;
        ZhongZhuan.GetComponent<CunChu>().HeadPages[Index-2].SetActive(false);
    }
}
