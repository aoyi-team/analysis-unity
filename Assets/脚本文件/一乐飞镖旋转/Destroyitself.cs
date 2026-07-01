using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Destroyitself : MonoBehaviour
{
    Animation anim;
    void Start()
    {
        anim = gameObject.GetComponent<Animation>();
        Destroy(gameObject, anim.clip.length);
        
    }

}
