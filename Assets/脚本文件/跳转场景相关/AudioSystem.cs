using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioSystem : MonoBehaviour
{
    public AudioClip[] BGMs;
    private AudioSource BgmAudio;
    public static bool IsPlayingMusic = false;
    void Start()
    {
        DontDestroyOnLoad(gameObject);
        if (!IsPlayingMusic)
        {
            IsPlayingMusic = true;
            BgmAudio = gameObject.GetComponent<AudioSource>();
            BgmAudio.clip = BGMs[0];
            BgmAudio.Play();
        }
    }

}
