using System.Collections.Generic;
using UnityEngine;

public class InteractionService : MonoBehaviour
{
    [SerializeField] private float interactDistance = 3f;

    public WorldInteractable CurrentInteractable { get; private set; }
    public GameObject CurrentInteractor { get; private set; }

    private readonly HashSet<WorldInteractable> _nearbyInteractables = new();
    private Player _localPlayer;

    private void Update()
    {
        if (_localPlayer == null || !LocalPlayerResolver.IsOwnedByLocalClient(_localPlayer))
            _localPlayer = LocalPlayerResolver.GetLocalPlayer();

        if (_localPlayer == null)
        {
            CurrentInteractable = null;
            CurrentInteractor = null;
            return;
        }

        CurrentInteractor = _localPlayer.gameObject;
        CurrentInteractable = FindBestInteractable(_localPlayer.transform.position, CurrentInteractor);

        if (CurrentInteractable == null)
            CurrentInteractor = null;
    }

    private WorldInteractable FindBestInteractable(Vector3 fromPosition, GameObject interactor)
    {
        WorldInteractable nearest = null;
        float bestSqrDistance = interactDistance * interactDistance;

        _nearbyInteractables.RemoveWhere(x => x == null);

        foreach (WorldInteractable interactable in _nearbyInteractables)
        {
            if (interactable == null)
                continue;

            if (!interactable.CanInteract(interactor))
                continue;

            float sqrDistance = (interactable.transform.position - fromPosition).sqrMagnitude;
            if (sqrDistance > bestSqrDistance)
                continue;

            bestSqrDistance = sqrDistance;
            nearest = interactable;
        }

        return nearest;
    }

    public void RegisterNearby(WorldInteractable interactable, Collider other)
    {
        if (interactable == null)
            return;

        if (!LocalPlayerResolver.IsLocalPlayerCollider(other))
            return;

        _nearbyInteractables.Add(interactable);
    }

    public void UnregisterNearby(WorldInteractable interactable, Collider other)
    {
        if (interactable == null)
            return;

        if (!LocalPlayerResolver.IsLocalPlayerCollider(other))
            return;

        _nearbyInteractables.Remove(interactable);

        if (CurrentInteractable == interactable)
        {
            CurrentInteractable = null;
            CurrentInteractor = null;
        }
    }

    public bool IsCurrentValid()
    {
        if (CurrentInteractable == null || CurrentInteractor == null)
            return false;

        if (!LocalPlayerResolver.IsLocalPlayer(CurrentInteractor))
            return false;

        if (!_nearbyInteractables.Contains(CurrentInteractable))
            return false;

        float sqrDistance =
            (CurrentInteractable.transform.position - CurrentInteractor.transform.position).sqrMagnitude;

        if (sqrDistance > interactDistance * interactDistance)
            return false;

        return CurrentInteractable.CanInteract(CurrentInteractor);
    }
}