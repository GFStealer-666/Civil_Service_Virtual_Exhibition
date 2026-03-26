using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AE_ProjectVideoControlsView : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button pauseButton;
    [SerializeField] private Slider timelineSlider;

    [Header("Labels")]
    [SerializeField] private TMP_Text timeLabel;
    [SerializeField] private TMP_Text statusText;

    private IAE_ProjectVideoPlaybackController  _player;
    private bool _isDraggingSlider;

    public void Bind(IAE_ProjectVideoPlaybackController  player)
    {
        Unbind();

        _player = player;

        if (_player == null)
            return;

        _player.Prepared += HandlePrepared;
        _player.Failed += HandleFailed;
        _player.PlayStateChanged += HandlePlayStateChanged;
        _player.TimeChanged += HandleTimeChanged;
        _player.Finished += HandleFinished;

        if (playButton != null)
            playButton.onClick.AddListener(HandlePlayClicked);

        if (pauseButton != null)
            pauseButton.onClick.AddListener(HandlePauseClicked);

        if (timelineSlider != null)
        {
            timelineSlider.onValueChanged.AddListener(HandleSliderChanged);
            timelineSlider.minValue = 0f;
            timelineSlider.maxValue = 1f;
            timelineSlider.wholeNumbers = false;
            timelineSlider.value = 0f;
        }

        ResetView();
    }

    public void Unbind()
    {
        if (_player != null)
        {
            _player.Prepared -= HandlePrepared;
            _player.Failed -= HandleFailed;
            _player.PlayStateChanged -= HandlePlayStateChanged;
            _player.TimeChanged -= HandleTimeChanged;
            _player.Finished -= HandleFinished;
        }

        if (playButton != null)
            playButton.onClick.RemoveListener(HandlePlayClicked);

        if (pauseButton != null)
            pauseButton.onClick.RemoveListener(HandlePauseClicked);

        if (timelineSlider != null)
            timelineSlider.onValueChanged.RemoveListener(HandleSliderChanged);

        _player = null;
        _isDraggingSlider = false;

        ResetView();
    }

    public void BeginSliderDrag()
    {
        _isDraggingSlider = true;
    }

    public void EndSliderDrag()
    {
        _isDraggingSlider = false;

        if (_player == null || timelineSlider == null)
            return;

        _player.SeekNormalized(timelineSlider.value);
    }

    private void HandlePrepared()
    {
        SetInteractable(true);
        SetStatus(string.Empty);
        UpdatePlayPauseButtons(false);

        if (timelineSlider != null)
            timelineSlider.value = 0f;
    }

    private void HandleFailed(string message)
    {
        //SetInteractable(false);
        SetStatus(message);
        UpdatePlayPauseButtons(false);
        SetTime(0d, 0d);

        if (timelineSlider != null)
            timelineSlider.value = 0f;
    }

    private void HandleFinished()
    {
        UpdatePlayPauseButtons(false);

        if (timelineSlider != null)
            timelineSlider.value = 1f;
    }

    private void HandlePlayStateChanged(bool isPlaying)
    {
        UpdatePlayPauseButtons(isPlaying);
    }

    private void HandleTimeChanged(double current, double duration)
    {
        if (!_isDraggingSlider && timelineSlider != null && duration > 0.01d)
            timelineSlider.value = (float)(current / duration);

        SetTime(current, duration);
    }

    private void HandlePlayClicked()
    {
        _player?.Play();
    }

    private void HandlePauseClicked()
    {
        _player?.Pause();
    }

    private void HandleSliderChanged(float value)
    {
        if (_isDraggingSlider)
            return;

        _player?.SeekNormalized(value);
    }

    private void UpdatePlayPauseButtons(bool isPlaying)
    {
        if (playButton != null)
            playButton.gameObject.SetActive(!isPlaying);

        if (pauseButton != null)
            pauseButton.gameObject.SetActive(isPlaying);
    }

    private void SetInteractable(bool interactable)
    {
        if (playButton != null)
            playButton.interactable = interactable;

        if (pauseButton != null)
            pauseButton.interactable = interactable;

        if (timelineSlider != null)
            timelineSlider.interactable = interactable;
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }

    private void SetTime(double current, double duration)
    {
        if (timeLabel == null)
            return;

        timeLabel.text = $"{FormatTime(current)} / {FormatTime(duration)}";
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

    public void ResetView()
    {
        _isDraggingSlider = false;

        //SetInteractable(false);
        SetStatus(string.Empty);
        SetTime(0d, 0d);
        UpdatePlayPauseButtons(false);

        if (timelineSlider != null)
            timelineSlider.value = 0f;
    }
}