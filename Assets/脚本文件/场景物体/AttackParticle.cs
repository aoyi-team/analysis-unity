using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackParticle : MonoBehaviour
{
    void Start()
    {
        Destroy(gameObject, 0.31f);
    }
}
