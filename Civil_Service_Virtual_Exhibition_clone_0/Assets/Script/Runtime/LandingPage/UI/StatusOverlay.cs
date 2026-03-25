using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

    [Header("State Objects")]
    [SerializeField] private GameObject loadingState;
    [SerializeField] private GameObject successState;
    [SerializeField] private GameObject failedState;
    [SerializeField] private GameObject overlay; // block raycast 
    [Header("Loading State Wiring")]
    [SerializeField] private TMP_Text loadingTitleText;
    [SerializeField] private TMP_Text loadingSubtitleText;

    [Header("Success State Wiring")]
    [SerializeField] private TMP_Text successTitleText;
    [SerializeField] private TMP_Text successSubtitleText;

    [Header("Failed State Wiring")]
    [SerializeField] private TMP_Text failedTitleText;
    [SerializeField] private TMP_Text failedSubtitleText;
    [SerializeField] private Button failedOkButton;

    [Header("Timing")]
    [SerializeField] private float successAutoDismissSeconds = 1.5f;
    [SerializeField] private float loadingDotInterval = 0.5f;
    [Header("Optional")]
    [SerializeField] private bool useOverlay = true;
    private Action _onFailedDismissed;
    private Coroutine _loadingDotsCoroutine;
    private Coroutine _autoDismissCoroutine;

    private string _loadingSubtitleBase;
    private State _currentState = State.Hidden;

    public State CurrentState => _currentState;

    private void Awake()
    {
        if (failedOkButton != null)
            failedOkButton.onClick.AddListener(DismissFailed);

        Hide();
    }

    private void OnDestroy()
    {
        if (failedOkButton != null)
            failedOkButton.onClick.RemoveListener(DismissFailed);
    }

    public void ShowLoading(string title, string subtitle)
    {
        StopOverlayCoroutines();
        
        SetVisible(true);
        Apply(State.Loading);

        if (loadingTitleText != null)
            loadingTitleText.text = title;

        _loadingSubtitleBase = subtitle ?? string.Empty;

        if (loadingSubtitleText != null)
            loadingSubtitleText.text = _loadingSubtitleBase;

        _loadingDotsCoroutine = StartCoroutine(AnimateLoadingDots());
    }

    public void ShowSuccess(string title, string subtitle, bool autoDismiss = true, Action onDone = null)
    {
        StopOverlayCoroutines();

        SetVisible(true);
        Apply(State.Success);

        if (successTitleText != null)
            successTitleText.text = title;

        if (successSubtitleText != null)
            successSubtitleText.text = subtitle;

        if (autoDismiss)
            _autoDismissCoroutine = StartCoroutine(AutoDismiss(successAutoDismissSeconds, onDone));
        else
            onDone?.Invoke();
    }

    public void ShowFailed(string title, string subtitle, Action onDismissed = null)
    {
        StopOverlayCoroutines();

        SetVisible(true);
        Apply(State.Failed);

        if (failedTitleText != null)
            failedTitleText.text = title;

        if (failedSubtitleText != null)
            failedSubtitleText.text = subtitle;

        _onFailedDismissed = onDismissed;
    }

    public void Hide()
    {
        StopOverlayCoroutines();
        _onFailedDismissed = null;

        Apply(State.Hidden);
        SetVisible(false);

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

        if(useOverlay || !overlay.activeSelf)
            overlay.SetActive(true);
    }

    private void DismissFailed()
    {
        Hide();

        Action callback = _onFailedDismissed;
        _onFailedDismissed = null;
        callback?.Invoke();
    }

    private IEnumerator AnimateLoadingDots()
    {
        if (loadingSubtitleText == null)
            yield break;

        int dotCount = 0;

        while (true)
        {
            string dots = new string('.', dotCount);
            loadingSubtitleText.text = _loadingSubtitleBase + dots;

            yield return new WaitForSeconds(loadingDotInterval);

            dotCount++;
            if (dotCount > 3)
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
        if (_loadingDotsCoroutine != null)
        {
            StopCoroutine(_loadingDotsCoroutine);
            _loadingDotsCoroutine = null;
        }

        if (_autoDismissCoroutine != null)
        {
            StopCoroutine(_autoDismissCoroutine);
            _autoDismissCoroutine = null;
        }
    }
}