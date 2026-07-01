using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InstantiateCharacterSystem : MonoBehaviour
{
    public GameObject CharacterSystem;
    private void Update()
    {
        if (CharacterSys() == false)
        {
            Instantiate(CharacterSystem, transform.position, Quaternion.identity);
            
        }
    }
    private bool CharacterSys()
    {
        if (GameObject.FindGameObjectWithTag("CharacterSystem")) return true;
        else return false;
    }
}
