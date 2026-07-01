using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class BackToHallOrGame : MonoBehaviour
{
    public GameObject SettingBoard;
    private bool hasExeCutedValue = AudioSystem.IsPlayingMusic;
    public void ReturnToGame()
    {
        SettingBoard.SetActive(false);
    }
    public void BackToHall()
    {
        GameObject CharacterSystem = GameObject.FindGameObjectWithTag("CharacterSystem");
        GameObject AudioSystem = GameObject.FindGameObjectWithTag("AudioSystem");
        hasExeCutedValue = false;
        Destroy(CharacterSystem);
        Destroy(AudioSystem);
        SettingBoard.SetActive(false);
        SceneManager.LoadScene(0);
    }
}
