using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// Keeps the register/login scene background video alive in standalone builds.
/// The editor can sometimes hide VideoPlayer timing issues that show up in CI builds,
/// so this script explicitly prepares and starts the scene VideoPlayer and logs failures.
/// </summary>
[RequireComponent(typeof(VideoPlayer))]
[RequireComponent(typeof(RawImage))]
public sealed class LoginVideoPlayerBootstrap : MonoBehaviour
{
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private RawImage targetImage;
    [SerializeField] private float retryDelaySeconds = 1.5f;

    private Coroutine retryCoroutine;
    private bool eventsSubscribed;

    private void Reset()
    {
        videoPlayer = GetComponent<VideoPlayer>();
        targetImage = GetComponent<RawImage>();
    }

    private void Awake()
    {
        if (videoPlayer == null)
        {
            videoPlayer = GetComponent<VideoPlayer>();
        }

        if (targetImage == null)
        {
            targetImage = GetComponent<RawImage>();
        }

        if (targetImage != null)
        {
            targetImage.enabled = true;
            targetImage.color = Color.white;

            if (targetImage.texture == null && videoPlayer != null && videoPlayer.targetTexture != null)
            {
                targetImage.texture = videoPlayer.targetTexture;
            }
        }

        if (videoPlayer == null)
        {
            Debug.LogWarning("[LoginVideoPlayerBootstrap] VideoPlayer component is missing.", this);
            return;
        }

        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = true;
        videoPlayer.waitForFirstFrame = true;
    }

    private void OnEnable()
    {
        SubscribeEvents();
        StartVideo();
    }

    private void OnDisable()
    {
        if (retryCoroutine != null)
        {
            StopCoroutine(retryCoroutine);
            retryCoroutine = null;
        }

        if (videoPlayer != null)
        {
            UnsubscribeEvents();
        }
    }

    private void SubscribeEvents()
    {
        if (videoPlayer == null || eventsSubscribed)
        {
            return;
        }

        videoPlayer.errorReceived += OnVideoError;
        videoPlayer.prepareCompleted += OnVideoPrepared;
        eventsSubscribed = true;
    }

    private void UnsubscribeEvents()
    {
        if (videoPlayer == null || !eventsSubscribed)
        {
            return;
        }

        videoPlayer.errorReceived -= OnVideoError;
        videoPlayer.prepareCompleted -= OnVideoPrepared;
        eventsSubscribed = false;
    }

    private void StartVideo()
    {
        if (videoPlayer == null || !isActiveAndEnabled)
        {
            return;
        }

        if (videoPlayer.clip == null && string.IsNullOrEmpty(videoPlayer.url))
        {
            Debug.LogWarning("[LoginVideoPlayerBootstrap] No video clip or URL is assigned.", this);
            return;
        }

        if (videoPlayer.isPrepared)
        {
            videoPlayer.Play();
            return;
        }

        videoPlayer.Prepare();

        if (retryCoroutine == null)
        {
            retryCoroutine = StartCoroutine(PlayAfterDelay());
        }
    }

    private IEnumerator PlayAfterDelay()
    {
        yield return new WaitForSecondsRealtime(retryDelaySeconds);

        retryCoroutine = null;

        if (videoPlayer == null || !isActiveAndEnabled)
        {
            yield break;
        }

        if (!videoPlayer.isPrepared)
        {
            Debug.LogWarning("[LoginVideoPlayerBootstrap] Video was not prepared in time; attempting Play() directly.", this);
        }

        videoPlayer.Play();
    }

    private void OnVideoPrepared(VideoPlayer source)
    {
        source.Play();
    }

    private void OnVideoError(VideoPlayer source, string message)
    {
        Debug.LogError($"[LoginVideoPlayerBootstrap] Login background video failed: {message}", this);
    }
}
