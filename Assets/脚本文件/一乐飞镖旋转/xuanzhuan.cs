using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class xuanzhuan : MonoBehaviour
{
    // Start is called before the first frame update
    private  float zAngle;
    public int dShu;
    public Vector2 speedVelocity;
    public bool first = true;
    private float flytime;
    public string PlayerName;//记录飞镖是由谁扔出来的
   
    
    private void Start()
    {
        speedVelocity = gameObject.GetComponent<Rigidbody2D>().velocity;
        

    }

    // Update is called once per frame
    private void FixedUpdate()//大飞镖的旋转和发射收回
    {
        if (first) 
        {
            flytime += Time.deltaTime;
            if (flytime >= 0.60f)
            {
                speedVelocity = gameObject.GetComponent<Rigidbody2D>().velocity = (GameObject.FindGameObjectWithTag("Characters").transform.position - transform.position).normalized ;
                first = false;

            }
          
        }
        if (!first)
        {
            Vector3 GamPos = GameObject.FindGameObjectWithTag("Characters").transform.position;
            GamPos.z = 0;
            float distance = Vector2.Distance(GamPos, transform.position);
            if (distance <= 1f) Destroy(gameObject);
            Vector2 dir = new Vector2(GamPos.x - transform.position.x, GamPos.y - transform.position.y).normalized * Time.deltaTime;
            transform.Translate(dir * Time.fixedDeltaTime * 450,Space.World );
        }
        zAngle += dShu * Time.deltaTime;
        transform.rotation = Quaternion.Euler(0, 0, zAngle);


    }
}
