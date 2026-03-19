using System.Collections.Generic;
using UnityEngine;

public class InteractionService : MonoBehaviour
{
    public WorldInteractable CurrentInteractable { get; private set; }
    public GameObject CurrentInteractor { get; private set; }

    private readonly List<WorldInteractable> _targets = new();

    public void EnterRange(WorldInteractable interactable, GameObject interactor)
    {
        if (interactable == null || interactor == null)
            return;

        if (!_targets.Contains(interactable))
            _targets.Add(interactable);

        CurrentInteractor = interactor;
        RefreshCurrent();
    }

    public void ExitRange(WorldInteractable interactable, GameObject interactor)
    {
        if (interactable != null)
            _targets.Remove(interactable);

        if (CurrentInteractor == interactor && _targets.Count == 0)
            CurrentInteractor = null;

        RefreshCurrent();
    }

    private void Update()
    {
        RefreshCurrent();
    }

    private void RefreshCurrent()
    {
        _targets.RemoveAll(target => target == null);

        if (CurrentInteractor == null)
        {
            CurrentInteractable = null;
            return;
        }

        WorldInteractable best = null;
        float bestDistance = float.MaxValue;
        Vector3 interactorPosition = CurrentInteractor.transform.position;

        for (int i = 0; i < _targets.Count; i++)
        {
            WorldInteractable target = _targets[i];
            if (target == null || !target.isActiveAndEnabled)
                continue;

            if (!target.CanInteract(CurrentInteractor))
                continue;

            float distance = (target.transform.position - interactorPosition).sqrMagnitude;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = target;
            }
        }

        CurrentInteractable = best;
    }
}