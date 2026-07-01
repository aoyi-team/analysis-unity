using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LargeMisle_Use : MonoBehaviour
{
    public Animator ThisMisle_Animator;
    public float Destroy_Time;
    void Start()
    {
        if (ThisMisle_Animator != null)
        {
            ThisMisle_Animator.Play("Haluda_LargeMisle");

        }
        Destroy(gameObject, Destroy_Time);
    }

}
