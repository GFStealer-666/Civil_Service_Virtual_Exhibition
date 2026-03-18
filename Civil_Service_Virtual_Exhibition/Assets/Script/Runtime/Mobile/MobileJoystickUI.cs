using UnityEngine;
using UnityEngine.EventSystems;

public class MobileJoystickUI : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [SerializeField] private RectTransform handle;
    [SerializeField] private float movementRadius = 60f;

    private RectTransform _baseRect;
    private Canvas _canvas;
    private Camera _eventCamera;

    private Vector2 _pointerStartLocalPos;

    private void Awake()
    {
        _baseRect = transform as RectTransform;
        _canvas = GetComponentInParent<Canvas>();

        if (_canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            _eventCamera = _canvas.worldCamera;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (_baseRect == null)
            return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _baseRect,
            eventData.position,
            _eventCamera,
            out _pointerStartLocalPos);

        if (handle != null)
            handle.anchoredPosition = Vector2.zero;

        MobileInputState.Instance?.SetMove(Vector2.zero);
    }

    public void OnDrag(PointerEventData eventData)
    {
        UpdateJoystick(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (handle != null)
            handle.anchoredPosition = Vector2.zero;

        MobileInputState.Instance?.ResetMove();
    }

    private void UpdateJoystick(PointerEventData eventData)
    {
        if (_baseRect == null)
            return;

        Vector2 currentLocalPos;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _baseRect,
                eventData.position,
                _eventCamera,
                out currentLocalPos))
            return;

        Vector2 delta = currentLocalPos - _pointerStartLocalPos;
        Vector2 clamped = Vector2.ClampMagnitude(delta, movementRadius);

        if (handle != null)
            handle.anchoredPosition = clamped;

        Vector2 normalized = clamped / movementRadius;
        MobileInputState.Instance?.SetMove(normalized);
    }
}