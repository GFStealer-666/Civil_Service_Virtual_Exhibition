using System.Threading.Tasks;
using UnityEngine;

public abstract class WorldInteractable : MonoBehaviour
{
    [Header("Prompt")]
    [SerializeField] private string desktopPrompt = "Press F";
    [SerializeField] private string mobilePrompt = "Tap";
    protected virtual void OnEnable()
    {
        if (InteractionRegistry.Instance != null)
            InteractionRegistry.Instance.Register(this);
    }

    protected virtual void OnDisable()
    {
        if (InteractionRegistry.Instance != null)
            InteractionRegistry.Instance.Unregister(this);
    }
    public string GetPrompt(bool useMobilePrompt)
    {
        return useMobilePrompt ? mobilePrompt : desktopPrompt;
    }

    public virtual bool CanInteract(GameObject interactor)
    {
        return true;
    }

    public abstract Task InteractAsync(GameObject interactor);
}