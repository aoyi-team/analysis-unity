using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ModesLoad : MonoBehaviour
{
    public float scalerFactor = 1.1f;
    public float transitionSpeed = 0.1f;
    public int OffsetX = 5;
    public int OffsetY = 5;
    private RectTransform This_Rect;
    private Vector2 Original_Vc;
    private Vector2 Original_Sc;
    private Animator ModesLoadBtn_Animator;
    void Start()
    {
        This_Rect = GetComponent<RectTransform>();
        Original_Vc = This_Rect.anchoredPosition;
        Original_Sc = This_Rect.localScale;
        ModesLoadBtn_Animator = GetComponent<Animator>();
    }
    public void OnMouseEnter()
    {
        Vector2 Newpos = Original_Vc + new Vector2(OffsetX, OffsetY);
        Vector2 NewSca = Original_Sc * scalerFactor;
        ModesLoadBtn_Animator.Play("DuizhanBtn_MouseIn_Animation");
        This_Rect.anchoredPosition = Newpos;
        This_Rect.localScale = NewSca;
    }
    public void OnMouseExit()
    {
        This_Rect.anchoredPosition = Original_Vc;
        This_Rect.localScale = Original_Sc;
        ModesLoadBtn_Animator.Play("DuizhanBtn_Solid_Animation");
    }
}
