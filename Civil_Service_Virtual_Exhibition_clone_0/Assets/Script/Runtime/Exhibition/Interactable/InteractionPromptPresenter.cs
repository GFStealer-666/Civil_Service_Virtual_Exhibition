using UnityEngine;

public class InteractionPromptPresenter : MonoBehaviour
{
    [SerializeField] private InteractionService interactionService;
    [SerializeField] private InteractionPromptView promptView;
    [SerializeField] private bool forceMobilePromptInEditor;

    private bool UseMobilePrompt
    {
        get
        {
            if (forceMobilePromptInEditor)
                return true;

            if (MobileInputState.Instance != null)
                return MobileInputState.Instance.UseMobileInput;

            return Application.isMobilePlatform;
        }
    }

    private void Update()
    {
        if (interactionService == null || promptView == null)
            return;

        WorldInteractable interactable = interactionService.CurrentInteractable;
        if (interactable == null)
        {
            promptView.Hide();
            return;
        }

        promptView.Show(interactable.GetPrompt(UseMobilePrompt), UseMobilePrompt);
    }
}