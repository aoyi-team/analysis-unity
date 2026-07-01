using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;


public class jiantou : MonoBehaviour
    
{

    public float xd;
    public float scaleFactor;

    // Update is called once per frame
    void Update()
    {
        Vector3 mouspos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3 direction = (mouspos - transform.position).normalized;
        float angle = 360 - Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;
        transform.eulerAngles = new Vector3(0, 0, angle);
        
        float _mc=Mathf.Sqrt(Mathf.Pow(direction.x,2)+Mathf.Pow(direction.y ,2));
        if (_mc >= 0.5) 
        {
            transform.localScale = new Vector3(1,1,1);
        }
        else transform.localScale = new Vector3(1, _mc * scale, 1);
        
        
    }
    public float scale;
    //箭头的实时方向
}
