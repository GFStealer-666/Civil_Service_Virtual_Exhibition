using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class AE_ProjectVideoControlsView : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button stopButton;
    [SerializeField] private Slider timelineSlider;

    [Header("Labels")]
    [SerializeField] private TMP_Text timeLabel;
    [SerializeField] private TMP_Text statusText;

    [Header("Overlay")]
    [SerializeField] private GameObject controlsOverlayRoot;
    [SerializeField] private CanvasGroup controlsCanvasGroup;
    [SerializeField] private RectTransform videoArea;

    [Header("Overlay Behavior")]
    [SerializeField] private bool alwaysShowWhenPaused = true;
    [SerializeField] private float autoHideDelay = 2f;
    [SerializeField] private float fadeSpeed = 8f;
    [SerializeField] private float centerRevealRadius = 180f;

    private IAE_ProjectVideoPlaybackController _player;

    private bool _isDraggingSlider;
    private bool _isPointerInsideVideoArea;
    private bool _isPointerInsideControls;
    private bool _isPlaying;
    private float _lastUserActivityTime = -999f;
    private Vector2 _lastPointerPosition;

    public void Bind(IAE_ProjectVideoPlaybackController player)
    {
        Unbind();

        _player = player;

        if (_player == null)
        {
            ResetView();
            return;
        }

        _player.Prepared += HandlePrepared;
        _player.Failed += HandleFailed;
        _player.PlayStateChanged += HandlePlayStateChanged;
        _player.TimeChanged += HandleTimeChanged;
        _player.Finished += HandleFinished;

        if (playButton != null)
            playButton.onClick.AddListener(HandlePlayClicked);

        if (pauseButton != null)
            pauseButton.onClick.AddListener(HandlePauseClicked);

        if (stopButton != null)
            stopButton.onClick.AddListener(HandleStopClicked);

        if (timelineSlider != null)
        {
            timelineSlider.minValue = 0f;
            timelineSlider.maxValue = 1f;
            timelineSlider.wholeNumbers = false;
            timelineSlider.SetValueWithoutNotify(0f);
            timelineSlider.onValueChanged.AddListener(HandleSliderChanged);

            RegisterSliderDragEvents(timelineSlider);
            RegisterPointerStateEvents(timelineSlider.gameObject, true);
        }

        RegisterPointerStateEvents(gameObject, false);

        if (TryGetPointerScreenPosition(out Vector2 pointerPosition))
            _lastPointerPosition = pointerPosition;

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

        if (stopButton != null)
            stopButton.onClick.RemoveListener(HandleStopClicked);

        if (timelineSlider != null)
            timelineSlider.onValueChanged.RemoveListener(HandleSliderChanged);

        _player = null;
        _isDraggingSlider = false;
        _isPointerInsideVideoArea = false;
        _isPointerInsideControls = false;
        _isPlaying = false;

        ResetView();
    }

    private void Update()
    {
        UpdatePointerState();
        UpdateOverlayVisibility();
    }

    public void ResetView()
    {
        _isDraggingSlider = false;
        _isPlaying = false;
        _lastUserActivityTime = Time.unscaledTime;

        SetInteractable(false);
        SetStatus(string.Empty);
        SetTime(0d, 0d);
        UpdatePlayPauseButtons(false);

        if (timelineSlider != null)
            timelineSlider.SetValueWithoutNotify(0f);

        SetOverlayVisible(true, true);
    }

    public void BeginSliderDrag()
    {
        _isDraggingSlider = true;
        MarkUserActivity();
        SetOverlayVisible(true, false);
    }

    public void EndSliderDrag()
    {
        MarkUserActivity();

        if (_player == null || timelineSlider == null)
        {
            _isDraggingSlider = false;
            return;
        }

        float seekValue = timelineSlider.value;

        _player.SeekNormalized(seekValue);

        if (_player.Duration > 0.01d)
            SetTime(seekValue * _player.Duration, _player.Duration);

        _isDraggingSlider = false;
    }

    private void HandlePrepared()
    {
        SetInteractable(true);
        SetStatus(string.Empty);
        UpdatePlayPauseButtons(false);

        if (timelineSlider != null)
            timelineSlider.SetValueWithoutNotify(0f);

        SetOverlayVisible(true, false);
        MarkUserActivity();
    }

    private void HandleFailed(string message)
    {
        SetInteractable(false);
        SetStatus(message);
        UpdatePlayPauseButtons(false);
        SetTime(0d, 0d);

        if (timelineSlider != null)
            timelineSlider.SetValueWithoutNotify(0f);

        SetOverlayVisible(true, false);
    }

    private void HandleFinished()
    {
        _isPlaying = false;
        UpdatePlayPauseButtons(false);

        if (timelineSlider != null)
            timelineSlider.SetValueWithoutNotify(1f);

        SetOverlayVisible(true, false);
    }

    private void HandlePlayStateChanged(bool isPlaying)
    {
        _isPlaying = isPlaying;
        UpdatePlayPauseButtons(isPlaying);
        SetOverlayVisible(true, false);
        MarkUserActivity();
    }

   private void HandleTimeChanged(double current, double duration)
    {
        if (!_isDraggingSlider && timelineSlider != null)
        {
            float normalized = duration > 0.01d
                ? Mathf.Clamp01((float)(current / duration))
                : 0f;

            timelineSlider.SetValueWithoutNotify(normalized);
        }

        SetTime(current, duration);
    }

    private void HandlePlayClicked()
    {
        _player?.Play();
        MarkUserActivity();
    }

    private void HandlePauseClicked()
    {
        _player?.Pause();
        MarkUserActivity();
    }

    private void HandleStopClicked()
    {
        _player?.StopPlayback();
        MarkUserActivity();
    }

    private void HandleSliderChanged(float value)
    {
        MarkUserActivity();

        if (!_isDraggingSlider || _player == null)
            return;

        double duration = _player.Duration;
        if (duration > 0.01d)
            SetTime(value * duration, duration);
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

        if (stopButton != null)
            stopButton.interactable = interactable;

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

    private void UpdatePointerState()
    {
        if (!TryGetPointerScreenPosition(out Vector2 pointerPosition))
        {
            _isPointerInsideVideoArea = false;
            return;
        }

        bool pointerMoved = Vector2.Distance(pointerPosition, _lastPointerPosition) > 0.01f;
        if (pointerMoved)
        {
            _lastPointerPosition = pointerPosition;

            if (IsPointerInsideVideoArea(pointerPosition) || IsPointerNearVideoCenter(pointerPosition))
                MarkUserActivity();
        }

        _isPointerInsideVideoArea = IsPointerInsideVideoArea(pointerPosition);
    }

    private void UpdateOverlayVisibility()
    {
        bool shouldShow = true;

        if (_isPlaying)
        {
            shouldShow =
                _isDraggingSlider ||
                _isPointerInsideControls ||
                _isPointerInsideVideoArea ||
                IsPointerNearVideoCenter() ||
                (Time.unscaledTime - _lastUserActivityTime) <= autoHideDelay;
        }
        else if (alwaysShowWhenPaused)
        {
            shouldShow = true;
        }

        SetOverlayVisible(shouldShow, false);
    }

    private bool TryGetPointerScreenPosition(out Vector2 position)
    {
        if (Pointer.current != null)
        {
            position = Pointer.current.position.ReadValue();
            return true;
        }

        if (Mouse.current != null)
        {
            position = Mouse.current.position.ReadValue();
            return true;
        }

        position = default;
        return false;
    }

    private bool IsPointerInsideVideoArea()
    {
        if (!TryGetPointerScreenPosition(out Vector2 pointerPosition))
            return false;

        return IsPointerInsideVideoArea(pointerPosition);
    }

    private bool IsPointerInsideVideoArea(Vector2 pointerPosition)
    {
        if (videoArea == null)
            return false;

        return RectTransformUtility.RectangleContainsScreenPoint(
            videoArea,
            pointerPosition,
            null
        );
    }

    private bool IsPointerNearVideoCenter()
    {
        if (!TryGetPointerScreenPosition(out Vector2 pointerPosition))
            return false;

        return IsPointerNearVideoCenter(pointerPosition);
    }

    private bool IsPointerNearVideoCenter(Vector2 pointerPosition)
    {
        if (videoArea == null)
            return false;

        Vector3[] corners = new Vector3[4];
        videoArea.GetWorldCorners(corners);

        Vector2 center = ((Vector2)corners[0] + (Vector2)corners[2]) * 0.5f;
        float distance = Vector2.Distance(pointerPosition, center);

        return distance <= centerRevealRadius && IsPointerInsideVideoArea(pointerPosition);
    }

    private void SetOverlayVisible(bool visible, bool instant)
    {
        if (controlsOverlayRoot != null && !controlsOverlayRoot.activeSelf)
            controlsOverlayRoot.SetActive(true);

        if (controlsCanvasGroup == null)
        {
            if (controlsOverlayRoot != null)
                controlsOverlayRoot.SetActive(visible);

            return;
        }

        float targetAlpha = visible ? 1f : 0f;

        if (instant)
            controlsCanvasGroup.alpha = targetAlpha;
        else
            controlsCanvasGroup.alpha = Mathf.MoveTowards(
                controlsCanvasGroup.alpha,
                targetAlpha,
                fadeSpeed * Time.unscaledDeltaTime
            );

        bool interactive = controlsCanvasGroup.alpha > 0.9f;
        controlsCanvasGroup.blocksRaycasts = interactive;
        controlsCanvasGroup.interactable = interactive;
    }

    private void MarkUserActivity()
    {
        _lastUserActivityTime = Time.unscaledTime;
    }

    private void RegisterSliderDragEvents(Slider slider)
    {
        if (slider == null)
            return;

        EventTrigger trigger = slider.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = slider.gameObject.AddComponent<EventTrigger>();

        AddEventTrigger(trigger, EventTriggerType.BeginDrag, _ => BeginSliderDrag());
        AddEventTrigger(trigger, EventTriggerType.EndDrag, _ => EndSliderDrag());
        AddEventTrigger(trigger, EventTriggerType.PointerDown, _ => BeginSliderDrag());
        AddEventTrigger(trigger, EventTriggerType.PointerUp, _ => EndSliderDrag());
    }

    private void RegisterPointerStateEvents(GameObject target, bool controlsArea)
    {
        if (target == null)
            return;

        EventTrigger trigger = target.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = target.AddComponent<EventTrigger>();

        AddEventTrigger(trigger, EventTriggerType.PointerEnter, _ =>
        {
            if (controlsArea)
                _isPointerInsideControls = true;

            MarkUserActivity();
            SetOverlayVisible(true, false);
        });

        AddEventTrigger(trigger, EventTriggerType.PointerExit, _ =>
        {
            if (controlsArea)
                _isPointerInsideControls = false;
        });

        AddEventTrigger(trigger, EventTriggerType.PointerDown, _ =>
        {
            MarkUserActivity();
            SetOverlayVisible(true, false);
        });
    }

    private void AddEventTrigger(
        EventTrigger trigger,
        EventTriggerType type,
        UnityAction<BaseEventData> callback)
    {
        if (trigger == null || callback == null)
            return;

        EventTrigger.Entry entry = new EventTrigger.Entry
        {
            eventID = type
        };
        entry.callback.AddListener(callback);
        trigger.triggers.Add(entry);
    }
}