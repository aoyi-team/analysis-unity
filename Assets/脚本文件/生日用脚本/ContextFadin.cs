using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ContextFadin : MonoBehaviour
{
    public float fadeDuration = 2.0f; // 渐显持续时间
    private Text textComponent;

    void Start()
    {
        textComponent = GetComponent<Text>();
        Color color = textComponent.color;
        color.a = 0; // 初始透明度为0
        textComponent.color = color;
    }

    public void FadeIn()
    {
        StartCoroutine(FadeTextToFullAlpha());
    }

    private IEnumerator FadeTextToFullAlpha()
    {
        float t = 0;
        Color color = textComponent.color;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            color.a = Mathf.Clamp01(t / fadeDuration);
            textComponent.color = color;
            yield return null;
        }

        color.a = 1; // 确保最后透明度为1
        textComponent.color = color;
    }
}
