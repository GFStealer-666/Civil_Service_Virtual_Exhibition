using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization.Settings;
public class StatusOverlay : MonoBehaviour
{
    public enum State
    {
        Hidden,
        Loading,
        Success,
        Failed
    }

    [Header("Root")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private GameObject overlay;

    [Header("State Objects")]
    [SerializeField] private GameObject loadingState;
    [SerializeField] private GameObject successState;
    [SerializeField] private GameObject failedState;

    [Header("Loading State Wiring")]
    [SerializeField] private TMP_Text loadingTitleText;
    [SerializeField] private TMP_Text loadingSubtitleText;
    [SerializeField] private Button loadingCancelButton;
    [SerializeField] private TMP_Text loadingCancelButtonText;

    [Header("Success State Wiring")]
    [SerializeField] private TMP_Text successTitleText;
    [SerializeField] private TMP_Text successSubtitleText;

    [Header("Failed State Wiring")]
    [SerializeField] private TMP_Text failedTitleText;
    [SerializeField] private TMP_Text failedSubtitleText;
    [SerializeField] private Button failedOkButton;
    [SerializeField] private TMP_Text failedOkButtonText;

    [Header("Timing")]
    [SerializeField] private float successAutoDismissSeconds = 1.5f;
    [SerializeField] private float loadingDotInterval = 0.5f;

    [Header("Optional")]
    [SerializeField] private bool useOverlay = true;
    private Action _onFailedDismissed;
    private Action _onLoadingCanceled;

    private Coroutine _autoDismissCoroutine;
    private Coroutine _dotsCoroutine;

    private TMP_Text _animatedSubtitleTarget;
    private string _animatedSubtitleBase = string.Empty;
    private State _currentState = State.Hidden;
    private bool _showOverlayBlocker = true;

    public State CurrentState => _currentState;

    private void Awake()
    {
        if (failedOkButton != null)
            failedOkButton.onClick.AddListener(DismissFailed);

        if (loadingCancelButton != null)
            loadingCancelButton.onClick.AddListener(HandleLoadingCanceled);

        Hide();
    }
    protected bool IsThaiLanguage()
    {
        var locale = LocalizationSettings.SelectedLocale;
        if (locale == null)
            return true;

        string code = locale.Identifier.Code;
        return !string.IsNullOrEmpty(code) &&
            code.StartsWith("th", StringComparison.OrdinalIgnoreCase);
    }
    private void OnDestroy()
    {
        if (failedOkButton != null)
            failedOkButton.onClick.RemoveListener(DismissFailed);

        if (loadingCancelButton != null)
            loadingCancelButton.onClick.RemoveListener(HandleLoadingCanceled);
    }

    public void ShowLoading(
    string title,
    string subtitle,
    bool showBlocker = true,
    bool cancelable = false,
    string cancelButtonLabel = "Cancel",
    Action onCancel = null,
    bool animateDots = true)
    {
        StopOverlayCoroutines();

        _showOverlayBlocker = showBlocker;
        _onFailedDismissed = null;
        _onLoadingCanceled = onCancel;

        SetVisible(true);
        Apply(State.Loading);

        if (loadingTitleText != null)
            loadingTitleText.text = title ?? string.Empty;

        if (loadingSubtitleText != null)
            loadingSubtitleText.text = subtitle ?? string.Empty;

        if (loadingCancelButton != null)
            loadingCancelButton.gameObject.SetActive(cancelable);

        if (loadingCancelButtonText != null)
            loadingCancelButtonText.text = string.IsNullOrWhiteSpace(cancelButtonLabel)
                ? "Cancel"
                : cancelButtonLabel;

        if (animateDots)
            StartDotsAnimation(loadingSubtitleText, subtitle);
    }
    public void ShowLoadingWaiting(
    string title,
    string subtitle,
    bool showBlocker = true,
    bool cancelable = false,
    string cancelButtonLabel = "Cancel",
    Action onCancel = null)
    {
        ShowLoading(
            title,
            subtitle,
            showBlocker,
            cancelable,
            cancelButtonLabel,
            onCancel,
            animateDots: true
        );
    }
    public void ShowSuccess(
    string title,
    string subtitle,
    bool autoDismiss = true,
    Action onDone = null,
    bool animateDots = false,
    bool showBlocker = true)
    {
        StopOverlayCoroutines();

        _showOverlayBlocker = showBlocker;
        _onFailedDismissed = null;
        _onLoadingCanceled = null;

        SetVisible(true);
        Apply(State.Success);

        if (successTitleText != null)
            successTitleText.text = title ?? string.Empty;

        if (successSubtitleText != null)
            successSubtitleText.text = subtitle ?? string.Empty;

        if (animateDots)
            StartDotsAnimation(successSubtitleText, subtitle);

        if (autoDismiss)
            _autoDismissCoroutine = StartCoroutine(AutoDismiss(successAutoDismissSeconds, onDone));
    }

    public void ShowSuccessWaiting(string title, string subtitle, bool showBlocker = true)
    {
        ShowSuccess(
            title,
            subtitle,
            autoDismiss: false,
            onDone: null,
            animateDots: true,
            showBlocker: showBlocker
        );
    }

    public void ShowFailed(
        string title,
        string subtitle,
        Action onDismissed = null,
        bool showBlocker = true)
    {
        StopOverlayCoroutines();

        _showOverlayBlocker = showBlocker;
        _onLoadingCanceled = null;
        _onFailedDismissed = onDismissed;
        failedOkButtonText.text = IsThaiLanguage() ? "ตกลง" : "OK";
        SetVisible(true);
        Apply(State.Failed);

        if (failedTitleText != null)
            failedTitleText.text = title ?? string.Empty;

        if (failedSubtitleText != null)
            failedSubtitleText.text = subtitle ?? string.Empty;
    }

    public void Hide()
    {
        StopOverlayCoroutines();

        _onFailedDismissed = null;
        _onLoadingCanceled = null;

        Apply(State.Hidden);

        if (panelRoot != null)
            panelRoot.SetActive(false);

        if (overlay != null)
            overlay.SetActive(false);
    }

    private void Apply(State state)
    {
        _currentState = state;

        if (loadingState != null)
            loadingState.SetActive(state == State.Loading);

        if (successState != null)
            successState.SetActive(state == State.Success);

        if (failedState != null)
            failedState.SetActive(state == State.Failed);
    }

    private void SetVisible(bool show)
    {
        if (panelRoot != null)
            panelRoot.SetActive(show);

        if (overlay != null)
            overlay.SetActive(show && useOverlay && _showOverlayBlocker);
    }

    private void HandleLoadingCanceled()
    {
        Action callback = _onLoadingCanceled;

        Hide();

        _onLoadingCanceled = null;
        callback?.Invoke();
    }

    private void DismissFailed()
    {
        Action callback = _onFailedDismissed;

        Hide();

        _onFailedDismissed = null;
        callback?.Invoke();
    }

    private void StartDotsAnimation(TMP_Text target, string baseText)
    {
        if (target == null)
            return;

        _animatedSubtitleTarget = target;
        _animatedSubtitleBase = baseText ?? string.Empty;
        _dotsCoroutine = StartCoroutine(AnimateDots());
    }

    private IEnumerator AnimateDots()
    {
        if (_animatedSubtitleTarget == null)
            yield break;

        int dotCount = 0;

        while (true)
        {
            _animatedSubtitleTarget.text = _animatedSubtitleBase + new string('.', dotCount);

            yield return new WaitForSeconds(loadingDotInterval);

            dotCount++;
            if (dotCount > 4)
                dotCount = 0;
        }
    }

    private IEnumerator AutoDismiss(float delay, Action callback)
    {
        yield return new WaitForSeconds(delay);

        Hide();
        callback?.Invoke();
    }

    private void StopOverlayCoroutines()
    {
        if (_dotsCoroutine != null)
        {
            StopCoroutine(_dotsCoroutine);
            _dotsCoroutine = null;
        }

        if (_autoDismissCoroutine != null)
        {
            StopCoroutine(_autoDismissCoroutine);
            _autoDismissCoroutine = null;
        }

        _animatedSubtitleTarget = null;
        _animatedSubtitleBase = string.Empty;
    }
}