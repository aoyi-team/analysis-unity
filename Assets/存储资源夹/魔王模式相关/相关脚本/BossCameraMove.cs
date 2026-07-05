using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class BossCameraMove : MonoBehaviour
{
    public CinemachineVirtualCamera virtualCamera;
    public Transform target1;
    public Transform target2;
    public float dampingSpeed = 2f; // ���Ƹ�����ٶ�
    public float targetFov ; // Ŀ����Ұ�Ƕ�
    public float duration = 3f; // �仯����ʱ��


    [Header("ʱ������")]
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
            // ��̬����Ŀ��Ļ���Ч��
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
        virtualCamera.m_Lens.OrthographicSize = endFov; // ȷ������ֵ��ȷ
    }
}
