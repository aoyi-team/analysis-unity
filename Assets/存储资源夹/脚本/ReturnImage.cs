using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ReturnImage : MonoBehaviour
{
    private bool isHovering = false;
    private Vector3 originalScale;
    public float scalerFactor = 1.1f;
    public float transitionSpeed = 3f;
    private GameObject ZhongZhuan;
    private CunChu ZhongZhuanCun;
    private void Start()
    {
        originalScale = transform.localScale;
        ZhongZhuan = GameObject.FindGameObjectWithTag("ZhongZhuan");
        ZhongZhuanCun = ZhongZhuan.GetComponent<CunChu>();
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
    public void OnbuttonClick()
    {
        ZhongZhuanCun.HeadAppear = gameObject.GetComponent<Image>();
        if (gameObject.tag != "Special1" && gameObject.tag != "Special2") GameObject.Find("CloseButton1").GetComponent<BackGroundAppear>().IsNormal = true;
    }
    public void ReturnOutLineImage()
    {
        ZhongZhuanCun.HeadOutLineAppear = gameObject.GetComponent<Image>();
    }
}
