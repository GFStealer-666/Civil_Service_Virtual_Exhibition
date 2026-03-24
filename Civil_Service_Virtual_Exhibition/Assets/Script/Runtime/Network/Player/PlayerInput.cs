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
    [Header("Desktop Look")]
    [SerializeField] private float lookSensitivity = 1.5f;
    [SerializeField] private bool invertY = false;

    public static bool GameplayInputBlocked;
    public static bool IsCursorUnlocked => Cursor.lockState != CursorLockMode.Locked;
    private static int _uiBlockCount;
    private NetworkedInput _accumulatedInput;
    private readonly Vector2Accumulator _lookRotationAccumulator = new Vector2Accumulator(0.02f, true);
    public override void Spawned()
    {
        if (!HasInputAuthority)
        {
            enabled = false;
            return;
        }

        var networkObject = GetComponentInParent<NetworkObject>();
        if (networkObject == null)
        {
            Debug.LogError("[PlayerInput] No NetworkObject found in parent.");
            return;
        }

        var networkEvents = networkObject.Runner.GetComponent<NetworkEvents>();
        if (networkEvents == null)
        {
            Debug.LogError("[PlayerInput] NetworkEvents missing on Runner.");
            return;
        }

        networkEvents.OnInput.AddListener(OnInput);

        if (InputModeResolver.UseMobileInput())
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        Debug.Log("[PlayerInput] Input registered.");
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (runner == null)
            return;

        var networkEvents = runner.GetComponent<NetworkEvents>();
        if (networkEvents != null)
            networkEvents.OnInput.RemoveListener(OnInput);
        if (HasInputAuthority)
        {
            _uiBlockCount = 0;
            GameplayInputBlocked = false;
            RefreshCursorState();
        }
    }
    public static void PushUIBlock()
    {
        Debug.Log("UI BLOCK");
        _uiBlockCount++;
        GameplayInputBlocked = _uiBlockCount > 0;
        RefreshCursorState();
    }

    public static void PopUIBlock()
    {
        _uiBlockCount = Mathf.Max(0, _uiBlockCount - 1);
        GameplayInputBlocked = _uiBlockCount > 0;
        RefreshCursorState();
    }

    public static void SetUIBlock(bool blocked)
    {
        _uiBlockCount = blocked ? 1 : 0;
        GameplayInputBlocked = blocked;
        RefreshCursorState();
    }
    private static void RefreshCursorState()
    {
        if (InputModeResolver.UseMobileInput())
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        bool unlockCursor = GameplayInputBlocked;

        Cursor.lockState = unlockCursor ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = unlockCursor;
    }
    void IBeforeUpdate.BeforeUpdate()
    {
        if (!HasInputAuthority)
            return;

        if (InputModeResolver.UseMobileInput())
        {
            ReadMobileInput();
        }
        else
        {
            ReadDesktopInput();
}
    }

    private void ReadDesktopInput()
    {
        var keyboard = Keyboard.current;
        var mouse = Mouse.current;

        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame && !GameplayInputBlocked)
        {
            bool locked = Cursor.lockState == CursorLockMode.Locked;
            Cursor.lockState = locked ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = locked;
        }

        if (GameplayInputBlocked)
        {
            _accumulatedInput.WorldMoveDirection = Vector3.zero;
            _accumulatedInput.LookRotationDelta = Vector2.zero;
            _accumulatedInput.Buttons = default;
            return;
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
            _accumulatedInput.Buttons.Set(InputButton.Interact, keyboard.fKey.isPressed);;
            _accumulatedInput.Buttons.Set(InputButton.Sprint, keyboard.leftShiftKey.isPressed);
        }

        _accumulatedInput.WorldMoveDirection = BuildWorldMove(rawMove.normalized);
    }

    private void ReadMobileInput()
    {
        if (GameplayInputBlocked)
        {
            _accumulatedInput.WorldMoveDirection = Vector3.zero;
            _accumulatedInput.LookRotationDelta = Vector2.zero;
            _accumulatedInput.Buttons = default;
            return;
        }

        MobileInputState mobile = MobileInputState.Instance;

        if (mobile == null)
        {
            _accumulatedInput.WorldMoveDirection = Vector3.zero;
            _accumulatedInput.Buttons.Set(InputButton.Jump, false);
            _accumulatedInput.Buttons.Set(InputButton.Interact, false);
            _accumulatedInput.Buttons.Set(InputButton.Sprint, false);
            return;
        }

        Vector2 rawMove = mobile.Move;

        _accumulatedInput.Buttons.Set(InputButton.Jump, false);
        _accumulatedInput.Buttons.Set(InputButton.Interact, mobile.ConsumeInteractPressed());
        _accumulatedInput.Buttons.Set(InputButton.Sprint, mobile.SprintHeld);

        _accumulatedInput.WorldMoveDirection = BuildWorldMove(rawMove);
    }

    private Vector3 BuildWorldMove(Vector2 rawMove)
    {
        if (Camera.main == null)
            return Vector3.zero;

        var follow = Camera.main.GetComponent<ThirdPersonCameraFollow>();
        if (follow == null)
            return Vector3.zero;

        Vector3 forward = follow.GetPlanarForward();
        Vector3 right = follow.GetPlanarRight();

        return (forward * rawMove.y + right * rawMove.x).normalized;
    }

    private void OnInput(NetworkRunner runner, NetworkInput input)
    {
        _accumulatedInput.LookRotationDelta = _lookRotationAccumulator.ConsumeTickAligned(runner);
        input.Set(_accumulatedInput);
    }
}