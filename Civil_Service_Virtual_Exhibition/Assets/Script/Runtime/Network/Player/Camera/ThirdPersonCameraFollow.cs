using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonCameraFollow : MonoBehaviour
{
    [SerializeField] private Vector3 pivotOffset = new Vector3(0f, 1.6f, 0f);
    [SerializeField] private float distance = 3.5f;
    [SerializeField] private float positionSmooth = 12f;
    [SerializeField] private float rotationSmooth = 18f;
    [SerializeField] private float lookSensitivity = 1.5f;
    [SerializeField] private bool invertY = false;
    [SerializeField] private float pitchMin = -30f;
    [SerializeField] private float pitchMax = 60f;

    private Player _targetPlayer;
    private float _yaw;
    private float _pitch;

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

        ReadMouseLook();

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

    private void ReadMouseLook()
    {
        if (Cursor.lockState != CursorLockMode.Locked)
            return;

        Mouse mouse = Mouse.current;
        if (mouse == null)
            return;

        Vector2 mouseDelta = mouse.delta.ReadValue();
        float yawDelta = mouseDelta.x * (lookSensitivity / 60f);
        float pitchDelta = (invertY ? mouseDelta.y : -mouseDelta.y) * (lookSensitivity / 60f);

        _yaw += yawDelta;
        _pitch += pitchDelta;
        _pitch = Mathf.Clamp(_pitch, pitchMin, pitchMax);
    }

    private float NormalizePitch(float angle)
    {
        if (angle > 180f)
            angle -= 360f;
        return angle;
    }
}