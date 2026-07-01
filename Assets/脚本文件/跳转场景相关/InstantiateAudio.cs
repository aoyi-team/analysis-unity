using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InstantiateAudio : MonoBehaviour
{
    public  GameObject AudioSystem;
    public GameObject SettingSystem;
    private void Start()
    {
        StartCoroutine(IntiateAudioSystem());
    }
    IEnumerator IntiateAudioSystem()
    {
        yield return new WaitForSeconds(0.05f);
        AudioSource AudioSYS;
        if (FindAudioSystem() == false)
        {
            GameObject AudioS = Instantiate(AudioSystem, transform.position, Quaternion.identity);
            AudioSYS = AudioS.GetComponent<AudioSource>();
            AudioSYS.Play();
        }
        else AudioSYS = GameObject.FindGameObjectWithTag("AudioSystem").GetComponent<AudioSource>();
        if (FindSettingSystem() == false)
        {
            GameObject SettingSYS= Instantiate(SettingSystem, transform.position, Quaternion.identity);
            SettingSYS.GetComponent<IsSettingBoardDemon>().VolumeSlider.value = AudioSYS.volume;
        }
    }
    private bool FindAudioSystem()
    {
        if (GameObject.FindGameObjectWithTag("AudioSystem")) return true;
        else return false;
    }
    private bool FindSettingSystem()
    {
        if (GameObject.FindGameObjectWithTag("SettingSystem")) return true;
        else return false;
    }
}
