using UnityEngine;

public class UIPopupCursorBlock : MonoBehaviour
{
    private void OnEnable()
    {
        PlayerInput.PushUIBlock();
    }

    private void OnDisable()
    {
        //PlayerInput.PopUIBlock();
        //_ForceHide();
    }

    public void _ForceHide()
    {
        PlayerInput._ForceHide();
    }
}