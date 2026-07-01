using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class BaseButtonBehaviour : MonoBehaviour,IPointerEnterHandler,IPointerExitHandler,IPointerClickHandler
{
    [Header("通用按钮配置")]
    [Tooltip("鼠标悬浮缩放系数")]
    public float scalerFactor = 1.1f;
    [Tooltip("悬浮偏移X")]
    public int OffsetX = 5;
    [Tooltip("悬浮偏移Y")]
    public int OffsetY = 5;

    [Header("动画配置（子类指定）")]
    [Tooltip("鼠标进入时播放的动画名")]
    public string mouseInAnimName;
    [Tooltip("鼠标离开时播放的动画名")]
    public string mouseOutAnimName;

    protected RectTransform _rectTransform;
    protected Animator _animator;
    protected AudioSource _audioSource;

    private Vector2 _originalPos;
    private Vector2 _originalScale;

    protected virtual void Start()
    {
        // 初始化通用组件
        _rectTransform = GetComponent<RectTransform>();
        _animator = GetComponent<Animator>();
        _audioSource = GetComponent<AudioSource>();

        // 记录初始位置和缩放
        _originalPos = _rectTransform.anchoredPosition;
        _originalScale = _rectTransform.localScale;
    }

    /// <summary>
    /// 鼠标进入按钮（通用逻辑）
    /// </summary>
    public virtual void OnPointerEnter(PointerEventData eventData)
    {
        // 缩放+偏移
        Vector2 newPos = _originalPos + new Vector2(OffsetX, OffsetY);
        Vector2 newScale = _originalScale * scalerFactor;
        _rectTransform.anchoredPosition = newPos;
        _rectTransform.localScale = newScale;

        // 播放悬浮动画
        if (_animator != null && !string.IsNullOrEmpty(mouseInAnimName))
        {
            _animator.Play(mouseInAnimName);
        }
    }

    /// <summary>
    /// 鼠标离开按钮（通用逻辑）
    /// </summary>
    public virtual void OnPointerExit(PointerEventData eventData)
    {
        // 恢复初始位置和缩放
        _rectTransform.anchoredPosition = _originalPos;
        _rectTransform.localScale = _originalScale;

        // 播放离开动画
        if (_animator != null && !string.IsNullOrEmpty(mouseOutAnimName))
        {
            _animator.Play(mouseOutAnimName);
        }
    }

    /// <summary>
    /// 按钮点击（通用：播放音效，子类可扩展场景加载等）
    /// </summary>
    public virtual void OnPointerClick(PointerEventData eventData)
    {
        // 播放点击音效（复用原ButtonVoice逻辑）
        if (_audioSource != null && _audioSource.clip != null)
        {
            _audioSource.PlayOneShot(_audioSource.clip);
        }
    }
}
