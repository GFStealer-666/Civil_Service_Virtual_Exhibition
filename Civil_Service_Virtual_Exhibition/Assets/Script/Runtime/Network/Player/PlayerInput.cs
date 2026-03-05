using UnityEngine;
using UnityEngine.InputSystem;
using Fusion;
using Fusion.Addons.SimpleKCC;


public enum InputButton
{
    Jump,
    Interact
}
public struct NetworkedInput : INetworkInput
{
    public Vector2 MoveDirection; 
    public Vector2 LookRotationDelta;
    public NetworkButtons Buttons;
}

[DefaultExecutionOrder(-10)]
public class PlayerInput : NetworkBehaviour, IBeforeUpdate
{
    public static float LookSensitivity = 1.0f;
    public bool InvertY = false;
    
    private NetworkedInput _accumulatedInput;
    private Vector2Accumulator _lookRotationAccumulator = new Vector2Accumulator(0.02f , true);
    public override void Spawned()
    {
        if(HasInputAuthority == false)
        {
            enabled = false;
            return;
        }

        var networkEvents = Runner.GetComponent<NetworkEvents>();
        if(networkEvents != null)
        {
            networkEvents.OnInput.AddListener(OnInput);
        }
        Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if(runner == null ) return;

        var networkEvents = runner.GetComponent<NetworkEvents>();
        if(networkEvents != null)
        {
            networkEvents.OnInput.RemoveListener(OnInput);
        }
    }

    // called before fixedupdatenetwork, so that we can accumulate input and send it to the server
    void IBeforeUpdate.BeforeUpdate()
    {
        // This method is called BEFORE ANY FixedUpdateNetwork() and is used to accumulate input from Keyboard/Mouse.
        // Input accumulation is mandatory - this method is called multiple times before new forward FixedUpdateNetwork() - common if rendering speed is faster than Fusion simulation.

        if (HasInputAuthority == false)
            return;

        // Enter key is used for locking/unlocking cursor in game view.
        var keyboard = Keyboard.current;
        if (keyboard != null && (keyboard.escapeKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame))
        {
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
        if (Cursor.lockState != CursorLockMode.Locked)
            return;

        var mouse = Mouse.current;
        if (mouse != null)
        {
            var mouseDelta = mouse.delta.ReadValue();

            var y = InvertY ? -mouseDelta.y : mouseDelta.y;
            var lookRotationDelta = new Vector2(y, mouseDelta.x);
            lookRotationDelta *= LookSensitivity / 60f;
            _lookRotationAccumulator.Accumulate(lookRotationDelta);
        }

        if (keyboard != null)
        {
            var moveDirection = Vector2.zero;

            if (keyboard.wKey.isPressed) { moveDirection += Vector2.up;    }
            if (keyboard.sKey.isPressed) { moveDirection += Vector2.down;  }
            if (keyboard.aKey.isPressed) { moveDirection += Vector2.left;  }
            if (keyboard.dKey.isPressed) { moveDirection += Vector2.right; }

            _accumulatedInput.MoveDirection = moveDirection.normalized;

            _accumulatedInput.Buttons.Set(InputButton.Jump, keyboard.spaceKey.isPressed);
            _accumulatedInput.Buttons.Set(InputButton.Interact, keyboard.eKey.isPressed);

        }
    }
    
    private void OnInput(NetworkRunner runner, NetworkInput input)
    {
        _accumulatedInput.LookRotationDelta = _lookRotationAccumulator.ConsumeTickAligned(runner);
        input.Set(_accumulatedInput); 
    }

}
