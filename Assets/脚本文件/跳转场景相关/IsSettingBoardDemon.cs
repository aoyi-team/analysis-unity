using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class IsSettingBoardDemon : MonoBehaviour
{
    public GameObject SettingCanvas;
    public Slider VolumeSlider;
    private GameObject AudioSYS;
    private void Start()
    {
        DontDestroyOnLoad(gameObject);
    }
    void Update()
    {
        CanDemon();
        if (FindAudioSystem() == true)
        {
            AudioSYS.GetComponent<AudioSource>().volume = VolumeSlider.value;
        }
    }
    private void CanDemon()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (SettingCanvas.activeSelf)
            {
                SettingCanvas.SetActive(false);
            }
            else { SettingCanvas.SetActive(true);}
        }
    }
    private  bool FindAudioSystem()//找到音频系统并且可以做出调节
    {
        if (AudioSYS= GameObject.FindGameObjectWithTag("AudioSystem")) { return true;}
        else return false;
    }
}
