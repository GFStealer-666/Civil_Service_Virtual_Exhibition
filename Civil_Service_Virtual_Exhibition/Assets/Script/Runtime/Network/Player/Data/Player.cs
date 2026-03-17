using UnityEngine;
using Fusion;
using Fusion.Addons.SimpleKCC;

[DefaultExecutionOrder(-5)]
public class Player : NetworkBehaviour
{
    [Networked] public string CurrentRoom { get; set; }
    [Header("Components")]
    [SerializeField] private SimpleKCC kcc;
    [SerializeField] private Animator animator;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float sprintSpeed = 7f;
    [SerializeField] private float jumpForce = 6f;
    [SerializeField] private float upGravity = 15f;
    [SerializeField] private float downGravity = 30f;
    [SerializeField] private float groundAcceleration = 30f;
    [SerializeField] private float groundDeceleration = 25f;
    [SerializeField] private float airAcceleration = 15f;
    [SerializeField] private float airDeceleration = 1.3f;
    [SerializeField] private float rotationSharpness = 12f;
    [Header("Settings")]
    [SerializeField] private bool allowJump = true;
    [Networked] private NetworkButtons PreviousButtons { get; set; }
    [Networked] private int JumpCount { get; set; }
    [Networked] private Vector3 MoveVelocity { get; set; }
    [Networked] private NetworkBool IsGroundedNetworked { get; set; }
    [Networked] private float SpeedNetworked { get; set; }
    [Networked] private float MotionSpeedNetworked { get; set; }

    private int _visibleJumpCount;
    private bool _hasAnimator;
    private float _bodyYawVelocity;

    private int _animIDSpeed;
    private int _animIDGrounded;
    private int _animIDJump;
    private int _animIDFreeFall;
    private int _animIDMotionSpeed;

    public override void Spawned()
    {
        name         = BuildPlayerName();
        _hasAnimator = animator != null;
        AssignAnimationIDs();

        if (HasInputAuthority)
        {
            gameObject.tag = "LocalPlayer";
            GetComponent<PlayerProfile>()?.ApplyLocalProfile();

            var ui      = FindFirstObjectByType<AppearanceCustomizeUI>();
            var profile = GetComponent<PlayerProfile>();
            var mapping = GetComponent<AppearanceSlotMapping>();
            var appearance = GetComponent<PlayerAppearance>();

            if (ui != null && profile != null && mapping != null)
            {
                ui.Initialize(profile, mapping, appearance);
            }
            else 
                Debug.Log("[Player] UI Components is null");
        }
    }
    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetworkedInput input))
        {
            ProcessInput(input);
        }
        else
        {
            UpdateGravity();
            MotionSpeedNetworked = 0f;
            MovePlayer(Vector3.zero, 0f);
        }
    }

    public override void Render()
    {
        if (!_hasAnimator) return;

        bool jumpStartedThisFrame = _visibleJumpCount < JumpCount;

        if (HasStateAuthority)
        {
            animator.SetBool(_animIDGrounded, kcc.IsGrounded);
            animator.SetBool(_animIDFreeFall, !kcc.IsGrounded);
            animator.SetFloat(_animIDSpeed, kcc.RealSpeed);
            animator.SetFloat(_animIDMotionSpeed, MoveVelocity.magnitude > 0.01f ? 1f : 0f);
        }
        else
        {
            // Use interpolated speed for smoother animation on remote players
            animator.SetBool(_animIDGrounded, IsGroundedNetworked);
            animator.SetBool(_animIDFreeFall, !IsGroundedNetworked);
            animator.SetFloat(_animIDSpeed,        SpeedNetworked,       0.15f, Time.deltaTime);
            animator.SetFloat(_animIDMotionSpeed,  MotionSpeedNetworked, 0.15f, Time.deltaTime);
        }

        if (jumpStartedThisFrame)
            animator.SetBool(_animIDJump, true);

        if (!IsGroundedNetworked)
            animator.SetBool(_animIDJump, false);

        _visibleJumpCount = JumpCount;
    }

    private void ProcessInput(NetworkedInput input)
    {
        Vector3 moveDirection = input.WorldMoveDirection;
        MotionSpeedNetworked = moveDirection.magnitude;

        UpdateGravity();
        RotateBodyToward(moveDirection);

        float jumpImpulse = ConsumeJumpInput(input);

        bool isSprinting = input.Buttons.IsSet(InputButton.Sprint);
        float currentSpeed = isSprinting ? sprintSpeed : moveSpeed;

        MovePlayer(moveDirection * currentSpeed, jumpImpulse);

        if (kcc.HasJumped)
            JumpCount++;

        PreviousButtons = input.Buttons;
    }
    private void RotateBodyToward(Vector3 moveDirection)
    {
        if (moveDirection.sqrMagnitude <= 0.0001f)
            return;

        float targetYaw = Mathf.Atan2(moveDirection.x, moveDirection.z) * Mathf.Rad2Deg;
        float currentYaw = kcc.TransformRotation.eulerAngles.y;

        float smoothedYaw = Mathf.SmoothDampAngle(
            currentYaw,
            targetYaw,
            ref _bodyYawVelocity,
            1f / rotationSharpness,
            Mathf.Infinity,
            Runner.DeltaTime
        );

        float yawDelta = Mathf.DeltaAngle(currentYaw, smoothedYaw);
        kcc.AddLookRotation(new Vector2(0f, yawDelta));
    }

    private float ConsumeJumpInput(NetworkedInput input)
    {
        if (!allowJump) return 0f;

        bool jumpPressed = input.Buttons.WasPressed(PreviousButtons, InputButton.Jump);
        return (jumpPressed && kcc.IsGrounded) ? jumpForce : 0f;
    }

    private void UpdateGravity()
    {
        kcc.SetGravity(kcc.RealVelocity.y >= 0f ? -upGravity : -downGravity);
    }

    private void MovePlayer(Vector3 desiredVelocity, float jumpImpulse)
    {
        float acceleration = CalculateAcceleration(desiredVelocity);

        MoveVelocity = Vector3.Lerp(MoveVelocity, desiredVelocity, acceleration * Runner.DeltaTime);
        kcc.Move(MoveVelocity, jumpImpulse);

        IsGroundedNetworked = kcc.IsGrounded;
        SpeedNetworked = MoveVelocity.magnitude;

        //Debug.Log($"[{name}] SpeedNetworked={SpeedNetworked}, Grounded={IsGroundedNetworked}, JumpCount={JumpCount}");
    }

    private float CalculateAcceleration(Vector3 desiredVelocity)
    {
        if (desiredVelocity == Vector3.zero)
            return kcc.IsGrounded ? groundDeceleration : airDeceleration;

        return kcc.IsGrounded ? groundAcceleration : airAcceleration;
    }

    private void AssignAnimationIDs()
    {
        _animIDSpeed = Animator.StringToHash("Speed");
        _animIDGrounded = Animator.StringToHash("Grounded");
        _animIDJump = Animator.StringToHash("Jump");
        _animIDFreeFall = Animator.StringToHash("FreeFall");
        _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
    }

    private void HandleJumpAnimation()
    {
        if (!_hasAnimator) return;
        if (_visibleJumpCount >= JumpCount) return;

        animator.SetBool(_animIDJump, true);
    }

    private string BuildPlayerName()
    {
        string role = HasInputAuthority ? "Input Authority"
                    : HasStateAuthority ? "State Authority"
                    : "Proxy";
        return $"{Object.InputAuthority} ({role})";
    }

    private void OnFootstep(AnimationEvent animationEvent)
    {
        if (animationEvent.animatorClipInfo.weight > 0.5f)
        {
        }
    }

    private void OnLand(AnimationEvent animationEvent)
    {
    }
}