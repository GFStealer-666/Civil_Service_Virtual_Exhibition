using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class UIFitImage : MonoBehaviour
{
    public enum FitMode
    {
        Contain,
        Cover
    }

    [SerializeField] private FitMode fitMode = FitMode.Cover;
    [SerializeField] private RectTransform targetRect;

    private Image _image;
    private RectTransform _rectTransform;

    private void Awake()
    {
        _image = GetComponent<Image>();
        _rectTransform = GetComponent<RectTransform>();

        if (targetRect == null)
            targetRect = transform.parent as RectTransform;
    }

    public void Refresh()
    {
        if (_image == null || _image.sprite == null || targetRect == null)
            return;

        Sprite sprite = _image.sprite;
        Rect spriteRect = sprite.rect;

        float spriteWidth = spriteRect.width;
        float spriteHeight = spriteRect.height;

        float targetWidth = targetRect.rect.width;
        float targetHeight = targetRect.rect.height;

        if (spriteWidth <= 0f || spriteHeight <= 0f || targetWidth <= 0f || targetHeight <= 0f)
            return;

        float spriteRatio = spriteWidth / spriteHeight;
        float targetRatio = targetWidth / targetHeight;

        float width;
        float height;

        if (fitMode == FitMode.Contain)
        {
            if (spriteRatio > targetRatio)
            {
                width = targetWidth;
                height = width / spriteRatio;
            }
            else
            {
                height = targetHeight;
                width = height * spriteRatio;
            }
        }
        else
        {
            if (spriteRatio > targetRatio)
            {
                height = targetHeight;
                width = height * spriteRatio;
            }
            else
            {
                width = targetWidth;
                height = width / spriteRatio;
            }
        }

        _rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        _rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        _rectTransform.anchoredPosition = Vector2.zero;
        _rectTransform.localScale = Vector3.one;
    }
}