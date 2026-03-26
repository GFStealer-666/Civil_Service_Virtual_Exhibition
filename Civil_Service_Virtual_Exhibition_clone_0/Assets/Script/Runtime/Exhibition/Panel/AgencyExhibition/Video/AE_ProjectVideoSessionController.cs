using System;
using UnityEngine;

public class AE_ProjectVideoSessionController : MonoBehaviour,
    IMediaControllable,
    IAE_ProjectVideoPlaybackController
{
    [Header("References")]
    [SerializeField] private AE_ProjectYoutubePlayerController youtubePlayerController;
    [SerializeField] private StatusOverlay overlay;
    [SerializeField] private MediaSessionCoordinator mediaCoordinator;

    [Header("Behavior")]
    [SerializeField] private bool autoPlayOnPrepared = true;

    [Header("Overlay Messages")]
    [SerializeField] private string overlayLoadingTitle = "กำลังเตรียมวิดีโอ";
    [SerializeField] private string overlayLoadingSubtitle = "กรุณารอสักครู่";
    [SerializeField] private string overlayLoadingCancelLabel = "ยกเลิก";
    [SerializeField] private string overlayFailedTitle = "โหลดวิดีโอไม่สำเร็จ";
    [SerializeField] private string overlayFailedSubtitle = "กรุณาลองใหม่อีกครั้ง";
    [SerializeField] private string overlayMissingUrlTitle = "ไม่สามารถเล่นวิดีโอ";
    [SerializeField] private string overlayMissingUrlSubtitle = "ไม่พบลิงก์วิดีโอ";
    [SerializeField] private string overlayMissingApiTitle = "ไม่สามารถเล่นวิดีโอ";
    [SerializeField] private string overlayMissingApiSubtitle = "ApiService ยังไม่พร้อมใช้งาน";

    public MediaPlaybackState State { get; private set; } = MediaPlaybackState.Idle;

    public event Action<MediaPlaybackState> StateChanged;
    public event Action LoadingCanceled;
    public event Action FailureAcknowledged;

    public event Action Prepared;
    public event Action<string> Failed;
    public event Action<bool> PlayStateChanged;
    public event Action<double, double> TimeChanged;
    public event Action Finished;

    public bool IsPrepared => youtubePlayerController != null && youtubePlayerController.IsPrepared;
    public bool IsPreparing => youtubePlayerController != null && youtubePlayerController.IsPreparing;
    public bool IsPlaying => youtubePlayerController != null && youtubePlayerController.IsPlaying;
    public double CurrentTime => youtubePlayerController != null ? youtubePlayerController.CurrentTime : 0d;
    public double Duration => youtubePlayerController != null ? youtubePlayerController.Duration : 0d;

    private string _currentResolvedUrl = string.Empty;

    private void Awake()
    {
        mediaCoordinator?.Register(this);
        BindBackend();
    }

    private void OnDestroy()
    {
        ResetSession();
        UnbindBackend();
        mediaCoordinator?.Unregister(this);
    }

    public void Prepare(string url)
    {
        Debug.Log($"[AE_ProjectVideoSessionController] : {url}");
        PrepareAndPlay(url);
    }

    public void PrepareAndPlay(string rawUrl)
    {
        if (ApiService.Instance == null)
        {
            ShowFailureOverlay(overlayMissingApiTitle, overlayMissingApiSubtitle);
            return;
        }

        string resolvedUrl = ApiService.Instance.ResolveUrl(rawUrl);

        if (string.IsNullOrWhiteSpace(resolvedUrl))
        {
            ShowFailureOverlay(overlayMissingUrlTitle, overlayMissingUrlSubtitle);
            return;
        }

        if (youtubePlayerController == null)
        {
            ShowFailureOverlay(overlayFailedTitle, "YouTube player is not assigned.");
            return;
        }

        mediaCoordinator?.TakeFocus(this);

        _currentResolvedUrl = resolvedUrl;

        overlay?.ShowLoading(
            overlayLoadingTitle,
            overlayLoadingSubtitle,
            showBlocker: true,
            cancelable: true,
            cancelButtonLabel: overlayLoadingCancelLabel,
            onCancel: CancelMediaLoading,
            animateDots: true
        );

        SetState(MediaPlaybackState.Loading);
        youtubePlayerController.Prepare(resolvedUrl);
    }

    public void Play()
    {
        if (State == MediaPlaybackState.Loading)
            return;

        youtubePlayerController?.Play();
    }

    public void Pause()
    {
        youtubePlayerController?.Pause();
    }

    public void CancelPrepare()
    {
        CancelMediaLoading();
    }

    public void CancelMediaLoading()
    {
        if (youtubePlayerController == null || !youtubePlayerController.IsPreparing)
            return;

        youtubePlayerController.CancelPrepare();
        overlay?.Hide();
        _currentResolvedUrl = string.Empty;
        SetState(MediaPlaybackState.Canceled);
        LoadingCanceled?.Invoke();
    }

    public void StopPlayback()
    {
        StopMedia();
    }

    public void StopMedia()
    {
        overlay?.Hide();
        youtubePlayerController?.StopPlayback();
        _currentResolvedUrl = string.Empty;
        SetState(MediaPlaybackState.Stopped);
    }

    public void ResetSession()
    {
        overlay?.Hide();
        youtubePlayerController?.StopPlayback();
        _currentResolvedUrl = string.Empty;
        SetState(MediaPlaybackState.Idle);
    }

    public void SeekNormalized(float normalizedValue)
    {
        youtubePlayerController?.SeekNormalized(normalizedValue);
    }

    private void BindBackend()
    {
        if (youtubePlayerController == null)
            return;

        youtubePlayerController.Prepared += HandleBackendPrepared;
        youtubePlayerController.Failed += HandleBackendFailed;
        youtubePlayerController.PlayStateChanged += HandleBackendPlayStateChanged;
        youtubePlayerController.TimeChanged += HandleBackendTimeChanged;
        youtubePlayerController.Finished += HandleBackendFinished;
    }

    private void UnbindBackend()
    {
        if (youtubePlayerController == null)
            return;

        youtubePlayerController.Prepared -= HandleBackendPrepared;
        youtubePlayerController.Failed -= HandleBackendFailed;
        youtubePlayerController.PlayStateChanged -= HandleBackendPlayStateChanged;
        youtubePlayerController.TimeChanged -= HandleBackendTimeChanged;
        youtubePlayerController.Finished -= HandleBackendFinished;
    }

    private void HandleBackendPrepared()
    {
        overlay?.Hide();
        Prepared?.Invoke();

        if (autoPlayOnPrepared)
            youtubePlayerController?.Play();
        else
            SetState(MediaPlaybackState.Stopped);
    }

    private void HandleBackendFailed(string message)
    {
        Debug.LogWarning($"[AE_ProjectVideoSessionController] Video failed: {message}");

        ShowFailureOverlay(
            overlayFailedTitle,
            string.IsNullOrWhiteSpace(message) ? overlayFailedSubtitle : message
        );

        Failed?.Invoke(message);
    }

    private void HandleBackendPlayStateChanged(bool isPlaying)
    {
        PlayStateChanged?.Invoke(isPlaying);

        if (isPlaying)
        {
            SetState(MediaPlaybackState.Playing);
            return;
        }

        if (State == MediaPlaybackState.Canceled ||
            State == MediaPlaybackState.Failed ||
            State == MediaPlaybackState.Completed)
            return;

        if (youtubePlayerController != null && youtubePlayerController.IsPrepared)
            SetState(MediaPlaybackState.Stopped);
    }

    private void HandleBackendTimeChanged(double current, double duration)
    {
        TimeChanged?.Invoke(current, duration);
    }

    private void HandleBackendFinished()
    {
        Finished?.Invoke();
        SetState(MediaPlaybackState.Completed);
    }

    private void ShowFailureOverlay(string title, string subtitle)
    {
        overlay?.ShowFailed(
            title,
            subtitle,
            onDismissed: HandleFailureDismissed,
            showBlocker: true
        );

        SetState(MediaPlaybackState.Failed);
    }

    private void HandleFailureDismissed()
    {
        overlay?.Hide();
        youtubePlayerController?.StopPlayback();
        _currentResolvedUrl = string.Empty;
        FailureAcknowledged?.Invoke();
    }

    private void SetState(MediaPlaybackState state)
    {
        if (State == state)
            return;

        State = state;
        StateChanged?.Invoke(State);
    }
}