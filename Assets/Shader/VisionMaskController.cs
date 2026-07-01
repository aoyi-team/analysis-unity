using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VisionMaskController : MonoBehaviour
{
    [Header("核心配置")]
    public Camera targetCamera;
    public Transform player;
    public RawImage rawImage;

    [Header("亮光参数（固定最终值，脚本自动过渡）")]
    public float finalRadius = 0.18f; // 开场过渡终点值
    public float finalSoftness = 0.14f; // 开场过渡终点值

    [Header("开场过渡配置")]
    public float fadeDuration = 2f;
    private float fadeTimer = 0f;
    private float currentFadeRatio = 0f;

    [Header("空格键紧急过渡配置（新增）")]
    public float emergencySmoothTime = 0.5f; // 先快后慢的平滑时间（值越小过渡越快，曲线越陡）
    private bool isEmergencyTransition = false; // 紧急过渡开关
    private float currentEmergencyRadius = 0f; // 紧急过渡当前Radius
    private float currentEmergencySoftness = 1f; // 紧急过渡当前Softness
    private float radiusVelocity = 0f; // SmoothDamp所需速度缓存（Radius）
    private float softnessVelocity = 0f; // SmoothDamp所需速度缓存（Softness）
    private readonly float emergencyTargetRadius = 1f; // 紧急过渡Radius目标值
    private readonly float emergencyTargetSoftness = 0f; // 紧急过渡Softness目标值

    [Header("缓存引用")]
    private Material material;
    private RectTransform canvasRect;
    private RectTransform rawImageRect;

    public GameObject Canvas;

    void Start()
    {
        // 原有初始化逻辑不变
        if (rawImage == null)
        {
            rawImage = GetComponent<RawImage>();
        }
        rawImageRect = GetComponent<RectTransform>();

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            canvasRect = canvas.GetComponent<RectTransform>();
        }
        else
        {
            Debug.LogError("未找到父级Canvas组件！");
            enabled = false;
            return;
        }

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
            if (targetCamera == null)
            {
                Debug.LogError("未找到主相机！");
                enabled = false;
                return;
            }
        }

        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Zhanwuyan");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
            else
            {
                Debug.LogError("未找到标签为Zhanwuyan的主角对象！");
                enabled = false;
                return;
            }
        }

        if (rawImage.material != null)
        {
            material = new Material(rawImage.material);
            rawImage.material = material;
        }
        else
        {
            Debug.LogError("RawImage未绑定材质（包含舞台灯Shader）！");
            enabled = false;
            return;
        }

        // 初始化过渡参数
        fadeTimer = 0f;
        currentFadeRatio = 0f;
        currentEmergencyRadius = 0f;
        currentEmergencySoftness = 1f;
        material.SetFloat("_Radius", currentEmergencyRadius);
        material.SetFloat("_Softness", currentEmergencySoftness);
        material.SetVector("_Center", new Vector4(0.5f, 0.5f, 0, 0));
    }

    void Update()
    {
        // 空值校验
        if (player == null || material == null || canvasRect == null || targetCamera == null)
        {
            return;
        }

        // 新增：检测空格键按下，触发紧急过渡（仅触发一次，直到过渡完成）
        if (Input.GetKeyDown(KeyCode.Space) && !isEmergencyTransition)
        {
            TriggerEmergencyTransition();
        }

        // 优先执行紧急过渡，再执行开场过渡
        if (isEmergencyTransition)
        {
            UpdateEmergencyTransition(); // 新增：更新紧急过渡逻辑
        }
        else
        {
            UpdateFadeInAnimation(); // 原有开场过渡
        }

        // 亮光中心坐标更新（保持原有逻辑，不受过渡状态影响）
        UpdateVisionCenterPos();

        // 根据状态更新Shader参数
        UpdateShaderParams();
    }

    /// <summary>
    /// 新增：触发空格键紧急过渡（初始化参数）
    /// </summary>
    private void TriggerEmergencyTransition()
    {
        isEmergencyTransition = true;
        // 初始化当前值为当前的亮光参数，保证过渡无缝衔接
        currentEmergencyRadius = material.GetFloat("_Radius");
        currentEmergencySoftness = material.GetFloat("_Softness");
        // 重置SmoothDamp速度缓存
        radiusVelocity = 0f;
        softnessVelocity = 0f;
        Debug.Log("触发紧急过渡：Radius→1，Softness→0（先快后慢）");
    }

    /// <summary>
    /// 新增：更新紧急过渡逻辑（先快后慢效果由Mathf.SmoothDamp实现）
    /// </summary>
    private void UpdateEmergencyTransition()
    {
        // Mathf.SmoothDamp：实现先快后慢的平滑过渡
        // 参数说明：当前值、目标值、速度缓存、平滑时间
        currentEmergencyRadius = Mathf.SmoothDamp(
            currentEmergencyRadius,
            emergencyTargetRadius,
            ref radiusVelocity,
            emergencySmoothTime
        );

        currentEmergencySoftness = Mathf.SmoothDamp(
            currentEmergencySoftness,
            emergencyTargetSoftness,
            ref softnessVelocity,
            emergencySmoothTime
        );

        // 检测过渡是否完成（误差范围内判定为完成）
        if (Mathf.Abs(currentEmergencyRadius - emergencyTargetRadius) < 0.001f &&
            Mathf.Abs(currentEmergencySoftness - emergencyTargetSoftness) < 0.001f)
        {
            isEmergencyTransition = false;
            currentEmergencyRadius = emergencyTargetRadius;
            currentEmergencySoftness = emergencyTargetSoftness;
            Debug.Log("紧急过渡完成");
            Canvas.SetActive(false);

        }
    }

    /// <summary>
    /// 原有：开场过渡逻辑
    /// </summary>
    private void UpdateFadeInAnimation()
    {
        if (fadeTimer < fadeDuration)
        {
            fadeTimer += Time.deltaTime;
            currentFadeRatio = Mathf.Clamp01(fadeTimer / fadeDuration);
            // 可选：开场缓动
            currentFadeRatio = Mathf.SmoothStep(0f, 1f, currentFadeRatio);
        }
        else
        {
            currentFadeRatio = 1f;
        }
    }

    /// <summary>
    /// 原有：更新亮光中心坐标
    /// </summary>
    private void UpdateVisionCenterPos()
    {
        Vector3 worldPos = player.position;
        worldPos.z = targetCamera.nearClipPlane + 0.1f;
        Vector2 screenPos = targetCamera.WorldToScreenPoint(worldPos);

        Canvas canvas = canvasRect.GetComponent<Canvas>();
        Vector2 localPos;
        bool isSuccess = RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPos,
            (canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : targetCamera,
            out localPos
        );

        if (!isSuccess)
        {
            Debug.LogWarning("屏幕坐标转换为Canvas局部坐标失败！");
            return;
        }

        float normalizedX = (localPos.x + canvasRect.rect.width / 2f) / canvasRect.rect.width;
        float normalizedY = (localPos.y + canvasRect.rect.height / 2f) / canvasRect.rect.height;

        normalizedX = Mathf.Clamp01(normalizedX);
        normalizedY = Mathf.Clamp01(normalizedY);

        material.SetVector("_Center", new Vector4(normalizedX, normalizedY, 0, 0));
    }

    /// <summary>
    /// 优化：根据状态更新Shader参数
    /// </summary>
    private void UpdateShaderParams()
    {
        if (isEmergencyTransition)
        {
            // 紧急过渡：使用SmoothDamp计算后的参数
            material.SetFloat("_Radius", currentEmergencyRadius);
            material.SetFloat("_Softness", currentEmergencySoftness);
        }
        else
        {
            // 开场过渡：原有线性/缓动参数
            float currentRadius = Mathf.Lerp(0f, finalRadius, currentFadeRatio);
            float currentSoftness = Mathf.Lerp(1f, finalSoftness, currentFadeRatio);
            material.SetFloat("_Radius", currentRadius);
            material.SetFloat("_Softness", currentSoftness);
        }
    }

    // 可选：手动重置紧急过渡（如需重复触发）
    public void ResetEmergencyTransition()
    {
        isEmergencyTransition = false;
        currentEmergencyRadius = 0f;
        currentEmergencySoftness = 1f;
        radiusVelocity = 0f;
        softnessVelocity = 0f;
        // 重置后回到开场过渡状态
        fadeTimer = 0f;
        currentFadeRatio = 0f;
    }

    // 销毁时释放材质实例
    void OnDestroy()
    {
        if (material != null)
        {
            Destroy(material);
        }
    }
}

