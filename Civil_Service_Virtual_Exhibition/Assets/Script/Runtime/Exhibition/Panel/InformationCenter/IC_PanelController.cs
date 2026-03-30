using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Networking;
using UnityEngine.UI;

[Serializable]
public class IC_LocalizedSection
{
    [Header("UI")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI subtitleText;

    [Header("Thai")]
    public string headerTh;

    [TextArea(2, 8)]
    public string subtitleTh;

    [Header("English")]
    public string headerEn;

    [TextArea(2, 8)]
    public string subtitleEn;
}

public class IC_PanelController : MonoBehaviour, IMediaControllable
{
    [Header("Panel")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button narratorButton;

    [Header("Main UI")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI subtitleText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI narrationLabelText;
    [SerializeField] private Image pictureImage;

    [Header("Main Thai Content")]
    [SerializeField] private string titleTh;
    [SerializeField] private string subtitleTh;
    [SerializeField] private string narrationLabelTh;

    [TextArea(4, 12)]
    [SerializeField] private string descriptionTh;

    [SerializeField] private Sprite imageTh;
    [SerializeField] private string narrationFileTh;

    [Header("Main English Content")]
    [SerializeField] private string titleEn;
    [SerializeField] private string subtitleEn;
    [SerializeField] private string narrationLabelEn;

    [TextArea(4, 12)]
    [SerializeField] private string descriptionEn;

    [SerializeField] private Sprite imageEn;
    [SerializeField] private string narrationFileEn;


    [Header("Localized Sections")]
    [SerializeField] private List<IC_LocalizedSection> sections = new List<IC_LocalizedSection>();

    [Header("Narration")]
    [SerializeField] private AudioType narrationAudioType = AudioType.MPEG;
    [SerializeField] private ExhibitionAudioSource narratorAudioSource;
    [SerializeField] private MediaSessionCoordinator mediaCoordinator;
    [SerializeField] private StatusOverlay overlay;
    [SerializeField] private GameObject narratorPlayingObject;

    [Header("Overlay Text TH")]
    [SerializeField] private string loadingTitleTh = "กำลังโหลดเสียงบรรยาย";
    [SerializeField] private string loadingSubtitleTh = "กรุณารอสักครู่";
    [SerializeField] private string loadingCancelLabelTh = "ยกเลิก";

    [SerializeField] private string missingClipTitleTh = "ไม่พบเสียงบรรยาย";
    [SerializeField] private string missingClipSubtitleTh = "กรุณาตรวจสอบไฟล์ใน StreamingAssets";

    [SerializeField] private string failedTitleTh = "โหลดเสียงบรรยายไม่สำเร็จ";
    [SerializeField] private string failedSubtitleTh = "กรุณาลองใหม่อีกครั้ง";

    [SerializeField] private string missingAudioSourceTitleTh = "ไม่สามารถเล่นเสียงบรรยาย";
    [SerializeField] private string missingAudioSourceSubtitleTh = "ไม่พบ Audio Source";

    [Header("Overlay Text EN")]
    [SerializeField] private string loadingTitleEn = "Loading narration";
    [SerializeField] private string loadingSubtitleEn = "Please wait";
    [SerializeField] private string loadingCancelLabelEn = "Cancel";

    [SerializeField] private string missingClipTitleEn = "Narration not found";
    [SerializeField] private string missingClipSubtitleEn = "Please check the file in StreamingAssets";

    [SerializeField] private string failedTitleEn = "Failed to load narration";
    [SerializeField] private string failedSubtitleEn = "Please try again";

    [SerializeField] private string missingAudioSourceTitleEn = "Unable to play narration";
    [SerializeField] private string missingAudioSourceSubtitleEn = "Audio Source is missing";

    [Header("Behavior")]
    [SerializeField] private bool refreshOnEnable = true;
    [SerializeField] private bool stopNarrationOnClose = true;
    [SerializeField] private bool stopNarrationWhenLocaleChanges = true;
    [SerializeField] private GameObject objectToEnableWhenNarrationPlays;

    [Header("Events")]
    [SerializeField] private UnityEvent onNarrationStopped;

    public MediaPlaybackState State { get; private set; } = MediaPlaybackState.Idle;
    public event Action<MediaPlaybackState> StateChanged;

    public bool HasTarget => !string.IsNullOrWhiteSpace(GetActiveNarrationFile());

    private Coroutine _monitorRoutine;
    private Coroutine _loadRoutine;
    private UnityWebRequest _activeRequest;
    private AudioClip _runtimeClip;
    private bool _cancelRequested;
    private bool _localeSubscribed;

    private void Awake()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(ClosePanel);
            closeButton.onClick.AddListener(ClosePanel);
        }

        if (narratorButton != null)
        {
            narratorButton.onClick.RemoveListener(ToggleNarration);
            narratorButton.onClick.AddListener(ToggleNarration);
        }

        mediaCoordinator?.Register(this);
        ApplyNarratorPlayingObject(State);
    }

    private void OnEnable()
    {
        SubscribeLocaleChanged();

        if (refreshOnEnable)
            StartCoroutine(RefreshWhenLocalizationReady());
    }

    private void OnDisable()
    {
        UnsubscribeLocaleChanged();

        if (stopNarrationOnClose)
            StopMedia();
    }

    private void OnDestroy()
    {
        UnsubscribeLocaleChanged();
        mediaCoordinator?.Unregister(this);
        StopInternal(clearClip: true, restoreBgm: true, nextState: MediaPlaybackState.Idle);
        ApplyNarratorPlayingObject(MediaPlaybackState.Idle);
    }

    private IEnumerator RefreshWhenLocalizationReady()
    {
        yield return LocalizationSettings.InitializationOperation;
        RefreshView();
    }

    private void SubscribeLocaleChanged()
    {
        if (_localeSubscribed)
            return;

        LocalizationSettings.SelectedLocaleChanged += HandleLocaleChanged;
        _localeSubscribed = true;
    }

    private void UnsubscribeLocaleChanged()
    {
        if (!_localeSubscribed)
            return;

        LocalizationSettings.SelectedLocaleChanged -= HandleLocaleChanged;
        _localeSubscribed = false;
    }

    private void HandleLocaleChanged(Locale locale)
    {
        RefreshView();

        if (stopNarrationWhenLocaleChanges &&
            (State == MediaPlaybackState.Playing || State == MediaPlaybackState.Loading))
        {
            StopMedia();
        }
    }

    [ContextMenu("Refresh View")]
    public void RefreshView()
    {
        bool isEnglish = IsEnglish();

        if (titleText != null)
        {
            titleText.text = isEnglish
                ? FirstNotEmpty(titleEn, titleTh)
                : FirstNotEmpty(titleTh, titleEn);
        }

        if (subtitleText != null)
        {
            subtitleText.text = isEnglish
                ? FirstNotEmpty(subtitleEn, subtitleTh)
                : FirstNotEmpty(subtitleTh, subtitleEn);
        }

        if (descriptionText != null)
        {
            descriptionText.text = isEnglish
                ? FirstNotEmpty(descriptionEn, descriptionTh)
                : FirstNotEmpty(descriptionTh, descriptionEn);
        }

        if (narrationLabelText != null)
        {
            narrationLabelText.text = isEnglish
                ? FirstNotEmpty(narrationLabelEn, narrationLabelTh)
                : FirstNotEmpty(narrationLabelTh, narrationLabelEn);
        }

        if (pictureImage != null)
        {
            Sprite sprite = isEnglish ? FirstSprite(imageEn, imageTh) : FirstSprite(imageTh, imageEn);

            pictureImage.sprite = sprite;
            pictureImage.enabled = sprite != null;
        }

        RefreshSections(isEnglish);
    }

    private void RefreshSections(bool isEnglish)
    {
        if (sections == null)
            return;

        for (int i = 0; i < sections.Count; i++)
        {
            IC_LocalizedSection section = sections[i];
            if (section == null)
                continue;

            if (section.titleText != null)
            {
                section.titleText.text = isEnglish
                    ? FirstNotEmpty(section.headerEn, section.headerTh)
                    : FirstNotEmpty(section.headerTh, section.headerEn);
            }

            if (section.subtitleText != null)
            {
                section.subtitleText.text = isEnglish
                    ? FirstNotEmpty(section.subtitleEn, section.subtitleTh)
                    : FirstNotEmpty(section.subtitleTh, section.subtitleEn);
            }
        }
    }

    public void OpenPanel()
    {
        GameObject target = panelRoot != null ? panelRoot : gameObject;
        target.SetActive(true);
        RefreshView();
    }

    public void ClosePanel()
    {
        if (stopNarrationOnClose)
            StopMedia();

        GameObject target = panelRoot != null ? panelRoot : gameObject;
        target.SetActive(false);
    }

    public void ToggleNarration()
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

        PlayNarration();
    }

    public void PlayNarration()
    {
        string relativeFile = GetActiveNarrationFile();
        if (string.IsNullOrWhiteSpace(relativeFile))
        {
            ShowMissingClipOverlay();
            SetState(MediaPlaybackState.Failed);
            return;
        }

        if (narratorAudioSource == null || narratorAudioSource.AudioSource == null)
        {
            ShowMissingAudioSourceOverlay();
            SetState(MediaPlaybackState.Failed);
            return;
        }

        string url = BuildStreamingAssetsUrl(relativeFile);
        if (string.IsNullOrWhiteSpace(url))
        {
            ShowMissingClipOverlay();
            SetState(MediaPlaybackState.Failed);
            return;
        }

        mediaCoordinator?.TakeFocus(this);

        StopInternal(clearClip: true, restoreBgm: false, nextState: MediaPlaybackState.Idle);
        _loadRoutine = StartCoroutine(LoadAndPlayNarration(url));
    }

    public void StopMedia()
    {
        StopInternal(clearClip: true, restoreBgm: true, nextState: MediaPlaybackState.Stopped);
    }

    public void CancelMediaLoading()
    {
        if (State != MediaPlaybackState.Loading && _loadRoutine == null && _activeRequest == null)
            return;

        _cancelRequested = true;
        StopInternal(clearClip: false, restoreBgm: true, nextState: MediaPlaybackState.Canceled);
    }

    public void ResetSession(bool restoreBgm = true)
    {
        StopInternal(clearClip: true, restoreBgm: restoreBgm, nextState: MediaPlaybackState.Idle);
    }

    private IEnumerator LoadAndPlayNarration(string url)
    {
        _cancelRequested = false;
        SetState(MediaPlaybackState.Loading);

        overlay?.ShowLoading(
            GetLocalized(loadingTitleTh, loadingTitleEn),
            GetLocalized(loadingSubtitleTh, loadingSubtitleEn),
            showBlocker: true,
            cancelable: true,
            cancelButtonLabel: GetLocalized(loadingCancelLabelTh, loadingCancelLabelEn),
            onCancel: CancelMediaLoading
        );

        _activeRequest = UnityWebRequestMultimedia.GetAudioClip(url, narrationAudioType);

        if (_activeRequest.downloadHandler is DownloadHandlerAudioClip audioHandler)
            audioHandler.streamAudio = false;

        yield return _activeRequest.SendWebRequest();

        UnityWebRequest finishedRequest = _activeRequest;
        _activeRequest = null;
        _loadRoutine = null;

        if (_cancelRequested)
        {
            DisposeRequest(finishedRequest);
            yield break;
        }

        if (finishedRequest == null || finishedRequest.result != UnityWebRequest.Result.Success)
        {
            overlay?.ShowFailed(
                GetLocalized(failedTitleTh, failedTitleEn),
                GetLocalized(failedSubtitleTh, failedSubtitleEn)
            );

            DisposeRequest(finishedRequest);
            SetState(MediaPlaybackState.Failed);
            yield break;
        }

        AudioClip clip = DownloadHandlerAudioClip.GetContent(finishedRequest);
        DisposeRequest(finishedRequest);

        if (clip == null)
        {
            overlay?.ShowFailed(
                GetLocalized(failedTitleTh, failedTitleEn),
                GetLocalized(failedSubtitleTh, failedSubtitleEn)
            );

            SetState(MediaPlaybackState.Failed);
            yield break;
        }

        if (narratorAudioSource == null || narratorAudioSource.AudioSource == null)
        {
            Destroy(clip);
            ShowMissingAudioSourceOverlay();
            SetState(MediaPlaybackState.Failed);
            yield break;
        }

        ReleaseRuntimeClip();
        _runtimeClip = clip;

        AudioSource source = narratorAudioSource.AudioSource;
        source.Stop();
        source.clip = _runtimeClip;
        source.loop = false;

        ApplyNarrationBgmMute(true);

        overlay?.Hide();
        source.Play();
        SetState(MediaPlaybackState.Playing);
        SafeSetActive(objectToEnableWhenNarrationPlays, true);

        if (_monitorRoutine != null)
        {
            StopCoroutine(_monitorRoutine);
            _monitorRoutine = null;
        }

        _monitorRoutine = StartCoroutine(MonitorPlayback(source));
    }

    private IEnumerator MonitorPlayback(AudioSource source)
    {
        while (source != null && source.isPlaying)
            yield return null;

        _monitorRoutine = null;
        ApplyNarrationBgmMute(false);

        if (State == MediaPlaybackState.Playing)
        {
            SetState(MediaPlaybackState.Completed);
            onNarrationStopped?.Invoke();
        }
    }

    private void StopInternal(bool clearClip, bool restoreBgm, MediaPlaybackState nextState)
    {
        bool wasActuallyPlaying = narratorAudioSource != null &&
                                  narratorAudioSource.AudioSource != null &&
                                  narratorAudioSource.AudioSource.isPlaying;

        if (_loadRoutine != null)
        {
            StopCoroutine(_loadRoutine);
            _loadRoutine = null;
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
            ReleaseRuntimeClip();

        if (restoreBgm)
            ApplyNarrationBgmMute(false);

        overlay?.Hide();
        SafeSetActive(objectToEnableWhenNarrationPlays, false);
        SetState(nextState);

        if (wasActuallyPlaying)
            onNarrationStopped?.Invoke();
    }

    private void ReleaseRuntimeClip()
    {
        if (_runtimeClip == null)
            return;

        Destroy(_runtimeClip);
        _runtimeClip = null;
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
        SafeSetActive(narratorPlayingObject, state == MediaPlaybackState.Playing);
    }

    private bool IsUnsafeToggleTarget(GameObject target)
    {
        if (target == null)
            return false;

        Transform host = transform;
        Transform actualPanel = panelRoot != null ? panelRoot.transform : host;
        Transform targetTransform = target.transform;

        if (targetTransform == host)
            return true;

        if (targetTransform == actualPanel)
            return true;

        if (host.IsChildOf(targetTransform))
            return true;

        if (actualPanel.IsChildOf(targetTransform))
            return true;

        return false;
    }

    private void SafeSetActive(GameObject target, bool active)
    {
        if (target == null)
            return;

        if (IsUnsafeToggleTarget(target))
            return;

        target.SetActive(active);
    }

    private void SetState(MediaPlaybackState state)
    {
        if (State == state)
            return;

        State = state;
        ApplyNarratorPlayingObject(state);
        StateChanged?.Invoke(state);
    }

    private string GetActiveNarrationFile()
    {
        return IsEnglish()
            ? FirstNotEmpty(narrationFileEn, narrationFileTh)
            : FirstNotEmpty(narrationFileTh, narrationFileEn);
    }

    private string BuildStreamingAssetsUrl(string relativeFile)
    {
        if (string.IsNullOrWhiteSpace(relativeFile))
            return string.Empty;

        string trimmed = relativeFile.Trim();

        if (trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("file://", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("jar:file://", StringComparison.OrdinalIgnoreCase))
        {
            return trimmed;
        }

        string combined = Path.Combine(Application.streamingAssetsPath, trimmed);

        if (combined.Contains("://") || combined.StartsWith("jar:", StringComparison.OrdinalIgnoreCase))
            return combined.Replace("\\", "/");

        return new Uri(combined).AbsoluteUri;
    }

    private void ShowMissingClipOverlay()
    {
        overlay?.ShowFailed(
            GetLocalized(missingClipTitleTh, missingClipTitleEn),
            GetLocalized(missingClipSubtitleTh, missingClipSubtitleEn)
        );
    }

    private void ShowMissingAudioSourceOverlay()
    {
        overlay?.ShowFailed(
            GetLocalized(missingAudioSourceTitleTh, missingAudioSourceTitleEn),
            GetLocalized(missingAudioSourceSubtitleTh, missingAudioSourceSubtitleEn)
        );
    }

    private string GetLocalized(string thai, string english)
    {
        return IsEnglish()
            ? FirstNotEmpty(english, thai)
            : FirstNotEmpty(thai, english);
    }

    private bool IsEnglish()
    {
        Locale locale = LocalizationSettings.SelectedLocale;
        if (locale == null)
            return false;

        string code = locale.Identifier.Code;
        return !string.IsNullOrWhiteSpace(code) &&
               code.StartsWith("en", StringComparison.OrdinalIgnoreCase);
    }

    private static string FirstNotEmpty(params string[] values)
    {
        if (values == null)
            return string.Empty;

        for (int i = 0; i < values.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(values[i]))
                return values[i];
        }

        return string.Empty;
    }

    private static Sprite FirstSprite(params Sprite[] values)
    {
        if (values == null)
            return null;

        for (int i = 0; i < values.Length; i++)
        {
            if (values[i] != null)
                return values[i];
        }

        return null;
    }
}