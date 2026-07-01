using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MubuFadeOut : MonoBehaviour
{
    public float FadeDuration;
    public CanvasGroup ThisCanvas;
    public Animator TarAnimator;
    public CanvasGroup BlackDrop;

    [Header("文字配置时间参数")]
    public float fadeInDuration = 0.5f;    // 渐显时长（自定义）
    public float scaleDuration = 0.5f;     // 放大时长（自定义）
    public float fadeOutDuration = 0.5f;   // 渐隐时长（自定义）
    public float ShowTime =0.5f;

    public Text _targetText;
    public Transform TextTransform;
    public CanvasGroup _canvasGroup;
    private Vector3 _originalScale;
    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.P))//P播放合上幕布动画
        {
            TarAnimator.Play("Mubu_Lakai");
        }
        if(Input.GetKeyDown(KeyCode.Q))//Q播放幕布渐显
        {
            OutUse();
            Debug.Log("触发了");
        }
        if(Input.GetKeyDown (KeyCode.I))
        {
            StartCoroutine(BlackDropDown());
        }
        if(Input.GetKeyDown(KeyCode.U))
        {
            StartTextComboAnimation();
        }
    }

    public void OutUse()//外部调用
    {
        StartCoroutine(StartToFadeIn());
    }
    IEnumerator StartToFadeIn()
    {
        ThisCanvas.alpha = 0;
        float fadeTime=0;
        while(fadeTime < FadeDuration)
        {
            ThisCanvas.alpha = Mathf.Lerp(0, 1, fadeTime / FadeDuration);
            fadeTime += Time.deltaTime;
            yield return null;
        }
        ThisCanvas.alpha = 1;
    }
    IEnumerator BlackDropDown()//黑色幕布拉下来
    {
        float fadeTime = 0;
        while (fadeTime < FadeDuration)
        {
            BlackDrop.alpha = Mathf.Lerp(0, 1, fadeTime / FadeDuration);
            fadeTime += Time.deltaTime;
            yield return null;
        }
        BlackDrop.alpha = 1;
    }
    private void ResetTextState()
    {
        _canvasGroup.alpha = 0;                      // 初始完全透明
        TextTransform.localScale = Vector3.one * 0.1f;   // 初始缩放0.1
        _canvasGroup.blocksRaycasts = false;         // 动画期间禁止交互
    }
    void Awake()
    {
        // 初始化状态
        _originalScale = TextTransform.localScale;
        ResetTextState();

    }
    public void StartTextComboAnimation()
    {
        // 重置初始状态
        ResetTextState();
        // 启动组合动画协程
        StartCoroutine(TextComboAnimationCoroutine());
    }
    private IEnumerator TextComboAnimationCoroutine()
    {
        // 阶段1：渐显（alpha从0→1）
        yield return StartCoroutine(FadeInCoroutine());

        // 阶段2：放大（缩放从0.1→1）
        yield return StartCoroutine(ScaleUpCoroutine());

        // 阶段3：渐隐（alpha从1→0）
        yield return StartCoroutine(FadeOutCoroutine());

        // 动画结束后恢复原始缩放
        TextTransform.localScale = _originalScale;
    }

    /// <summary>
    /// 渐显协程（alpha 0→1）
    /// </summary>
    private IEnumerator FadeInCoroutine()
    {
        float elapsedTime = 0;
        while (elapsedTime < fadeInDuration)
        {
            // 平滑插值计算透明度
            _canvasGroup.alpha = Mathf.Lerp(0, 1, elapsedTime / fadeInDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        // 确保最终alpha为1，避免误差
        _canvasGroup.alpha = 1;
    }

    /// <summary>
    /// 放大协程（缩放 0.1→1）
    /// </summary>
    private IEnumerator ScaleUpCoroutine()
    {
        float elapsedTime = 0;
        Vector3 startScale = Vector3.one * 0.1f;
        Vector3 targetScale = Vector3.one;
        while (elapsedTime < scaleDuration)
        {
            // 平滑插值计算缩放
            TextTransform.localScale = Vector3.Lerp(startScale, targetScale, elapsedTime / scaleDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        // 确保最终缩放为1，避免误差
        TextTransform.localScale = targetScale;
        yield return new WaitForSeconds(ShowTime);
    }

    /// <summary>
    /// 渐隐协程（alpha 1→0）
    /// </summary>
    private IEnumerator FadeOutCoroutine()
    {
        float elapsedTime = 0;
        while (elapsedTime < fadeOutDuration)
        {
            // 平滑插值计算透明度
            _canvasGroup.alpha = Mathf.Lerp(1, 0, elapsedTime / fadeOutDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        // 确保最终alpha为0，避免误差
        _canvasGroup.alpha = 0;
    }
}
