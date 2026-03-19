using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

public class RoomPortal : WorldInteractable
{
    [Header("Room Config")]
    [SerializeField] private RoomDefinition roomDefinition;

    [Header("UI")]
    [SerializeField] private GameObject loadingSceneUI;
    [SerializeField] private GameObject roomFullUI;

    [Header("Behavior")]
    [SerializeField] private float retryDelay = 0.5f;

    private bool _travelStarted;

    public override bool CanInteract(GameObject interactor)
    {
        return !_travelStarted && roomDefinition != null && NetworkLauncher.Instance != null;
    }

    public override async Task InteractAsync(GameObject interactor)
    {
        if (_travelStarted)
            return;

        _travelStarted = true;

        if (loadingSceneUI != null)
            loadingSceneUI.SetActive(true);

        if (roomFullUI != null)
            roomFullUI.SetActive(false);

        bool joined = false;

        try
        {
            joined = await NetworkLauncher.Instance.JoinBestRoom(roomDefinition);

            if (!joined && roomFullUI != null)
                roomFullUI.SetActive(true);
        }
        finally
        {
            if (loadingSceneUI != null)
                loadingSceneUI.SetActive(false);

            StartCoroutine(ResetTravelFlag());
        }
    }

    private IEnumerator ResetTravelFlag()
    {
        yield return new WaitForSeconds(retryDelay);
        _travelStarted = false;
    }
}