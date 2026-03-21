using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;

public class VideoPlayerUIController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private Slider timelineSlider;
    [SerializeField] private Button playPauseButton;
    [SerializeField] private TMP_Text timeLabel;

    [Header("Optional")]
    [SerializeField] private TMP_Text playPauseButtonLabel;

    private bool _isDraggingTimeline;
    private bool _isPrepared;

    private void Awake()
    {
        if (timelineSlider != null)
        {
            timelineSlider.minValue = 0f;
            timelineSlider.maxValue = 1f;
            timelineSlider.wholeNumbers = false;
        }

        if (playPauseButton != null)
            playPauseButton.onClick.AddListener(TogglePlayPause);

        if (timelineSlider != null)
        {
            timelineSlider.onValueChanged.AddListener(HandleSliderValueChanged);
        }

        if (videoPlayer != null)
        {
            videoPlayer.prepareCompleted += HandlePrepareCompleted;
            videoPlayer.loopPointReached += HandleVideoFinished;
        }
    }

    private void Start()
    {
        if (videoPlayer != null)
        {
            videoPlayer.Prepare();
            UpdatePlayPauseLabel();
            UpdateTimeLabel(0d, 0d);
        }
    }

    private void Update()
    {
        if (videoPlayer == null || !_isPrepared)
            return;

        if (!_isDraggingTimeline && videoPlayer.length > 0d)
        {
            float normalizedValue = (float)(videoPlayer.time / videoPlayer.length);
            timelineSlider.SetValueWithoutNotify(normalizedValue);
        }

        UpdateTimeLabel(videoPlayer.time, videoPlayer.length);
        UpdatePlayPauseLabel();
    }

    private void HandlePrepareCompleted(VideoPlayer source)
    {
        _isPrepared = true;
        UpdateTimeLabel(0d, videoPlayer.length);
        timelineSlider.SetValueWithoutNotify(0f);
    }

    private void HandleVideoFinished(VideoPlayer source)
    {
        timelineSlider.SetValueWithoutNotify(1f);
        UpdatePlayPauseLabel();
    }

    public void BeginTimelineDrag()
    {
        _isDraggingTimeline = true;
    }

    public void EndTimelineDrag()
    {
        _isDraggingTimeline = false;
        SeekToSliderValue();
    }

    private void HandleSliderValueChanged(float value)
    {
        if (_isDraggingTimeline)
            UpdatePreviewTimeLabel(value);
    }

    private void SeekToSliderValue()
    {
        if (videoPlayer == null || !_isPrepared || videoPlayer.length <= 0d)
            return;

        double targetTime = timelineSlider.value * videoPlayer.length;
        videoPlayer.time = targetTime;
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

    private void UpdatePreviewTimeLabel(float normalizedValue)
    {
        if (videoPlayer == null || !_isPrepared || videoPlayer.length <= 0d)
            return;

        double previewTime = normalizedValue * videoPlayer.length;
        UpdateTimeLabel(previewTime, videoPlayer.length);
    }

    private void UpdateTimeLabel(double currentTime, double totalTime)
    {
        if (timeLabel == null)
            return;

        timeLabel.text = $"{FormatTime(currentTime)} / {FormatTime(totalTime)}";
    }

    private void UpdatePlayPauseLabel()
    {
        if (playPauseButtonLabel == null || videoPlayer == null)
            return;

        playPauseButtonLabel.text = videoPlayer.isPlaying ? "Pause" : "Play";
    }

    private string FormatTime(double seconds)
    {
        if (seconds < 0d)
            seconds = 0d;

        int totalSeconds = Mathf.FloorToInt((float)seconds);
        int minutes = totalSeconds / 60;
        int remainingSeconds = totalSeconds % 60;
        return $"{minutes:00}:{remainingSeconds:00}";
    }

    private void OnDestroy()
    {
        if (playPauseButton != null)
            playPauseButton.onClick.RemoveListener(TogglePlayPause);

        if (timelineSlider != null)
            timelineSlider.onValueChanged.RemoveListener(HandleSliderValueChanged);

        if (videoPlayer != null)
        {
            videoPlayer.prepareCompleted -= HandlePrepareCompleted;
            videoPlayer.loopPointReached -= HandleVideoFinished;
        }
    }
}