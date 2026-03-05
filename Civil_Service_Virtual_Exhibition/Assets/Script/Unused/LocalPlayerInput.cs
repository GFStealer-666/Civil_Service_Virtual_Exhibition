using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class LocalPlayerInput : MonoBehaviour
{
    [Header("Input Values")]
    public Vector2 Move;
    public Vector2 Look;
    public bool Jump;
    public bool Sprint;

    [Header("Mouse Sensitivity")]
    public float MouseSensitivity = 1.0f;

    [Header("Options")]
    public bool CursorLocked = true;
    public bool CursorInputForLook = true;

#if ENABLE_INPUT_SYSTEM
    public void OnMove(InputValue value)
    {
        Move = value.Get<Vector2>();
    }

    public void OnLook(InputValue value)
    {
        if (!CursorInputForLook) return;
        Look += value.Get<Vector2>() * MouseSensitivity;
    }

    public void OnJump(InputValue value) => Jump = value.isPressed;
    public void OnSprint(InputValue value) => Sprint = value.isPressed;
#endif

    // Called by FusionInputProvider after it reads input each tick
    public void ConsumeOneShotInputs()
    {
        Jump = false;
        Look = Vector2.zero; // Clear accumulated look after Fusion reads it
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        SetCursorState(CursorLocked);
    }

    private void SetCursorState(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
    }
}