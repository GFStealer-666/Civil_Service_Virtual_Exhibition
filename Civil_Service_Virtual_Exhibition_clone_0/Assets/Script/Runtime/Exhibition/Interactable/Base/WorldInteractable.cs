using System.Threading.Tasks;
using UnityEngine;

public abstract class WorldInteractable : MonoBehaviour
{
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

    public virtual bool CanInteract(GameObject interactor)
    {
        return true;
    }

    public abstract Task InteractAsync(GameObject interactor);

}