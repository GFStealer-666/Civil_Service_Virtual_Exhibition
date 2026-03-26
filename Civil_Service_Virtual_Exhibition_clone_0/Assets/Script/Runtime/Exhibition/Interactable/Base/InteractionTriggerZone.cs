using UnityEngine;

[RequireComponent(typeof(Collider))]
public class InteractionTriggerZone : MonoBehaviour
{
    [SerializeField] private InteractionService interactionService;
    [SerializeField] private WorldInteractable interactable;

    private Collider _trigger;

    private void Reset()
    {
        _trigger = GetComponent<Collider>();
        _trigger.isTrigger = true;

        if (interactable == null)
            interactable = GetComponentInParent<WorldInteractable>();
    }

    private void Awake()
    {
        _trigger = GetComponent<Collider>();
        _trigger.isTrigger = true;

        if (interactionService == null)
            interactionService = FindFirstObjectByType<InteractionService>();

        if (interactable == null)
            interactable = GetComponentInParent<WorldInteractable>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (interactionService == null || interactable == null)
            return;

        if (!LocalPlayerResolver.IsLocalPlayerCollider(other))
            return;

        interactionService.RegisterNearby(interactable, other);
    }

    private void OnTriggerExit(Collider other)
    {
        if (interactionService == null || interactable == null)
            return;

        if (!LocalPlayerResolver.IsLocalPlayerCollider(other))
            return;

        interactionService.UnregisterNearby(interactable, other);
    }
}