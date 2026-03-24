using UnityEngine;

public class InteractionService : MonoBehaviour
{
    public WorldInteractable CurrentInteractable { get; private set; }
    public GameObject CurrentInteractor { get; private set; }

    [SerializeField] private float interactDistance = 3f;

    private void Update()
    {
        GameObject localPlayer = LocalPlayerResolver.GetLocalPlayerByTag();

        if (localPlayer == null)
        {
            CurrentInteractable = null;
            CurrentInteractor = null;
            return;
        }

        WorldInteractable nearest = FindNearestInteractable(localPlayer.transform.position);

        CurrentInteractable = nearest;
        CurrentInteractor = nearest != null ? localPlayer : null;
    }

    private WorldInteractable FindNearestInteractable(Vector3 fromPosition)
    {
        WorldInteractable[] interactables = FindObjectsByType<WorldInteractable>(FindObjectsSortMode.None);

        WorldInteractable nearest = null;
        float bestSqrDistance = interactDistance * interactDistance;

        for (int i = 0; i < interactables.Length; i++)
        {
            if (interactables[i] == null)
                continue;

            float sqrDistance = (interactables[i].transform.position - fromPosition).sqrMagnitude;
            if (sqrDistance > bestSqrDistance)
                continue;

            bestSqrDistance = sqrDistance;
            nearest = interactables[i];
        }

        return nearest;
    }

    public void SetCurrent(WorldInteractable interactable, GameObject interactor)
    {
        CurrentInteractable = interactable;
        CurrentInteractor = interactor;
    }

    public void ClearCurrent(WorldInteractable interactable, GameObject interactor)
    {
        if (CurrentInteractable != interactable)
            return;

        if (CurrentInteractor != interactor)
            return;

        CurrentInteractable = null;
        CurrentInteractor = null;
    }
}