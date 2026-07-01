using System;
using System.Collections;
using UnityEngine;

// 单个Sprite文字的基础配置（existDuration改为私有并重新命名）
[Serializable]
public class BirthdaySpriteWordConfig
{
    public int spriteId; // 文字唯一标识（与指令中的SpriteId对应）
    public GameObject wordSpriteObj; // 非UI Sprite文字GameObject
    public float moveSpeed; // 向上运动速度（像素/秒，独立配置）
    public float targetYOffset; // 向上运动的终点Y轴偏移（像素）
    public float fadeInTime = 0.1f; // 快速渐显时间（0.05~0.2秒）

    [SerializeField] // 序列化私有变量，允许在Inspector面板编辑
    private float wordExistTotalTime; // 原existDuration，重命名为私有变量（可自定义其他名称）

    // 提供公共访问方法，供外部获取私有存在周期
    public float GetWordExistTotalTime()
    {
        return wordExistTotalTime;
    }
}

// 你更新后的表演指令类（完全保留，新增existDuration）
[Serializable]
public class ShowSequenceSpriteCommand//表演指令
{
    public int SpriteId; // 要执行动画的文字SpriteId
    public float delayAfterThis;//该条指令执行后需要延迟的时间
    public float existDuration; // 文字存在总周期（秒，指令中独立配置）
}

public class DanziShow : MonoBehaviour
{
    [Header("文字基础配置")]
    public BirthdaySpriteWordConfig[] wordSpriteConfigs; // 文字配置（关联SpriteId与文字对象）
    public GameObject boxObj; // 场景中央的箱子（校准起始位置，可选）

    [Header("指令序列配置（核心：按步骤执行）")]
    public ShowSequenceSpriteCommand[] showSequenceCommands; // 自定义指令数组（按执行顺序排列）

    [Header("运动曲线配置")]
    public float smoothDampSpeed = 2f; // 阻尼系数（值越小，加减速越明显）
    public bool isDebug = false; // 调试开关

    private bool isPlaying = false; // 是否正在播放指令序列（防止重复触发）
    private Vector3 defaultWordStartPos; // 文字默认起始位置（箱子正上方）

    private void Awake()
    {
        // 校验文字配置
        if (wordSpriteConfigs.Length == 0)
        {
            Debug.LogError("文字基础配置数组不能为空！");
            enabled = false;
            return;
        }
        // 校验指令数组
        if (showSequenceCommands.Length == 0)
        {
            Debug.LogWarning("指令序列数组为空，请添加自定义指令！");
        }

        // 初始化文字起始位置
        InitWordStartPosition();
        // 初始化所有Sprite文字状态
        InitAllWordSprites();
    }

    private void Update()
    {
        // 按下空格触发指令序列（仅非播放状态生效）
        if (Input.GetKeyDown(KeyCode.Space) && !isPlaying)
        {
            StartExecuteCommandSequence();
        }
    }

    /// <summary>
    /// 初始化文字起始位置（箱子正上方或第一个文字位置）
    /// </summary>
    private void InitWordStartPosition()
    {
        if (boxObj != null)
        {
            // 箱子正上方偏移50像素（可自定义）
            defaultWordStartPos = boxObj.transform.position;
        }
        else
        {
            // 无箱子时，用第一个文字的初始位置作为默认起始位置
            defaultWordStartPos = wordSpriteConfigs[0].wordSpriteObj.transform.position;
            Debug.LogWarning("未赋值箱子Obj，使用第一个文字位置作为默认起始位置");
        }
    }

    /// <summary>
    /// 初始化所有Sprite文字状态（透明、隐藏、归位）
    /// </summary>
    private void InitAllWordSprites()
    {
        foreach (var config in wordSpriteConfigs)
        {
            if (config.wordSpriteObj == null) continue;

            // 获取SpriteRenderer组件（控制透明度与显示）
            SpriteRenderer spriteRenderer = config.wordSpriteObj.GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                Debug.LogError($"SpriteId={config.spriteId}的文字缺少SpriteRenderer组件！");
                continue;
            }

            // 初始化状态：隐藏、透明（Alpha=0）、归位到起始位置
            config.wordSpriteObj.SetActive(false);
            Color initColor = spriteRenderer.color;
            initColor.a = 0;
            spriteRenderer.color = initColor;
            config.wordSpriteObj.transform.position = defaultWordStartPos;
        }
    }

    /// <summary>
    /// 启动指令序列执行（空格触发）
    /// </summary>
    private void StartExecuteCommandSequence()
    {
        if (showSequenceCommands.Length == 0)
        {
            Debug.LogError("指令序列为空，无法执行！");
            return;
        }

        isPlaying = true;
        // 重置所有文字状态
        InitAllWordSprites();
        // 启动协程，按顺序执行指令
        StartCoroutine(ExecuteCommandSequenceCoroutine());
    }

    /// <summary>
    /// 核心协程：按顺序遍历指令数组，执行文字动画+等待延迟
    /// </summary>
    private IEnumerator ExecuteCommandSequenceCoroutine()
    {
        // 按数组顺序执行每一条指令（从上到下=执行顺序）
        foreach (var command in showSequenceCommands)
        {
            // 1. 根据指令的SpriteId，找到对应的文字配置
            BirthdaySpriteWordConfig targetWordConfig = FindWordConfigBySpriteId(command.SpriteId);
            if (targetWordConfig == null || targetWordConfig.wordSpriteObj == null)
            {
                Debug.LogError($"指令中SpriteId={command.SpriteId}对应的文字不存在！");
                // 即使当前指令无效，仍按配置等待延迟
                yield return new WaitForSeconds(command.delayAfterThis);
                continue;
            }

            // 2. 执行当前指令对应的文字动画（传入指令中的existDuration）
            yield return PlaySingleWordSpriteAnim(targetWordConfig, command.existDuration);

            // 3. 动画完成后，等待指令配置的延迟时间
            if (isDebug)
            {
                Debug.Log($"SpriteId={command.SpriteId}文字动画完成，等待{command.delayAfterThis}秒");
            }
            yield return new WaitForSeconds(command.delayAfterThis);
        }

        // 所有指令执行完毕，重置播放状态
        isPlaying = false;
        if (isDebug)
        {
            Debug.Log("所有表演指令执行完毕，可再次按下空格触发");
        }
    }

    /// <summary>
    /// 根据SpriteId查找对应的文字配置
    /// </summary>
    private BirthdaySpriteWordConfig FindWordConfigBySpriteId(int spriteId)
    {
        foreach (var config in wordSpriteConfigs)
        {
            if (config.spriteId == spriteId)
            {
                return config;
            }
        }
        return null;
    }

    /// <summary>
    /// 单个Sprite文字的核心动画：快速渐显→先加速后减速上移→终点渐隐
    /// </summary>
    /// <param name="config">文字基础配置</param>
    /// <param name="commandExistDuration">指令中传入的存在周期</param>
    private IEnumerator PlaySingleWordSpriteAnim(BirthdaySpriteWordConfig config, float commandExistDuration)
    {
        GameObject wordObj = config.wordSpriteObj;
        SpriteRenderer spriteRenderer = wordObj.GetComponent<SpriteRenderer>();
        if (wordObj == null || spriteRenderer == null) yield break;

        // 1. 激活文字，重置位置与透明度
        wordObj.SetActive(true);
        wordObj.transform.position = defaultWordStartPos;
        Color currentColor = spriteRenderer.color;
        currentColor.a = 0;
        spriteRenderer.color = currentColor;
        Vector3 currentPos = wordObj.transform.position;
        Vector3 targetPos = defaultWordStartPos + new Vector3(0, config.targetYOffset, 0);
        Vector3 velocity = Vector3.zero; // SmoothDamp速度缓存（加减速核心）

        // 2. 快速渐显（Alpha从0→1）
        float fadeInElapsed = 0f;
        while (fadeInElapsed < config.fadeInTime)
        {
            fadeInElapsed += Time.deltaTime;
            float progress = fadeInElapsed / config.fadeInTime;
            currentColor.a = Mathf.Lerp(0, 1, progress);
            spriteRenderer.color = currentColor;
            yield return null;
        }
        currentColor.a = 1;
        spriteRenderer.color = currentColor;

        // 3. 向上运动（先加速后减速，非匀速）：使用指令传入的existDuration
        float remainingTime = commandExistDuration - config.fadeInTime;
        // 防止剩余时间为负数，做容错处理
        remainingTime = Mathf.Max(remainingTime, 0.1f);
        float moveElapsed = 0f;

        while (moveElapsed < remainingTime)
        {
            moveElapsed += Time.deltaTime;

            // SmoothDamp实现先加速后减速
            currentPos = Vector3.SmoothDamp(
                currentPos,
                targetPos,
                ref velocity,
                smoothDampSpeed,
                Mathf.Infinity,
                Time.deltaTime
            );
            wordObj.transform.position = currentPos;

            // 4. 终点渐隐（最后1/3时间开始，Alpha从1→0）
            if (moveElapsed > remainingTime * 2 / 3)
            {
                float fadeOutProgress = (moveElapsed - remainingTime * 2 / 3) / (remainingTime / 3);
                currentColor.a = Mathf.Lerp(1, 0, fadeOutProgress);
                spriteRenderer.color = currentColor;
            }

            // 到达终点后停止移动
            if (Vector2.Distance(currentPos, targetPos) < 1f)
            {
                currentPos = targetPos;
                wordObj.transform.position = currentPos;
                break;
            }

            yield return null;
        }

        // 5. 动画结束：隐藏文字，重置状态
        currentColor.a = 0;
        spriteRenderer.color = currentColor;
        wordObj.SetActive(false);
        wordObj.transform.position = defaultWordStartPos;
    }

    // 编辑器参数校验，防止无效值
    /*private void OnValidate()
    {
        // 校验文字配置参数
        foreach (var config in wordSpriteConfigs)
        {
            config.moveSpeed = Mathf.Clamp(config.moveSpeed, 50f, 500f);
            config.targetYOffset = Mathf.Clamp(config.targetYOffset, 100f, 500f);
            config.fadeInTime = Mathf.Clamp(config.fadeInTime, 0.05f, 0.2f);
        }

        // 校验指令参数
        foreach (var command in showSequenceCommands)
        {
            command.delayAfterThis = Mathf.Clamp(command.delayAfterThis, 0f, 10f);
            command.existDuration = Mathf.Clamp(command.existDuration, 0.5f, 5f); // 限制指令中的存在周期
        }

        smoothDampSpeed = Mathf.Clamp(smoothDampSpeed, 0.5f, 10f);
    }*/
}