using Cinemachine;
using DG.Tweening.Core.Easing;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Control_Event : MonoBehaviour
{
    public GameObject[] All_Characters;
    public float[] Opposite_Time;// 透明度变化的时间
    private int ShowNumber;
    [Range(1,2)]
    public float TimeFactor;

    public AnimationCurve fadecurve;

    public CinemachineVirtualCamera MainMachine;

    public Transform Haluda_Trans;
    private void Start()
    {
        Turn_To_Show();
    }
    private void Turn_To_Show()
    {
        StartCoroutine(FadeOutCoroutine(All_Characters[ShowNumber], Opposite_Time[ShowNumber]));
    }
    IEnumerator FadeOutCoroutine(GameObject TargetCharacter,float OppositeTime)//透明度修改方法
    {
        All_Characters[ShowNumber].SetActive(true);
        if (ShowNumber < 13)
        {
            SpriteRenderer sr = TargetCharacter.GetComponent<SpriteRenderer>();
            Color color = sr.color; // 保存当前颜色
            float elapsedTime = 0;
            bool IsFirst = true;
            while (elapsedTime < OppositeTime)
            {
                elapsedTime += Time.deltaTime;
                float alpha = fadecurve.Evaluate(elapsedTime / OppositeTime); // 插值透明度
                color.a = alpha; // 设置新的透明度
                sr.color = color; // 应用新颜色
                yield return null;
                if (ShowNumber > 1 && ShowNumber < 10)
                {
                    if (elapsedTime > 0.62f && IsFirst)
                    {
                        IsFirst = false;
                        TargetCharacter.GetComponent<AllUse_Deabled>().DisabledThe_Component();
                        ShowNumber++;
                        MainMachine.Follow = All_Characters[ShowNumber].GetComponent<Transform>();
                        if (ShowNumber < 13) StartCoroutine(FadeOutCoroutine(All_Characters[ShowNumber], Opposite_Time[ShowNumber]));
                    }
                }
                else if (ShowNumber == 11)
                {
                    if ((elapsedTime / OppositeTime) > 0.9 && IsFirst)
                    {
                        IsFirst = false;
                        TargetCharacter.GetComponent<AllUse_Deabled>().DisabledThe_Component();
                        ShowNumber++;
                        MainMachine.Follow = All_Characters[ShowNumber].GetComponent<Transform>();
                        if (ShowNumber < 13) StartCoroutine(FadeOutCoroutine(All_Characters[ShowNumber], Opposite_Time[ShowNumber]));
                    }
                }
                else
                {
                    if ((elapsedTime / OppositeTime) > 0.6 && IsFirst)
                    {
                        IsFirst = false;
                        TargetCharacter.GetComponent<AllUse_Deabled>().DisabledThe_Component();
                        ShowNumber++;
                        if (ShowNumber != 13) MainMachine.Follow = All_Characters[ShowNumber].GetComponent<Transform>();
                        else if (ShowNumber == 13) MainMachine.Follow = Haluda_Trans;
                        if (ShowNumber < 13) StartCoroutine(FadeOutCoroutine(All_Characters[ShowNumber], Opposite_Time[ShowNumber]));
                    }
                }
            }
            // 确保透明度为0
            sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 0);
            TargetCharacter.SetActive(false); // 禁用游戏对象
        }
    }
}
