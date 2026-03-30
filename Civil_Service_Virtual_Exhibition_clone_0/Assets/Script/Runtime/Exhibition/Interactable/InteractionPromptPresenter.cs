using UnityEngine;
using UnityEngine.UI;

public class InteractionPromptPresenter : MonoBehaviour
{
    [SerializeField] private InteractionService interactionService;
    [SerializeField] private InteractionPromptView promptView;
    [SerializeField] private InteractionController interactionController;
    [SerializeField] private bool forceMobilePromptInEditor;

    private void Awake()
    {
        if (promptView != null && promptView.MobileButton != null && interactionController != null)
        {
            promptView.MobileButton.onClick.RemoveListener(interactionController.OnMobileInteractPressed);
            promptView.MobileButton.onClick.AddListener(interactionController.OnMobileInteractPressed);
        }
    }

    private void OnDestroy()
    {
        if (promptView != null && promptView.MobileButton != null && interactionController != null)
            promptView.MobileButton.onClick.RemoveListener(interactionController.OnMobileInteractPressed);
    }

    private void Update()
    {
        if (interactionService == null || promptView == null)
            return;

        if (!interactionService.IsCurrentValid())
        {
            promptView.Hide();
            return;
        }

        bool useMobilePrompt = InputModeResolver.UseMobileInput(forceMobilePromptInEditor);
        promptView.Show(useMobilePrompt);
    }
}