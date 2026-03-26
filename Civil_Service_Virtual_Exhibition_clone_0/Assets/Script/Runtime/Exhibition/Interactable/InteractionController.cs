using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

public class InteractionController : MonoBehaviour
{
    [SerializeField] private InteractionService interactionService;
    [SerializeField] private Key desktopInteractKey = Key.F;

    private bool _busy;

    private void Update()
    {
        if (_busy || interactionService == null)
            return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (keyboard[desktopInteractKey].wasPressedThisFrame)
            _ = TryInteractAsync();
    }

    public void OnMobileInteractPressed()
    {
        if (_busy || interactionService == null)
            return;

        _ = TryInteractAsync();
    }

    private async Task TryInteractAsync()
    {
        if (!interactionService.IsCurrentValid())
            return;

        WorldInteractable interactable = interactionService.CurrentInteractable;
        GameObject interactor = interactionService.CurrentInteractor;

        _busy = true;

        try
        {
            await interactable.InteractAsync(interactor);
        }
        finally
        {
            _busy = false;
        }
    }
}