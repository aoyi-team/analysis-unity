using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DishitanAttackVoice : MonoBehaviour
{
    void Start()
    {
        Destroy(gameObject, GetComponent<AudioSource>().clip.length + 0.02f);
    }

}
