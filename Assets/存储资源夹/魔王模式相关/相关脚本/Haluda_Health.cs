using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Haluda_Health : MonoBehaviour
{
    public Image[] healthBars; // 血条的图片数组，从上到下依次是红、紫、蓝、绿、黄
    private int CurrentNumber = 0;//当前血条数组位置
    private Image Now_HealthBar;//记录当前控制的血条
    void Start()
    {
        Now_HealthBar = healthBars[CurrentNumber];
    }
    public void Take_Damage(int damage)//damage的数值必须是小于100，最好是小于80
    {
        float Trans_Fillamount = (float)damage / 100;
        if (Trans_Fillamount > Now_HealthBar.fillAmount)
        {
            StartCoroutine(SmoothFill(Now_HealthBar, 0));
        }
        else 
        {
            float Destination = Now_HealthBar.fillAmount - Trans_Fillamount;
            StartCoroutine(SmoothFill(Now_HealthBar, Destination));
        }
    }
    IEnumerator SmoothFill(Image healthBar, float targetFill)
    {
        float duration = 0.5f; // 血条动画持续时间
        float elapsedTime = 0f;
        float startFill = healthBar.fillAmount;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            healthBar.fillAmount = Mathf.Lerp(startFill, targetFill, elapsedTime / duration);
            yield return null; // 等待下一帧
        }
        if (targetFill <= 0.001)
        {
            Now_HealthBar.transform.SetAsFirstSibling();
            Now_HealthBar.fillAmount = 1f;
            if (CurrentNumber == 4) CurrentNumber = 0;
            else CurrentNumber = CurrentNumber % 4 + 1;
            Now_HealthBar = healthBars[CurrentNumber];
        }
        else  healthBar.fillAmount = targetFill; // 确保最终到达目标值
    }
  
}
