using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollowTarget : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float triggerRadius = 3f;      // 触发偏移的圆半径
    [SerializeField] private float maxOffsetDistance = 2f;  // 最大偏移距离
    [SerializeField] private float smoothSpeed = 5f;       // 移动平滑度

    public Transform playerTransform;    // 人物 Transform
    private Vector3 targetPosition;       // 目标位置
    public Camera mainCamera;

    private void Awake()
    {
        playerTransform = transform.parent; // 假设脚本挂在人物子物体上
        mainCamera = Camera.main;
    }

    private void Update()
    {
        // 获取鼠标世界坐标（2D空间）
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;

        // 计算人物到鼠标的向量
        Vector3 playerToMouse = mouseWorldPos - playerTransform.position;

        // 计算鼠标到人物的距离
        float mouseDistance = playerToMouse.magnitude;

        if (mouseDistance > triggerRadius)
        {
            // 计算超出触发半径的比例
            float exceedRatio = (mouseDistance - triggerRadius) / triggerRadius;

            // 计算目标偏移（限制最大偏移距离）
            Vector3 offset = playerToMouse.normalized *
                            Mathf.Min(maxOffsetDistance, exceedRatio * maxOffsetDistance);

            targetPosition = playerTransform.position + offset;
        }
        else
        {
            targetPosition = playerTransform.position;
        }

        // 平滑移动跟随目标位置
        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            Time.deltaTime * smoothSpeed
        );
    }

    // 可视化调试范围
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.parent.position, triggerRadius);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.2f);
    }
}
