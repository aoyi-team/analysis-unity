using UnityEngine;

/// <summary>
/// 全局帧率管理器
/// 在游戏启动时设置目标帧率上限，避免在高刷新率显示器上无限制渲染。
/// </summary>
public class FrameRateManager : MonoBehaviour
{
    [Header("帧率上限")]
    [Tooltip("0 表示不限制，-1 使用平台默认值")]
    [SerializeField] private int targetFrameRate = 230;

    [Header("垂直同步")]
    [Tooltip("0 关闭垂直同步，1 开启")]
    [SerializeField] private int vSyncCount = 0;

    private static FrameRateManager _instance;
    public static FrameRateManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("FrameRateManager");
                _instance = go.AddComponent<FrameRateManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    /// <summary>
    /// 游戏启动时自动初始化，无需手动挂载到场景。
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoInitialize()
    {
        _ = Instance;
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        ApplySettings();
    }

    private void ApplySettings()
    {
        QualitySettings.vSyncCount = vSyncCount;
        Application.targetFrameRate = targetFrameRate;
        Debug.Log($"[FrameRateManager] 设置目标帧率上限为 {targetFrameRate} FPS，垂直同步={vSyncCount}");
    }

    /// <summary>运行时动态修改帧率上限</summary>
    public void SetTargetFrameRate(int fps)
    {
        targetFrameRate = fps;
        Application.targetFrameRate = fps;
    }

    /// <summary>运行时动态修改垂直同步</summary>
    public void SetVSyncCount(int count)
    {
        vSyncCount = count;
        QualitySettings.vSyncCount = count;
    }
}
