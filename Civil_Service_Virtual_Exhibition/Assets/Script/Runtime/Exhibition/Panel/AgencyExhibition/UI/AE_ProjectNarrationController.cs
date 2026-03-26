using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class AE_ProjectNarrationController : MonoBehaviour, IMediaControllable
{
    [Header("Audio")]
    [SerializeField] private ExhibitionAudioSource narratorAudioSource;
    [SerializeField] private AudioType audioType = AudioType.MPEG;

    [Header("Overlay")]
    [SerializeField] private StatusOverlay overlay;
    [SerializeField] private string loadingTitle = "กำลังดาวน์โหลดเสียงบรรยาย";
    [SerializeField] private string loadingSubtitle = "กรุณารอสักครู่";
    [SerializeField] private string failedTitle = "โหลดเสียงบรรยายไม่สำเร็จ";
    [SerializeField] private string failedSubtitle = "กรุณาลองใหม่อีกครั้ง";
    [SerializeField] private string emptyUrlTitle = "ไม่สามารถโหลดเสียงบรรยาย";
    [SerializeField] private string emptyUrlSubtitle = "ไม่พบลิงก์เสียงบรรยาย";
    [SerializeField] private string missingAudioSourceTitle = "ไม่สามารถเล่นเสียงบรรยาย";
    [SerializeField] private string missingAudioSourceSubtitle = "ไม่พบ Audio Source";
    [SerializeField] private string loadingCancelLabel = "ยกเลิก";

    [Header("Coordination")]
    [SerializeField] private MediaSessionCoordinator mediaCoordinator;

    [Header("Runtime Visual")]
    [SerializeField] private GameObject rawImage;

    public MediaPlaybackState State { get; private set; } = MediaPlaybackState.Idle;
    public event Action<MediaPlaybackState> StateChanged;

    private Coroutine _downloadRoutine;
    private Coroutine _monitorRoutine;
    private UnityWebRequest _activeRequest;

    private bool _cancelRequested;
    private string _currentUrl = string.Empty;
    private AudioClip _currentClip;

    private void Awake()
    {
        mediaCoordinator?.Register(this);
        ApplyNarratorPlayingObject(State);
    }

    private void OnDestroy()
    {
        mediaCoordinator?.Unregister(this);
        ResetSession(true);
        ApplyNarratorPlayingObject(MediaPlaybackState.Idle);
    }

    public void ToggleFromUrl(string url)
    {
        if (State == MediaPlaybackState.Loading)
        {
            CancelMediaLoading();
            return;
        }

        if (State == MediaPlaybackState.Playing)
        {
            StopMedia();
            return;
        }

        PlayFromUrl(url);
    }

    public void PlayFromUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            overlay?.ShowFailed(emptyUrlTitle, emptyUrlSubtitle);
            SetState(MediaPlaybackState.Failed);
            return;
        }

        mediaCoordinator?.TakeFocus(this);

        ResetSession(true);

        _currentUrl = url;
        _downloadRoutine = StartCoroutine(DownloadAndPlay(url));
    }

    public void StopMedia()
    {
        StopInternal(clearClip: true, restoreBgm: true, nextState: MediaPlaybackState.Stopped);
    }

    public void CancelMediaLoading()
    {
        if (State != MediaPlaybackState.Loading && _downloadRoutine == null && _activeRequest == null)
            return;

        _cancelRequested = true;
        StopInternal(clearClip: false, restoreBgm: true, nextState: MediaPlaybackState.Canceled);
    }

    public void ResetSession(bool restoreBgm = true)
    {
        StopInternal(clearClip: true, restoreBgm: restoreBgm, nextState: MediaPlaybackState.Idle);
    }

    private IEnumerator DownloadAndPlay(string url)
    {
        _cancelRequested = false;

        SetState(MediaPlaybackState.Loading);

        overlay?.ShowLoading(
            loadingTitle,
            loadingSubtitle,
            showBlocker: true,
            cancelable: true,
            cancelButtonLabel: loadingCancelLabel,
            onCancel: CancelMediaLoading
        );

        _activeRequest = CreateAudioRequest(url);

        yield return _activeRequest.SendWebRequest();

        UnityWebRequest finishedRequest = _activeRequest;
        _activeRequest = null;
        _downloadRoutine = null;

        if (_cancelRequested)
        {
            DisposeRequest(finishedRequest);
            yield break;
        }

        if (finishedRequest == null)
        {
            overlay?.ShowFailed(failedTitle, failedSubtitle);
            SetState(MediaPlaybackState.Failed);
            yield break;
        }

        string contentType = finishedRequest.GetResponseHeader("Content-Type");
        Debug.Log(
            $"[AE_ProjectNarrationController] TTS response | code={finishedRequest.responseCode} | " +
            $"type={contentType} | error={finishedRequest.error}"
        );

        if (finishedRequest.result != UnityWebRequest.Result.Success)
        {
            overlay?.ShowFailed(failedTitle, failedSubtitle);
            SetState(MediaPlaybackState.Failed);
            DisposeRequest(finishedRequest);
            yield break;
        }

        AudioClip clip = DownloadHandlerAudioClip.GetContent(finishedRequest);
        DisposeRequest(finishedRequest);

        if (clip == null)
        {
            overlay?.ShowFailed(failedTitle, "ไม่พบข้อมูลเสียงบรรยาย");
            SetState(MediaPlaybackState.Failed);
            yield break;
        }

        if (narratorAudioSource == null || narratorAudioSource.AudioSource == null)
        {
            Destroy(clip);
            overlay?.ShowFailed(missingAudioSourceTitle, missingAudioSourceSubtitle);
            SetState(MediaPlaybackState.Failed);
            yield break;
        }

        ReleaseCurrentClip();

        _currentClip = clip;

        ApplyNarrationBgmMute(true);

        narratorAudioSource.AudioSource.Stop();
        narratorAudioSource.AudioSource.clip = _currentClip;
        narratorAudioSource.AudioSource.Play();

        overlay?.Hide();
        SetState(MediaPlaybackState.Playing);

        if (_monitorRoutine != null)
        {
            StopCoroutine(_monitorRoutine);
            _monitorRoutine = null;
        }

        _monitorRoutine = StartCoroutine(MonitorPlayback());
    }

    private IEnumerator MonitorPlayback()
    {
        if (narratorAudioSource == null || narratorAudioSource.AudioSource == null)
        {
            ApplyNarrationBgmMute(false);
            SetState(MediaPlaybackState.Failed);
            _monitorRoutine = null;
            yield break;
        }

        AudioSource source = narratorAudioSource.AudioSource;

        while (source != null && source.isPlaying)
            yield return null;

        ApplyNarrationBgmMute(false);

        _monitorRoutine = null;

        if (State == MediaPlaybackState.Playing)
            SetState(MediaPlaybackState.Completed);
    }

    private UnityWebRequest CreateAudioRequest(string url)
    {
        string accessToken = PlayerPrefs.GetString("access_token", string.Empty);

        if (ApiService.Instance != null)
            return ApiService.Instance.GetAudioClip(url, audioType, accessToken);

        UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(url, audioType);

        if (!string.IsNullOrWhiteSpace(accessToken))
            request.SetRequestHeader("Authorization", $"Bearer {accessToken}");

        request.SetRequestHeader("Accept", "audio/mpeg,audio/*,*/*");

        DownloadHandlerAudioClip downloadHandler = request.downloadHandler as DownloadHandlerAudioClip;
        if (downloadHandler != null)
            downloadHandler.streamAudio = false;

        return request;
    }

    private void StopInternal(bool clearClip, bool restoreBgm, MediaPlaybackState nextState)
    {
        if (_downloadRoutine != null)
        {
            StopCoroutine(_downloadRoutine);
            _downloadRoutine = null;
        }

        if (_monitorRoutine != null)
        {
            StopCoroutine(_monitorRoutine);
            _monitorRoutine = null;
        }

        if (_activeRequest != null)
        {
            _activeRequest.Abort();
            DisposeRequest(_activeRequest);
            _activeRequest = null;
        }

        if (narratorAudioSource != null && narratorAudioSource.AudioSource != null)
        {
            narratorAudioSource.AudioSource.Stop();

            if (clearClip)
                narratorAudioSource.AudioSource.clip = null;
        }

        if (clearClip)
            ReleaseCurrentClip();

        if (restoreBgm)
            ApplyNarrationBgmMute(false);

        overlay?.Hide();

        SetState(nextState);
    }

    private void ReleaseCurrentClip()
    {
        if (_currentClip == null)
            return;

        Destroy(_currentClip);
        _currentClip = null;
    }

    private void DisposeRequest(UnityWebRequest request)
    {
        if (request == null)
            return;

        request.Dispose();
    }

    private void ApplyNarrationBgmMute(bool mute)
    {
        if (ExhibitionAudioManager.Instance == null)
            return;

        if (mute)
            ExhibitionAudioManager.Instance.SetTemporaryBgmVolume(0f);
        else
            ExhibitionAudioManager.Instance.ClearTemporaryBgmVolume();
    }

    private void ApplyNarratorPlayingObject(MediaPlaybackState state)
    {
        if (rawImage == null)
            return;

        rawImage.SetActive(state == MediaPlaybackState.Playing);
    }

    private void SetState(MediaPlaybackState state)
    {
        if (State == state)
            return;

        State = state;
        ApplyNarratorPlayingObject(State);
        StateChanged?.Invoke(State);
    }
}