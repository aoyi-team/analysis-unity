using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmallBiaoLeft : MonoBehaviour
{
    public float fadeInDuration;
    public float fadeOutDuration;
    private SpriteRenderer FeibiaoYin;
    private bool IsFading = true;
    private float currentAlpha = 0.7f;
    void Start()
    {
        FeibiaoYin = gameObject.GetComponent<SpriteRenderer>();
    }
    void Update()
    {
        if (IsFading)
        {
            currentAlpha -= Time.deltaTime/fadeInDuration;
            if (currentAlpha <= 0.2f)
            {
                currentAlpha = 0.2f;
                IsFading = false;
            }
        }
        if (!IsFading)
        {
            currentAlpha += Time.deltaTime / fadeOutDuration;
            if (currentAlpha >=0.7f)
            {
                currentAlpha =0.7f;
                IsFading = true;
            }
        }
        Material FeibiaoMat = FeibiaoYin.material;
        Color color = FeibiaoMat.color;
        color.a = currentAlpha;
        FeibiaoMat.color = color;
    }
}
