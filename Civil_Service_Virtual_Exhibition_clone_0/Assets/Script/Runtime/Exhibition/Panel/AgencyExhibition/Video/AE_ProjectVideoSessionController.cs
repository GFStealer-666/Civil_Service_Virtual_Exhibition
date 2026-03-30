using System;
using UnityEngine;

public class AE_ProjectVideoSessionController : MonoBehaviour,
    IMediaControllable,
    IAE_ProjectVideoPlaybackController
{
    [Header("References")]
    [SerializeField] private AE_ProjectVideoPlayerController videoPlayerController;
    [SerializeField] private StatusOverlay overlay;
    [SerializeField] private MediaSessionCoordinator mediaCoordinator;

    [Header("Behavior")]
    [SerializeField] private bool autoPlayOnPrepared = true;

    [Header("Thai Messages")]
    [SerializeField] private string overlayLoadingTitleTh = "กำลังเตรียมวิดีโอ";
    [SerializeField] private string overlayLoadingSubtitleTh = "กรุณารอสักครู่";
    [SerializeField] private string overlayLoadingCancelLabelTh = "ยกเลิก";
    [SerializeField] private string overlayFailedTitleTh = "โหลดวิดีโอไม่สำเร็จ";
    [SerializeField] private string overlayFailedSubtitleTh = "กรุณาลองใหม่อีกครั้ง";
    [SerializeField] private string overlayMissingUrlTitleTh = "ไม่สามารถเล่นวิดีโอ";
    [SerializeField] private string overlayMissingUrlSubtitleTh = "ไม่พบลิงก์วิดีโอ";
    [SerializeField] private string overlayMissingApiTitleTh = "ไม่สามารถเล่นวิดีโอ";
    [SerializeField] private string overlayMissingApiSubtitleTh = "ApiService ยังไม่พร้อมใช้งาน";

    [Header("English Messages")]
    [SerializeField] private string overlayLoadingTitleEn = "Preparing video";
    [SerializeField] private string overlayLoadingSubtitleEn = "Please wait a moment";
    [SerializeField] private string overlayLoadingCancelLabelEn = "Cancel";
    [SerializeField] private string overlayFailedTitleEn = "Unable to load video";
    [SerializeField] private string overlayFailedSubtitleEn = "Please try again";
    [SerializeField] private string overlayMissingUrlTitleEn = "Unable to play video";
    [SerializeField] private string overlayMissingUrlSubtitleEn = "Video link was not found";
    [SerializeField] private string overlayMissingApiTitleEn = "Unable to play video";
    [SerializeField] private string overlayMissingApiSubtitleEn = "ApiService is not available";

    public MediaPlaybackState State { get; private set; } = MediaPlaybackState.Idle;

    public event Action<MediaPlaybackState> StateChanged;
    public event Action LoadingCanceled;
    public event Action FailureAcknowledged;

    public event Action Prepared;
    public event Action<string> Failed;
    public event Action<bool> PlayStateChanged;
    public event Action<double, double> TimeChanged;
    public event Action Finished;

    public bool IsPrepared => videoPlayerController != null && videoPlayerController.IsPrepared;
    public bool IsPreparing => videoPlayerController != null && videoPlayerController.IsPreparing;
    public bool IsPlaying => videoPlayerController != null && videoPlayerController.IsPlaying;
    public double CurrentTime => videoPlayerController != null ? videoPlayerController.CurrentTime : 0d;
    public double Duration => videoPlayerController != null ? videoPlayerController.Duration : 0d;

    private string _currentResolvedUrl = string.Empty;

    private bool UseEnglish
    {
        get
        {
            string code = LocalizationService.CurrentLocaleCode;
            return !string.IsNullOrWhiteSpace(code) &&
                   code.StartsWith("en", StringComparison.OrdinalIgnoreCase);
        }
    }

    private string OverlayLoadingTitle => UseEnglish ? overlayLoadingTitleEn : overlayLoadingTitleTh;
    private string OverlayLoadingSubtitle => UseEnglish ? overlayLoadingSubtitleEn : overlayLoadingSubtitleTh;
    private string OverlayLoadingCancelLabel => UseEnglish ? overlayLoadingCancelLabelEn : overlayLoadingCancelLabelTh;
    private string OverlayFailedTitle => UseEnglish ? overlayFailedTitleEn : overlayFailedTitleTh;
    private string OverlayFailedSubtitle => UseEnglish ? overlayFailedSubtitleEn : overlayFailedSubtitleTh;
    private string OverlayMissingUrlTitle => UseEnglish ? overlayMissingUrlTitleEn : overlayMissingUrlTitleTh;
    private string OverlayMissingUrlSubtitle => UseEnglish ? overlayMissingUrlSubtitleEn : overlayMissingUrlSubtitleTh;
    private string OverlayMissingApiTitle => UseEnglish ? overlayMissingApiTitleEn : overlayMissingApiTitleTh;
    private string OverlayMissingApiSubtitle => UseEnglish ? overlayMissingApiSubtitleEn : overlayMissingApiSubtitleTh;

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
        Debug.Log($"[AE_ProjectVideoSessionController] Prepare: {url}");
        PrepareAndPlay(url);
    }

    public void PrepareAndPlay(string rawUrl)
    {
        if (ApiService.Instance == null)
        {
            ShowFailureOverlay(OverlayMissingApiTitle, OverlayMissingApiSubtitle);
            return;
        }

        string resolvedUrl = ApiService.Instance.ResolveUrl(rawUrl);

        if (string.IsNullOrWhiteSpace(resolvedUrl))
        {
            ShowFailureOverlay(OverlayMissingUrlTitle, OverlayMissingUrlSubtitle);
            return;
        }

        if (videoPlayerController == null)
        {
            ShowFailureOverlay(OverlayFailedTitle, OverlayFailedSubtitle);
            return;
        }

        mediaCoordinator?.TakeFocus(this);

        _currentResolvedUrl = resolvedUrl;

        overlay?.ShowLoading(
            OverlayLoadingTitle,
            OverlayLoadingSubtitle,
            showBlocker: true,
            cancelable: true,
            cancelButtonLabel: OverlayLoadingCancelLabel,
            onCancel: CancelMediaLoading,
            animateDots: true
        );

        SetState(MediaPlaybackState.Loading);
        videoPlayerController.Prepare(resolvedUrl);
    }

    public void Play()
    {
        if (State == MediaPlaybackState.Loading)
            return;

        videoPlayerController?.Play();
    }

    public void Pause()
    {
        videoPlayerController?.Pause();
    }

    public void CancelPrepare()
    {
        CancelMediaLoading();
    }

    public void CancelMediaLoading()
    {
        if (videoPlayerController == null || !videoPlayerController.IsPreparing)
            return;

        videoPlayerController.CancelPrepare();
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
        ApplyVideoBgmMute(false);

        overlay?.Hide();
        videoPlayerController?.StopPlayback();
        _currentResolvedUrl = string.Empty;
        SetState(MediaPlaybackState.Stopped);
    }

    public void ResetSession()
    {
        ApplyVideoBgmMute(false);

        overlay?.Hide();
        videoPlayerController?.StopPlayback();
        _currentResolvedUrl = string.Empty;
        SetState(MediaPlaybackState.Idle);
    }

    public void SeekNormalized(float normalizedValue)
    {
        videoPlayerController?.SeekNormalized(normalizedValue);
    }

    private void BindBackend()
    {
        if (videoPlayerController == null)
            return;

        videoPlayerController.Prepared += HandleBackendPrepared;
        videoPlayerController.Failed += HandleBackendFailed;
        videoPlayerController.PlayStateChanged += HandleBackendPlayStateChanged;
        videoPlayerController.TimeChanged += HandleBackendTimeChanged;
        videoPlayerController.Finished += HandleBackendFinished;
    }

    private void UnbindBackend()
    {
        if (videoPlayerController == null)
            return;

        videoPlayerController.Prepared -= HandleBackendPrepared;
        videoPlayerController.Failed -= HandleBackendFailed;
        videoPlayerController.PlayStateChanged -= HandleBackendPlayStateChanged;
        videoPlayerController.TimeChanged -= HandleBackendTimeChanged;
        videoPlayerController.Finished -= HandleBackendFinished;
    }

    private void HandleBackendPrepared()
    {
        overlay?.Hide();
        Prepared?.Invoke();

        if (autoPlayOnPrepared)
            videoPlayerController?.Play();
        else
            SetState(MediaPlaybackState.Stopped);
    }

    private void HandleBackendFailed(string message)
    {
        ApplyVideoBgmMute(false);

        Debug.LogWarning($"[AE_ProjectVideoSessionController] Video failed: {message}");

        ShowFailureOverlay(
            OverlayFailedTitle,
            OverlayFailedSubtitle
        );

        Failed?.Invoke(message);
    }

    private void HandleBackendPlayStateChanged(bool isPlaying)
    {
        PlayStateChanged?.Invoke(isPlaying);

        ApplyVideoBgmMute(isPlaying);

        if (isPlaying)
        {
            SetState(MediaPlaybackState.Playing);
            return;
        }

        if (State == MediaPlaybackState.Canceled ||
            State == MediaPlaybackState.Failed ||
            State == MediaPlaybackState.Completed)
            return;

        if (videoPlayerController != null && videoPlayerController.IsPrepared)
            SetState(MediaPlaybackState.Stopped);
    }

    private void HandleBackendTimeChanged(double current, double duration)
    {
        TimeChanged?.Invoke(current, duration);
    }

    private void HandleBackendFinished()
    {
        ApplyVideoBgmMute(false);
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
        ApplyVideoBgmMute(false);
        overlay?.Hide();
        videoPlayerController?.StopPlayback();
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

    private void ApplyVideoBgmMute(bool mute)
    {
        if (ExhibitionAudioManager.Instance == null)
            return;

        if (mute)
            ExhibitionAudioManager.Instance.SetTemporaryBgmVolume(0f);
        else
            ExhibitionAudioManager.Instance.ClearTemporaryBgmVolume();
    }
}