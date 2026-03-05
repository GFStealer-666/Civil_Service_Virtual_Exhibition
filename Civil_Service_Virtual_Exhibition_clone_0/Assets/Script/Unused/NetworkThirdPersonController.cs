using Fusion;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(CharacterController))]
public class NetworkThirdPersonController : NetworkBehaviour
{
    [Header("Player")]
    public float MoveSpeed = 2.0f;
    public float SprintSpeed = 5.335f;
    [Range(0.0f, 0.3f)] public float RotationSmoothTime = 0.12f;
    public float SpeedChangeRate = 10.0f;

    [Header("Jump & Gravity")]
    public float JumpHeight = 1.2f;
    public float Gravity = -15.0f;
    public float JumpTimeout = 0.50f;
    public float FallTimeout = 0.15f;
    public float TerminalVelocity = 53.0f;

    [Header("Grounded")]
    public bool Grounded = true;
    public float GroundedOffset = -0.14f;
    public float GroundedRadius = 0.28f;
    public LayerMask GroundLayers;

    [Header("Camera / Cinemachine")]
    public Transform CinemachineCameraTarget;
    public float TopClamp = 70.0f;
    public float BottomClamp = -30.0f;
    public float CameraAngleOverride = 0.0f;
    public bool LockCameraPosition = false;

    // components
    private CharacterController _controller;
    private Animator _animator;
    private Camera _mainCamera;

#if ENABLE_INPUT_SYSTEM
    private LocalPlayerInput _playerInput;
    private FusionInputProvider _fusionInputProvider;
#endif

    // camera state — only touched in Render(), never in FixedUpdateNetwork()
    private float _cinemachineTargetYaw;
    private float _cinemachineTargetPitch;

    // movement state
    private float _speed;
    private float _animationBlend;
    private float _targetRotation;
    private float _rotationVelocity;
    private float _verticalVelocity;

    // timeouts
    private float _jumpTimeoutDelta;
    private float _fallTimeoutDelta;

    // animator IDs
    private int _animIDSpeed;
    private int _animIDGrounded;
    private int _animIDJump;
    private int _animIDFreeFall;
    private int _animIDMotionSpeed;

    private float _visualRotationY;
    private float _visualRotationVelocity;
    private bool _hasAnimator;
    private const float _threshold = 0.01f;

    // Look is read ONLY in Render() — never cached across ticks
    private Vector2 _renderLook;

    public override void Spawned()
    {
        _controller = GetComponent<CharacterController>();
        _hasAnimator = TryGetComponent(out _animator);

#if ENABLE_INPUT_SYSTEM
        _playerInput = GetComponent<LocalPlayerInput>();
        _fusionInputProvider = FindFirstObjectByType<FusionInputProvider>();

        // Only the local player needs input components active
        if (_playerInput != null)
            _playerInput.enabled = Object.HasInputAuthority;

        if (_fusionInputProvider != null)
            _fusionInputProvider.enabled = Object.HasInputAuthority;

        if (_playerInput != null && _fusionInputProvider != null)
            _fusionInputProvider.SetLocalInputSource(Runner.LocalPlayer, _playerInput);
#endif

        _mainCamera = Camera.main;
        AssignAnimationIDs();

        // Initialise camera yaw from current facing direction
        if (CinemachineCameraTarget != null)
            _cinemachineTargetYaw = transform.rotation.eulerAngles.y;

        _jumpTimeoutDelta = JumpTimeout;
        _fallTimeoutDelta = FallTimeout;
    }

    public override void FixedUpdateNetwork()
    {
        GroundedCheck();

        if (!GetInput(out NetworkInputData input))
        {
            // No input this tick — keep gravity running so we don't float
            ApplyGravityOnly();
            return;
        }

        // CRITICAL: In Shared mode every peer runs FixedUpdateNetwork on all objects.
        // Only the state authority should actually move this character.
        if (!Object.HasStateAuthority) return;

        JumpAndGravity(input);
        Move(input);
    }

    public override void Render()
    {
        if (!Object.HasInputAuthority) return;

        if (_playerInput != null)
            _renderLook = _playerInput.Look;

        CameraRotation();
        _renderLook = Vector2.zero;
        SmoothVisualRotation();
    }

    private void AssignAnimationIDs()
    {
        _animIDSpeed       = Animator.StringToHash("Speed");
        _animIDGrounded    = Animator.StringToHash("Grounded");
        _animIDJump        = Animator.StringToHash("Jump");
        _animIDFreeFall    = Animator.StringToHash("FreeFall");
        _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
    }

    private void GroundedCheck()
    {
        Vector3 spherePosition = new Vector3(
            transform.position.x,
            transform.position.y - GroundedOffset,
            transform.position.z);

        Grounded = Physics.CheckSphere(
            spherePosition, GroundedRadius, GroundLayers,
            QueryTriggerInteraction.Ignore);

        if (_hasAnimator)
            _animator.SetBool(_animIDGrounded, Grounded);
    }

    private void CameraRotation()
    {
        // if (CinemachineCameraTarget == null) return;

        if (_renderLook.sqrMagnitude >= _threshold && !LockCameraPosition)
        {
            float mouseSensitivity = 0.5f;

            _cinemachineTargetYaw   += _renderLook.x * mouseSensitivity;
            _cinemachineTargetPitch += _renderLook.y * mouseSensitivity;
        }

        _cinemachineTargetYaw   = ClampAngle(_cinemachineTargetYaw,   float.MinValue, float.MaxValue);
        _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp,    TopClamp);

        CinemachineCameraTarget.rotation = Quaternion.Euler(
            _cinemachineTargetPitch + CameraAngleOverride,
            _cinemachineTargetYaw,
            0.0f);
    }

    private void Move(NetworkInputData input)
    {
        float dt = Runner.DeltaTime;
        Vector2 moveInput = input.Move;

        float targetSpeed = input.Sprint ? SprintSpeed : MoveSpeed;
        if (moveInput == Vector2.zero) targetSpeed = 0.0f;

        float inputMagnitude = Mathf.Clamp01(moveInput.magnitude);

        _speed = Mathf.Lerp(_speed, targetSpeed * inputMagnitude, dt * SpeedChangeRate);
        if (_speed < 0.01f) _speed = 0f;

        _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, dt * SpeedChangeRate);
        if (_animationBlend < 0.01f) _animationBlend = 0f;

        if (moveInput != Vector2.zero)
        {
            Vector3 inputDir = new Vector3(moveInput.x, 0f, moveInput.y).normalized;
            float camYaw = _cinemachineTargetYaw;

            _targetRotation = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg + camYaw;

            transform.rotation = Quaternion.Euler(0f, _targetRotation, 0f);
        }

        Vector3 targetDirection = Quaternion.Euler(0f, _targetRotation, 0f) * Vector3.forward;

        _controller.Move(
            targetDirection.normalized * (_speed * dt) +
            new Vector3(0f, _verticalVelocity, 0f) * dt);

        if (_hasAnimator)
        {
            _animator.SetFloat(_animIDSpeed, _animationBlend);
            _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
        }
    }

    private void JumpAndGravity(NetworkInputData input)
    {
        float dt = Runner.DeltaTime;

        if (Grounded)
        {
            _fallTimeoutDelta = FallTimeout;

            if (_hasAnimator)
            {
                _animator.SetBool(_animIDJump, false);
                _animator.SetBool(_animIDFreeFall, false);
            }

            if (_verticalVelocity < 0f) _verticalVelocity = -2f;

            if (input.Jump && _jumpTimeoutDelta <= 0f)
            {
                _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
                if (_hasAnimator) _animator.SetBool(_animIDJump, true);
            }

            if (_jumpTimeoutDelta >= 0f) _jumpTimeoutDelta -= dt;
        }
        else
        {
            _jumpTimeoutDelta = JumpTimeout;

            if (_fallTimeoutDelta >= 0f)
                _fallTimeoutDelta -= dt;
            else if (_hasAnimator)
            {
                _animator.SetBool(_animIDFreeFall, true);
            }
        }

        if (_verticalVelocity < TerminalVelocity)
            _verticalVelocity += Gravity * dt;
    }

    private void ApplyGravityOnly()
    {
        float dt = Runner.DeltaTime;

        if (Grounded)
        {
            if (_verticalVelocity < 0f) _verticalVelocity = -2f;
            if (_jumpTimeoutDelta >= 0f) _jumpTimeoutDelta -= dt;
        }
        else
        {
            _jumpTimeoutDelta = JumpTimeout;
        }

        if (_verticalVelocity < TerminalVelocity)
            _verticalVelocity += Gravity * dt;

        _controller.Move(new Vector3(0f, _verticalVelocity, 0f) * dt);
    }

    private static float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360f) angle += 360f;
        if (angle > 360f) angle -= 360f;
        return Mathf.Clamp(angle, min, max);
    }
    private void SmoothVisualRotation()
    {
        if (!Object.HasInputAuthority) return;
        float smoothY = Mathf.SmoothDampAngle(
            transform.eulerAngles.y,
            _targetRotation,
            ref _rotationVelocity,
            RotationSmoothTime);
        transform.rotation = Quaternion.Euler(0f, smoothY, 0f);
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Grounded
            ? new Color(0f, 1f, 0f, 0.35f)
            : new Color(1f, 0f, 0f, 0.35f);

        Gizmos.DrawSphere(
            new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z),
            GroundedRadius);
    }
}