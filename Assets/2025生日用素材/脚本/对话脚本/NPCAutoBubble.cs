using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class NPCBubbleConfig
{
    [Header("气泡UI组件")]
    public GameObject bubbleRoot; // 气泡根节点（NPCBubble）
    public CanvasGroup bubbleBgCanvasGroup; // 气泡背景的CanvasGroup
    public CanvasGroup bubbleTextCanvasGroup; // 气泡文字的CanvasGroup
    public Text bubbleText; // 文字组件（若用TMP可替换为TMPro.TMP_Text）

    [Header("动画时间配置")]
    public float fadeInDuration = 0.3f; // 气泡/文字渐显时长（建议0.2~0.5秒）
    public float showDuration = 1.5f; // 气泡显示时长（固定1.5秒，可自定义）
    public float fadeOutDuration = 0.3f; // 气泡/文字渐隐时长（建议0.2~0.5秒）

    [Header("文案配置")]
    public string[] bubbleContentList; // 自定义自言自语文案池（可添加多条）
}

public class NPCAutoBubble : MonoBehaviour
{
    [Header("核心配置")]
    public NPCBubbleConfig bubbleConfig; // 气泡配置
    public float minTriggerInterval = 2f; // 最小触发间隔（2秒）
    public float maxTriggerInterval = 4f; // 最大触发间隔（4秒）
    public bool isAutoStart = true; // 是否启动后自动开始触发气泡

    private bool isBubblePlaying = false; // 是否正在播放气泡动画（防止叠加）
    private Coroutine bubbleCoroutine; // 气泡协程缓存

    private void Awake()
    {
        // 校验组件
        if (bubbleConfig.bubbleRoot == null || bubbleConfig.bubbleBgCanvasGroup == null ||
            bubbleConfig.bubbleTextCanvasGroup == null || bubbleConfig.bubbleText == null)
        {
            Debug.LogError("气泡UI组件未完整赋值！");
            enabled = false;
            return;
        }

        // 初始化气泡状态
        bubbleConfig.bubbleRoot.SetActive(false);
        bubbleConfig.bubbleBgCanvasGroup.alpha = 0;
        bubbleConfig.bubbleTextCanvasGroup.alpha = 0;

        // 校验文案池
        if (bubbleConfig.bubbleContentList == null || bubbleConfig.bubbleContentList.Length == 0)
        {
            Debug.LogWarning("气泡文案池为空，请添加自定义内容！");
            // 默认添加测试文案
            bubbleConfig.bubbleContentList = new string[] { "今天天气不错~", "好无聊啊...", "不知道什么时候能任务完成~" };
        }
    }

    private void Start()
    {
        // 自动开始触发气泡
        if (isAutoStart)
        {
            StartAutoBubble();
        }
    }

    /// <summary>
    /// 启动自动气泡触发（外部可调用，用于手动开启）
    /// </summary>
    public void StartAutoBubble()
    {
        if (bubbleCoroutine != null)
        {
            StopCoroutine(bubbleCoroutine);
        }
        bubbleCoroutine = StartCoroutine(AutoTriggerBubbleCoroutine());
    }

    /// <summary>
    /// 停止自动气泡触发（外部可调用，用于手动暂停）
    /// </summary>
    public void StopAutoBubble()
    {
        if (bubbleCoroutine != null)
        {
            StopCoroutine(bubbleCoroutine);
            bubbleCoroutine = null;
        }
        // 重置气泡状态
        ResetBubbleState();
    }

    /// <summary>
    /// 自动触发气泡核心协程：随机时间等待 → 播放气泡动画 → 循环
    /// </summary>
    private IEnumerator AutoTriggerBubbleCoroutine()
    {
        while (true)
        {
            // 计算随机等待时间（2~4秒之间）
            float randomWaitTime = UnityEngine.Random.Range(minTriggerInterval, maxTriggerInterval);
            yield return new WaitForSeconds(randomWaitTime);

            // 播放气泡动画（非播放状态才触发）
            if (!isBubblePlaying)
            {
                yield return PlayBubbleAnimationCoroutine();
            }
        }
    }

    /// <summary>
    /// 单个气泡动画核心：气泡渐显 → 文字渐显 → 停留 → 文字渐隐 → 气泡渐隐
    /// </summary>
    private IEnumerator PlayBubbleAnimationCoroutine()
    {
        isBubblePlaying = true;
        var config = bubbleConfig;

        // 1. 激活气泡，重置透明度
        config.bubbleRoot.SetActive(true);
        config.bubbleBgCanvasGroup.alpha = 0;
        config.bubbleTextCanvasGroup.alpha = 0;

        // 2. 随机抽取文案并赋值
        string randomContent = GetRandomBubbleContent();
        config.bubbleText.text = randomContent;

        // 3. 气泡背景先渐显（单独控制，更有层次感）
        float fadeInElapsed = 0f;
        while (fadeInElapsed < config.fadeInDuration)
        {
            fadeInElapsed += Time.deltaTime;
            float progress = fadeInElapsed / config.fadeInDuration;
            config.bubbleBgCanvasGroup.alpha = Mathf.Lerp(0, 1, progress);
            yield return null;
        }
        config.bubbleBgCanvasGroup.alpha = 1;

        // 4. 文字随后渐显（分步渐显，更自然）
        fadeInElapsed = 0f;
        while (fadeInElapsed < config.fadeInDuration)
        {
            fadeInElapsed += Time.deltaTime;
            float progress = fadeInElapsed / config.fadeInDuration;
            config.bubbleTextCanvasGroup.alpha = Mathf.Lerp(0, 1, progress);
            yield return null;
        }
        config.bubbleTextCanvasGroup.alpha = 1;

        // 5. 停留指定时间（1.5秒，可配置）
        yield return new WaitForSeconds(config.showDuration);

        // 6. 文字先渐隐
        float fadeOutElapsed = 0f;
        while (fadeOutElapsed < config.fadeOutDuration)
        {
            fadeOutElapsed += Time.deltaTime;
            float progress = fadeOutElapsed / config.fadeOutDuration;
            config.bubbleTextCanvasGroup.alpha = Mathf.Lerp(1, 0, progress);
            yield return null;
        }
        config.bubbleTextCanvasGroup.alpha = 0;

        // 7. 气泡背景后渐隐
        fadeOutElapsed = 0f;
        while (fadeOutElapsed < config.fadeOutDuration)
        {
            fadeOutElapsed += Time.deltaTime;
            float progress = fadeOutElapsed / config.fadeOutDuration;
            config.bubbleBgCanvasGroup.alpha = Mathf.Lerp(1, 0, progress);
            yield return null;
        }
        config.bubbleBgCanvasGroup.alpha = 0;

        // 8. 隐藏气泡，重置状态
        config.bubbleRoot.SetActive(false);
        isBubblePlaying = false;
    }

    /// <summary>
    /// 从文案池中随机抽取一条内容
    /// </summary>
    private string GetRandomBubbleContent()
    {
        if (bubbleConfig.bubbleContentList.Length == 0)
        {
            return "默认自言自语~";
        }
        // 随机索引
        int randomIndex = UnityEngine.Random.Range(0, bubbleConfig.bubbleContentList.Length);
        return bubbleConfig.bubbleContentList[randomIndex];
    }

    /// <summary>
    /// 重置气泡到初始状态
    /// </summary>
    private void ResetBubbleState()
    {
        isBubblePlaying = false;
        bubbleConfig.bubbleRoot.SetActive(false);
        bubbleConfig.bubbleBgCanvasGroup.alpha = 0;
        bubbleConfig.bubbleTextCanvasGroup.alpha = 0;
        bubbleConfig.bubbleText.text = "";
    }

    // 编辑器参数校验
    private void OnValidate()
    {
        // 限制时间范围，防止无效值
        minTriggerInterval = Mathf.Clamp(minTriggerInterval, 0.5f, 10f);
        maxTriggerInterval = Mathf.Clamp(maxTriggerInterval, minTriggerInterval, 10f);
        bubbleConfig.fadeInDuration = Mathf.Clamp(bubbleConfig.fadeInDuration, 0.1f, 1f);
        bubbleConfig.showDuration = Mathf.Clamp(bubbleConfig.showDuration, 0.5f, 5f);
        bubbleConfig.fadeOutDuration = Mathf.Clamp(bubbleConfig.fadeOutDuration, 0.1f, 1f);
    }
}