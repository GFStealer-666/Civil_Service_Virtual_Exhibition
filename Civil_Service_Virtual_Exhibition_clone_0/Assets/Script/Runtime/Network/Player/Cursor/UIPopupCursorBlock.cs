using UnityEngine;

public class UIPopupCursorBlock : MonoBehaviour
{
    private void OnEnable()
    {
        PlayerInput.PushUIBlock();
    }

    private void OnDisable()
    {
        PlayerInput.PopUIBlock();
    }
}