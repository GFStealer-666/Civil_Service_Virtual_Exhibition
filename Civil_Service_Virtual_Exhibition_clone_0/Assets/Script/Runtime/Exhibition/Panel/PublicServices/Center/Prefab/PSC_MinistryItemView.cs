using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PSC_MinistryItemView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button rootButton;
    [SerializeField] private TMP_Text ministryNameText;
    [SerializeField] private TMP_Text countText;
    [SerializeField] private Image logoImage;
    [SerializeField] private UniversalImageLoader logoLoader;
    [SerializeField] private Sprite fallbackLogo;

    [Header("Display")]
    [SerializeField] private bool showOrganizationCount = true;
    [SerializeField] private string organizationCountFormatTh = "{0} หน่วยบริการ";
    [SerializeField] private string organizationCountFormatEn = "{0} service units";
    [SerializeField] private string serviceCountFormatTh = "{0} บริการ";
    [SerializeField] private string serviceCountFormatEn = "{0} services";

    private PSC_ServiceMinistryDto _data;
    private Action<PSC_ServiceMinistryDto> _onClicked;
    private string _lastLocaleCode = string.Empty;

    public PSC_ServiceMinistryDto BoundData => _data;

    private void Awake()
    {
        if (rootButton != null)
            rootButton.onClick.AddListener(HandleClicked);
    }

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

    private void OnDestroy()
    {
        if (rootButton != null)
            rootButton.onClick.RemoveListener(HandleClicked);
    }

    public void Bind(PSC_ServiceMinistryDto data, Action<PSC_ServiceMinistryDto> onClicked)
    {
        _data = data;
        _onClicked = onClicked;
        RefreshLocalization();
        BindLogo(data);
    }

    public void RefreshLocalization()
    {
        _lastLocaleCode = GetLocaleCode();

        if (ministryNameText != null)
            ministryNameText.text = GetDisplayName(_data);

        if (countText != null)
        {
            int count = showOrganizationCount
                ? (_data != null ? _data.runtimeOrganizationCount : 0)
                : (_data != null ? _data.runtimeServiceCount : 0);

            countText.text = GetCountText(count);
        }
    }

    private void BindLogo(PSC_ServiceMinistryDto data)
    {
        string imageUrl = data != null ? data.ministryLogo : string.Empty;

        if (logoImage != null)
        {
            logoImage.sprite = fallbackLogo;
            logoImage.enabled = true;
        }

        if (logoLoader == null)
            return;

        if (string.IsNullOrWhiteSpace(imageUrl))
            return;

        logoLoader.Load(imageUrl);
    }

    private string GetDisplayName(PSC_ServiceMinistryDto data)
    {
        if (data == null)
            return string.Empty;

        return Pick(data.ministry, data.ministryEn, "-");
    }

    private string GetCountText(int count)
    {
        if (showOrganizationCount)
        {
            string format = IsEnglish()
                ? Clean(organizationCountFormatEn, "{0} service units")
                : Clean(organizationCountFormatTh, "{0} หน่วยบริการ");

            return string.Format(format, count);
        }

        string serviceFormat = IsEnglish()
            ? Clean(serviceCountFormatEn, "{0} services")
            : Clean(serviceCountFormatTh, "{0} บริการ");

        return string.Format(serviceFormat, count);
    }

    private void HandleClicked()
    {
        if (_data == null)
            return;

        _onClicked?.Invoke(_data);
    }

    private static string Pick(string thai, string english, string fallback)
    {
        bool useEnglish = IsEnglish();

        string primary = useEnglish ? english : thai;
        if (!string.IsNullOrWhiteSpace(primary))
            return primary.Trim();

        string secondary = useEnglish ? thai : english;
        if (!string.IsNullOrWhiteSpace(secondary))
            return secondary.Trim();

        return fallback;
    }

    private static bool IsEnglish()
    {
        return GetLocaleCode().StartsWith("en", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetLocaleCode()
    {
        string code = LocalizationService.CurrentLocaleCode;
        return string.IsNullOrWhiteSpace(code) ? "th" : code;
    }

    private static string Clean(string value, string fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
            return fallback;

        return value.Replace("\r", " ").Replace("\n", " ").Trim();
    }
}