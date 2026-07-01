using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OutFlameAnim : MonoBehaviour
{
    public float TargetScale = 1.1f;
    public float transitionSpeed = 1f;
    private Vector3 originalScale;
    private float Timer = 0;
    void Start()
    {
        originalScale = transform.localScale;
    }

    // Update is called once per frame
    void Update()
    {
        Timer += Time.deltaTime;
        if (Timer <= 3f)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, originalScale * TargetScale, Time.deltaTime * transitionSpeed);
        }
        else
        {
            transform.localScale = Vector3.Lerp(transform.localScale, originalScale, Time.deltaTime * transitionSpeed);
            if (Timer >= 6f) Timer = 0;
        }
    }
}
