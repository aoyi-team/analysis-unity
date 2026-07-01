using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;

public class ButtonBigModify : MonoBehaviour
{
    private bool isHovering = false;
    private Vector3 originalScale;
    public float scalerFactor = 1.05f;
    public float transitionSpeed = 1f;
    private void Start()
    {
        originalScale = transform.localScale;
    }
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

}
