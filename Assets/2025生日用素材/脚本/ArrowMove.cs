using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArrowMove : MonoBehaviour
{
    [Header("左右摆幅（像素）")]
    [SerializeField] float amplitude = 30f;

    [Header("每秒像素速度")]
    [SerializeField] float speed = 120f;

    RectTransform rect;
    Vector3 startPos;
    float timer;        // 0 → 1 → 0 往复

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        startPos = rect.anchoredPosition;
    }

    void Update()
    {
        timer += speed / amplitude * Time.deltaTime;   // 速率直接关联速度
        float t = Mathf.PingPong(timer, 1f);           // 永远 0~1~0
        rect.anchoredPosition = startPos + Vector3.right * amplitude * (t * 2f - 1f);
    }
}
