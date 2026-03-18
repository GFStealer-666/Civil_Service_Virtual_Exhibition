using UnityEngine;

public class MobileInputState : MonoBehaviour
{
    public static MobileInputState Instance { get; private set; }

    [Header("Canvas Toggle")]
    [SerializeField] private Canvas mobileCanvas;
    [SerializeField] private bool useMobileInputInEditor = false;

    public Vector2 Move { get; private set; }
    public bool SprintHeld { get; private set; }

    private Vector2 _lookDelta;
    private float _zoomDelta;
    private bool _interactPressed;

    public bool UseMobileInput
    {
        get
        {
            if (Application.isMobilePlatform)
                return true;

            if (Application.platform == RuntimePlatform.WebGLPlayer)
                return false;

            return useMobileInputInEditor;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        ApplyCanvasState();
    }

    private void OnEnable()
    {
        ApplyCanvasState();
    }

    private void Start()
    {
        ApplyCanvasState();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void ApplyCanvasState()
    {
        if (mobileCanvas != null)
            mobileCanvas.gameObject.SetActive(UseMobileInput);
    }

    public void SetMove(Vector2 value)
    {
        Move = Vector2.ClampMagnitude(value, 1f);
    }

    public void ResetMove()
    {
        Move = Vector2.zero;
    }

    public void SetSprintHeld(bool isHeld)
    {
        SprintHeld = isHeld;
    }

    public void PressInteract()
    {
        _interactPressed = true;
    }

    public bool ConsumeInteractPressed()
    {
        bool value = _interactPressed;
        _interactPressed = false;
        return value;
    }

    public void AddLookDelta(Vector2 delta)
    {
        _lookDelta += delta;
    }

    public Vector2 ConsumeLookDelta()
    {
        Vector2 value = _lookDelta;
        _lookDelta = Vector2.zero;
        return value;
    }

    public void AddZoomDelta(float delta)
    {
        _zoomDelta += delta;
    }

    public float ConsumeZoomDelta()
    {
        float value = _zoomDelta;
        _zoomDelta = 0f;
        return value;
    }
}