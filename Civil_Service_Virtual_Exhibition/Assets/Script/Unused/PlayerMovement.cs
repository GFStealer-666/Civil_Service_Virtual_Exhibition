using Fusion;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : NetworkBehaviour
{
    [Header("Speed")]
    public float MoveSpeed = 2f;
    public float SprintSpeed = 5.335f;
    public float SpeedChangeRate = 10f;

    [Header("Rotation")]
    [Range(0f, 0.3f)]
    public float RotationSmoothTime = 0.12f;

    [Header("Jump & Gravity")]
    public float JumpHeight = 1.2f;      // like StarterAssets
    public float Gravity = -15f;         // like StarterAssets
    public float TerminalVelocity = 53f;
    public float JumpTimeout = 0.25f;    // small timeout feels better online

    private CharacterController _controller;
    private Camera _mainCam;

    // input intent (set in Update, used in FixedUpdateNetwork)
    private Vector2 _moveInput;
    private bool _jumpPressed;
    private bool _sprintHeld;

    // motion state
    private float _speed;
    private float _targetRotation;
    private float _rotationVelocity;
    private float _verticalVelocity;
    private float _jumpTimeoutDelta;

    public override void Spawned()
    {
        _controller = GetComponent<CharacterController>();
        _mainCam = Camera.main; // simplest; you can cache a child camera instead if you use that setup
        _jumpTimeoutDelta = JumpTimeout;
    }

    void Update()
    {
        // Only the local player reads input
        if (!Object.HasInputAuthority) return;

        _moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        _sprintHeld = Input.GetKey(KeyCode.LeftShift);

        if (Input.GetButtonDown("Jump"))
            _jumpPressed = true;
    }

    public override void FixedUpdateNetwork()
    {
        // Only the local player drives the controller in this simple setup
        if (!Object.HasInputAuthority) return;

        GroundedJumpAndGravity();
        MoveWithSmoothing();

        // consume one-shot input
        _jumpPressed = false;
    }

    private void MoveWithSmoothing()
    {
        // target speed (walk/sprint) + stop when no input
        float targetSpeed = _sprintHeld ? SprintSpeed : MoveSpeed;
        if (_moveInput == Vector2.zero) targetSpeed = 0f;

        // current horizontal speed (like StarterAssets)
        float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0f, _controller.velocity.z).magnitude;

        // accel/decel (curved result)
        float speedOffset = 0.1f;
        float inputMagnitude = Mathf.Clamp01(_moveInput.magnitude);

        if (currentHorizontalSpeed < targetSpeed - speedOffset ||
            currentHorizontalSpeed > targetSpeed + speedOffset)
        {
            _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude, Runner.DeltaTime * SpeedChangeRate);
            _speed = Mathf.Round(_speed * 1000f) / 1000f;
        }
        else
        {
            _speed = targetSpeed;
        }

        // camera-relative direction
        Vector3 inputDir = new Vector3(_moveInput.x, 0f, _moveInput.y).normalized;

        if (_moveInput != Vector2.zero)
        {
            float camYaw = (_mainCam != null) ? _mainCam.transform.eulerAngles.y : 0f;

            _targetRotation = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg + camYaw;

            float rotation = Mathf.SmoothDampAngle(
                transform.eulerAngles.y,
                _targetRotation,
                ref _rotationVelocity,
                RotationSmoothTime
            );

            transform.rotation = Quaternion.Euler(0f, rotation, 0f);
        }

        Vector3 moveDir = Quaternion.Euler(0f, _targetRotation, 0f) * Vector3.forward;

        // Move: horizontal + vertical
        Vector3 motion =
            moveDir.normalized * (_speed * Runner.DeltaTime) +
            new Vector3(0f, _verticalVelocity, 0f) * Runner.DeltaTime;

        _controller.Move(moveDir.normalized * (_speed * Runner.DeltaTime)
                 + Vector3.up * (_verticalVelocity * Runner.DeltaTime));
    }

    private void GroundedJumpAndGravity()
    {
        bool grounded = _controller.isGrounded;

        if (grounded)
        {
            // keep grounded (prevents tiny bounce)
            if (_verticalVelocity < 0f)
                _verticalVelocity = -2f;

            // jump timeout
            if (_jumpTimeoutDelta > 0f)
                _jumpTimeoutDelta -= Runner.DeltaTime;

            // Jump
            if (_jumpPressed && _jumpTimeoutDelta <= 0f)
            {
                // v = sqrt(h * -2 * g)
                _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
                _jumpTimeoutDelta = JumpTimeout;
            }
        }
        else
        {
            // reset timeout while airborne
            _jumpTimeoutDelta = JumpTimeout;
        }

        // apply gravity (cap)
        if (_verticalVelocity < TerminalVelocity)
            _verticalVelocity += Gravity * Runner.DeltaTime;
    }
}