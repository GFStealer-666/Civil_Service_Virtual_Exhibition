using UnityEngine;
using UnityEngine.UI;

public class ImageZoomViewer : MonoBehaviour
{
    public static ImageZoomViewer Instance { get; private set; }

    [Header("References")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Image largeImage;
    [SerializeField] private AspectRatioFitter aspectRatioFitter;
    [SerializeField] private Button closeButton;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (closeButton != null)
            closeButton.onClick.AddListener(Hide);

        HideImmediate();
    }

    public void Show(Sprite sprite)
    {
        if (sprite == null)
        {
            Debug.LogWarning("[ImageZoomViewer] Sprite is null.");
            return;
        }

        if (largeImage != null)
            largeImage.sprite = sprite;

        if (aspectRatioFitter != null && sprite.rect.height > 0f)
            aspectRatioFitter.aspectRatio = sprite.rect.width / sprite.rect.height;

        if (panelRoot != null)
            panelRoot.SetActive(true);
        else
            gameObject.SetActive(true);
    }

    public void Hide()
    {
        if (largeImage != null)
            largeImage.sprite = null;

        if (panelRoot != null)
            panelRoot.SetActive(false);
        else
            gameObject.SetActive(false);
    }

    private void HideImmediate()
    {
        if (largeImage != null)
            largeImage.sprite = null;

        if (panelRoot != null)
            panelRoot.SetActive(false);
        else
            gameObject.SetActive(false);
    }
}