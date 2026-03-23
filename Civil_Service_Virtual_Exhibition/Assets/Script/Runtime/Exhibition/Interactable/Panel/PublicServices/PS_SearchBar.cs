using System.Collections;
using TMPro;
using UnityEngine;

public class PS_SearchBar : MonoBehaviour
{
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private PS_PanelController targetPanel;
    [SerializeField] private bool notifyInitialValue = true;

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

        inputField.SetTextWithoutNotify(string.Empty);
        inputField.ForceLabelUpdate();
        NotifyPanel(string.Empty);
    }

    private void HandleValueChanged(string value)
    {
        NotifyPanel(value);
    }

    private void NotifyPanel(string value)
    {
        if (targetPanel != null)
            targetPanel.SetSearchKeyword(value ?? string.Empty);
    }
}