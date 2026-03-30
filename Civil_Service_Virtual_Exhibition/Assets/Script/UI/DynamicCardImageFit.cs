using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class DynamicCardImageFit : MonoBehaviour
{
    [SerializeField] private RectTransform frameRect;
    [SerializeField] private bool cropWideImages = true;

    private Image _image;
    private RectTransform _rect;

    private void Awake()
    {
        _image = GetComponent<Image>();
        _rect = GetComponent<RectTransform>();

        if (frameRect == null)
            frameRect = transform.parent as RectTransform;
    }

    public void Refresh()
    {
        if (_image == null || _image.sprite == null || frameRect == null)
            return;

        float frameWidth = frameRect.rect.width;
        float frameHeight = frameRect.rect.height;

        Rect spriteRect = _image.sprite.rect;
        float imageWidth = spriteRect.width;
        float imageHeight = spriteRect.height;

        if (imageWidth <= 0f || imageHeight <= 0f || frameWidth <= 0f || frameHeight <= 0f)
            return;

        float imageRatio = imageWidth / imageHeight;

        _rect.anchorMin = new Vector2(0.5f, 0.5f);
        _rect.anchorMax = new Vector2(0.5f, 0.5f);
        _rect.pivot = new Vector2(0.5f, 0.5f);
        _rect.anchoredPosition = Vector2.zero;
        _rect.localScale = Vector3.one;

        if (imageRatio < 0.9f)
        {
            float height = frameHeight;
            float width = height * imageRatio;

            _rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            _rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            _image.preserveAspect = true;
        }
        else
        {
            if (cropWideImages)
            {
                float width = frameWidth;
                float height = width / imageRatio;

                if (height < frameHeight)
                {
                    height = frameHeight;
                    width = height * imageRatio;
                }

                _rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
                _rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            }
            else
            {
                _rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, frameWidth);
                _rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, frameHeight);
                _image.preserveAspect = false;
            }
        }
    }
}