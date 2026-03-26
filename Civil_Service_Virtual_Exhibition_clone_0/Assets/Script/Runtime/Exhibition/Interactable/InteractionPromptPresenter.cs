using UnityEngine;

public class InteractionPromptPresenter : MonoBehaviour
{
    [SerializeField] private InteractionService interactionService;
    [SerializeField] private InteractionPromptView promptView;
    [SerializeField] private bool forceMobilePromptInEditor;

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