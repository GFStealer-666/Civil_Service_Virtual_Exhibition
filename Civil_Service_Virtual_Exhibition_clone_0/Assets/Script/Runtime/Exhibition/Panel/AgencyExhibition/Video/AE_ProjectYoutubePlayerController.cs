using System;
using System.Collections;
using LightShaft.Scripts;
using UnityEngine;

public class AE_ProjectYoutubePlayerController : YoutubePlayer, IAE_ProjectVideoPlaybackController
{
    public event Action Prepared;
    public event Action<string> Failed;
    public event Action<bool> PlayStateChanged;
    public event Action<double, double> TimeChanged;
    public event Action Finished;

    [Header("Adapter")]
    [SerializeField] private float prepareTimeoutSeconds = 20f;
    [SerializeField] private bool allowSeeking = false;

    private Coroutine _prepareTimeoutRoutine;
    private bool _isPreparing;
    private bool _isPrepared;
    private bool _isPlaying;

    public bool IsPrepared => _isPrepared;
    public bool IsPreparing => _isPreparing;
    public bool IsPlaying => _isPlaying;
    public double CurrentTime => ReadCurrentTime();
    public double Duration => ReadDuration();

    public override void Start()
    {
        base.Start();

        if (events != null)
        {
            events.OnVideoReadyToStart.AddListener(HandleReady);
            events.OnVideoFinished.AddListener(HandleFinishedInternal);
            events.OnVideoStarted.AddListener(HandleStarted);
            events.OnVideoPaused.AddListener(HandlePaused);
            events.OnVideoResumed.AddListener(HandleResumed);
        }
    }

    private void OnDestroy()
    {
        if (events != null)
        {
            events.OnVideoReadyToStart.RemoveListener(HandleReady);
            events.OnVideoFinished.RemoveListener(HandleFinishedInternal);
            events.OnVideoStarted.RemoveListener(HandleStarted);
            events.OnVideoPaused.RemoveListener(HandlePaused);
            events.OnVideoResumed.RemoveListener(HandleResumed);
        }

        StopPrepareTimeoutRoutine();
    }

    private void Update()
    {
        if (!_isPrepared)
            return;

        TimeChanged?.Invoke(ReadCurrentTime(), ReadDuration());
    }

    public void Prepare(string url)
    {
        Debug.Log($"[AE_ProjectYoutubePlayerController] {url}");
        if (string.IsNullOrWhiteSpace(url))
        {
            Failed?.Invoke("This project has no video.");
            return;
        }

        StopPlayback();

        _isPreparing = true;
        _isPrepared = false;
        _isPlaying = false;

        PreLoadVideo(url);

        StopPrepareTimeoutRoutine();
        _prepareTimeoutRoutine = StartCoroutine(PrepareTimeoutRoutine(prepareTimeoutSeconds));
    }

    public override void Play()
    {
        if (!_isPrepared)
            return;

        base.Play();
        _isPlaying = true;
        PlayStateChanged?.Invoke(true);
    }

    public void Pause()
    {
        if (!_isPrepared || !_isPlaying)
            return;

        PlayPause();
        _isPlaying = false;
        PlayStateChanged?.Invoke(false);
    }

    public void CancelPrepare()
    {
        if (!_isPreparing)
            return;

        StopPlayback();
    }

    public void StopPlayback()
    {
        StopPrepareTimeoutRoutine();

        _isPreparing = false;
        _isPrepared = false;
        _isPlaying = false;

        Stop();

        PlayStateChanged?.Invoke(false);
        TimeChanged?.Invoke(0d, 0d);
    }

    public void SeekNormalized(float normalizedValue)
    {
        if (!allowSeeking || !_isPrepared)
            return;

        double duration = ReadDuration();
        if (duration <= 0.01d)
            return;

        double targetTime = Mathf.Clamp01(normalizedValue) * duration;

        if (videoQuality == YoutubeVideoQuality.Standard || audioPlayer == null)
            videoPlayer.time = targetTime;
        else
            audioPlayer.time = targetTime;

        TimeChanged?.Invoke(ReadCurrentTime(), duration);
    }

    private void HandleReady()
    {
        StopPrepareTimeoutRoutine();

        _isPreparing = false;
        _isPrepared = true;
        _isPlaying = false;

        Prepared?.Invoke();
        PlayStateChanged?.Invoke(false);
        TimeChanged?.Invoke(ReadCurrentTime(), ReadDuration());
    }

    private void HandleStarted()
    {
        _isPlaying = true;
        PlayStateChanged?.Invoke(true);
    }

    private void HandlePaused()
    {
        _isPlaying = false;
        PlayStateChanged?.Invoke(false);
    }

    private void HandleResumed()
    {
        _isPlaying = true;
        PlayStateChanged?.Invoke(true);
    }

    private void HandleFinishedInternal()
    {
        _isPlaying = false;
        PlayStateChanged?.Invoke(false);
        Finished?.Invoke();
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

        StopPlayback();
        Failed?.Invoke("Unable to load YouTube video.");
    }

    private void StopPrepareTimeoutRoutine()
    {
        if (_prepareTimeoutRoutine != null)
        {
            StopCoroutine(_prepareTimeoutRoutine);
            _prepareTimeoutRoutine = null;
        }
    }

    private double ReadCurrentTime()
    {
        if (!_isPrepared)
            return 0d;

        if (videoQuality == YoutubeVideoQuality.Standard || audioPlayer == null)
            return videoPlayer != null ? videoPlayer.time : 0d;

        return audioPlayer.time;
    }

    private double ReadDuration()
    {
        if (!_isPrepared)
            return 0d;

        if (videoQuality == YoutubeVideoQuality.Standard || audioPlayer == null)
            return videoPlayer != null ? videoPlayer.length : 0d;

        return audioPlayer.length;
    }
}