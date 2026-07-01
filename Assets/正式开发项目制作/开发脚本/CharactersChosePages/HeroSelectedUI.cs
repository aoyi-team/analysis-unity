using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 英雄图标缩放逻辑（仅挂载在HeroIcon层）
/// </summary>
[RequireComponent(typeof(Image), typeof(RectTransform))]
public class HeroSelectedUI: MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("缩放设置")]
    public float targetScale = 1.1f;
    public float duration = 0.2f;

    private Vector3 originalScale;
    private Tween currentTween;  // 当前正在执行的动画
    public bool CanScaler = true;
    void Start()
    {
        originalScale = transform.localScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if(CanScaler)
        {
            // 杀死之前的动画，避免冲突
            currentTween?.Kill();

            // 放大动画
            currentTween = transform.DOScale(originalScale * targetScale, duration)
                .SetEase(Ease.OutBack)  // 弹性效果，更自然
                .SetUpdate(true);       // 不受Time.timeScale影响
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if(CanScaler)
        {
            // 杀死之前的动画
            currentTween?.Kill();

            // 还原动画
            currentTween = transform.DOScale(originalScale, duration)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true);
        }
    }
    //外部调用禁用放大功能
    public void StopScaler()
    {
        CanScaler = false;
        currentTween?.Kill();
        transform.localScale = originalScale;
    }
    //外部开启放大功能
    public void TurnOnScaler()
    {
        CanScaler = true;
    }
    // 可选：组件销毁时清理
    void OnDestroy()
    {
        currentTween?.Kill();
    }
}