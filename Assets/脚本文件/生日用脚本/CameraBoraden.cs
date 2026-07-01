using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraBoraden : MonoBehaviour
{
    private CinemachineVirtualCamera cameraComponent;

    // 初始和目标的视野角度
    public float targetFov = 100f; // 目标视野角度
    public float duration = 3f; // 变化持续时间

    void Start()
    {
        cameraComponent = GetComponent<CinemachineVirtualCamera>();
        cameraComponent.m_Lens.FieldOfView = 60; // 设置初始值（可以根据需要调整）
        ChangeFovGradually();
    }

    public void ChangeFovGradually()
    {
        StartCoroutine(ChangeFov(cameraComponent.m_Lens.FieldOfView, targetFov, duration));
    }

    private IEnumerator ChangeFov(float startFov, float endFov, float duration)
    {
        float time = 0;
        while (time < duration)
        {
            time += Time.deltaTime;
            cameraComponent.m_Lens.FieldOfView = Mathf.Lerp(startFov, endFov, time / duration);
            yield return null;
        }
        cameraComponent.m_Lens.FieldOfView = endFov; // 确保最终值正确
    }
}
