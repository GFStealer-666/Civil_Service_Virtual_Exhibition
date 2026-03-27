using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PSC_OrganizationItemView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button rootButton;
    [SerializeField] private Image logoImage;
    [SerializeField] private TMP_Text organizationNameText;

    [Header("Fallback")]
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

    public void Bind(
        PSC_ServiceOrganizationDto data,
        Action<PSC_ServiceOrganizationDto> onClicked,
        Sprite logoOverride = null)
    {
        _data = data;
        _onClicked = onClicked;

        if (organizationNameText != null)
            organizationNameText.text = GetDisplayName(data);
        if (logoImage != null)
        {
            logoImage.sprite = logoOverride != null ? logoOverride : fallbackLogo;
            logoImage.enabled = logoImage.sprite != null;
        }
    }

    private void HandleClicked()
    {
        if (_data == null)
            return;

        _onClicked?.Invoke(_data);
    }

    private string GetDisplayName(PSC_ServiceOrganizationDto data)
    {
        if (data == null)
            return string.Empty;

        if (!string.IsNullOrWhiteSpace(data.name))
            return data.name;

        return data.nameEn;
    }

    private string BuildServiceCountText(PSC_ServiceOrganizationDto data)
    {
        int count = data != null ? data.runtimeServiceCount : 0;
        return $"จำนวน {count} บริการ";
    }
}