using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class XiaoYe_Move : MonoBehaviour
{
    public GameObject[] Xiaoye_MofaZhen;
    public float Jiange_Time;
    public CinemachineVirtualCamera MainCamera;
    public Transform AoyiShuiJin;
    void Start()
    {
        StartCoroutine(Turns_To_Show());
    }
    IEnumerator Turns_To_Show()
    {
        yield return new WaitForSeconds(0.5f);
        foreach (GameObject Mofazhen in Xiaoye_MofaZhen)
        {
            Mofazhen.SetActive(true);
            MainCamera.Follow = Mofazhen.transform;
            yield return new WaitForSeconds(Jiange_Time);
        }
        yield return new WaitForSeconds(0.1f);
        MainCamera.Follow = AoyiShuiJin;
    }
}
