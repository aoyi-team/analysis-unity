using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NuoyaAoYi : MonoBehaviour
{
    private  Animation DestroyAnim;
    void Start()
    {
        DestroyAnim = gameObject.GetComponent<Animation>();
        Destroy(gameObject, DestroyAnim.clip.length+0.05f);
    }
}
