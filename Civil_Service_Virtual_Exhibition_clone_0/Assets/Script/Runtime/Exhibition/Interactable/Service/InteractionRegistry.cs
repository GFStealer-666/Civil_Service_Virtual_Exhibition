using System.Collections.Generic;
using UnityEngine;

public class InteractionRegistry : MonoBehaviour
{
    public static InteractionRegistry Instance { get; private set; }

    private readonly HashSet<WorldInteractable> _interactables = new();
    [SerializeField] private List<WorldInteractable> _interactables_Debug = new List<WorldInteractable>();
    public IReadOnlyCollection<WorldInteractable> Interactables => _interactables;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void Register(WorldInteractable interactable)
    {
        if (interactable == null)
            return;
        _interactables_Debug.Add(interactable);
        _interactables.Add(interactable);
    }

    public void Unregister(WorldInteractable interactable)
    {
        if (interactable == null)
            return;
        _interactables_Debug.Remove(interactable);
        _interactables.Remove(interactable);
    }
}