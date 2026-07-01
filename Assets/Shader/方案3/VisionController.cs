using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VisionController : MonoBehaviour
{
    public GameObject SolideObjetcs;
    [Header("双角色配置（编辑器手动赋值）")]
    public GameObject roleA; // 角色A（编辑器赋值）
    public GameObject roleB; // 角色B（编辑器赋值）
    private Transform roleATrans;
    private Transform roleBTrans;

    [Header("核心配置")]
    public Camera targetCamera;
    public RawImage rawImage; // 仅一个RawImage，无新增！
    public GameObject canvasObj;

    [Header("亮光参数（双角色共享，可独立修改）")]
    public float finalRadius = 0.18f; // 双角色光圈最终半径
    public float finalSoftness = 0.14f; // 双角色光圈最终柔和度
    // 若需角色B使用不同参数，可新增finalRadius2、finalSoftness2

    [Header("开场过渡配置")]
    public float fadeDuration = 2f;
    private float fadeTimer = 0f;
    private float currentFadeRatio = 0f;

    [Header("空格键紧急过渡配置")]
    public float emergencySmoothTime = 0.5f;
    private bool isEmergencyTransition = false;
    private float currentEmergencyRadius = 0f;
    private float currentEmergencySoftness = 1f;
    private float radiusVelocity = 0f;
    private float softnessVelocity = 0f;
    private readonly float emergencyTargetRadius = 1f;
    private readonly float emergencyTargetSoftness = 0f;

    [Header("缓存引用")]
    private Material material;
    private RectTransform canvasRect;
    private RectTransform rawImageRect;

    void Start()
    {
        // 1. 校验双角色
        if (roleA == null || roleB == null)
        {
            Debug.LogError("请在编辑器中为roleA和roleB赋值两个角色！");
            enabled = false;
            return;
        }
        roleATrans = roleA.transform;
        roleBTrans = roleB.transform;

        // 2. 校验RawImage（仅一个，无需新增）
        if (rawImage == null)
        {
            rawImage = GetComponent<RawImage>();
            if (rawImage == null)
            {
                Debug.LogError("当前对象无RawImage组件，请挂载到RawImage上！");
                enabled = false;
                return;
            }
        }
        rawImageRect = rawImage.GetComponent<RectTransform>();

        // 3. 校验Canvas
        if (canvasObj == null)
        {
            canvasObj = GetComponentInParent<Canvas>().gameObject;
            if (canvasObj == null)
            {
                Debug.LogError("未找到Canvas对象！");
                enabled = false;
                return;
            }
        }
        canvasRect = canvasObj.GetComponent<RectTransform>();

        // 4. 校验相机
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

        // 5. 初始化材质（仅一个材质实例）
        if (rawImage.material != null)
        {
            material = new Material(rawImage.material);
            rawImage.material = material;
        }
        else
        {
            Debug.LogError("RawImage未绑定修改后的VisionMask Shader材质！");
            enabled = false;
            return;
        }

        // 6. 初始化过渡参数
        fadeTimer = 0f;
        currentFadeRatio = 0f;
        currentEmergencyRadius = 0f;
        currentEmergencySoftness = 1f;
        InitMaterialParams();
    }

    /// <summary>
    /// 初始化材质初始参数（双角色光圈同步初始化）
    /// </summary>
    private void InitMaterialParams()
    {
        if (material == null) return;
        // 角色A参数
        material.SetFloat("_Radius", currentEmergencyRadius);
        material.SetFloat("_Softness", currentEmergencySoftness);
        material.SetVector("_Center", new Vector4(0.5f, 0.5f, 0, 0));
        // 角色B参数
        material.SetFloat("_Radius2", currentEmergencyRadius);
        material.SetFloat("_Softness2", currentEmergencySoftness);
        material.SetVector("_Center2", new Vector4(0.5f, 0.5f, 0, 0));
    }

    void Update()
    {
        // 空值校验
        if (roleATrans == null || roleBTrans == null || material == null || canvasRect == null || targetCamera == null)
        {
            return;
        }

        // 空格键紧急过渡触发
        if (Input.GetKeyDown(KeyCode.J) && !isEmergencyTransition)
        {
            SolideObjetcs.SetActive(true);
            TriggerEmergencyTransition();
        }

        // 过渡逻辑（优先紧急过渡）
        if (isEmergencyTransition)
        {
            UpdateEmergencyTransition();
        }
        else
        {
            UpdateFadeInAnimation();
        }

        // 更新双角色的光圈中心坐标（关键：分别计算，分别传递）
        UpdateVisionCenterPos(roleATrans, "_Center");
        UpdateVisionCenterPos(roleBTrans, "_Center2");

        // 更新Shader双光圈参数
        UpdateShaderParams();
    }

    /// <summary>
    /// 触发紧急过渡
    /// </summary>
    private void TriggerEmergencyTransition()
    {
        isEmergencyTransition = true;
        // 从材质获取当前参数，无缝衔接
        currentEmergencyRadius = material.GetFloat("_Radius");
        currentEmergencySoftness = material.GetFloat("_Softness");
        radiusVelocity = 0f;
        softnessVelocity = 0f;
        Debug.Log("触发紧急过渡：双光圈同步过渡到Radius=1，Softness=0");
    }

    /// <summary>
    /// 更新紧急过渡逻辑（先快后慢）
    /// </summary>
    private void UpdateEmergencyTransition()
    {
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

        // 检测过渡完成
        if (Mathf.Abs(currentEmergencyRadius - emergencyTargetRadius) < 0.001f &&
            Mathf.Abs(currentEmergencySoftness - emergencyTargetSoftness) < 0.001f)
        {
            isEmergencyTransition = false;
            currentEmergencyRadius = emergencyTargetRadius;
            currentEmergencySoftness = emergencyTargetSoftness;
            Debug.Log("紧急过渡完成");
            if (canvasObj != null)
            {
                canvasObj.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 开场淡入过渡逻辑
    /// </summary>
    private void UpdateFadeInAnimation()
    {
        if (fadeTimer < fadeDuration)
        {
            fadeTimer += Time.deltaTime;
            currentFadeRatio = Mathf.Clamp01(fadeTimer / fadeDuration);
            currentFadeRatio = Mathf.SmoothStep(0f, 1f, currentFadeRatio); // 缓动效果
        }
        else
        {
            currentFadeRatio = 1f;
        }
    }

    /// <summary>
    /// 更新单个角色的光圈中心坐标（通用方法，支持双角色）
    /// </summary>
    /// <param name="roleTrans">角色Transform</param>
    /// <param name="centerParamName">Shader中对应的中心参数名（_Center / _Center2）</param>
    private void UpdateVisionCenterPos(Transform roleTrans, string centerParamName)
    {
        // 新增：判断角色是否激活（关键！未激活则直接跳过，不更新光圈）
        if (!roleTrans.gameObject.activeSelf)
        {
            // 若角色未激活，将对应光圈的alpha强制设为1（完全遮罩，即隐藏光圈）
            // 区分角色A和角色B，只处理角色B
            if (centerParamName == "_Center2") // 角色B对应的参数名
            {
                // 强制将角色B的光圈透明值拉满，实现隐藏
                material.SetFloat("_Radius2", 0f); // 半径设为0，直接隐藏
            }
            return;
        }
        Vector3 worldPos = roleTrans.position;
        worldPos.z = targetCamera.nearClipPlane + 0.1f; // 修正2D Z轴偏移
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
            Debug.LogWarning($"角色{roleTrans.gameObject.name}坐标转换失败！");
            return;
        }

        // 计算归一化坐标（0~1范围，匹配Shader UV）
        float normalizedX = (localPos.x + canvasRect.rect.width / 2f) / canvasRect.rect.width;
        float normalizedY = (localPos.y + canvasRect.rect.height / 2f) / canvasRect.rect.height;

        // 限制坐标，避免光圈超出屏幕
        normalizedX = Mathf.Clamp01(normalizedX);
        normalizedY = Mathf.Clamp01(normalizedY);

        // 传递给Shader对应参数
        material.SetVector(centerParamName, new Vector4(normalizedX, normalizedY, 0, 0));
    }

    /// <summary>
    /// 更新Shader双光圈参数（单材质，双角色同步过渡）
    /// </summary>
    private void UpdateShaderParams()
    {
        if (isEmergencyTransition)
        {
            // 紧急过渡：双光圈同步更新
            material.SetFloat("_Radius", currentEmergencyRadius);
            material.SetFloat("_Softness", currentEmergencySoftness);
            // 新增：判断角色B是否激活，激活才更新参数，未激活则设为0
            if (roleBTrans.gameObject.activeSelf)
            {
                material.SetFloat("_Radius2", currentEmergencyRadius);
                material.SetFloat("_Softness2", currentEmergencySoftness);
            }
            else
            {
                material.SetFloat("_Radius2", 0f); // 未激活时半径设0，隐藏光圈
                material.SetFloat("_Softness2", currentEmergencySoftness); // 柔和度可保持不变
            }
        }
        else
        {
            // 开场过渡：计算当前参数，双光圈同步更新
            float currentRadius = Mathf.Lerp(0f, finalRadius, currentFadeRatio);
            float currentSoftness = Mathf.Lerp(1f, finalSoftness, currentFadeRatio);
            material.SetFloat("_Radius", currentRadius);
            material.SetFloat("_Softness", currentSoftness);
            // 新增：判断角色B是否激活，激活才更新参数，未激活则设为0
            if (roleBTrans.gameObject.activeSelf)
            {
                material.SetFloat("_Radius2", currentRadius);
                material.SetFloat("_Softness2", currentSoftness);
            }
            else
            {
                material.SetFloat("_Radius2", 0f); // 未激活时半径设0，隐藏光圈
                material.SetFloat("_Softness2", currentSoftness);
            }
        }
    }

    // 手动重置紧急过渡
    public void ResetEmergencyTransition()
    {
        isEmergencyTransition = false;
        currentEmergencyRadius = 0f;
        currentEmergencySoftness = 1f;
        radiusVelocity = 0f;
        softnessVelocity = 0f;
        fadeTimer = 0f;
        currentFadeRatio = 0f;
        InitMaterialParams();
        // 重新激活画布
        if (canvasObj != null && !canvasObj.activeSelf)
        {
            canvasObj.SetActive(true);
        }
    }

    // 销毁时释放材质
    void OnDestroy()
    {
        if (material != null)
        {
            Destroy(material);
        }
    }
}