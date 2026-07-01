using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class CoinMoveMent : MonoBehaviour
{
    public float NowMoveTime = 0f;
    public float ChangeTimes;
    public  bool IsCanMove = true;
    Rigidbody2D rd;
    void Start()
    {
        rd = gameObject.GetComponent<Rigidbody2D>();
        rd.velocity = new Vector2(Random.Range(-5f, 5f), Random.Range(-5f, 5f));
    }
    private void Update()
    {
        float a = 0f; float b = 0f;
        if (IsCanMove)
        {
            NowMoveTime += Time.deltaTime * ChangeTimes;
            if (rd.velocity.x < 0) a = rd.velocity.x + NowMoveTime;
            else a = rd.velocity.x - NowMoveTime;
            if (rd.velocity.y < 0) b = rd.velocity.y + NowMoveTime;
            else b = rd.velocity.y - NowMoveTime;
            Vector2 newVelocity = new Vector2(a, b);//还差一个绝对值没有求
            gameObject.GetComponent<Rigidbody2D>().velocity = newVelocity;
            if (NowMoveTime >= 5f)
            {
                rd.velocity = Vector2.zero;
                IsCanMove = false;
            }
        }
    }


}
