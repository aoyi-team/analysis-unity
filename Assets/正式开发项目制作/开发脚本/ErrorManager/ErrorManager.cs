using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace ErrorManagement
{
    public class ErrorManager : MonoBehaviour
    {
        private static ErrorManager _instance;
        public static ErrorManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject obj = new GameObject("ErrorManager");
                    _instance = obj.AddComponent<ErrorManager>();
                    DontDestroyOnLoad(obj);
                }
                return _instance;
            }
        }

        [SerializeField] private bool _enableFileLogging = true;
        [SerializeField] private bool _enableToastUI = true;
        [SerializeField] private int _maxLogFiles = 10;
        [SerializeField] private float _toastDuration = 3f;
        [SerializeField] [Range(0f, 1f)] private float _toastBgAlpha = 0.88f;

        private StreamWriter _logWriter;
        private string _logFilePath;
        private Canvas _toastCanvas;
        private readonly Queue<ErrorEntry> _toastQueue = new Queue<ErrorEntry>();
        private readonly Queue<ErrorEntry> _pendingToasts = new Queue<ErrorEntry>();
        private readonly object _pendingLock = new object();
        private bool _isShowingToast;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            Application.logMessageReceived += OnLogMessageReceived;
            InitializeFileLogging();
        }

        private void Update()
        {
            lock (_pendingLock)
            {
                while (_pendingToasts.Count > 0)
                {
                    _toastQueue.Enqueue(_pendingToasts.Dequeue());
                }
            }

            if (_toastQueue.Count > 0 && !_isShowingToast)
            {
                ProcessToastQueue();
            }
        }

        private void OnDestroy()
        {
            Application.logMessageReceived -= OnLogMessageReceived;
            CloseFileLogging();
        }

        #region File Logging

        private void InitializeFileLogging()
        {
            if (!_enableFileLogging) return;

            try
            {
                string logDir = Path.Combine(Application.persistentDataPath, "ErrorLogs");
                if (!Directory.Exists(logDir))
                    Directory.CreateDirectory(logDir);

                string fileName = $"error_{DateTime.Now:yyyyMMdd_HHmmss}.log";
                _logFilePath = Path.Combine(logDir, fileName);
                _logWriter = new StreamWriter(_logFilePath, false, Encoding.UTF8)
                {
                    AutoFlush = true
                };

                _logWriter.WriteLine($"=== Aoyi Error Log [{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ===");
                _logWriter.WriteLine($"Device: {SystemInfo.deviceModel}");
                _logWriter.WriteLine($"OS: {SystemInfo.operatingSystem}");
                _logWriter.WriteLine($"Unity: {Application.unityVersion}");
                _logWriter.WriteLine();

                CleanOldLogFiles(logDir);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ErrorManager] Failed to init log file: {ex.Message}");
                _enableFileLogging = false;
            }
        }

        private void CleanOldLogFiles(string logDir)
        {
            try
            {
                string[] files = Directory.GetFiles(logDir, "error_*.log");
                if (files.Length > _maxLogFiles)
                {
                    Array.Sort(files);
                    for (int i = 0; i < files.Length - _maxLogFiles; i++)
                    {
                        File.Delete(files[i]);
                    }
                }
            }
            catch { }
        }

        private void WriteToFile(ErrorEntry entry)
        {
            if (!_enableFileLogging || _logWriter == null) return;

            try
            {
                string line = entry.Level >= ErrorLevel.Error
                    ? $"━━━ [{entry.Timestamp}] [{entry.Level}] [{entry.Category}] ━━━"
                    : $"[{entry.Timestamp}] [{entry.Level}] [{entry.Category}]";

                _logWriter.WriteLine(line);
                _logWriter.WriteLine($"  {entry.Message}");

                if (!string.IsNullOrEmpty(entry.StackTrace))
                {
                    _logWriter.WriteLine($"  StackTrace:");
                    _logWriter.WriteLine($"  {entry.StackTrace.Replace("\n", "\n  ")}");
                }

                if (entry.Level >= ErrorLevel.Error)
                    _logWriter.WriteLine();
            }
            catch { }
        }

        private void CloseFileLogging()
        {
            if (_logWriter == null) return;

            try
            {
                _logWriter.WriteLine();
                _logWriter.WriteLine($"=== Log Ended [{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ===");
                _logWriter.Close();
                _logWriter.Dispose();
            }
            catch { }
            _logWriter = null;
        }

        #endregion

        #region Global Exception Capture

        private void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            switch (type)
            {
                case LogType.Exception:
                    HandleCaptured(ErrorLevel.Critical, ErrorCategory.System, condition, stackTrace, true);
                    break;
                case LogType.Error:
                    HandleCaptured(ErrorLevel.Error, ErrorCategory.System, condition, stackTrace, true);
                    break;
                case LogType.Assert:
                    HandleCaptured(ErrorLevel.Error, ErrorCategory.System, condition, stackTrace, true);
                    break;
                case LogType.Warning:
                    HandleCaptured(ErrorLevel.Warning, ErrorCategory.System, condition, stackTrace, false);
                    break;
            }
        }

        private void HandleCaptured(ErrorLevel level, ErrorCategory category, string message, string stackTrace, bool showToast)
        {
            ErrorEntry entry = new ErrorEntry(level, category, message, stackTrace);
            WriteToFile(entry);

            if (showToast)
            {
                EnqueueToast(entry);
            }
        }

        #endregion

        #region Public API

        public static void LogInfo(string message, ErrorCategory category = ErrorCategory.Other)
        {
            ErrorEntry entry = new ErrorEntry(ErrorLevel.Info, category, message);
            Debug.Log($"[{category}] {message}");
            Instance.WriteToFile(entry);
        }

        public static void LogWarning(string message, ErrorCategory category = ErrorCategory.Other)
        {
            ErrorEntry entry = new ErrorEntry(ErrorLevel.Warning, category, message);
            Debug.LogWarning($"[{category}] {message}");
            Instance.WriteToFile(entry);
        }

        public static void LogError(string message, ErrorCategory category = ErrorCategory.Other, bool showToast = true)
        {
            ErrorEntry entry = new ErrorEntry(ErrorLevel.Error, category, message);
            Debug.LogError($"[{category}] {message}");
            Instance.WriteToFile(entry);
            if (showToast) Instance.EnqueueToast(entry);
        }

        public static void LogCritical(string message, ErrorCategory category = ErrorCategory.Other, bool showToast = true)
        {
            ErrorEntry entry = new ErrorEntry(ErrorLevel.Critical, category, message);
            Debug.LogError($"[CRITICAL] [{category}] {message}");
            Instance.WriteToFile(entry);
            if (showToast) Instance.EnqueueToast(entry);
        }

        public static void LogException(Exception ex, ErrorCategory category = ErrorCategory.Other, bool showToast = true)
        {
            ErrorEntry entry = new ErrorEntry(ErrorLevel.Error, category, ex.Message, ex.StackTrace);
            Debug.LogError($"[{category}] Exception: {ex.Message}\n{ex.StackTrace}");
            Instance.WriteToFile(entry);
            if (showToast) Instance.EnqueueToast(entry);
        }

        #endregion

        #region Toast UI

        private void EnqueueToast(ErrorEntry entry)
        {
            if (!_enableToastUI) return;

            lock (_pendingLock)
            {
                _pendingToasts.Enqueue(entry);
            }
        }

        private void ProcessToastQueue()
        {
            if (_toastQueue.Count == 0)
            {
                _isShowingToast = false;
                return;
            }

            _isShowingToast = true;
            ErrorEntry entry = _toastQueue.Dequeue();
            StartCoroutine(ShowToastCoroutine(entry));
        }

        private System.Collections.IEnumerator ShowToastCoroutine(ErrorEntry entry)
        {
            EnsureToastCanvas();

            GameObject toastObj = new GameObject("ErrorToast");
            toastObj.transform.SetParent(_toastCanvas.transform, false);

            RectTransform rect = toastObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.88f);
            rect.anchorMax = new Vector2(0.5f, 0.88f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0, 40f);

            Image bg = toastObj.AddComponent<Image>();
            Color bgColor = GetToastColor(entry.Level);
            bgColor.a = _toastBgAlpha;
            bg.color = bgColor;

            GameObject textObj = new GameObject("ToastText");
            textObj.transform.SetParent(toastObj.transform, false);
            RectTransform textRt = textObj.AddComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(24, 10);
            textRt.offsetMax = new Vector2(-24, -10);

            Text text = textObj.AddComponent<Text>();
            text.text = entry.Message;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 22;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;

            ContentSizeFitter fitter = toastObj.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            HorizontalLayoutGroup layout = toastObj.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.padding = new RectOffset(16, 16, 12, 12);

            CanvasGroup canvasGroup = toastObj.AddComponent<CanvasGroup>();

            float elapsed = 0f;
            float fadeInDuration = 0.25f;
            Vector2 startPos = rect.anchoredPosition;
            Vector2 targetPos = startPos;

            while (elapsed < fadeInDuration)
            {
                float t = elapsed / fadeInDuration;
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
                rect.anchoredPosition = startPos + Vector2.up * Mathf.Lerp(20f, 0f, t);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            canvasGroup.alpha = 1f;
            rect.anchoredPosition = targetPos;

            yield return new WaitForSecondsRealtime(_toastDuration);

            elapsed = 0f;
            float fadeOutDuration = 0.4f;
            while (elapsed < fadeOutDuration)
            {
                float t = elapsed / fadeOutDuration;
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
                rect.anchoredPosition = targetPos + Vector2.up * Mathf.Lerp(0f, 15f, t);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            Destroy(toastObj);
            ProcessToastQueue();
        }

        private void EnsureToastCanvas()
        {
            if (_toastCanvas != null) return;

            GameObject canvasObj = new GameObject("ErrorToastCanvas");
            DontDestroyOnLoad(canvasObj);
            _toastCanvas = canvasObj.AddComponent<Canvas>();
            _toastCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _toastCanvas.sortingOrder = short.MaxValue;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            canvasObj.AddComponent<GraphicRaycaster>();
        }

        private Color GetToastColor(ErrorLevel level)
        {
            switch (level)
            {
                case ErrorLevel.Info:
                    return new Color(0.18f, 0.55f, 0.85f);
                case ErrorLevel.Warning:
                    return new Color(0.92f, 0.65f, 0.12f);
                case ErrorLevel.Error:
                    return new Color(0.85f, 0.18f, 0.18f);
                case ErrorLevel.Critical:
                    return new Color(0.65f, 0.05f, 0.05f);
                default:
                    return new Color(0.25f, 0.25f, 0.25f);
            }
        }

        #endregion
    }
}
