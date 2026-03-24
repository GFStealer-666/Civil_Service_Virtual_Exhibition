using System.Collections;
using TMPro;
using UnityEngine;

public class PS_SearchBar : MonoBehaviour
{
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private PS_PanelController targetPanel;
    [SerializeField] private bool notifyInitialValue = true;

    private Coroutine _notifyRoutine;

    private void Awake()
    {
        if (inputField != null)
            inputField.onValueChanged.AddListener(HandleValueChanged);
    }

    private IEnumerator Start()
    {
        if (!notifyInitialValue || inputField == null)
            yield break;

        yield return null;

        inputField.ForceLabelUpdate();
        NotifyPanel(inputField.text);
    }

    private void OnDestroy()
    {
        if (inputField != null)
            inputField.onValueChanged.RemoveListener(HandleValueChanged);
    }

    public void ClearSearch()
    {
        if (inputField == null)
            return;

        if (_notifyRoutine != null)
        {
            StopCoroutine(_notifyRoutine);
            _notifyRoutine = null;
        }

        inputField.DeactivateInputField();
        inputField.SetTextWithoutNotify(string.Empty);

        StartCoroutine(ClearAndNotifyNextFrame());
    }

    private IEnumerator ClearAndNotifyNextFrame()
    {
        yield return null;

        if (inputField == null)
            yield break;

        inputField.ForceLabelUpdate();
        NotifyPanel(string.Empty);
    }

    private void HandleValueChanged(string value)
    {
        if (_notifyRoutine != null)
            StopCoroutine(_notifyRoutine);

        _notifyRoutine = StartCoroutine(NotifyNextFrame(value));
    }

    private IEnumerator NotifyNextFrame(string value)
    {
        yield return null;

        if (this == null || !isActiveAndEnabled)
            yield break;

        if (inputField == null)
            yield break;

        inputField.ForceLabelUpdate();
        NotifyPanel(value ?? string.Empty);

        _notifyRoutine = null;
    }

    private void NotifyPanel(string value)
    {
        if (targetPanel != null)
            targetPanel.SetSearchKeyword(value ?? string.Empty);
    }
}