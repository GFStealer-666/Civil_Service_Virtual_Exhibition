using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AE_ProjectCardUI : MonoBehaviour
{
    [SerializeField] private Button rootButton;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TMP_Text titleText;

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
            backgroundImage.sprite = data != null ? data.FallbackBackgroundSprite : null;
            backgroundImage.preserveAspect = false;
        }
    }

    public void SetBackground(Sprite sprite, int bindVersion)
    {
        if (bindVersion != _bindVersion)
            return;

        if (backgroundImage == null || sprite == null)
            return;

        backgroundImage.sprite = sprite;
    }

    private void HandleClicked()
    {
        if (_data == null)
            return;

        _onClicked?.Invoke(_data);
    }
}