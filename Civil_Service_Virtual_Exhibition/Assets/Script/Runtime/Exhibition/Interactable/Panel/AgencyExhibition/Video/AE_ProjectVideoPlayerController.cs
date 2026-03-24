using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;

public class AE_ProjectVideoPlayerController : MonoBehaviour
{
    public event Action Prepared;
    public event Action<string> Failed;
    public event Action<bool> PlayStateChanged;
    public event Action<double, double> TimeChanged;
    public event Action Finished;

    [Header("Video")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private RawImage videoOutputImage;
    [SerializeField] private AspectRatioFitter aspectRatioFitter;
    [SerializeField] private float prepareTimeoutSeconds = 8f;

    private Coroutine _prepareTimeoutRoutine;
    private bool _isPrepared;

    public bool IsPrepared => _isPrepared;
    public bool IsPlaying => videoPlayer != null && videoPlayer.isPlaying;
    public double CurrentTime => videoPlayer != null ? videoPlayer.time : 0d;
    public double Duration => videoPlayer != null ? videoPlayer.length : 0d;

    private void Awake()
    {
        if (videoPlayer != null)
        {
            videoPlayer.playOnAwake = false;
            videoPlayer.waitForFirstFrame = true;
            videoPlayer.skipOnDrop = true;

            videoPlayer.prepareCompleted += HandlePrepareCompleted;
            videoPlayer.errorReceived += HandleErrorReceived;
            videoPlayer.loopPointReached += HandleLoopPointReached;
        }
    }

    private void OnDestroy()
    {
        if (videoPlayer != null)
        {
            videoPlayer.prepareCompleted -= HandlePrepareCompleted;
            videoPlayer.errorReceived -= HandleErrorReceived;
            videoPlayer.loopPointReached -= HandleLoopPointReached;
        }

        StopPrepareTimeoutRoutine();
    }

    private void Update()
    {
        if (!_isPrepared || videoPlayer == null)
            return;

        TimeChanged?.Invoke(videoPlayer.time, videoPlayer.length);
    }

    public void Prepare(string url)
    {
        if (videoPlayer == null)
        {
            Failed?.Invoke("VideoPlayer is not assigned.");
            return;
        }

        if (string.IsNullOrWhiteSpace(url))
        {
            Failed?.Invoke("This project has no video.");
            return;
        }

        StopPlayback();

        _isPrepared = false;

        if (videoOutputImage != null)
            videoOutputImage.texture = null;

        videoPlayer.source = VideoSource.Url;
        videoPlayer.url = url;
        videoPlayer.Prepare();

        StopPrepareTimeoutRoutine();
        _prepareTimeoutRoutine = StartCoroutine(PrepareTimeoutRoutine(prepareTimeoutSeconds));
    }

    public void Play()
    {
        if (!_isPrepared || videoPlayer == null)
            return;

        videoPlayer.Play();
        PlayStateChanged?.Invoke(true);
    }

    public void Pause()
    {
        if (!_isPrepared || videoPlayer == null)
            return;

        videoPlayer.Pause();
        PlayStateChanged?.Invoke(false);
    }

    public void StopPlayback()
    {
        StopPrepareTimeoutRoutine();

        if (videoPlayer != null)
        {
            if (videoPlayer.isPlaying)
                videoPlayer.Stop();

            videoPlayer.targetTexture = null;
        }

        if (videoOutputImage != null)
            videoOutputImage.texture = null;

        _isPrepared = false;
        PlayStateChanged?.Invoke(false);
        TimeChanged?.Invoke(0d, 0d);
    }

    public void SeekNormalized(float normalizedValue)
    {
        if (!_isPrepared || videoPlayer == null)
            return;

        if (videoPlayer.length <= 0.01d)
            return;

        double targetTime = Mathf.Clamp01(normalizedValue) * videoPlayer.length;
        videoPlayer.time = targetTime;
        TimeChanged?.Invoke(videoPlayer.time, videoPlayer.length);
    }

    private IEnumerator PrepareTimeoutRoutine(float timeoutSeconds)
    {
        float timer = timeoutSeconds;

        while (timer > 0f && !_isPrepared)
        {
            timer -= Time.unscaledDeltaTime;
            yield return null;
        }

        _prepareTimeoutRoutine = null;

        if (_isPrepared)
            yield break;

        Failed?.Invoke("Unable to load video.");
    }

    private void HandlePrepareCompleted(VideoPlayer source)
    {
        StopPrepareTimeoutRoutine();

        _isPrepared = true;

        if (videoOutputImage != null)
            videoOutputImage.texture = source.texture;

        UpdateAspectRatio(source);

        Prepared?.Invoke();
        PlayStateChanged?.Invoke(false);
        TimeChanged?.Invoke(source.time, source.length);
    }

    private void HandleErrorReceived(VideoPlayer source, string message)
    {
        StopPrepareTimeoutRoutine();

        _isPrepared = false;
        Failed?.Invoke(string.IsNullOrWhiteSpace(message) ? "Unable to load video." : message);
    }

    private void HandleLoopPointReached(VideoPlayer source)
    {
        PlayStateChanged?.Invoke(false);
        Finished?.Invoke();
    }

    private void UpdateAspectRatio(VideoPlayer source)
    {
        if (aspectRatioFitter == null || source == null)
            return;

        Texture texture = source.texture;
        if (texture == null || texture.height == 0)
            return;

        aspectRatioFitter.aspectRatio = (float)texture.width / texture.height;
    }

    private void StopPrepareTimeoutRoutine()
    {
        if (_prepareTimeoutRoutine != null)
        {
            StopCoroutine(_prepareTimeoutRoutine);
            _prepareTimeoutRoutine = null;
        }
    }
}