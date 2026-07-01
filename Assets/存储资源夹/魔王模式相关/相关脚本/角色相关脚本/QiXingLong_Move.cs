using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QiXingLong_Move : MonoBehaviour
{
    private Animator This_Animator;
    public float InstantiateTime;
    public GameObject LightningFile;
    private void Start()
    {
        This_Animator = GetComponent<Animator>();
    }
    private void Update()
    {
        Aoyi();
    }
    private void Aoyi()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            This_Animator.Play("QiXingLong_Aoyi");
            StartCoroutine(InstanLightningField());
        }

    }
    IEnumerator InstanLightningField()
    {
        yield return new WaitForSeconds(InstantiateTime);
        Vector3 WorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3 HeightPos = new Vector3(WorldPos.x, WorldPos.y, 1f);
        Instantiate(LightningFile, HeightPos, Quaternion.identity);
    }
}
