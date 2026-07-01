using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class InstiateCharacter : MonoBehaviour
{
    private bool IsFindSystem = false;
    private bool IsFindAudio = false;
    public GameObject[] BornPoints;
    public GameObject[] TeamsAdd; 
    void Start()
    {
        
    }
    private void Update()
    {
        if (!IsFindSystem)
        {
            if (GameObject.FindGameObjectWithTag("CharacterSystem"))
            {
                IsFindSystem = true;
                GameObject targetsystem = GameObject.FindGameObjectWithTag("CharacterSystem");
                int i = targetsystem.GetComponent<CharacterSystem>().CharacterNumber;
                GameObject ChoosedCharacter = targetsystem.GetComponent<CharacterSystem>().Characters[i];
                GameObject Character= Instantiate(ChoosedCharacter, BornPoints[0].transform.position, Quaternion.identity);
                GameObject.FindGameObjectWithTag("PlayerName").GetComponent<Text>().text = GameObject.FindGameObjectWithTag("CharacterSystem").GetComponent<CharacterSystem>().Name;
                GameObject.FindGameObjectWithTag("ScoredBoardName").GetComponent<Text>().text = GameObject.FindGameObjectWithTag("CharacterSystem").GetComponent<CharacterSystem>().Name; 
            }
            TeamsAdd[0].GetComponent<TeamMarkTotal>().enabled = true;
        }
        if (!IsFindAudio)
        {
            if (GameObject.FindGameObjectWithTag("AudioSystem"))
            {
                IsFindAudio = true;
                GameObject AudioSys = GameObject.FindGameObjectWithTag("AudioSystem");
                AudioClip RankBGM = AudioSys.GetComponent<AudioSystem>().BGMs[1];
                AudioSource ThisAudio = AudioSys.GetComponent<AudioSource>();
                ThisAudio.clip = RankBGM;
                ThisAudio.Play();

                
            }
        }
    }
}
