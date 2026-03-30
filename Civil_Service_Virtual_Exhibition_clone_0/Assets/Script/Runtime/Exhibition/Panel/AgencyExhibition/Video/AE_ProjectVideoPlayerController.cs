using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

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
    private Coroutine _seekRoutine;
    private bool _isPrepared;
    private bool _isPreparing;
    private bool _cancelPrepareRequested;
    private bool _isPlaying;

    public bool IsPrepared => _isPrepared;
    public bool IsPreparing => _isPreparing;
    public bool IsPlaying => _isPlaying;
    public double CurrentTime => videoPlayer != null ? videoPlayer.time : 0d;
    public double Duration => videoPlayer != null ? videoPlayer.length : 0d;

    private void Awake()
    {
        if (videoPlayer == null)
            return;

        videoPlayer.playOnAwake = false;
        videoPlayer.waitForFirstFrame = true;
        videoPlayer.skipOnDrop = true;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;

        videoPlayer.prepareCompleted += HandlePrepareCompleted;
        videoPlayer.errorReceived += HandleErrorReceived;
        videoPlayer.loopPointReached += HandleLoopPointReached;
        videoPlayer.started += HandleStarted;
    }

    private void OnDestroy()
    {
        if (videoPlayer != null)
        {
            videoPlayer.prepareCompleted -= HandlePrepareCompleted;
            videoPlayer.errorReceived -= HandleErrorReceived;
            videoPlayer.loopPointReached -= HandleLoopPointReached;
            videoPlayer.started -= HandleStarted;
        }

        StopPrepareTimeoutRoutine();

        if (_seekRoutine != null)
        {
            StopCoroutine(_seekRoutine);
            _seekRoutine = null;
        }
    }

    private void Update()
    {
        if (!_isPrepared || videoPlayer == null)
            return;

        if (_isPlaying != videoPlayer.isPlaying)
        {
            _isPlaying = videoPlayer.isPlaying;
            PlayStateChanged?.Invoke(_isPlaying);
        }

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

        Debug.Log($"[AE_ProjectVideoPlayerController] Preparing URL: {url}");

        StopPlaybackInternal(resetOutput: true, notifyState: true);

        _cancelPrepareRequested = false;
        _isPrepared = false;
        _isPreparing = true;
        _isPlaying = false;

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
    }

    public void Pause()
    {
        if (!_isPrepared || videoPlayer == null)
            return;

        videoPlayer.Pause();

        if (_isPlaying)
        {
            _isPlaying = false;
            PlayStateChanged?.Invoke(false);
        }
    }

    public void CancelPrepare()
    {
        if (!_isPreparing)
            return;

        _cancelPrepareRequested = true;
        StopPlaybackInternal(resetOutput: true, notifyState: true);
    }

    public void StopPlayback()
    {
        _cancelPrepareRequested = false;
        StopPlaybackInternal(resetOutput: true, notifyState: true);
    }

    public void SeekNormalized(float normalizedValue)
    {
        if (!_isPrepared || videoPlayer == null)
            return;

        if (videoPlayer.length <= 0.01d)
            return;

        if (!videoPlayer.canSetTime)
        {
            Debug.LogWarning("[AE_ProjectVideoPlayerController] VideoPlayer cannot seek this source.");
            return;
        }

        if (_seekRoutine != null)
            StopCoroutine(_seekRoutine);

        _seekRoutine = StartCoroutine(SeekRoutine(Mathf.Clamp01(normalizedValue)));
    }

    private IEnumerator SeekRoutine(float normalizedValue)
    {
        if (videoPlayer == null)
        {
            _seekRoutine = null;
            yield break;
        }

        bool resumeAfterSeek = videoPlayer.isPlaying;
        double duration = videoPlayer.length;
        double targetTime = normalizedValue * duration;

        if (targetTime >= duration)
            targetTime = Math.Max(0d, duration - 0.05d);

        videoPlayer.Pause();
        videoPlayer.time = targetTime;

        int waitedFrames = 0;
        const int maxFrames = 15;
        const double tolerance = 0.25d;

        while (videoPlayer != null && waitedFrames < maxFrames)
        {
            if (Math.Abs(videoPlayer.time - targetTime) <= tolerance)
                break;

            waitedFrames++;
            yield return null;
        }

        TimeChanged?.Invoke(videoPlayer.time, videoPlayer.length);

        if (videoPlayer != null && _isPrepared && resumeAfterSeek)
            videoPlayer.Play();

        _seekRoutine = null;
    }

    private IEnumerator PrepareTimeoutRoutine(float timeoutSeconds)
    {
        float timer = timeoutSeconds;

        while (timer > 0f && _isPreparing && !_isPrepared)
        {
            timer -= Time.unscaledDeltaTime;
            yield return null;
        }

        _prepareTimeoutRoutine = null;

        if (_isPrepared || !_isPreparing)
            yield break;

        StopPlaybackInternal(resetOutput: true, notifyState: true);
        Failed?.Invoke("Unable to load video.");
    }

    private void HandlePrepareCompleted(VideoPlayer source)
    {
        StopPrepareTimeoutRoutine();

        if (_cancelPrepareRequested)
        {
            _cancelPrepareRequested = false;
            StopPlaybackInternal(resetOutput: true, notifyState: true);
            return;
        }

        _isPreparing = false;
        _isPrepared = true;
        _isPlaying = false;

        if (videoOutputImage != null)
            videoOutputImage.texture = source.texture;

        UpdateAspectRatio(source);

        Prepared?.Invoke();
        TimeChanged?.Invoke(source.time, source.length);
    }

    private void HandleStarted(VideoPlayer source)
    {
        if (!_isPrepared)
            return;

        if (!_isPlaying)
        {
            _isPlaying = true;
            PlayStateChanged?.Invoke(true);
        }
    }

    private void HandleErrorReceived(VideoPlayer source, string message)
    {
        StopPrepareTimeoutRoutine();

        Debug.LogError(
            $"[AE_ProjectVideoPlayerController] URL failed: {source.url}\nError: {message}"
        );

        if (_cancelPrepareRequested)
        {
            _cancelPrepareRequested = false;
            StopPlaybackInternal(resetOutput: true, notifyState: true);
            return;
        }

        _isPreparing = false;
        _isPrepared = false;
        _isPlaying = false;

        Failed?.Invoke(string.IsNullOrWhiteSpace(message) ? "Unable to load video." : message);
    }

    private void HandleLoopPointReached(VideoPlayer source)
    {
        if (source == null)
            return;

        if (source.isLooping)
        {
            _isPlaying = true;
            TimeChanged?.Invoke(0d, source.length);
            return;
        }

        _isPlaying = false;
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

    private void StopPlaybackInternal(bool resetOutput, bool notifyState)
    {
        StopPrepareTimeoutRoutine();

        if (_seekRoutine != null)
        {
            StopCoroutine(_seekRoutine);
            _seekRoutine = null;
        }

        if (videoPlayer != null)
        {
            if (videoPlayer.isPlaying || videoPlayer.isPrepared)
                videoPlayer.Stop();

            videoPlayer.url = string.Empty;
            videoPlayer.clip = null;
            videoPlayer.targetTexture = null;
        }

        if (resetOutput && videoOutputImage != null)
            videoOutputImage.texture = null;

        _isPrepared = false;
        _isPreparing = false;
        _isPlaying = false;

        if (notifyState)
        {
            PlayStateChanged?.Invoke(false);
            TimeChanged?.Invoke(0d, 0d);
        }
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