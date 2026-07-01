using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Panels;

public class LogininBtn : UI_Base_Button_DynamicEffect, IPointerEnterHandler,IPointerExitHandler
{
    public Vector3 HavorOffset=new Vector3(0,8f,0);
    public float Duration = 0.1f;
    private Vector3 OriginalPosition;
    private RectTransform ButtonTrans;
    private void Start()
    {
        ButtonTrans = GetComponent<RectTransform>();
        OriginalPosition = ButtonTrans.anchoredPosition;
    }
    public void OnPointerEnter(PointerEventData EventData)
    {
        if (ButtonTrans == null) return;
        Button_Anchored_UpMove(ButtonTrans,Duration,HavorOffset);
    }
    public void OnPointerExit(PointerEventData EventData)
    {
        if (ButtonTrans==null) return;
        Button_Anchored_Recover(ButtonTrans, Duration, HavorOffset);
    }

}
