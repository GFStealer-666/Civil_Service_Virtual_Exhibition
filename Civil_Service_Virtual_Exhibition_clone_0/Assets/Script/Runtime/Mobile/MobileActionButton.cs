using UnityEngine;
using UnityEngine.EventSystems;

public class MobileActionButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public void OnPointerDown(PointerEventData eventData)
    {
        MobileInputState.Instance?.SetSprintHeld(true);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        MobileInputState.Instance?.SetSprintHeld(false);
    }

    private void OnDisable()
    {
        MobileInputState.Instance?.SetSprintHeld(false);
    }
}