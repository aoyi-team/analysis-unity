using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class LevelUpNotTurn : MonoBehaviour
{
    GameObject FatherGameObject;
    private void Start()
    {
        FatherGameObject = transform.parent.gameObject;
    }
    void Update()
    {
        gameObject.transform.localScale = FatherGameObject.transform.localScale;
    }

}
