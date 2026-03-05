using UnityEngine;
using Fusion;
using Fusion.Addons.SimpleKCC;
using Cinemachine;

[DefaultExecutionOrder(-5)]
public class Player : NetworkBehaviour
{
    [Header("Components")]
    public SimpleKCC KCC;
    public Animator Animator;

    [Header("Setup")]
    public float MoveSpeed = 6f;
    public float JumpForce = 10f;

    [Header("Third Person Camera")]
    public Transform PlayerCameraRoot;
    public Transform LookAtPoint;

    [Header("Movement")]
    public float UpGravity = 15f;
    public float DownGravity = 25f;
    public float GroundAcceleration = 55f;
    public float GroundDeceleration = 25f;
    public float AirAcceleration = 25f;
    public float AirDeceleration = 1.3f;

    [Header("Camera")]
    public float PitchClamp = 60f;
    public float CameraSmooth = 10f;

    private float _targetPitch;
    private float _currentPitch;

    [Networked] private NetworkButtons _previousButtons { get; set; }
    [Networked] private int _jumpCount { get; set; }
    [Networked] private Vector3 _moveVelocity { get; set; }

    private int _visibleJumpCount;
    private SceneObject _sceneObjects;

    public override void Spawned()
    {
        name = $"{Object.InputAuthority} ({(HasInputAuthority ? "Input Authority" : (HasStateAuthority ? "State Authority" : "Proxy"))})";

        if (!HasInputAuthority)
        {
            var virtualCameras = GetComponentsInChildren<CinemachineVirtualCamera>(true);
            for (int i = 0; i < virtualCameras.Length; i++)
            {
                virtualCameras[i].enabled = false;
            }
        }

        _sceneObjects = Runner.GetSingleton<SceneObject>();
    }

    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetworkedInput input))
        {
            ProcessInput(input);
        }
        else
        {
            MovePlayer();
        }
    }

    public override void Render()
    {
        if (_visibleJumpCount < _jumpCount)
        {
            Animator.SetTrigger("Jump");
        }

        _visibleJumpCount = _jumpCount;

        // Smooth camera update every frame
        if (HasInputAuthority)
        {
            _currentPitch = Mathf.Lerp(_currentPitch, _targetPitch, 1f - Mathf.Exp(-CameraSmooth * Time.deltaTime));
            PlayerCameraRoot.localRotation = Quaternion.Euler(_currentPitch, 0f, 0f);
        }
    }

    private void ProcessInput(NetworkedInput input)
    {
        // Horizontal rotation (player body)
        KCC.AddLookRotation(new Vector2(0f, input.LookRotationDelta.y));

        // Vertical rotation (camera pivot)
        _targetPitch += input.LookRotationDelta.x;
        _targetPitch = Mathf.Clamp(_targetPitch, -PitchClamp, PitchClamp);

        // Gravity
        KCC.SetGravity(KCC.RealVelocity.y >= 0f ? -UpGravity : -DownGravity);

        var inputDirection = KCC.TransformRotation * new Vector3(input.MoveDirection.x, 0f, input.MoveDirection.y);
        var jumpImpulse = 0f;

        if (input.Buttons.WasPressed(_previousButtons, InputButton.Jump) && KCC.IsGrounded)
        {
            jumpImpulse = JumpForce;
        }

        MovePlayer(inputDirection * MoveSpeed, jumpImpulse);

        if (KCC.HasJumped)
        {
            _jumpCount++;
        }

        if (input.Buttons.WasPressed(_previousButtons, InputButton.Interact))
        {
            var cam = Camera.main.transform;

            if (Runner.GetPhysicsScene().Raycast(
                cam.position,
                cam.forward,
                out var hit,
                2.5f,
                LayerMask.GetMask("Default"),
                QueryTriggerInteraction.Ignore))
            {
                
            }
        }

        _previousButtons = input.Buttons;
    }

    private void MovePlayer(Vector3 desiredMoveVelocity = default, float jumpImpulse = default)
    {
        float acceleration;

        if (desiredMoveVelocity == Vector3.zero)
            acceleration = KCC.IsGrounded ? GroundDeceleration : AirDeceleration;
        else
            acceleration = KCC.IsGrounded ? GroundAcceleration : AirAcceleration;

        _moveVelocity = Vector3.Lerp(_moveVelocity, desiredMoveVelocity, acceleration * Runner.DeltaTime);
        KCC.Move(_moveVelocity, jumpImpulse);
    }
}