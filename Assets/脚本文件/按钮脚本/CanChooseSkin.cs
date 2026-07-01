using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CanChooseSkin : MonoBehaviour
{
    public GameObject[] contents;
    public void SetCanbeSeen()//当被点击时，实现对应于刚刚按钮选择英雄的皮肤
    {
        int Number = GameObject.Find("PosterManger").GetComponent<SkinNumber>().CallNumber;
        foreach (GameObject SmallSkins in contents)
        {
            SmallSkins.SetActive(false);
        }
        if (Number == 0)
        {
            contents[0].SetActive(true);
        }
        if (Number == 1)
        {
            contents[1].SetActive(true);
        }
        if (Number == 2)
        {
            contents[2].SetActive(true);
        }
        if (Number == 3)
        {
            contents[3].SetActive(true);
        }
        if (Number == 4)
        {
            contents[4].SetActive(true);
        }
        if (Number == 5)
        {
            contents[5].SetActive(true);
        }
        if (Number == 6)
        {
            contents[6].SetActive(true);
        }

    }
}
