using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Upload_Name : MonoBehaviour
{
    public Text Text_Show;
    private Color targetColor; // 目标颜色
    public float fadeInTime = 1.0f; // 淡入时间
    public float fadeOutTime = 1.0f; // 淡出时间 
    private Vector3 initialPosition; // 初始位置
    public RectTransform Thisrect;
    public float moveUpDistance = 100.0f; // 向上移动的距离
    private void Start()
    {
        if (Text_Show != null)
        {
            targetColor = Text_Show.color; // 保存初始颜色
            Text_Show.color = new Color(targetColor.r, targetColor.g, targetColor.b, 0); // 初始透明度为0
            initialPosition = Thisrect.position;
        }
    }
    public void UploadName()
    {
        if (gameObject.GetComponent<InputField>() != null) gameObject.GetComponent<InputField>().interactable = false;
        FadeIn();
        GameObject charactersys = GameObject.FindGameObjectWithTag("CharacterSystem");
        charactersys.GetComponent<CharacterSystem>().Name = GetComponent<InputField>().text.ToString();
    }
    // 淡入
    public void FadeIn()
    {
        StartCoroutine(FadeInCoroutine());
    }

    // 淡出
    public void FadeOut()
    {
        StartCoroutine(FadeOutCoroutine());
    }

    private IEnumerator FadeInCoroutine()
    {
        float elapsedTime = 0;
        while (elapsedTime < fadeInTime)
        {
            elapsedTime += Time.deltaTime;
            float alpha = elapsedTime / fadeInTime;
            Text_Show.color = new Color(targetColor.r, targetColor.g, targetColor.b, alpha);
            float moveDistance = moveUpDistance * alpha;
            Text_Show.rectTransform.position = initialPosition + new Vector3(0, moveDistance, 0);
            yield return null;
        }
        Text_Show.color = targetColor; // 确保最终透明度为1
        FadeOut();
    }

    private IEnumerator FadeOutCoroutine()
    {
        float elapsedTime = 0;
        while (elapsedTime < fadeOutTime)
        {
            elapsedTime += Time.deltaTime;
            float alpha = 1 - (elapsedTime / fadeOutTime);
            Text_Show.color = new Color(targetColor.r, targetColor.g, targetColor.b, alpha);
            yield return null;
        }
        Text_Show.color = new Color(targetColor.r, targetColor.g, targetColor.b, 0); // 确保最终透明度为0
    }
}

