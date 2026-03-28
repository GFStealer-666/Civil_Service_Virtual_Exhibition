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

    private PSC_ServiceOrganizationDto _data;
    private Action<PSC_ServiceOrganizationDto> _onClicked;

    public PSC_ServiceOrganizationDto BoundData => _data;

    private void Awake()
    {
        if (rootButton != null)
            rootButton.onClick.AddListener(HandleClicked);
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

        if (organizationNameText != null)
            organizationNameText.text = GetDisplayName(data);

        if (serviceCountText != null)
            serviceCountText.text = $"จำนวน {GetServiceCount(data)} บริการ";

        BindLogo(data);
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

        if (!string.IsNullOrWhiteSpace(data.name))
            return data.name;

        return data.nameEn;
    }

    private int GetServiceCount(PSC_ServiceOrganizationDto data)
    {
        if (data == null)
            return 0;

        if (data.runtimeServiceCount > 0)
            return data.runtimeServiceCount;

        return data.services != null ? data.services.Length : 0;
    }

    private void HandleClicked()
    {
        if (_data == null)
            return;

        _onClicked?.Invoke(_data);
    }
}