using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AoYiShuiJIng_Event : MonoBehaviour
{
    public GameObject TargetShowCharacter;
    public CinemachineVirtualCamera virtualCamera;
    public float targetFov;
    public float duration;

    public float XiaoYeShowTime;
    public float Faster_Speed;
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Haluda")
        {
            ChangeFovGradually();
            collision.GetComponent<BossPathFind>().moveSpeed = 0;
            collision.GetComponent<Animator>().Play("Haluda_cemian_Attack");
            collision.GetComponent<Haluda_Jineng>().Shoot_LightningBall();
            StartCoroutine(XiaoyeShow());
            
        }
    }
    IEnumerator XiaoyeShow()
    {
        yield return new WaitForSeconds(XiaoYeShowTime);
        TargetShowCharacter.SetActive(true);
        TargetShowCharacter.GetComponent<Animator>().Play("XiaoYeCemianAoyi");
    }
    public void ChangeFovGradually()
    {
        virtualCamera.gameObject.GetComponent<BossCameraMove>().dampingSpeed = Faster_Speed;
        virtualCamera.Follow = gameObject.transform;
        StartCoroutine(ChangeFov(virtualCamera.m_Lens.OrthographicSize, targetFov, duration));
    }

    private IEnumerator ChangeFov(float startFov, float endFov, float duration)
    {
        float time = 0;
        while (time < duration)
        {
            time += Time.deltaTime;
            virtualCamera.m_Lens.OrthographicSize = Mathf.Lerp(startFov, endFov, time / duration);
            yield return null;
        }
        virtualCamera.m_Lens.OrthographicSize = endFov; // 确保最终值正确
    }
}
