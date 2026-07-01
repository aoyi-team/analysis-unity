using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Xml.Serialization;
using UnityEngine.UIElements;

public class CharacterAndFudong : MonoBehaviour
{
    public float scalerFactor = 1.2f;
    public float transitionSpeed = 1f;
    private Vector3 originalScale;
    private bool isHovering = false;
    public GameObject Character;//外部转载的原皮海报
    public string[] Characters = { "YileSkinGathering", "LongYanSkinGathering", "NuoYaSkinGathering", "XiaoYeSkinGathering" , "TaiErSkinGathering" };//原皮海报栏
    public UnityEngine.UI . Button[] TwoButtons;
    public GameObject[] Panels;
    public UnityEngine.UI.Button[] CharacterChoices;
    public GameObject TargetCharacter;//对应英雄的皮肤栏显示；
    bool isFisrt=true;//用于判断是否是第一次按这个按钮，如果是第一次，那么就生成原皮海报，不是就不是。
    public GameObject StarGameButton;
    public GameObject ImageDemonstrate;
    void Start()
    {
        originalScale = transform.localScale;
    }

    // Update is called once per frame
    void Update()
    {
        if (isHovering)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, originalScale * scalerFactor, Time.deltaTime * transitionSpeed);
        }
        else
            transform.localScale = Vector3.Lerp(transform.localScale, originalScale, Time.deltaTime * transitionSpeed);

    }
    public void OnMouseEnter()
    {
        isHovering = true;
    }
    public void OnMouseExit()
    {
        isHovering = false;
    }
    public void CharacterPlayer()
    {
        foreach (string Targetname in Characters)
        {
            GameObject Term = GameObject.Find(Targetname);
            if (Term != null)
            {
                if (Term.name == TargetCharacter.name)
                {
                    continue;
                }
                else
                {
                    Term.SetActive(false);
                }
            }
        }
        if (isFisrt)
        {
            TargetCharacter.SetActive(true);
            Character.SetActive(true);
            StarGameButton.SetActive(true);
            isFisrt = false;

        }
        else if(!isFisrt )
        {
            TargetCharacter.SetActive(true);

        }
        foreach (UnityEngine.UI.Button RightNow in CharacterChoices )
        {
            RightNow.interactable = true;
            RightNow.gameObject.GetComponent<CharacterAndFudong>().ImageDemonstrate.SetActive(false);
        }
        gameObject.GetComponent<UnityEngine.UI.Button>().interactable = false;//遍历所有英雄选择按钮可用，并且将当前按钮禁用
        ImageDemonstrate.SetActive(true);
    }//展示选择那个英雄的海报
    public void ChooseCharacterOrskin()
    {
        if(gameObject .name == "ChooseCharacterButton")
        {
            TwoButtons[1].interactable = true; 
            TwoButtons[0].interactable = false;

        }
        else
        {
            TwoButtons[1].interactable = false ;
            TwoButtons[0].interactable = true;
        }

    }//控制皮肤和英雄选择按钮
    public void ChooseSkin()
    {
        Panels[0].SetActive(false);
        Panels[1].SetActive(true);
    }
    public void ChooseCharacter()
    {
        Panels[0].SetActive(true);
        Panels[1].SetActive(false);
    }
    public void YileNumber()
    {
        GameObject.Find("PosterManger").GetComponent<SkinNumber>().CallNumber=0;
        GameObject.FindGameObjectWithTag("CharacterSystem").GetComponent<CharacterSystem>().CharacterNumber = 0;

    }
    public void LongYanNumber()
    {
        GameObject.Find("PosterManger").GetComponent<SkinNumber>().CallNumber=1;
        GameObject.FindGameObjectWithTag("CharacterSystem").GetComponent<CharacterSystem>().CharacterNumber = 1;
    }
    public void NuoyaNumber()
    {
        GameObject.Find("PosterManger").GetComponent<SkinNumber>().CallNumber = 2;
        GameObject.FindGameObjectWithTag("CharacterSystem").GetComponent<CharacterSystem>().CharacterNumber = 2;
    }
    public void XiaoyeNumber()
    {
        GameObject.Find("PosterManger").GetComponent<SkinNumber>().CallNumber = 3;
        GameObject.FindGameObjectWithTag("CharacterSystem").GetComponent<CharacterSystem>().CharacterNumber = 3;
    }
    public void TaierNumber()
    {
        GameObject.Find("PosterManger").GetComponent<SkinNumber>().CallNumber = 4;
        GameObject.FindGameObjectWithTag("CharacterSystem").GetComponent<CharacterSystem>().CharacterNumber = 4;
    }
    public void DiShiTianNumber()
    {
        GameObject.Find("PosterManger").GetComponent<SkinNumber>().CallNumber = 5;
        GameObject.FindGameObjectWithTag("CharacterSystem").GetComponent<CharacterSystem>().CharacterNumber = 5;
    }
    public void AimiNumber()
    {
        GameObject.Find("PosterManger").GetComponent<SkinNumber>().CallNumber = 6;
        GameObject.FindGameObjectWithTag("CharacterSystem").GetComponent<CharacterSystem>().CharacterNumber = 6;
    }


}

