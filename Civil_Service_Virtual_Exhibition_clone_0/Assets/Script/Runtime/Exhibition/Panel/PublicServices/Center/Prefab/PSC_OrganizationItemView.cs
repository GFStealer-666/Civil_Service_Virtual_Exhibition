using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PSC_OrganizationItemView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button rootButton;
    [SerializeField] private Image logoImage;
    [SerializeField] private UniversalImageLoader logoLoader;
    [SerializeField] private TMP_Text organizationNameText;
    [SerializeField] private TMP_Text serviceCountText;
    [SerializeField] private Sprite fallbackLogo;

    [Header("Localization")]
    [SerializeField] private string serviceCountFormatTh = "จำนวน {0} บริการ";
    [SerializeField] private string serviceCountFormatEn = "{0} services";

    private PSC_ServiceOrganizationDto _data;
    private Action<PSC_ServiceOrganizationDto> _onClicked;
    private string _lastLocaleCode = string.Empty;

    public PSC_ServiceOrganizationDto BoundData => _data;

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

    public void Bind(PSC_ServiceOrganizationDto data, Action<PSC_ServiceOrganizationDto> onClicked)
    {
        _data = data;
        _onClicked = onClicked;

        RefreshLocalization();
        BindLogo(data);
    }

    public void RefreshLocalization()
    {
        _lastLocaleCode = GetLocaleCode();

        if (organizationNameText != null)
            organizationNameText.text = GetDisplayName(_data);

        if (serviceCountText != null)
            serviceCountText.text = GetServiceCountText(GetServiceCount(_data));
    }

    private void BindLogo(PSC_ServiceOrganizationDto data)
    {
        if (logoImage != null)
        {
            logoImage.sprite = fallbackLogo;
            logoImage.enabled = logoImage.sprite != null;
        }

        if (logoLoader == null)
            return;

        string imageUrl = data != null ? data.logoUrl : string.Empty;
        logoLoader.Load(string.IsNullOrWhiteSpace(imageUrl) ? string.Empty : imageUrl);
    }

    private string GetDisplayName(PSC_ServiceOrganizationDto data)
    {
        if (data == null)
            return string.Empty;

        return Pick(data.name, data.nameEn, "-");
    }

    private int GetServiceCount(PSC_ServiceOrganizationDto data)
    {
        if (data == null)
            return 0;

        if (data.runtimeServiceCount > 0)
            return data.runtimeServiceCount;

        return data.services != null ? data.services.Length : 0;
    }

    private string GetServiceCountText(int count)
    {
        string format = IsEnglish()
            ? Clean(serviceCountFormatEn, "{0} services")
            : Clean(serviceCountFormatTh, "จำนวน {0} บริการ");

        return string.Format(format, count);
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