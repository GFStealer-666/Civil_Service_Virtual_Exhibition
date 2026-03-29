using System;
using TMPro;
using UnityEngine;

public class PS_ServiceItemView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text departmentText;
    [SerializeField] private TMP_Text dateText;
    [SerializeField] private TMP_Text placesText;

    private PS_ServiceActivityDto _data;
    private string _lastLocaleCode = string.Empty;

    private void OnEnable()
    {
        RefreshLocalization();
    }

    private void Update()
    {
        string currentLocaleCode = GetLocaleCode();
        if (string.Equals(_lastLocaleCode, currentLocaleCode, StringComparison.OrdinalIgnoreCase))
            return;

        RefreshLocalization();
    }

    public void Bind(PS_ServiceActivityDto data)
    {
        _data = data;
        RefreshLocalization();
    }

    public void Bind(PS_ServiceActivityDto data, bool useEnglish)
    {
        _data = data;
        Apply(useEnglish);
    }

    public void RefreshLocalization()
    {
        _lastLocaleCode = GetLocaleCode();
        Apply(IsEnglish());
    }

    private void Apply(bool useEnglish)
    {
        if (_data == null)
            return;

        if (titleText != null)
            titleText.text = Clean(
                useEnglish ? _data.activityNameEn : _data.activityName,
                "-");

        if (departmentText != null)
            departmentText.text = Clean(
                useEnglish ? _data.departmentEn : _data.department,
                "-");

        if (dateText != null)
            dateText.text = Clean(
                useEnglish ? _data.activityDateEn : _data.activityDate,
                useEnglish ? "Not specified" : "ไม่ระบุ");

        if (placesText != null)
            placesText.text = useEnglish ? "Not specified" : "ไม่ระบุ";
    }

    private static bool IsEnglish()
    {
        string localeCode = GetLocaleCode();
        return localeCode.StartsWith("en", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetLocaleCode()
    {
        string localeCode = LocalizationService.CurrentLocaleCode;
        return string.IsNullOrWhiteSpace(localeCode) ? "th" : localeCode;
    }

    private static string Clean(string value, string fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
            return fallback;

        return value.Replace("\r", " ").Replace("\n", " ").Trim();
    }
}