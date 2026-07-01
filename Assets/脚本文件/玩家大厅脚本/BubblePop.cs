using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
//用于显示气泡信息框提示效果和文字渐显
public class BubblePop : MonoBehaviour
{
    public GameObject bubbleImage;//气泡图片
    public GameObject textContent;//文字
    [Header("动画时长")]
    public float popDuration = 0.4f;
    public float showDuration = 3f;
    public float closeDuration = 0.3f;
    public float textCloseDuration = 0.4f;
    public float repeatTime = 10.0f;

    private void Start()
    {
        InvokeRepeating(nameof(StartSequence), 5f, repeatTime);
    }
    private void StartSequence()
    {
        StopAllCoroutines();          // 防止重叠
        StartCoroutine(Sequence());
    }
    private IEnumerator Sequence()
    {
        // 0. 激活
        bubbleImage.SetActive(true);
        bubbleImage.transform.localScale = Vector3.zero;
        textContent.SetActive(true);

        // 文字先完全透明
        CanvasGroup txtCG = textContent.GetComponent<CanvasGroup>();
        if (txtCG == null) txtCG = textContent.AddComponent<CanvasGroup>();
        txtCG.alpha = 0;

        // 1. 气泡 0→1.75
        bubbleImage.transform
                   .DOScale((Vector3.one)*1.75f, popDuration)
                   .SetEase(Ease.OutBack);   // 可选弹性

        // 2. 文字 alpha 动画：在 1.2→1.75 的区间里 0→1
        //    用 DOVirtual 实现
        float fadeDuration = popDuration * 0.2f;      // 20 % 时长
        float fadeStart = popDuration * 0.8f;      // 0.8 处开始
        yield return new WaitForSeconds(fadeStart);

        txtCG.DOFade(1f, fadeDuration);

        // 3. 等待文字停留
        yield return new WaitForSeconds(showDuration);

        // 4. 文字逐渐消失
        txtCG.DOFade(0f, textCloseDuration);
        yield return new WaitForSeconds(textCloseDuration);

        // 5. 气泡缩回
        yield return bubbleImage.transform
                                .DOScale(Vector3.zero, closeDuration)
                                .WaitForCompletion();

        // 6. 隐藏
        bubbleImage.SetActive(false);
    }
}
