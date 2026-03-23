using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonCameraFollow : MonoBehaviour
{
    [Header("Follow")]
    [SerializeField] private Vector3 pivotOffset = new Vector3(0f, 1.6f, 0f);
    [SerializeField] private float distance = 3.5f;
    [SerializeField] private float minDistance = 2.0f;
    [SerializeField] private float maxDistance = 6.0f;
    [SerializeField] private float zoomSpeed = 0.02f;

    [Header("Smoothing")]
    [SerializeField] private float positionSmooth = 12f;
    [SerializeField] private float rotationSmooth = 18f;

    [Header("Look")]
    [SerializeField] private float lookSensitivity = 1.5f;
    [SerializeField] private bool invertY = false;
    [SerializeField] private float pitchMin = -30f;
    [SerializeField] private float pitchMax = 60f;
    private bool IsCameraInputBlocked => PlayerInput.GameplayInputBlocked;
    private Player _targetPlayer;
    private float _yaw;
    private float _pitch;

    private bool UseMobileInput
    {
        get
        {
            if (MobileInputState.Instance != null)
                return MobileInputState.Instance.UseMobileInput;

            return Application.isMobilePlatform;
        }
    }

    public void SetTarget(Player player)
    {
        _targetPlayer = player;

        if (_targetPlayer != null)
        {
            Vector3 euler = transform.eulerAngles;
            _yaw = euler.y;
            _pitch = NormalizePitch(euler.x);
        }
    }

    public Vector3 GetPlanarForward()
    {
        Quaternion yawRotation = Quaternion.Euler(0f, _yaw, 0f);
        return yawRotation * Vector3.forward;
    }

    public Vector3 GetPlanarRight()
    {
        Quaternion yawRotation = Quaternion.Euler(0f, _yaw, 0f);
        return yawRotation * Vector3.right;
    }

    private void LateUpdate()
    {
        if (_targetPlayer == null)
            return;

        ReadLookAndZoom();

        Vector3 pivot = _targetPlayer.transform.position + pivotOffset;
        Quaternion orbitRotation = Quaternion.Euler(_pitch, _yaw, 0f);

        Vector3 desiredPosition = pivot - orbitRotation * Vector3.forward * distance;

        transform.position = Vector3.Lerp(
            transform.position,
            desiredPosition,
            1f - Mathf.Exp(-positionSmooth * Time.deltaTime)
        );

        Quaternion desiredRotation = Quaternion.LookRotation(pivot - transform.position, Vector3.up);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            desiredRotation,
            1f - Mathf.Exp(-rotationSmooth * Time.deltaTime)
        );
    }

    private void ReadLookAndZoom()
    {
        if (IsCameraInputBlocked)
            return;
        if (UseMobileInput)
            ReadMobileLookAndZoom();
        else
            ReadDesktopLookAndZoom();
    }

    private void ReadDesktopLookAndZoom()
    {
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            Mouse mouse = Mouse.current;
            if (mouse != null)
            {
                ApplyLookDelta(mouse.delta.ReadValue());
            }
        }

        Mouse wheelMouse = Mouse.current;
        if (wheelMouse != null)
        {
            float scroll = wheelMouse.scroll.ReadValue().y;
            ApplyZoomDelta(-scroll * zoomSpeed);
        }
    }

    private void ReadMobileLookAndZoom()
    {
        MobileInputState mobile = MobileInputState.Instance;
        if (mobile == null)
            return;

        Vector2 lookDelta = mobile.ConsumeLookDelta();
        if (lookDelta.sqrMagnitude > 0.0001f)
            ApplyLookDelta(lookDelta);

        float zoomDelta = mobile.ConsumeZoomDelta();
        if (Mathf.Abs(zoomDelta) > 0.0001f)
            ApplyZoomDelta(zoomDelta);
    }

    private void ApplyLookDelta(Vector2 delta)
    {
        float yawDelta = delta.x * (lookSensitivity / 60f);
        float pitchDelta = (invertY ? delta.y : -delta.y) * (lookSensitivity / 60f);

        _yaw += yawDelta;
        _pitch += pitchDelta;
        _pitch = Mathf.Clamp(_pitch, pitchMin, pitchMax);
    }

    private void ApplyZoomDelta(float delta)
    {
        distance = Mathf.Clamp(distance + delta, minDistance, maxDistance);
    }

    private float NormalizePitch(float angle)
    {
        if (angle > 180f)
            angle -= 360f;

        return angle;
    }
}