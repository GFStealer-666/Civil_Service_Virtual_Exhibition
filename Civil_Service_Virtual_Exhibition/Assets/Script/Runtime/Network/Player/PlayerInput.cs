using UnityEngine;
using UnityEngine.InputSystem;
using Fusion;
using Fusion.Addons.SimpleKCC;

public enum InputButton
{
    Jump,
    Interact,
    Sprint
}

public struct NetworkedInput : INetworkInput
{
    public Vector3 WorldMoveDirection;
    public Vector2 LookRotationDelta;
    public NetworkButtons Buttons;
}

[DefaultExecutionOrder(-10)]
public class PlayerInput : NetworkBehaviour, IBeforeUpdate
{
    [SerializeField] private float lookSensitivity = 1.5f;
    [SerializeField] private bool invertY = false;

    private NetworkedInput _accumulatedInput;
    private readonly Vector2Accumulator _lookRotationAccumulator = new Vector2Accumulator(0.02f, true);

    public override void Spawned()
    {
        if (!HasInputAuthority)
        {
            enabled = false;
            return;
        }

        var networkEvents = Runner.GetComponent<NetworkEvents>();
        if (networkEvents != null)
            networkEvents.OnInput.AddListener(OnInput);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (runner == null) return;

        var networkEvents = runner.GetComponent<NetworkEvents>();
        if (networkEvents != null)
            networkEvents.OnInput.RemoveListener(OnInput);
    }

    void IBeforeUpdate.BeforeUpdate()
    {
        if (!HasInputAuthority)
            return;

        var keyboard = Keyboard.current;
        var mouse = Mouse.current;

        if (keyboard != null && (keyboard.escapeKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame))
        {
            bool locked = Cursor.lockState == CursorLockMode.Locked;
            Cursor.lockState = locked ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = locked;
        }

        if (Cursor.lockState != CursorLockMode.Locked)
            return;

        if (mouse != null)
        {
            Vector2 mouseDelta = mouse.delta.ReadValue();
            float pitch = invertY ? mouseDelta.y : -mouseDelta.y;
            float yaw = mouseDelta.x;

            Vector2 lookRotationDelta = new Vector2(pitch, yaw) * (lookSensitivity / 60f);
            _lookRotationAccumulator.Accumulate(lookRotationDelta);
        }

        Vector2 rawMove = Vector2.zero;
        if (keyboard != null)
        {
            if (keyboard.wKey.isPressed) rawMove += Vector2.up;
            if (keyboard.sKey.isPressed) rawMove += Vector2.down;
            if (keyboard.aKey.isPressed) rawMove += Vector2.left;
            if (keyboard.dKey.isPressed) rawMove += Vector2.right;

            _accumulatedInput.Buttons.Set(InputButton.Jump, keyboard.spaceKey.isPressed);
            _accumulatedInput.Buttons.Set(InputButton.Interact, keyboard.eKey.isPressed);
            _accumulatedInput.Buttons.Set(InputButton.Sprint,   keyboard.leftShiftKey.isPressed);
        }

        rawMove = rawMove.normalized;

        Vector3 worldMove = Vector3.zero;

        if (Camera.main != null)
        {
            var follow = Camera.main.GetComponent<ThirdPersonCameraFollow>();
            if (follow != null)
            {
                Vector3 forward = follow.GetPlanarForward();
                Vector3 right = follow.GetPlanarRight();
                worldMove = (forward * rawMove.y + right * rawMove.x).normalized;
            }
        }

        _accumulatedInput.WorldMoveDirection = worldMove;
    }

    private void OnInput(NetworkRunner runner, NetworkInput input)
    {
        _accumulatedInput.LookRotationDelta = _lookRotationAccumulator.ConsumeTickAligned(runner);
        input.Set(_accumulatedInput);
    }
}