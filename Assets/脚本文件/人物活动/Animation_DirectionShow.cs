using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Animation_DirectionShow : MonoBehaviour
{

    Vector3 LowDirection = new Vector3(0, -1, 0);
    private bool isMirrored = false;
    private Animator PlayerAnimController;
    public Canvas ChildCanvas;
    public Vector3 scale;
    Vector3 ScreenCenter = new Vector3(Screen.width / 2, Screen.height / 2, 0);
    public Vector3 SymmetryPos;
    public Vector3 OriginalPos;
    void Start()
    {
        PlayerAnimController = gameObject.GetComponent<Animator>();
    }
    private void FixedUpdate()
    {
        if (WhetherMove())
        {
            PlayerAnimController.SetBool("isMove", true);
        }
        else if (!WhetherMove()) PlayerAnimController.SetBool("isMove", false);
        if (!isMirrored)//ÍùÓÒ¿´
        {
            scale = transform.localScale;
            scale.x = 0.8f;
            transform.localScale = scale;
            ChildCanvas.gameObject.transform.localScale = scale;
            ChildCanvas.GetComponent<RectTransform>().anchoredPosition = OriginalPos;
        }
        if (isMirrored)//Íù×ó¿´
        {
            scale = transform.localScale;
            scale.x = -0.8f;
            transform.localScale = scale;
            ChildCanvas.gameObject.transform.localScale = scale;
            ChildCanvas.GetComponent<RectTransform>().anchoredPosition = SymmetryPos;
        }
        IsMirrored();
    }
    private void IsMirrored()
    {
        Vector3 Mousedpos = Input.mousePosition;
        if (Mousedpos.x < ScreenCenter.x) isMirrored = true;
        if (Mousedpos.x > ScreenCenter.x) isMirrored = false;
    }
    private bool WhetherMove()
    {
        bool Keydown;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D))
        { Keydown = true; return Keydown; }
        else { Keydown = false; return Keydown; }
    }
}
