using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//大厅下方按钮的通用功能
public class ButtonsPublicUse : MonoBehaviour
{
    public int offsetY;//竖直上的偏移量
    [NonSerialized] public Animator This_BtnAnimator;
    [NonSerialized] public RectTransform This_RectTransform;
    [NonSerialized] public Vector2 Original_Vc;//初始位置

    private void Start()
    {
        This_BtnAnimator= GetComponent<Animator>();
        This_RectTransform = GetComponent<RectTransform>();
        Original_Vc = This_RectTransform.anchoredPosition;
    }
    //鼠标退出
    public virtual void MouseEnter()
    {
        Vector2 Newpos = Original_Vc + new Vector2(0, offsetY);
        This_RectTransform.anchoredPosition = Newpos;
        This_BtnAnimator.Play("MouseIn_Animation");

    }
    //鼠标进入
    public virtual void MouseExit()
    {
        Vector2 Newpos = Original_Vc;
        This_RectTransform.anchoredPosition = Newpos;
        This_BtnAnimator.Play("Solid_Animation");
    }
    //鼠标点击
    public virtual void OnMouseClick()
    {

    }
}
