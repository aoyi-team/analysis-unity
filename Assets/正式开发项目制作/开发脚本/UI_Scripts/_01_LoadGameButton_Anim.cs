using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class _01_LoadGameButton_Anim : UI_Base_Button_DynamicEffect
{
    public float intervalTime;
    public float ScaleUp_Facotr;
    public float Animator_interval;
    private void Start()
    {
        InvokeRepeating("FacotrLoad", intervalTime, intervalTime);
    }
    private void FacotrLoad()
    {
        Button_Bounce(ScaleUp_Facotr, Animator_interval);
    }
}
