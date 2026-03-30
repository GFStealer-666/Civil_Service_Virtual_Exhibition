using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class IC_LocalNarrationController : MonoBehaviour, IMediaControllable
{
    [Header("Audio")]
    [SerializeField] private ExhibitionAudioSource narratorAudioSource;
    [Header("Overlay")]
    [SerializeField] private StatusOverlay overlay;

    [SerializeField] private string missingClipTitleTh = "ไม่พบเสียงบรรยาย";
    [SerializeField] private string missingClipTitleEn = "Narration not found";

    [SerializeField] private string missingClipSubtitleTh = "กรุณาตรวจสอบ Audio Clip ใน Inspector";
    [SerializeField] private string missingClipSubtitleEn = "Please assign an Audio Clip in the Inspector";

    [SerializeField] private string missingAudioSourceTitleTh = "ไม่สามารถเล่นเสียงบรรยาย";
    [SerializeField] private string missingAudioSourceTitleEn = "Unable to play narration";

    [SerializeField] private string missingAudioSourceSubtitleTh = "ไม่พบ Audio Source";
    [SerializeField] private string missingAudioSourceSubtitleEn = "Audio Source is missing";

    [Header("Coordination")]
    [SerializeField] private MediaSessionCoordinator mediaCoordinator;

    [Header("Runtime Visual")]
    [SerializeField] private GameObject rawImage;

    [Header("Behavior")]
    [SerializeField] private bool stopWhenLocaleChanges = true;

    public MediaPlaybackState State { get; private set; } = MediaPlaybackState.Idle;
    public event Action<MediaPlaybackState> StateChanged;


    private Coroutine _monitorRoutine;
    private bool _subscribed;

    private void Awake()
    {
        mediaCoordinator?.Register(this);
        ApplyNarratorPlayingObject(State);
    }

    private void OnEnable()
    {
        SubscribeLocaleEvent();
        RefreshLocaleBinding();
    }

    private void OnDisable()
    {
        UnsubscribeLocaleEvent();
        StopInternal(restoreBgm: true, nextState: MediaPlaybackState.Idle);
    }

    private void OnDestroy()
    {
        mediaCoordinator?.Unregister(this);
        StopInternal(restoreBgm: true, nextState: MediaPlaybackState.Idle);
        ApplyNarratorPlayingObject(MediaPlaybackState.Idle);
    }

    public void RefreshLocaleBinding()
    {
        if (!stopWhenLocaleChanges)
            return;

        if (State == MediaPlaybackState.Playing)
            StopMedia();
    }

    public void ToggleNarration()
    {
        if (State == MediaPlaybackState.Playing)
        {
            StopMedia();
            return;
        }

        PlaySelectedNarration();
    }

    public void PlaySelectedNarration()
    {
        AudioClip clip = null;
        if (clip == null)
        {
            ShowMissingClip();
            SetState(MediaPlaybackState.Failed);
            return;
        }

        if (narratorAudioSource == null || narratorAudioSource.AudioSource == null)
        {
            ShowMissingAudioSource();
            SetState(MediaPlaybackState.Failed);
            return;
        }

        mediaCoordinator?.TakeFocus(this);

        StopInternal(restoreBgm: false, nextState: MediaPlaybackState.Idle);

        AudioSource source = narratorAudioSource.AudioSource;
        source.Stop();
        source.clip = clip;
        source.loop = false;

        ApplyNarrationBgmMute(true);
        source.Play();

        overlay?.Hide();
        SetState(MediaPlaybackState.Playing);

        if (_monitorRoutine != null)
        {
            StopCoroutine(_monitorRoutine);
            _monitorRoutine = null;
        }

        _monitorRoutine = StartCoroutine(MonitorPlayback(source));
    }

    public void StopMedia()
    {
        StopInternal(restoreBgm: true, nextState: MediaPlaybackState.Stopped);
    }

    public void CancelMediaLoading()
    {
        StopMedia();
    }

    public void ResetSession(bool restoreBgm = true)
    {
        StopInternal(restoreBgm, MediaPlaybackState.Idle);
    }

    private IEnumerator MonitorPlayback(AudioSource source)
    {
        while (source != null && source.isPlaying)
            yield return null;

        _monitorRoutine = null;
        ApplyNarrationBgmMute(false);

        if (State == MediaPlaybackState.Playing)
            SetState(MediaPlaybackState.Completed);
    }

    private void StopInternal(bool restoreBgm, MediaPlaybackState nextState)
    {
        if (_monitorRoutine != null)
        {
            StopCoroutine(_monitorRoutine);
            _monitorRoutine = null;
        }

        if (narratorAudioSource != null && narratorAudioSource.AudioSource != null)
        {
            narratorAudioSource.AudioSource.Stop();
            narratorAudioSource.AudioSource.clip = null;
        }

        if (restoreBgm)
            ApplyNarrationBgmMute(false);

        overlay?.Hide();
        SetState(nextState);
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

    private void ShowMissingClip()
    {
        overlay?.ShowFailed(
            GetLocalized(missingClipTitleTh, missingClipTitleEn),
            GetLocalized(missingClipSubtitleTh, missingClipSubtitleEn)
        );
    }

    private void ShowMissingAudioSource()
    {
        overlay?.ShowFailed(
            GetLocalized(missingAudioSourceTitleTh, missingAudioSourceTitleEn),
            GetLocalized(missingAudioSourceSubtitleTh, missingAudioSourceSubtitleEn)
        );
    }

    private void SubscribeLocaleEvent()
    {
        if (_subscribed)
            return;

        LocalizationSettings.SelectedLocaleChanged += HandleLocaleChanged;
        _subscribed = true;
    }

    private void UnsubscribeLocaleEvent()
    {
        if (!_subscribed)
            return;

        LocalizationSettings.SelectedLocaleChanged -= HandleLocaleChanged;
        _subscribed = false;
    }

    private void HandleLocaleChanged(Locale locale)
    {
        RefreshLocaleBinding();
    }

    private bool IsEnglishActive()
    {
        Locale locale = LocalizationSettings.SelectedLocale;
        if (locale == null)
            return false;

        string code = locale.Identifier.Code;
        return !string.IsNullOrWhiteSpace(code) &&
               code.StartsWith("en", StringComparison.OrdinalIgnoreCase);
    }

    private string GetLocalized(string thai, string english)
    {
        return IsEnglishActive()
            ? FirstNotEmpty(english, thai)
            : FirstNotEmpty(thai, english);
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

    private static AudioClip FirstClip(params AudioClip[] clips)
    {
        if (clips == null)
            return null;

        for (int i = 0; i < clips.Length; i++)
        {
            if (clips[i] != null)
                return clips[i];
        }

        return null;
    }
}