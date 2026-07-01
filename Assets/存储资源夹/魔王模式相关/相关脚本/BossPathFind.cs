using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossPathFind : MonoBehaviour
{
    public Transform[] pathPoints;  // 路径点数组
    public float moveSpeed = 3f;    // Boss移动速度
    private int currentPointIndex = 0; // 当前目标路径点的索引

    void Update()
    {
        if (pathPoints.Length == 0) return;  // 如果路径点为空，则返回

        // 获取当前位置与目标路径点之间的方向
        Vector2 direction = (pathPoints[currentPointIndex].position - transform.position).normalized;

        // 移动Boss
        transform.position = Vector2.MoveTowards(transform.position, pathPoints[currentPointIndex].position, moveSpeed * Time.deltaTime);

        // 检查是否到达目标路径点
        if (Vector2.Distance(transform.position, pathPoints[currentPointIndex].position) < 0.1f)
        {
            // 如果到达目标点，则更新目标点为下一个路径点
            currentPointIndex = (currentPointIndex + 1) % pathPoints.Length;
        }
    }
}
