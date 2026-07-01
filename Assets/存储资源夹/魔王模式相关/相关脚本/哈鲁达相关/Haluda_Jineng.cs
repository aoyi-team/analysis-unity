using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Haluda_Jineng : MonoBehaviour
{
    [Header("闪电球发射相关")]
    public GameObject Lightning_Ball;
    public Transform AoyiZhiShi;
    public float LightningBall_Speed;

    [Header("导弹发射相关")]
    public GameObject LargeMisle;
    public Transform areaCenter; // 区域中心点
    public Vector2 areaSize;// 区域的宽度和高度
    public int Max_Fire_Misle_Number;
    public float Total_JianGe_Time;
    public float Each_Jiange_Time;

    private float Timer = 0f;
    private void Update()
    {
        Timer += Time.deltaTime;
        if(Timer>= Total_JianGe_Time)
        {
            StartCoroutine(Shoot_Misle());
            Timer = 0f;
        }
    }
    IEnumerator Shoot_Misle()
    {
        int Fire_Number = Random.Range(3, Max_Fire_Misle_Number);
        for (int i = 0; i < Fire_Number; i++)
        {
            // 计算随机的x和y坐标
            float randomX = Random.Range(areaCenter.position.x - areaSize.x / 2, areaCenter.position.x + areaSize.x / 2);
            float randomY = Random.Range(areaCenter.position.y - areaSize.y / 2, areaCenter.position.y + areaSize.y / 2);

            // 生成随机坐标点
            Vector2 randomPosition = new Vector2(randomX, randomY);

            // 在该坐标点生成物体
            Instantiate(LargeMisle, randomPosition, Quaternion.identity);
            yield return new WaitForSeconds(Each_Jiange_Time);
        }
    }

    public void Shoot_LightningBall()
    {
        Vector3 ShuiJin_Pos = AoyiZhiShi.position;
        ShuiJin_Pos.z = 0;
        Vector3 Direction = ShuiJin_Pos - transform.position;
        GameObject LightningBall_01 = Instantiate(Lightning_Ball, transform.position + Direction.normalized*1f, Quaternion.identity);
        LightningBall_01.GetComponent<Rigidbody2D>().velocity = Direction.normalized * LightningBall_Speed;
    }
}
