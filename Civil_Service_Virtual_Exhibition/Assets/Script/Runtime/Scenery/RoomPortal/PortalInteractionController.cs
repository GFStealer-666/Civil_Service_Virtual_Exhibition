using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

public class PortalInteractionController : MonoBehaviour
{
    [SerializeField] private PortalInteractionService interactionService;

    private bool _busy;

    private void Update()
    {
        if (_busy || interactionService == null)
            return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.fKey.wasPressedThisFrame)
        {
            _ = TryInteractAsync();
        }
    }

    public void OnMobileInteractPressed()
    {
        if (_busy || interactionService == null)
            return;

        _ = TryInteractAsync();
    }

    private async Task TryInteractAsync()
    {
        RoomPortal portal = interactionService.CurrentPortal;
        if (portal == null)
            return;

        _busy = true;
        await portal.TryTravelAsync();
        _busy = false;
    }
}