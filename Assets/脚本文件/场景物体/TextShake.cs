using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TextShake : MonoBehaviour
{//制作伤害文字的动画效果
    private Vector3 OriginatalPosition;
    private float TimeElapsed = 0f;//渐变时间计时器
    private Vector3 OriginalScale;
    public float shakeStrength = 10.0f; // 水平抖动的强度
    public float shakeFrequency = 5.0f; // 水平抖动的频率
    public float maxScale = 1.1f; // 最大缩放比例
    public float duration = 0.5f; // 效果持续时间
    [Header("放大时间分数(数字越大越快)")]
    public int ScalerNum=2;
    private float Half_Duration;
    private void Start()
    {
        OriginalScale = transform.localScale;
        transform.localScale *= 0.5f;
        OriginatalPosition = transform.position;
        Half_Duration = duration / ScalerNum;
        
    }
    void Update()
    {
        float shakeOffset = Mathf.Sin(Time.time * shakeFrequency) * shakeStrength;
        transform.position = OriginatalPosition + new Vector3(shakeOffset, 0, 0);
        if (TimeElapsed >= 0 && TimeElapsed < Half_Duration)
        {
            transform.localScale = Vector3.Lerp(OriginalScale*0.5f, OriginalScale * maxScale, TimeElapsed/Half_Duration);
        }
        TimeElapsed += Time.deltaTime;
        Delete_WenZi();
    }
    void Delete_WenZi()
    {
        if (TimeElapsed >= duration) Destroy(gameObject);
    }

}
