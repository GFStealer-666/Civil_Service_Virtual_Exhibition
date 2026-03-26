using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class HOH_SearchBar : MonoBehaviour
{
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Button clearButton;
    [SerializeField] private bool trimText = true;

    public event Action<string> SearchChanged;

    public string CurrentText
    {
        get
        {
            if (inputField == null)
                return string.Empty;

            return Sanitize(inputField.text);
        }
    }

    private void Awake()
    {
        if (inputField != null)
            inputField.onValueChanged.AddListener(HandleInputChanged);

        if (clearButton != null)
            clearButton.onClick.AddListener(Clear);
    }

    private void Start()
    {
        UpdateClearButtonState();
    }

    public void SetText(string value, bool notify = true)
    {
        if (inputField == null)
            return;

        string sanitized = Sanitize(value);

        inputField.SetTextWithoutNotify(sanitized);
        UpdateClearButtonState();

        if (notify)
            SearchChanged?.Invoke(sanitized);

        if(Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Clear();
        }
    }

    public void Clear()
    {
        SetText(string.Empty, true);
    }

    private void HandleInputChanged(string value)
    {
        string sanitized = Sanitize(value);
        UpdateClearButtonState();
        SearchChanged?.Invoke(sanitized);
    }

    private void UpdateClearButtonState()
    {
        if (clearButton == null)
            return;

        clearButton.gameObject.SetActive(!string.IsNullOrWhiteSpace(CurrentText));
    }

    private string Sanitize(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return trimText ? value.Trim() : value;
    }
}