using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

[RequireComponent(typeof(TMP_Dropdown))]
public class GenderDropdownSetup : MonoBehaviour
{
    [Serializable]
    public class GenderOption
    {
        public string value;
        public string thaiLabel;
        public string englishLabel;
    }

    private const int PLACEHOLDER_INDEX = 0;

    [Header("Options")]
    [SerializeField] private List<GenderOption> options = new()
    {
        new GenderOption
        {
            value = "",
            thaiLabel = "เลือกเพศ",
            englishLabel = "Select gender"
        },
        new GenderOption
        {
            value = "male",
            thaiLabel = "ชาย",
            englishLabel = "Male"
        },
        new GenderOption
        {
            value = "female",
            thaiLabel = "หญิง",
            englishLabel = "Female"
        }
    };

    [Header("Colors")]
    [SerializeField] private Color placeholderColor = new Color(0.35f, 0.35f, 0.35f, 1f);
    [SerializeField] private Color selectedColor = new Color(0.15f, 0.15f, 0.15f, 1f);

    private TMP_Dropdown _dropdown;
    private int _selectedIndex = PLACEHOLDER_INDEX;

    private void Awake()
    {
        _dropdown = GetComponent<TMP_Dropdown>();

        RebuildOptions();

        _dropdown.onValueChanged.RemoveListener(OnValueChanged);
        _dropdown.onValueChanged.AddListener(OnValueChanged);

        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;

    }

    private void Start()
    {
        RebuildOptions();
    }

    private void OnDestroy()
    {
        if (_dropdown != null)
            _dropdown.onValueChanged.RemoveListener(OnValueChanged);

        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
    }

    private void OnLocaleChanged(Locale _)
    {
        RebuildOptions();
    }

    private void OnValueChanged(int index)
    {
        _selectedIndex = Mathf.Clamp(index, 0, Mathf.Max(0, options.Count - 1));
        UpdateLabelColor();
    }

    private void RebuildOptions()
    {
        if (_dropdown == null)
            return;

        _selectedIndex = Mathf.Clamp(_selectedIndex, 0, Mathf.Max(0, options.Count - 1));

        bool isThai = IsThaiLanguage();
        List<string> displayOptions = new List<string>(options.Count);

        for (int i = 0; i < options.Count; i++)
        {
            displayOptions.Add(isThai ? options[i].thaiLabel : options[i].englishLabel);
        }

        _dropdown.ClearOptions();
        _dropdown.AddOptions(displayOptions);
        _dropdown.value = _selectedIndex;
        _dropdown.RefreshShownValue();

        UpdateLabelColor();
    }

    private void UpdateLabelColor()
    {
        if (_dropdown == null || _dropdown.captionText == null)
            return;

        _dropdown.captionText.color = _dropdown.value == PLACEHOLDER_INDEX
            ? placeholderColor
            : selectedColor;
    }

    private static bool IsThaiLanguage()
    {
        var locale = LocalizationSettings.SelectedLocale;
        if (locale == null)
            return true;

        string code = locale.Identifier.Code;
        return !string.IsNullOrEmpty(code) && code.StartsWith("th", StringComparison.OrdinalIgnoreCase);
    }

    public bool IsValidSelection()
    {
        return _dropdown != null && _dropdown.value != PLACEHOLDER_INDEX;
    }

    public string GetSelectedGender()
    {
        if (_dropdown == null)
            return string.Empty;

        int index = _dropdown.value;
        if (index < 0 || index >= options.Count)
            return string.Empty;

        return options[index].value;
    }
}