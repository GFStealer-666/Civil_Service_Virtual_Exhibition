using System;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

public class HOH_SearchBar : MonoBehaviour
{
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Button clearButton;
    [SerializeField] private bool trimText = true;

    [Header("Optional Localization UI")]
    [SerializeField] private TMP_Text searchLabelText;
    [SerializeField] private TMP_Text placeholderText;

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

    private bool UseEnglish
    {
        get
        {
            var locale = LocalizationSettings.SelectedLocale;
            string code = locale != null ? locale.Identifier.Code : "th";
            return code.StartsWith("en", StringComparison.OrdinalIgnoreCase);
        }
    }

    private string SearchText => UseEnglish ? "Quick Search" : "ค้นหาด่วน";

    private void Awake()
    {
        if (inputField != null)
            inputField.onValueChanged.AddListener(HandleInputChanged);

        if (clearButton != null)
            clearButton.onClick.AddListener(Clear);
    }

    private void OnEnable()
    {
        ApplyLocalization();
        UpdateClearButtonState();
    }

    private void Start()
    {
        UpdateClearButtonState();
    }

    private void OnDestroy()
    {
        if (inputField != null)
            inputField.onValueChanged.RemoveListener(HandleInputChanged);

        if (clearButton != null)
            clearButton.onClick.RemoveListener(Clear);
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
    }

    public void Clear()
    {
        SetText(string.Empty, true);
    }

    public void RefreshLocalization()
    {
        ApplyLocalization();
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

    private void ApplyLocalization()
    {
        if (searchLabelText != null)
            searchLabelText.text = SearchText;

        if (placeholderText != null)
            placeholderText.text = SearchText;
    }

    private string Sanitize(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return trimText ? value.Trim() : value;
    }
}