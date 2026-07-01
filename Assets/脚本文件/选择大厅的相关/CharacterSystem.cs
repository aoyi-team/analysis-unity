using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CharacterSystem : MonoBehaviour
{
    public GameObject[] Characters;
    public int CharacterNumber=0;
    public string Name;
    void Start()
    {
        DontDestroyOnLoad(gameObject);
    }
}
