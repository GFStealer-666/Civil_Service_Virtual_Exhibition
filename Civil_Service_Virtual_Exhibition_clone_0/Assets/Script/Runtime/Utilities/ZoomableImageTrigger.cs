using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class ZoomableImageTrigger : MonoBehaviour, IPointerClickHandler
{
    [Header("References")]
    [SerializeField] private Image sourceImage;
    [SerializeField] private Button targetButton;

    [Header("Options")]
    [SerializeField] private bool allowDirectPointerClick = true;

    private void Reset()
    {
        sourceImage = GetComponent<Image>();
        targetButton = GetComponent<Button>();
    }

    public void OnEnable()
    {
        if (sourceImage == null || sourceImage.sprite == null)
        {
            if (GetComponent<Button>())
            {
                Debug.LogWarning("[ZoomableImageTrigger] No ImageZoomViewer found in scene.");
                GetComponent<Button>().enabled = false;
            }
        }
    }

    private void Awake()
    {
        if (targetButton != null)
            targetButton.onClick.AddListener(OpenZoom);
    }

    private void OnDestroy()
    {
        if (targetButton != null)
            targetButton.onClick.RemoveListener(OpenZoom);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!allowDirectPointerClick)
            return;

        if (targetButton == null)
            OpenZoom();
    }

    public void OpenZoom()
    {
        if (ImageZoomViewer.Instance == null)
        {
            Debug.LogWarning("[ZoomableImageTrigger] No ImageZoomViewer found in scene.");
            return;
        }

        if (sourceImage == null || sourceImage.sprite == null)
        {
            Debug.LogWarning("[ZoomableImageTrigger] Source image or sprite is null.");
            return;
        }

        ImageZoomViewer.Instance.Show(sourceImage.sprite);
    }
}