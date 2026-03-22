using System;
using UnityEngine;
using UnityEngine.UI;

public class PaginationDotUI : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image dotImage;
    [SerializeField] private Color activeColor = new Color32(110, 85, 20, 255);
    [SerializeField] private Color inactiveColor = new Color32(225, 205, 120, 255);
    [SerializeField] private Vector3 activeScale = new Vector3(1.15f, 1.15f, 1f);
    [SerializeField] private Vector3 inactiveScale = Vector3.one;

    private int _pageIndex;
    private Action<int> _onClicked;

    private void Awake()
    {
        if (button != null)
            button.onClick.AddListener(HandleClicked);
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(HandleClicked);
    }

    public void Bind(int pageIndex, Action<int> onClicked)
    {
        _pageIndex = pageIndex;
        _onClicked = onClicked;
    }

    public void SetSelected(bool isSelected)
    {
        if (dotImage != null)
            dotImage.color = isSelected ? activeColor : inactiveColor;

        transform.localScale = isSelected ? activeScale : inactiveScale;
    }

    private void HandleClicked()
    {
        _onClicked?.Invoke(_pageIndex);
    }
}