using System.Threading.Tasks;
using UnityEngine;

public class ExhibitionInfoInteractable : WorldInteractable
{
    [Header("Info")]
    [SerializeField] private string exhibitTitle;
    [TextArea(5, 20)]
    [SerializeField] private string exhibitDescription;

    [Header("UI")]
    [SerializeField] private ExhibitionInfoPanelController panelController;

    public override bool CanInteract(GameObject interactor)
    {
        return panelController != null && !panelController.IsOpen;
    }

    public override Task InteractAsync(GameObject interactor)
    {
        panelController.Open(exhibitTitle, exhibitDescription);
        return Task.CompletedTask;
    }
}