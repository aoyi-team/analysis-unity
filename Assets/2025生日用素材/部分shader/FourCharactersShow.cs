using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FourCharactersShow : MonoBehaviour
{
    [Header("核心配置")]
    public Camera targetCamera;
    public Transform player; // 直接赋值主角，避免动态查找的性能损耗
    public RawImage rawImage; // 直接赋值RawImage，简化获取逻辑

    [Header("亮光参数（固定最终值，脚本自动过渡）")]
    public float finalRadius = 0.18f; // Radius终点值（0 → 0.18）
    public float finalSoftness = 0.14f; // Softness终点值（1 → 0.14）

    [Header("开场过渡配置")]
    public float fadeDuration = 2f; // 从初始值到最终值的过渡时间
    private float fadeTimer = 0f; // 过渡计时器
    private float currentFadeRatio = 0f; // 当前过渡比例（0~1），统一控制Radius和Softness

    [Header("缓存引用")]
    private Material material;
    private RectTransform canvasRect;
    private RectTransform rawImageRect;

    void Start()
    {
        // 1. 缓存引用，避免每帧重复获取
        if (rawImage == null)
        {
            rawImage = GetComponent<RawImage>();
        }
        rawImageRect = GetComponent<RectTransform>();

        // 获取Canvas RectTransform
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

        // 2. 初始化相机
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

        // 3. 初始化主角（优先直接赋值，其次动态查找）
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

        // 4. 初始化材质
        if (rawImage.material != null)
        {
            material = new Material(rawImage.material); // 复制材质实例，避免影响其他UI
            rawImage.material = material;
        }
        else
        {
            Debug.LogError("RawImage未绑定材质（包含舞台灯Shader）！");
            enabled = false;
            return;
        }

        // 5. 初始化过渡参数（强制设置初始值：Radius=0，Softness=1）
        fadeTimer = 0f;
        currentFadeRatio = 0f;
        material.SetFloat("_Radius", 0f); // Radius初始值
        material.SetFloat("_Softness", 1f); // Softness初始值
        material.SetVector("_Center", new Vector4(0.5f, 0.5f, 0, 0)); // 初始中心居中
    }

    void Update()
    {
        // 空值校验
        if (player == null || material == null || canvasRect == null || targetCamera == null)
        {
            return;
        }

        // 1. 处理开场参数过渡逻辑（Radius:0→0.18；Softness:1→0.14）
        UpdateFadeInAnimation();

        // 2. 计算主角对应的UI归一化中心坐标
        UpdateVisionCenterPos();

        // 3. 更新Shader参数（带过渡效果）
        UpdateShaderParams();
    }

    /// <summary>
    /// 更新开场淡入动画，统一控制Radius和Softness的过渡
    /// </summary>
    private void UpdateFadeInAnimation()
    {
        if (fadeTimer < fadeDuration)
        {
            fadeTimer += Time.deltaTime;
            currentFadeRatio = Mathf.Clamp01(fadeTimer / fadeDuration);
            // 可选：添加Mathf.SmoothStep实现缓动效果，过渡更平滑
            currentFadeRatio = Mathf.SmoothStep(0f, 1f, currentFadeRatio);
        }
        else
        {
            currentFadeRatio = 1f; // 过渡完成，锁定最终值
        }
    }

    /// <summary>
    /// 计算并更新亮光中心坐标（保持精准跟随主角）
    /// </summary>
    private void UpdateVisionCenterPos()
    {
        // 步骤1：世界坐标 → 屏幕坐标（修正2D Z轴偏移）
        Vector3 worldPos = player.position;
        worldPos.z = targetCamera.nearClipPlane + 0.1f; // 避免相机远近影响坐标
        Vector2 screenPos = targetCamera.WorldToScreenPoint(worldPos);

        // 步骤2：屏幕坐标 → Canvas局部坐标（传入正确相机参数）
        Vector2 localPos;
        Canvas canvas = canvasRect.GetComponent<Canvas>();
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

        // 步骤3：Canvas局部坐标 → 归一化坐标（0~1，精准对应Shader UV）
        float normalizedX = (localPos.x + canvasRect.rect.width / 2f) / canvasRect.rect.width;
        float normalizedY = (localPos.y + canvasRect.rect.height / 2f) / canvasRect.rect.height;

        // 限制坐标在0~1范围内，避免亮光超出屏幕
        normalizedX = Mathf.Clamp01(normalizedX);
        normalizedY = Mathf.Clamp01(normalizedY);

        // 设置Shader中心坐标
        material.SetVector("_Center", new Vector4(normalizedX, normalizedY, 0, 0));
    }

    /// <summary>
    /// 更新Shader的Radius和Softness参数（适配新的过渡要求）
    /// </summary>
    private void UpdateShaderParams()
    {
        // Radius：从0线性过渡到finalRadius（0.18）
        float currentRadius = Mathf.Lerp(0f, finalRadius, currentFadeRatio);

        // Softness：从1线性过渡到finalSoftness（0.14）
        float currentSoftness = Mathf.Lerp(1f, finalSoftness, currentFadeRatio);

        // 设置Shader参数（直接使用过渡后的值，无需额外归一化）
        material.SetFloat("_Radius", currentRadius);
        material.SetFloat("_Softness", currentSoftness);
    }

    // 可选：手动重置开场过渡（如需重新播放开场动画）
    public void ResetFadeInAnimation()
    {
        fadeTimer = 0f;
        currentFadeRatio = 0f;
        // 重置初始值
        material.SetFloat("_Radius", 0f);
        material.SetFloat("_Softness", 1f);
    }

    // 销毁时释放材质实例，避免内存泄漏
    void OnDestroy()
    {
        if (material != null)
        {
            Destroy(material);
        }
    }
}
