using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ExhibitionAgencyCardUI : MonoBehaviour
{
    [SerializeField] private Button rootButton;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image logoImage;
    [SerializeField] private TMP_Text titleText;

    private ExhibitionAgencyData _data;
    private Action<ExhibitionAgencyData> _onClicked;

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

    public void Bind(ExhibitionAgencyData data, Action<ExhibitionAgencyData> onClicked)
    {
        _data = data;
        _onClicked = onClicked;

        if (titleText != null)
            titleText.text = data != null ? data.Title : string.Empty;

        if (backgroundImage != null)
        {
            backgroundImage.sprite = data != null ? data.BackgroundSprite : default;
        }

        if (logoImage != null)
        {
            logoImage.sprite = data != null ? data.LogoSprite : default;
        }
    }

    private void HandleClicked()
    {
        if (_data == null)
            return;

        _onClicked?.Invoke(_data);
    }
}