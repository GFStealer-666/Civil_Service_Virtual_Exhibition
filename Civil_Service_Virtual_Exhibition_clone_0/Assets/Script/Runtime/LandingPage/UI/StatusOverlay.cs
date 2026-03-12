
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StatusOverlay : MonoBehaviour
{
    public enum State { Loading, Success, Error }

    [Header("Root")]
    [SerializeField] private GameObject  panelRoot;      // the white card

    [Header("State Objects")]
    [SerializeField] private GameObject loadingState;   // gold clock card
    [SerializeField] private GameObject successState;   // green tick card
    [SerializeField] private GameObject errorState;     // red X card

    [Header("Error State Wiring")]
    [SerializeField] private TMP_Text   errorTitleText;    // "เข้าสู่ระบบล้มเหลว"
    [SerializeField] private TMP_Text   errorSubtitleText; // dynamic message
    [SerializeField] private Button     errorOkButton;     // "ตกลง"

    [Header("Timing")]
    [SerializeField] private float successAutoDismissSeconds = 10f;

    private Action _onErrorDismissed;

    private void Awake()
    {
        errorOkButton.onClick.AddListener(DismissError);
        Hide();
    }

    public void ShowLoading()
    {
        SetVisible(true);
        Apply(State.Loading);
    }

    /// <summary>Show success card, then auto-dismiss and run <paramref name="onDone"/>.</summary>
    public void ShowSuccess(Action onDone = null)
    {
        SetVisible(true);   // ← add this
        Apply(State.Success);
        StartCoroutine(AutoDismiss(successAutoDismissSeconds, onDone));
    }

    public void ShowError(string message, Action onDismissed = null)
    {
        SetVisible(true);   // ← add this
        Apply(State.Error);
        if (errorSubtitleText != null) errorSubtitleText.text = message;
        _onErrorDismissed = onDismissed;
    }

    public void Hide()
    {
        SetVisible(false);
    }

    // ── Private ─────────────────────────────────────────────────

    private void Apply(State state)
    {
        loadingState.SetActive(state == State.Loading);
        successState.SetActive(state == State.Success);
        errorState  .SetActive(state == State.Error);
    }

    private void SetVisible(bool show)
    {
        if (panelRoot      != null) panelRoot     .SetActive(show);
    }

    private void DismissError()
    {
        Hide();
        var cb = _onErrorDismissed;
        _onErrorDismissed = null;
        cb?.Invoke();
    }

    private IEnumerator AutoDismiss(float delay, Action callback)
    {
        yield return new WaitForSeconds(delay);

        callback?.Invoke();
    }
}