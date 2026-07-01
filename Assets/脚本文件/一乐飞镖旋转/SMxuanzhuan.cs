using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SMxuanzhuan : MonoBehaviour
{
    private float zAngle;
    public int dShu;
    // Start is called before the first frame update
    void Start()
    {
        Destroy(gameObject, 0.6f);
    }

    // Update is called once per frame
    void Update()
    {
        zAngle += dShu * Time.deltaTime;
        transform.rotation = Quaternion.Euler(0, 0, zAngle);
    }//Ð¡·ÉïÚ×Ô×ª
}
