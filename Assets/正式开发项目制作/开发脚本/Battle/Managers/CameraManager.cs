using System.Collections;
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

    [Header("��ͷ����")]
    [SerializeField] float orthographicSize = 5.25f;

    [Header("���ƫ��")]
    [Tooltip("����ڴ˰뾶��ʱ����ͷ�������ڽ�ɫ��")]
    [SerializeField] float triggerRadius = 2f;
    [Tooltip("��ͷ������Խ�ɫ�����ƫ��")]
    [SerializeField] float maxOffsetDistance = 1.6f;
    [Tooltip("��ͷ����Ŀ��λ�õ�ƽ���ٶ�")]
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
        StartCoroutine(InitCoroutine());
    }

    IEnumerator InitCoroutine()
    {
        int retries = 0;
        const int maxRetries = 10;
        const float retryInterval = 0.3f;

        while (retries < maxRetries)
        {
            var localView = PlayerManager.Instance.LocalPlayerView;
            if (localView != null)
            {
                _followTarget = localView.transform;

                CreateMainCamera();
                CreateCameraRig();
                CreateVirtualCamera();
                TrySetupConfiner();

                _vcam.Follow = _cameraRig;
                _vcam.m_Lens.OrthographicSize = orthographicSize;

                SnapToPlayer();
                _initialized = true;
                Debug.Log("CameraManager: 初始化完成");
                yield break;
            }

            retries++;
            yield return new WaitForSeconds(retryInterval);
        }

        Debug.LogError($"CameraManager: 等待 {maxRetries * retryInterval:F1} 秒后 LocalPlayerView 仍为空，相机初始化失败");
    }

    /// <summary>Rig �� Update �ﶯ��Cinemachine Brain �� LateUpdate �� Rig</summary>
    void Update()
    {
        if (!_initialized || _followTarget == null || _mainCamera == null)
            return;
        UpdateCameraRigPosition();
    }

    /// <summary>
    /// ���ø���Ŀ�꣬ͨ������ҽ�ɫ��������á�CameraRig �����Ŀ���ƶ���Cinemachine Virtual Camera ����� CameraRig��
    /// </summary>
    /// <param name="view"></param>
    public void SetFollowTarget(BasePlayerView view)
    {
        if (view == null) return;
        _followTarget = view.transform;
        SnapToPlayer();
    }

    /// <summary>ԭ CameraFollowTarget.Update �߼��������� CameraRig ��</summary>
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
    #region �������
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
            Debug.LogWarning("CameraManager: δ�ҵ� MapBounds");
            return;
        }

        var bounds = boundsGo.GetComponent<Collider2D>();
        if (bounds == null)
        {
            Debug.LogWarning("CameraManager: MapBounds �� Collider2D");
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