using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HideIn_Bush : MonoBehaviour
{
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private float targetAlpha = 0.6f;

    private SpriteRenderer spriteRenderer;
    public int bushCounter = 0; // 用于处理多个草丛重叠的情况
    private Coroutine currentCoroutine;
    public float ShowCharacter_Time;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogError("需要SpriteRenderer组件");
        }
    }
    public void Show_Character(float alpha)
    {
        Color c = spriteRenderer.color;
        spriteRenderer.color = new Color(c.r, c.g, c.b, alpha);
        StartCoroutine(WhetherShowCharacter());
    }
    IEnumerator WhetherShowCharacter()
    {
        yield return new WaitForSeconds(ShowCharacter_Time);
        if (bushCounter == 2)
        {
            if (currentCoroutine != null) StopCoroutine(currentCoroutine);
            currentCoroutine = StartCoroutine(FadeTo(targetAlpha));
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Bush"))
        {
            bushCounter++;
            if (bushCounter == 1) // 只在第一次进入时触发
            {
                if (currentCoroutine != null) StopCoroutine(currentCoroutine);
                currentCoroutine = StartCoroutine(FadeTo(targetAlpha));
            }
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Bush"))
        {
            bushCounter--;
            if (bushCounter <= 0) // 完全离开所有草丛时恢复
            {
                bushCounter = 0;
                if (currentCoroutine != null) StopCoroutine(currentCoroutine);
                if(gameObject.activeSelf)currentCoroutine = StartCoroutine(FadeTo(1f));
            }
        }
    }

    IEnumerator FadeTo(float targetAlpha)
    {
        Color originalColor = spriteRenderer.color;
        float startAlpha = originalColor.a;
        float time = 0;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            float newAlpha = Mathf.Lerp(startAlpha, targetAlpha, time / fadeDuration);
            spriteRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, newAlpha);
            yield return null;
        }
    }
}
