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
    [SerializeField] private Sprite fallbackLogo;

    [Header("Display")]
    [SerializeField] private bool useEnglishText;
    [SerializeField] private bool showOrganizationCount = true;

    private PSC_ServiceMinistryDto _data;
    private Action<PSC_ServiceMinistryDto> _onClicked;

    public PSC_ServiceMinistryDto BoundData => _data;

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

    public void Bind(PSC_ServiceMinistryDto data, Action<PSC_ServiceMinistryDto> onClicked)
    {
        _data = data;
        _onClicked = onClicked;

        if (ministryNameText != null)
            ministryNameText.text = GetDisplayName(data);

        if (countText != null)
        {
            int count = showOrganizationCount
                ? (data != null ? data.runtimeOrganizationCount : 0)
                : (data != null ? data.runtimeServiceCount : 0);

            countText.text = showOrganizationCount
                ? $"{count} หน่วยบริการ"
                : $"{count} บริการ";
        }

        if (logoImage != null)
        {
            logoImage.sprite = fallbackLogo;
            logoImage.enabled = logoImage.sprite != null;
        }
    }

    private string GetDisplayName(PSC_ServiceMinistryDto data)
    {
        if (data == null)
            return string.Empty;

        if (useEnglishText && !string.IsNullOrWhiteSpace(data.ministryEn))
            return data.ministryEn;

        return data.ministry;
    }

    private void HandleClicked()
    {
        if (_data == null)
            return;

        _onClicked?.Invoke(_data);
    }
}