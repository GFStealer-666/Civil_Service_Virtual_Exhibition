using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AE_ProjectCardUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button rootButton;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private UniversalImageLoader backgroundLoader;

    [Header("Fallback")]
    [SerializeField] private Sprite fallbackBackground;
    [SerializeField] private Color backgroundFallbackColor = new Color(219f / 255f, 219f / 255f, 219f / 255f, 1f);

    private ExhibitionProjectData _data;
    private Action<ExhibitionProjectData> _onClicked;
    private int _bindVersion;

    public int BindVersion => _bindVersion;
    public ExhibitionProjectData Data => _data;

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

    public void Bind(ExhibitionProjectData data, Action<ExhibitionProjectData> onClicked)
    {
        _bindVersion++;

        _data = data;
        _onClicked = onClicked;

        if (titleText != null)
            titleText.text = data != null ? data.Title : string.Empty;

        if (backgroundImage != null)
        {
            backgroundImage.sprite = data != null && data.FallbackBackgroundSprite != null
                ? data.FallbackBackgroundSprite
                : fallbackBackground;

            if (backgroundImage.sprite != null)
            {
                backgroundImage.color = Color.white;
            }
            else
            {
                backgroundImage.color = backgroundFallbackColor;
            }

                backgroundImage.preserveAspect = false;
        }

        if (backgroundLoader != null)
            backgroundLoader.Load(GetBackgroundUrl(data));
    }

    private void HandleClicked()
    {
        if (_data == null)
            return;

        _onClicked?.Invoke(_data);
    }

    private string GetBackgroundUrl(ExhibitionProjectData data)
    {
        if (data == null)
            return string.Empty;

        return data.BackgroundUrl;
    }
}