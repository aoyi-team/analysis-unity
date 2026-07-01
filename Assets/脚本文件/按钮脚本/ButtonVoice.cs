using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonVoice : MonoBehaviour
{
    private AudioSource ButtonSource;
    private void Start()
    {
        ButtonSource = gameObject.GetComponent<AudioSource>();
    }
    public void ButtonVoicePlay()
    {
        ButtonSource.PlayOneShot(ButtonSource.clip);
    }
}
