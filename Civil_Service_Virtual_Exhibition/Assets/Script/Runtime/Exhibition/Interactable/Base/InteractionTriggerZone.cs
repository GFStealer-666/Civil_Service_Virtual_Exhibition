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
        Debug.Log($"[InteractionTriggerZone] Enter by {other.name}, layer={LayerMask.LayerToName(other.gameObject.layer)}");
        if (interactionService == null || interactable == null)
            return;
        if (!LocalPlayerResolver.IsLocalPlayerCollider(other))
            return;

        Debug.Log($"[{gameObject.name}] Player get in the zone");
        interactionService.SetCurrent(interactable, other.transform.root.gameObject);
    }

    private void OnTriggerExit(Collider other)
    {
        if (interactionService == null || interactable == null)
            return;

        GameObject root = other.transform.root.gameObject;
        if (!root.CompareTag("LocalPlayer"))
            return;

        interactionService.ClearCurrent(interactable, other.transform.root.gameObject);
    }
}