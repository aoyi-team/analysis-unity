using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using Photon.Pun.Demo.PunBasics;

public class BossCameraMove : MonoBehaviour
{
    public CinemachineVirtualCamera virtualCamera;
    public Transform target1;
    public Transform target2;
    public float dampingSpeed = 2f; // 控制跟随的速度
    public float targetFov ; // 目标视野角度
    public float duration = 3f; // 变化持续时间


    [Header("时间因素")]
    public float TimeFactor=1f;

    private CinemachineFramingTransposer framingTransposer;
    private bool isSwitchingTarget = false;
    public GameObject BossUI;
    public float ShowUiTime;
    public float ToMoveCamera_Time;
    public float CloseUi_Time;
    void Start()
    {
        framingTransposer = virtualCamera.GetCinemachineComponent<CinemachineFramingTransposer>();
        StartCoroutine(StartShowUI());
    }
    IEnumerator StartShowUI()
    {
        yield return new WaitForSeconds(ShowUiTime);
        BossUI.SetActive(true);
        StartCoroutine(StarMove_Camera());

    }
    IEnumerator StarMove_Camera()
    {
        yield return new WaitForSeconds(ToMoveCamera_Time);
        SwitchTarget();
        yield return new WaitForSeconds(CloseUi_Time);
        BossUI.SetActive(false);
    }
    void Update()
    {
        if (isSwitchingTarget)
        {
            // 动态控制目标的缓动效果
            framingTransposer.m_YDamping = Mathf.Lerp(framingTransposer.m_YDamping, dampingSpeed, Time.deltaTime*TimeFactor);
            framingTransposer.m_XDamping = Mathf.Lerp(framingTransposer.m_XDamping, dampingSpeed, Time.deltaTime * TimeFactor);
            framingTransposer.m_ZDamping = Mathf.Lerp(framingTransposer.m_ZDamping, dampingSpeed, Time.deltaTime * TimeFactor);
        }
    }

    void SwitchTarget()
    {
        ChangeFovGradually();
        if (virtualCamera.Follow == target1)
        {
            virtualCamera.Follow = target2;
        }
        else
        {
            virtualCamera.Follow = target1;
        }

        isSwitchingTarget = true;
    }
    public void ChangeFovGradually()
    {
        StartCoroutine(ChangeFov(virtualCamera.m_Lens.OrthographicSize, targetFov, duration));
    }

    private IEnumerator ChangeFov(float startFov, float endFov, float duration)
    {
        float time = 0;
        while (time < duration)
        {
            time += Time.deltaTime;
            virtualCamera.m_Lens.OrthographicSize=Mathf.Lerp(startFov, endFov, time / duration);
            yield return null;
        }
        virtualCamera.m_Lens.OrthographicSize = endFov; // 确保最终值正确
    }
}
