using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

public class SFbiao : MonoBehaviour
{
    public GameObject biao;
    private yileHuodong yiledong;
    public float flyspeed = 6f;
    
    void Start()
    {
        yiledong = GetComponent<yileHuodong>();

    }
    public void Smallbiao()
    {

            float[] angles = { 0f, 60f, 120f, 180f, 240f, 300f };
            foreach (float angle in angles)
            {
                Vector3 direction = Quaternion.Euler(0, 0, angle) * Vector3.right;
                Vector2 Playerpo = transform.position;
                GameObject Sbia = Instantiate(biao, Playerpo, Quaternion.identity);
                Rigidbody2D Fei = Sbia.GetComponent<Rigidbody2D>();
                Fei.velocity = direction * flyspeed;
            }//Ð¡·ÉïÚÉú³É

    }

}
