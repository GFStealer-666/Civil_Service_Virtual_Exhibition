using UnityEngine;

public class PortalPromptPresenter : MonoBehaviour
{
    [SerializeField] private PortalInteractionService interactionService;
    [SerializeField] private PortalPromptView promptView;
    [SerializeField] private string desktopPrompt = "Press F";
    [SerializeField] private string mobilePrompt = "Tap to Enter";
    [SerializeField] private bool useMobilePrompt;

    private void Update()
    {
        if (interactionService == null || promptView == null)
            return;

        RoomPortal portal = interactionService.CurrentPortal;

        if (portal == null)
        {
            promptView.Hide();
            return;
        }

        string prompt = useMobilePrompt ? mobilePrompt : desktopPrompt;
        promptView.Show(prompt);
    }
}