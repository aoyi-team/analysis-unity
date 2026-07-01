using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//对话内容整体
[Serializable]
public class DialogueContent
{
    public int SpeakerId;//(0战无炎)1角色群像第一版)2角色群像第二版)
    public string textContent;//对话内容

    [Header("文本动画参数")]
    public float typewriterSpeed = 0.05f; // 逐字显示速度
    public float fadeOutDelay = 2f; // 显示完毕后延迟渐隐时间
    public float fadeOutSpeed = 0.02f; // 文字渐隐速度

    [Header("人物海报动画参数")]
    public float slowMoveDuration = 1f; // 慢速左移时长
    public float fastThrowDuration = 0.5f; // 快速右甩时长
    public Vector2 slowMoveOffset = new Vector2(-150, 0); // 慢速左移偏移（UI坐标）
    public Vector2 fastThrowOffset = new Vector2(1200, 0); // 快速右甩偏移（超出屏幕）
}
public class DialogueDataManager : MonoBehaviour
{
    public GameObject Characters;//四个静态角色
    [Header("核心配置")]
    public DialogueContent[] dialogueConfigs; // 编辑器配置的对话列表
    public Dictionary<int, DialogueContent> Dialogues; // 对话字典（按序号索引）
    public GameObject[] CharactersGameObjects; // 替换Image[]：0=战无炎,1=群像1,2=群像2（整 GameObject）
    public Text dialogueText; // 对话文本显示组件
    public Text Name;
    public GameObject DialogueOutline; // 对话框（需自动关闭）

    [Header("全局开关")]
    public bool isAutoHideCharacter = true; // 文本结束后是否自动隐藏人物
    public bool isAutoCloseDialogue = true; // 对话结束后是否自动关闭对话框

    // 协程控制
    private Coroutine _textCoroutine;
    private Coroutine _characterCoroutine;
    private int _currentDialogueId; // 当前播放的对话ID

    [Header("初始化信息")]
    public float DefaultX;//初始X坐标
    public float HideCharacterPosterTimeFactor;//隐藏海报的时间参数

    private void Awake()
    {
        // 初始化对话字典
        InitDialogueDictionary();
        // 初始化人物海报和对话框状态
        InitCharacterAndDialogue();
    }

    #region 初始化方法
    /// <summary>
    /// 初始化对话字典（从编辑器配置加载）
    /// </summary>
    private void InitDialogueDictionary()
    {
        Dialogues = new Dictionary<int, DialogueContent>();
        for (int i = 0; i < dialogueConfigs.Length; i++)
        {
            if (!Dialogues.ContainsKey(i))
            {
                Dialogues.Add(i, dialogueConfigs[i]);
            }
            else
            {
                Debug.LogWarning($"重复的对话序号：{i}，已跳过");
            }
        }
    }

    /// <summary>
    /// 初始化人物海报和对话框（隐藏+归位）
    /// </summary>
    private void InitCharacterAndDialogue()
    {
        foreach (var go in CharactersGameObjects)
        {
            if (go != null)
            {
                go.SetActive(false);
                // 重置海报位置到屏幕右侧外（获取RectTransform组件）
                RectTransform rt = go.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchoredPosition = new Vector2(DefaultX, rt.anchoredPosition.y);
                }
            }
        }

        // 初始化对话框
        if (DialogueOutline != null)
        {
            DialogueOutline.SetActive(false);
        }
        else
        {
            Debug.LogError("对话框DialogueOutline未赋值！");
        }

        // 初始化文本
        if (dialogueText != null)
        {
            dialogueText.text = "";
            dialogueText.color = new Color(dialogueText.color.r, dialogueText.color.g, dialogueText.color.b, 1);
        }
        if(Name!=null)
        {
            Name.text = "";
        }
    }
    #endregion

    #region 外部调用核心方法
    /// <summary>
    /// 外部按钮触发：播放指定序号的对话
    /// </summary>
    /// <param name="number">对话字典的序号（从0开始）</param>
    public void UseDialogue(int number)
    {
        // 校验参数
        if (!Dialogues.ContainsKey(number))
        {
            Debug.LogError($"不存在序号为{number}的对话！");
            return;
        }

        // 停止当前正在播放的动画
        StopAllCoroutines();
        _textCoroutine = null;
        _characterCoroutine = null;

        // 显示对话框（讲话前先打开）
        DialogueOutline.SetActive(true);


        DialogueContent currentDialogue = Dialogues[number];
        int speakerID = currentDialogue.SpeakerId;
        //显示人物名字
        if(speakerID == 2)
        {
            Name.text = "大家";
        }
        else Name.text = speakerID == 0 ? "战无炎" : "?";
        // 1. 先播放人物海报动画
        _characterCoroutine = StartCoroutine(ShowCharacter(currentDialogue));
        // 2. 延迟0.5秒播放文本动画（匹配人物移动节奏）
        _textCoroutine = StartCoroutine(DelayPlayText(currentDialogue, 0.5f));
    }
    #endregion

    #region 文本动画（逐字渐显+整体渐隐）
    private IEnumerator DelayPlayText(DialogueContent dialogue, float delay)
    {
        yield return new WaitForSeconds(delay);
        yield return ShowText(dialogue);
    }

    private IEnumerator ShowText(DialogueContent dialogue)
    {
        // 重置文本状态
        dialogueText.text = "";
        dialogueText.color = new Color(dialogueText.color.r, dialogueText.color.g, dialogueText.color.b, 1);
        string targetText = dialogue.textContent;

        // 逐字渐显（打字机效果）
        for (int i = 0; i < targetText.Length; i++)
        {
            dialogueText.text += targetText[i];
            yield return new WaitForSeconds(dialogue.typewriterSpeed);
        }

        // 文本显示完毕后，等待指定时间再渐隐
        yield return new WaitForSeconds(dialogue.fadeOutDelay);

        // 整体渐隐
        while (dialogueText.color.a > 0)
        {
            dialogueText.color = new Color(
                dialogueText.color.r,
                dialogueText.color.g,
                dialogueText.color.b,
                dialogueText.color.a - dialogue.fadeOutSpeed
            );
            yield return null;
        }

        // 重置文本
        dialogueText.text = "";
        dialogueText.color = new Color(dialogueText.color.r, dialogueText.color.g, dialogueText.color.b, 1);

        // 对话结束后关闭对话框
        if (isAutoCloseDialogue && DialogueOutline != null)
        {
            DialogueOutline.SetActive(false);
        }
    }
    #endregion

    #region 人物海报动画（慢右移 → 快速左甩）
    private IEnumerator ShowCharacter(DialogueContent dialogue)
    {
        // 1. 获取当前说话人对应的海报GameObject
        GameObject targetGo = GetSpeakerGameObject(dialogue.SpeakerId);
        if (targetGo == null) yield break;

        // 2. 显示海报并重置初始位置（屏幕右侧外）
        targetGo.SetActive(true);
        RectTransform rt = targetGo.GetComponent<RectTransform>();
        if (rt == null)
        {
            Debug.LogError($"角色海报{targetGo.name}缺少RectTransform组件！");
            yield break;
        }
        Vector2 originalPos = rt.anchoredPosition;
        rt.anchoredPosition = dialogue.fastThrowOffset;

        // 3. 第一步：慢速向右移动（缓动效果更自然）
        float slowTimer = 0;
        while (slowTimer < dialogue.slowMoveDuration)
        {
            slowTimer += Time.deltaTime;
            float progress = Mathf.SmoothStep(0, 1, slowTimer / dialogue.slowMoveDuration);
            rt.anchoredPosition = Vector2.Lerp(
                dialogue.fastThrowOffset,
                originalPos + dialogue.slowMoveOffset,
                progress
            );
            yield return null;
        }

        // 4. 停留（直到文本动画完全结束 + 渐隐完成 + 对话框关闭）
        float waitTime = dialogue.textContent.Length * dialogue.typewriterSpeed + dialogue.fadeOutDelay-HideCharacterPosterTimeFactor;
        yield return new WaitForSeconds(waitTime);

        // 5. 第二步：快速向右甩出（退出屏幕）
        if (isAutoHideCharacter)
        {
            float fastTimer = 0;
            while (fastTimer < dialogue.fastThrowDuration)
            {
                fastTimer += Time.deltaTime;
                float progress = Mathf.SmoothStep(0, 1, fastTimer / dialogue.fastThrowDuration);
                rt.anchoredPosition = Vector2.Lerp(
                    originalPos + dialogue.slowMoveOffset,
                    dialogue.fastThrowOffset,
                    progress
                );
                yield return null;
            }

            // 6. 隐藏海报并重置位置
            targetGo.SetActive(false);
            rt.anchoredPosition = originalPos;
        }
    }

    /// <summary>
    /// 根据SpeakerId获取对应的角色海报GameObject
    /// </summary>
    private GameObject GetSpeakerGameObject(int speakerId)
    {
        if (speakerId < 0 || speakerId >= CharactersGameObjects.Length)
        {
            Debug.LogError($"SpeakerId={speakerId}超出海报数组范围！");
            return null;
        }

        GameObject go = CharactersGameObjects[speakerId];
        if (go == null)
        {
            Debug.LogError($"SpeakerId={speakerId}对应的海报GameObject未赋值！");
            return null;
        }
        return go;
    }
    #endregion

    #region 辅助方法
    /// <summary>
    /// 外部调用：强制停止所有对话动画
    /// </summary>
    public void StopAllDialogue()
    {
        StopAllCoroutines();
        _textCoroutine = null;
        _characterCoroutine = null;

        // 重置文本
        if (dialogueText != null)
        {
            dialogueText.text = "";
            dialogueText.color = new Color(dialogueText.color.r, dialogueText.color.g, dialogueText.color.b, 1);
        }

        // 关闭对话框
        if (DialogueOutline != null)
        {
            DialogueOutline.SetActive(false);
        }

        // 重置人物海报
        if (CharactersGameObjects != null)
        {
            foreach (var go in CharactersGameObjects)
            {
                if (go != null)
                {
                    go.SetActive(false);
                    RectTransform rt = go.GetComponent<RectTransform>();
                    if (rt != null)
                    {
                        rt.anchoredPosition = new Vector2(1200, rt.anchoredPosition.y);
                    }
                }
            }
        }
    }
    #endregion
}
