using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;

public class SkinChange : MonoBehaviour
{
    public GameObject[] Skins;
    public Vector3[] FixedPositions;
    public GameObject[] posters;


    void Start()
    {
        FixedPositions = new Vector3[3];
        FixedPositions[0] = new Vector3(-230, -22,0);
        FixedPositions[1] = new Vector3(-100, 0,0);
        FixedPositions[2] = new Vector3(30, -22,0);

        
    }

    public void ChangeSkin()
    {
        if (transform.localPosition.x < -100 && transform.localPosition.x > -260)//如果是在中间居左的位置则右移
        {
            for (int i=0 ; i<5 ; i++)
            {
                RightMoveposition();
            }
            posters[CurrentID()].SetActive(true);
            posters[CurrentID()+1].SetActive(false);


        }
        else if(transform.localPosition.x > 0 && transform.localPosition.x < 60)//如果是在中间居右的位置则左移
        {
            for (int i = 0; i < 5; i++)
            {
                LeftMoveposition();
            }
            posters[CurrentID()].SetActive(true);
            posters[CurrentID()-1].SetActive(false);
        }
    }
    int CurrentID()//获取当前图片在原数组的ID是哪个
    {
        int numeber = 0;
        for(int i=0;i<5;i++)
        {
            if (transform.name == Skins[i].name) numeber =i;
        }
        return numeber;
    }
    private void LeftMoveposition()//进行图片向左移动操作
    {
        foreach (GameObject MovedOnes in Skins)
        {
            if (MovedOnes.transform.localPosition.x >0 && MovedOnes.transform.localPosition.x <60)
            {
                MovedOnes.GetComponent<Transform>().DOLocalMove(FixedPositions[1], 0.1f);
                MovedOnes.GetComponent<Transform>().SetAsLastSibling();
                continue;
            }
            if (MovedOnes.transform.localPosition.x > -150 && MovedOnes.transform.localPosition.x < 0)
            {
                MovedOnes.GetComponent<Transform>().DOLocalMove(FixedPositions[0], 0.1f);
                continue;
            }
            else MovedOnes.GetComponent<Transform>().DOLocalMoveX(MovedOnes.GetComponent<Transform>().localPosition.x - 250, 0.1f);

        }
        //if(i>0) Position.GetComponent<Transform>().DOLocalMove(FixedPositions[i - 1], 0.2f);
        //if (i - 1 == 4) Position.GetComponent<Transform>().SetAsLastSibling();
    }
    private void RightMoveposition()//进行图片向右移动操作
    {
        //Position.GetComponent <Transform >().DOLocalMove(FixedPositions[i + 1], 0.2f);
        foreach (GameObject MovedOnes in Skins)
        {
            if (MovedOnes.transform.localPosition.x < -100 && MovedOnes.transform.localPosition.x > -260)
            {
                MovedOnes.GetComponent<Transform>().DOLocalMove(FixedPositions[1], 0.1f);
                MovedOnes.GetComponent<Transform>().SetAsLastSibling();
                continue;
            }
            if (MovedOnes.transform.localPosition.x > -150 && MovedOnes.transform.localPosition.x < 0)
            {
                MovedOnes.GetComponent<Transform>().DOLocalMove(FixedPositions[2], 0.1f);
                continue;
            }
            else MovedOnes.GetComponent<Transform>().DOLocalMoveX (MovedOnes.GetComponent<Transform>().localPosition.x+250, 0.1f);

        }

    }
}
