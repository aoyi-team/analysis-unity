using Cinemachine;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    private static CameraManager _instance;

    public static CameraManager Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject(nameof(CameraManager));
                _instance = go.AddComponent<CameraManager>();
            }
            return _instance;
        }
    }

    [Header("镜头基础")]
    [SerializeField] float orthographicSize = 5.25f;

    [Header("鼠标偏移")]
    [Tooltip("鼠标在此半径内时，镜头中心贴在角色上")]
    [SerializeField] float triggerRadius = 2f;
    [Tooltip("镜头中心相对角色的最大偏移")]
    [SerializeField] float maxOffsetDistance = 1.6f;
    [Tooltip("镜头跟随目标位置的平滑速度")]
    [SerializeField] float smoothSpeed = 1.8f;

    Camera _mainCamera;
    CinemachineVirtualCamera _vcam;
    CinemachineConfiner2D _confiner;
    Transform _cameraRig;
    Transform _followTarget;
    bool _initialized;

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }

    void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }

    public void Init()
    {
        if (_initialized) return;

        var localView = PlayerManager.Instance.LocalPlayerView;
        if (localView == null)
        {
            Debug.LogError("CameraManager.Init: LocalPlayerView 为空");
            return;
        }

        _followTarget = localView.transform;

        CreateMainCamera();
        CreateCameraRig();
        CreateVirtualCamera();
        TrySetupConfiner();

        _vcam.Follow = _cameraRig;
        _vcam.m_Lens.OrthographicSize = orthographicSize;

        SnapToPlayer();
        _initialized = true;
    }

    /// <summary>Rig 在 Update 里动，Cinemachine Brain 在 LateUpdate 跟 Rig</summary>
    void Update()
    {
        if (!_initialized || _followTarget == null || _mainCamera == null)
            return;
        UpdateCameraRigPosition();
    }

    /// <summary>
    /// 设置跟随目标，通常在玩家角色创建后调用。CameraRig 会跟随目标移动，Cinemachine Virtual Camera 会跟随 CameraRig。
    /// </summary>
    /// <param name="view"></param>
    public void SetFollowTarget(BasePlayerView view)
    {
        if (view == null) return;
        _followTarget = view.transform;
        SnapToPlayer();
    }

    /// <summary>原 CameraFollowTarget.Update 逻辑，作用在 CameraRig 上</summary>
    void UpdateCameraRigPosition()
    {
        Vector3 playerPos = _followTarget.position;

        float zDist = Mathf.Abs(_mainCamera.transform.position.z - playerPos.z);
        Vector3 mouseScreen = new Vector3(Input.mousePosition.x, Input.mousePosition.y, zDist);
        Vector3 mouseWorldPos = _mainCamera.ScreenToWorldPoint(mouseScreen);
        mouseWorldPos.z = playerPos.z;

        Vector3 playerToMouse = mouseWorldPos - playerPos;
        playerToMouse.z = 0f;
        float mouseDistance = playerToMouse.magnitude;

        Vector3 targetPosition;
        if (mouseDistance > triggerRadius)
        {
            float exceedRatio = (mouseDistance - triggerRadius) / triggerRadius;
            Vector3 offset = playerToMouse.normalized *
                Mathf.Min(maxOffsetDistance, exceedRatio * maxOffsetDistance);
            targetPosition = playerPos + offset;
        }
        else
        {
            targetPosition = playerPos;
        }

        _cameraRig.position = Vector3.Lerp(
            _cameraRig.position,
            targetPosition,
            Time.deltaTime * smoothSpeed);
    }
    #region 创建相机
    void SnapToPlayer()
    {
        if (_followTarget == null || _cameraRig == null) return;
        _cameraRig.position = _followTarget.position;
    }

    void CreateMainCamera()
    {
        if (Camera.main != null)
            _mainCamera = Camera.main;
        else
        {
            var go = new GameObject("MainCamera");
            _mainCamera = go.AddComponent<Camera>();
            _mainCamera.orthographic = true;
            go.tag = "MainCamera";
            go.AddComponent<AudioListener>();
        }

        if (_mainCamera.GetComponent<CinemachineBrain>() == null)
            _mainCamera.gameObject.AddComponent<CinemachineBrain>();
    }

    void CreateCameraRig()
    {
        var rigGo = new GameObject("CameraRig");
        _cameraRig = rigGo.transform;
        _cameraRig.SetParent(_followTarget);
        SnapToPlayer();
    }

    void CreateVirtualCamera()
    {
        var vcamGo = new GameObject("CM_PlayerFollow");
        _vcam = vcamGo.AddComponent<CinemachineVirtualCamera>();
        _vcam.Priority = 20;

        var transposer = _vcam.AddCinemachineComponent<CinemachineTransposer>();
        transposer.m_FollowOffset = new Vector3(0f, 0f, -10f);
        transposer.m_XDamping = 0f;
        transposer.m_YDamping = 0f;
        transposer.m_ZDamping = 0f;
    }

    void TrySetupConfiner()
    {
        var boundsGo = GameObject.Find("MapBounds");
        if (boundsGo == null)
        {
            Debug.LogWarning("CameraManager: 未找到 MapBounds");
            return;
        }

        var bounds = boundsGo.GetComponent<Collider2D>();
        if (bounds == null)
        {
            Debug.LogWarning("CameraManager: MapBounds 无 Collider2D");
            return;
        }

        _confiner = _vcam.gameObject.AddComponent<CinemachineConfiner2D>();
        _confiner.m_BoundingShape2D = bounds;
        _confiner.InvalidateCache();
    }
    #endregion
#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (_followTarget == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(_followTarget.position, triggerRadius);
        if (_cameraRig != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(_cameraRig.position, 0.2f);
        }
    }
#endif
}