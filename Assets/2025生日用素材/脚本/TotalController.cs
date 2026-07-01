using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[Serializable]
public class ShowSequenceCommand//表演指令
{
    public int roleID;//演奏ID(太二0,龙炎1,阿楚2,悟空3,潘多拉4,碧霄5)
    public float delayAfterThis;//该条指令之后该延迟的时间
}
public class TotalController : MonoBehaviour
{
    [Header("总延时")]
    public float TotalPreTime;//调整最后一段一起演唱部分
    public float[] LastFiveTimes;

    public DialogueDataManager dialogueDataManager;
    [Header("角色演奏指令列表")]
    public ShowSequenceCommand[] SequenceCommands;

    [Header("出演角色&&箱子")]
    public GameObject[] Characters;
    public GameObject AllCharas;
    public GameObject Boxes;

    [Header("观众")]
    public GameObject[] Watchers;

    public AudioClip birthdayBGM; // 生日快乐歌音频
    public AudioSource audioSource; // 音频播放源（可拖入或动态创建）

    [Header("前奏时间")]
    [Range(0,max:15)]
    public float PreTime=0;

    private int Times = 0;

    public void ShowAllCharacters()
    {
        AllCharas.SetActive(true);
        Boxes.SetActive(true);
    }
    private void Start()
    {
        // 自动创建音频源（若未赋值）
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.loop = false; // 生日歌只播放一次
            audioSource.clip = birthdayBGM;
        }
    }
    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Space))
        {

            dialogueDataManager.UseDialogue(Times);
            Times++;
        }
        if(Input.GetKeyDown(KeyCode.K))//演奏音乐
        {
            StartBirthdayShow();
        }
    }
    public void StartBirthdayShow()
    {
        ShowAllCharacters();
        // 播放BGM
        audioSource.Play();
        // 执行序列指令
        StartCoroutine(ExecuteSequenceCoroutine());
    }
    //指令协程
    private IEnumerator ExecuteSequenceCoroutine()
    {
        yield return new WaitForSeconds(PreTime);
        // 遍历每一条演出指令
        foreach (var cmd in SequenceCommands)
        {

            // 通过roleID找到对应角色配置（确保角色-单字固定映射）
            if (Characters[cmd.roleID]!=null)
            {
                // 触发角色攻击+爆字逻辑
                Characters[cmd.roleID].GetComponent<BaseCharController>().Attack();
            }
            else
            {
                Debug.LogError($"未找到角色配置");
            }
            // 等待指令延迟时间
            yield return new WaitForSeconds(cmd.delayAfterThis);
        }
    }
    IEnumerator TogetherPlay()
    {
        yield return new WaitForSeconds (TotalPreTime);
        int i= 0;
        foreach(var Player in Characters)
        {
            Player.GetComponent<BaseCharController>().Attack();
            yield return new WaitForSeconds(LastFiveTimes[i]);
            i++;
        }
    }
}
