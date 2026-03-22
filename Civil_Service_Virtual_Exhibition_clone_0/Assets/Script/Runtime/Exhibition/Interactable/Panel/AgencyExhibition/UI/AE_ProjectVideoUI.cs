using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class AE_ProjectVideoUI : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button closeButton;

    [Header("Header")]
    [SerializeField] private TMP_Text projectNameText;
    [SerializeField] private TMP_Text agencyNameText;

    [Header("Video")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private RawImage videoOutputImage;
    [SerializeField] private AspectRatioFitter aspectRatioFitter;

    [Header("Controls")]
    [SerializeField] private Slider timelineSlider;
    [SerializeField] private Button playPauseButton;
    [SerializeField] private TMP_Text playPauseLabel;
    [SerializeField] private TMP_Text timeLabel;

    [Header("Messages")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private string loadingMessage = "Loading video...";
    [SerializeField] private string readyMessage = "";
    [SerializeField] private string failedMessage = "Unable to load video.";
    [SerializeField] private string noVideoMessage = "This project has no video.";

    private GovernmentProjectDto _currentProject;
    private string _currentAgencyName;
    private bool _isPrepared;
    private bool _isDraggingSlider;
    private Coroutine _prepareTimeoutRoutine;

    public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

    private void Awake()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(Hide);

        if (playPauseButton != null)
            playPauseButton.onClick.AddListener(TogglePlayPause);

        if (timelineSlider != null)
        {
            timelineSlider.onValueChanged.AddListener(HandleSliderValueChanged);
        }

        if (videoPlayer != null)
        {
            videoPlayer.playOnAwake = false;
            videoPlayer.waitForFirstFrame = true;
            videoPlayer.skipOnDrop = true;

            videoPlayer.prepareCompleted += HandlePrepareCompleted;
            videoPlayer.errorReceived += HandleVideoError;
            videoPlayer.loopPointReached += HandleVideoFinished;
        }

        if (panelRoot != null)
            panelRoot.SetActive(false);

        ApplyIdleState();
    }

    private void OnDestroy()
    {
        if (closeButton != null)
            closeButton.onClick.RemoveListener(Hide);

        if (playPauseButton != null)
            playPauseButton.onClick.RemoveListener(TogglePlayPause);

        if (timelineSlider != null)
            timelineSlider.onValueChanged.RemoveListener(HandleSliderValueChanged);

        if (videoPlayer != null)
        {
            videoPlayer.prepareCompleted -= HandlePrepareCompleted;
            videoPlayer.errorReceived -= HandleVideoError;
            videoPlayer.loopPointReached -= HandleVideoFinished;
        }
    }

    private void Update()
    {
        if (!IsOpen || videoPlayer == null || !_isPrepared)
            return;

        UpdateTimelineUI();
    }

    public void Show(GovernmentProjectDto project, string agencyName)
    {
        _currentProject = project;
        _currentAgencyName = agencyName;

        if (panelRoot != null)
            panelRoot.SetActive(true);

        BindHeader();
        LoadVideo();
    }

    public void Hide()
    {
        StopPrepareTimeoutRoutine();
        StopVideo();

        _currentProject = null;
        _currentAgencyName = string.Empty;
        _isPrepared = false;

        ApplyIdleState();

        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    private void BindHeader()
    {
        if (projectNameText != null)
            projectNameText.text = FirstNotEmpty(_currentProject?.name, _currentProject?.nameEn);

        if (agencyNameText != null)
            agencyNameText.text = _currentAgencyName ?? string.Empty;
    }

    private void LoadVideo()
    {
        if (videoPlayer == null)
        {
            SetStatus("VideoPlayer is not assigned.");
            return;
        }

        string url = _currentProject != null ? _currentProject.videoUrl : string.Empty;

        if (string.IsNullOrWhiteSpace(url))
        {
            SetStatus(noVideoMessage);
            SetControlsInteractable(false);
            return;
        }

        StopVideo();

        _isPrepared = false;
        SetStatus(loadingMessage);
        SetControlsInteractable(false);

        videoPlayer.source = VideoSource.Url;
        videoPlayer.url = url;

        if (videoOutputImage != null)
            videoOutputImage.texture = null;

        videoPlayer.Prepare();

        StopPrepareTimeoutRoutine();
        _prepareTimeoutRoutine = StartCoroutine(PrepareTimeoutRoutine(8f));

        Debug.Log($"[AE_ProjectVideoUI] Preparing video: {url}");
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

        Debug.LogWarning("[AE_ProjectVideoUI] Video prepare timeout.");
        SetStatus(failedMessage);
        SetControlsInteractable(false);
    }

    private void HandlePrepareCompleted(VideoPlayer source)
    {
        StopPrepareTimeoutRoutine();

        _isPrepared = true;

        if (videoOutputImage != null)
            videoOutputImage.texture = source.texture;

        UpdateAspectRatio(source);

        source.Play();

        SetStatus(readyMessage);
        SetControlsInteractable(true);
        UpdatePlayPauseLabel();

        Debug.Log($"[AE_ProjectVideoUI] Video prepared successfully: {source.url}");
    }

    private void HandleVideoError(VideoPlayer source, string message)
    {
        StopPrepareTimeoutRoutine();

        _isPrepared = false;
        SetStatus($"{failedMessage} {message}");
        SetControlsInteractable(false);

        Debug.LogWarning($"[AE_ProjectVideoUI] Video error: {message} | url={source.url}");
    }

    private void HandleVideoFinished(VideoPlayer source)
    {
        UpdatePlayPauseLabel();

        if (timelineSlider != null)
            timelineSlider.value = 1f;

        Debug.Log("[AE_ProjectVideoUI] Video finished.");
    }

    private void TogglePlayPause()
    {
        if (videoPlayer == null || !_isPrepared)
            return;

        if (videoPlayer.isPlaying)
            videoPlayer.Pause();
        else
            videoPlayer.Play();

        UpdatePlayPauseLabel();
    }

    private void HandleSliderValueChanged(float value)
    {
        if (videoPlayer == null || !_isPrepared || _isDraggingSlider)
            return;

        if (videoPlayer.length <= 0.01d)
            return;

        double targetTime = value * videoPlayer.length;
        videoPlayer.time = targetTime;
        UpdateTimelineUI();
    }

    public void BeginSliderDrag()
    {
        _isDraggingSlider = true;
    }

    public void EndSliderDrag()
    {
        _isDraggingSlider = false;

        if (videoPlayer == null || !_isPrepared || timelineSlider == null)
            return;

        if (videoPlayer.length <= 0.01d)
            return;

        double targetTime = timelineSlider.value * videoPlayer.length;
        videoPlayer.time = targetTime;
    }

    private void UpdateTimelineUI()
    {
        if (videoPlayer == null)
            return;

        double length = videoPlayer.length;
        double current = videoPlayer.time;

        if (!_isDraggingSlider && timelineSlider != null && length > 0.01d)
            timelineSlider.value = (float)(current / length);

        if (timeLabel != null)
            timeLabel.text = $"{FormatTime(current)} / {FormatTime(length)}";

        UpdatePlayPauseLabel();
    }

    private void UpdatePlayPauseLabel()
    {
        if (playPauseLabel == null || videoPlayer == null)
            return;

        playPauseLabel.text = videoPlayer.isPlaying ? "Pause" : "Play";
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

    private void StopVideo()
    {
        if (videoPlayer == null)
            return;

        if (videoPlayer.isPlaying)
            videoPlayer.Stop();

        videoPlayer.targetTexture = null;

        if (videoOutputImage != null)
            videoOutputImage.texture = null;
    }

    private void StopPrepareTimeoutRoutine()
    {
        if (_prepareTimeoutRoutine != null)
        {
            StopCoroutine(_prepareTimeoutRoutine);
            _prepareTimeoutRoutine = null;
        }
    }

    private void SetControlsInteractable(bool interactable)
    {
        if (playPauseButton != null)
            playPauseButton.interactable = interactable;

        if (timelineSlider != null)
            timelineSlider.interactable = interactable;
    }

    private void ApplyIdleState()
    {
        SetControlsInteractable(false);

        if (timelineSlider != null)
            timelineSlider.value = 0f;

        if (timeLabel != null)
            timeLabel.text = "00:00 / 00:00";

        if (playPauseLabel != null)
            playPauseLabel.text = "Play";

        SetStatus(string.Empty);
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }

    private string FormatTime(double seconds)
    {
        if (seconds < 0d || double.IsNaN(seconds) || double.IsInfinity(seconds))
            return "00:00";

        int totalSeconds = Mathf.FloorToInt((float)seconds);
        int minutes = totalSeconds / 60;
        int remainSeconds = totalSeconds % 60;

        return $"{minutes:00}:{remainSeconds:00}";
    }

    private string FirstNotEmpty(params string[] values)
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
}