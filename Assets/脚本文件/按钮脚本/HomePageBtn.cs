using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HomePageBtn : ButtonsPublicUse
{
    public GameObject SelectedImg;
    private bool IsSelected;
    private void Start()
    {
        This_RectTransform = GetComponent<RectTransform>();
        Original_Vc = This_RectTransform.anchoredPosition;
    }

    public override void MouseEnter()
    {
        if (IsSelected) return;
        Vector2 Newpos = Original_Vc + new Vector2(0, offsetY);
        This_RectTransform.anchoredPosition = Newpos;
    }
    public override void MouseExit()
    {
        if (IsSelected) return;
        Vector2 Newpos = Original_Vc;
        This_RectTransform.anchoredPosition = Newpos;
    }
    public override void OnMouseClick()
    {
        IsSelected = true;
        gameObject.GetComponent<Button>().Select();
        SelectedImg.SetActive(true);
    }
}
