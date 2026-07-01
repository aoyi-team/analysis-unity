using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BackGroundAppear : MonoBehaviour
{
    public GameObject PersonalImage;
    public GameObject HeadInfImage;
    public GameObject Cover;
    private GameObject ZhongZhuan;
    private CunChu ZhongzhuanCun;
    private Image HallHeadImage;
    private Image PersonaldHeadImage;
    public GameObject HallHead;
    public GameObject PersonalHeadImage;
    public bool IsSpecial1=false;
    public bool IsSpecial2 = false;
    public bool IsNormal = true;
    public GameObject HallHeadOutline;
    public GameObject PersonalHeadOutline;
    private void Start()
    {
        if(HallHead!=null) HallHeadImage = HallHead.GetComponent<Image>();
        if(PersonalHeadImage!=null) PersonaldHeadImage = PersonalHeadImage.GetComponent<Image>();
        ZhongZhuan = GameObject.FindGameObjectWithTag("ZhongZhuan");
        ZhongzhuanCun = ZhongZhuan.GetComponent<CunChu>();
    }
    public void PersonalImageAppear()
    {
        if (PersonalImage.activeSelf)
        {
            PersonalImage.SetActive(false);
            GameObject.FindGameObjectWithTag("HallHeadInfo").GetComponent<Button>().interactable = true;
        }
        else
        {
            PersonalImage.SetActive(true);
            if (gameObject.name == "HallHeadInfo")
            {
                gameObject.GetComponent<Button>().interactable = false;
            }
        }
    }
    public void HeadInfoImage()
    {
        if (HeadInfImage.activeSelf)
        {
            HeadInfImage.SetActive(false);
            Cover.SetActive(false);
            if (IsSpecial1)
            {
                IsSpecial1 = false;
                HallHead.GetComponent<Animator>().runtimeAnimatorController = gameObject.GetComponent<CunChu>().SpecialHeads[0];
                PersonalHeadImage.GetComponent<Animator>().runtimeAnimatorController = gameObject.GetComponent<CunChu>().SpecialHeads[0];
            }
            else if (IsSpecial2)
            {
                IsSpecial2 = false;
                HallHead.GetComponent<Animator>().runtimeAnimatorController = gameObject.GetComponent<CunChu>().SpecialHeads[1];
                PersonalHeadImage.GetComponent<Animator>().runtimeAnimatorController = gameObject.GetComponent<CunChu>().SpecialHeads[1];
            }
            else if(IsNormal&&!IsSpecial1&&!IsSpecial2)
            {
                HallHead.GetComponent<Animator>().runtimeAnimatorController = null;
                PersonalHeadImage.GetComponent<Animator>().runtimeAnimatorController = null;
            }
            if(HallHeadImage!=null&&ZhongzhuanCun.HeadAppear!=null) HallHeadImage.sprite = ZhongzhuanCun.HeadAppear.sprite;
            if(PersonaldHeadImage!=null && ZhongzhuanCun.HeadAppear != null) PersonaldHeadImage .sprite= ZhongzhuanCun.HeadAppear.sprite;
            if (ZhongzhuanCun.HeadOutLineAppear != null)
            {
                HallHeadOutline.GetComponent<Image>().sprite = ZhongzhuanCun.HeadOutLineAppear.sprite;
                PersonalHeadOutline.GetComponent<Image>().sprite = ZhongzhuanCun.HeadOutLineAppear.sprite;
            }

        }
        else { HeadInfImage.SetActive(true);
            Cover.SetActive(true);
        }
    }
}
