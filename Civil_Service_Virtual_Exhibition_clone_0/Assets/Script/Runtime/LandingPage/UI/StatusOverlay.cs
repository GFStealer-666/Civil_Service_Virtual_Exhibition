using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StatusOverlay : MonoBehaviour
{
    public enum State { Loading, Success, Error }

    [Header("Root")]
    [SerializeField] private GameObject panelRoot;

    [Header("State Objects")]
    [SerializeField] private GameObject loadingState;
    [SerializeField] private GameObject successState;
    [SerializeField] private GameObject errorState;

    [Header("Success State Wiring")]
    [SerializeField] private TMP_Text successTitleText;
    [SerializeField] private TMP_Text successSubtitleText;
    [Header("Error State Wiring")]
    [SerializeField] private TMP_Text errorTitleText;
    [SerializeField] private TMP_Text errorSubtitleText;
    [SerializeField] private Button errorOkButton;

    [Header("Timing")]
    [SerializeField] private float successAutoDismissSeconds = 3f;
    [SerializeField] private float loadingDotInterval = 0.5f;
    private Action _onErrorDismissed;
    private Coroutine _successTypingCoroutine;
    private string _animatedBaseTitle;
    private void Awake()
    {
        errorOkButton.onClick.AddListener(DismissError);
        Hide();
    }

    public void ShowLoading()
    {
        StopAllCoroutines();
        SetVisible(true);
        Apply(State.Loading);
    }

    // No auto dismiss
    public void ShowSuccessNoDismiss(string title, string subtitle, Action onDone = null)
    {
        StopAllCoroutines();
        _successTypingCoroutine = null;

        SetVisible(true);
        Apply(State.Success);

        _animatedBaseTitle = title;

        if (successTitleText != null)
            successTitleText.text = title;

        if (successSubtitleText != null)
            successSubtitleText.text = subtitle;

        _successTypingCoroutine = StartCoroutine(AnimateSuccessDots());

        onDone?.Invoke();
    }

    // With Auto dismiss
    public void ShowSuccessDismiss(string title, string subtitle, Action onDone = null)
    {
        StopAllCoroutines();
        SetVisible(true);
        Apply(State.Success);

        if (successTitleText != null)
            successTitleText.text = title;

        if (successSubtitleText != null)
            successSubtitleText.text = subtitle;

        StartCoroutine(AutoDismiss(successAutoDismissSeconds, onDone));
    }

    public void ShowError(string message, Action onDismissed = null)
    {
        StopAllCoroutines();
        SetVisible(true);
        Apply(State.Error);

        if (errorSubtitleText != null)
            errorSubtitleText.text = message;

        _onErrorDismissed = onDismissed;
    }

    public void Hide()
    {
        StopAllCoroutines();
        _successTypingCoroutine = null;
        SetVisible(false);
    }

    private void Apply(State state)
    {
        loadingState.SetActive(state == State.Loading);
        successState.SetActive(state == State.Success);
        errorState.SetActive(state == State.Error);
    }

    private void SetVisible(bool show)
    {
        if (panelRoot != null)
            panelRoot.SetActive(show);
    }

    private void DismissError()
    {
        Hide();
        var cb = _onErrorDismissed;
        _onErrorDismissed = null;
        cb?.Invoke();
    }
    private IEnumerator AnimateSuccessDots()
    {
        if (successTitleText == null || string.IsNullOrEmpty(_animatedBaseTitle))
            yield break;

        int dotCount = 0;

        while (true)
        {
            string dots = new string('.', dotCount);
            successTitleText.text = _animatedBaseTitle + dots;

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
}