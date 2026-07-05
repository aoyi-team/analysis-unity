using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

// 泡泡提示消息显示效果（缩放 + 渐隐）
public class BubblePop : MonoBehaviour
{
    public GameObject bubbleImage;  // 气泡图片
    public GameObject textContent;  // 文字内容

    [Header("动画时长")]
    public float popDuration = 0.4f;        // 弹出时长
    public float showDuration = 3f;         // 停留显示时长
    public float closeDuration = 0.3f;      // 气泡关闭时长
    public float textCloseDuration = 0.4f;  // 文字消失时长
    public float repeatTime = 10.0f;        // 重复间隔

    private void Start()
    {
        InvokeRepeating(nameof(StartSequence), 5f, repeatTime);
    }

    private void StartSequence()
    {
        StopAllCoroutines();  // 防止动画重叠
        StartCoroutine(Sequence());
    }

    private IEnumerator Sequence()
    {
        // 0. 初始化
        bubbleImage.SetActive(true);
        bubbleImage.transform.localScale = Vector3.zero;
        textContent.SetActive(true);

        // 文本初始全透明
        CanvasGroup txtCG = textContent.GetComponent<CanvasGroup>();
        if (txtCG == null) txtCG = textContent.AddComponent<CanvasGroup>();
        txtCG.alpha = 0;

        // 1. 气泡从 0 放大到 1.75
        bubbleImage.transform
                   .DOScale((Vector3.one) * 1.75f, popDuration)
                   .SetEase(Ease.OutBack)
                   .SetLink(bubbleImage);   // 目标销毁时自动 Kill Tween

        // 2. 文本 alpha 随气泡放大从 0 到 1
        float fadeDuration = popDuration * 0.2f;
        float fadeStart = popDuration * 0.8f;
        yield return new WaitForSeconds(fadeStart);

        if (txtCG == null) yield break;
        txtCG.DOFade(1f, fadeDuration).SetLink(textContent);

        // 3. 等待展示停留
        yield return new WaitForSeconds(showDuration);

        // 4. 文字逐渐消失
        if (txtCG == null) yield break;
        txtCG.DOFade(0f, textCloseDuration).SetLink(textContent);
        yield return new WaitForSeconds(textCloseDuration);

        // 5. 气泡缩小
        if (bubbleImage == null) yield break;
        yield return bubbleImage.transform
                                .DOScale(Vector3.zero, closeDuration)
                                .SetLink(bubbleImage)
                                .WaitForCompletion();

        // 6. 结束
        if (bubbleImage != null)
            bubbleImage.SetActive(false);
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
        if (bubbleImage != null)
            DOTween.Kill(bubbleImage.transform);
        if (textContent != null)
            DOTween.Kill(textContent.GetComponent<CanvasGroup>());
    }
}
