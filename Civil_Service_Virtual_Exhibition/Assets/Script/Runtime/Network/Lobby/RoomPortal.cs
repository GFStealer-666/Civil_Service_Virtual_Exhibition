using System.Threading.Tasks;
using UnityEngine;

public class RoomPortal : MonoBehaviour
{
    [Header("Destination")]
    [SerializeField] private string destinationScene;

    [Header("UI")]
    [SerializeField] private GameObject promptUI;
    [SerializeField] private GameObject loadingSceneUI;
    [SerializeField] private GameObject allRoomsFullUI;   // shown only when all 10 overflow slots full (very rare)

    [Header("Detection")]
    [SerializeField] private Vector3   detectionSize = Vector3.one;
    [SerializeField] private LayerMask playerLayerMask;

    private bool _playerInRange  = false;
    private bool _travelStarted  = false;

    private void OnEnable()
    {
        if (NetworkLauncher.Instance == null) return;
        NetworkLauncher.Instance.OnSessionFailed += HandleAllRoomsFull;
    }

    private void OnDisable()
    {
        if (NetworkLauncher.Instance == null) return;
        NetworkLauncher.Instance.OnSessionFailed -= HandleAllRoomsFull;
    }

    private void Update()
    {
        if (_travelStarted) return;
        DetectPlayer();
        if (_playerInRange) BeginTravel();
    }

    private void DetectPlayer()
    {
        Collider[] hits = Physics.OverlapBox(
            transform.position,
            detectionSize * 0.5f,
            transform.rotation,
            playerLayerMask);

        bool found = false;
        foreach (var hit in hits)
        {
            if (hit.transform.root.CompareTag("LocalPlayer"))
            {
                found = true;
                break;
            }
        }

        if (found && !_playerInRange)
        {
            _playerInRange = true;
            if (promptUI != null) promptUI.SetActive(true);
            Debug.Log($"[RoomPortal] {name}: player in range");
        }
        else if (!found && _playerInRange)
        {
            _playerInRange = false;
            if (promptUI != null) promptUI.SetActive(false);
        }
    }
    private async void BeginTravel()
    {
        if (string.IsNullOrEmpty(destinationScene))
        {
            Debug.LogWarning($"[RoomPortal] {name}: destinationScene not set.");
            return;
        }

        _travelStarted = true;
        if (promptUI       != null) promptUI.SetActive(false);
        if (loadingSceneUI != null) loadingSceneUI.SetActive(true);

        // Portal hands off to NetworkLauncher — no capacity knowledge here
        await NetworkLauncher.Instance.StartSession(destinationScene);

        // If IsSessionRunning is false, OnSessionFailed already fired
        if (!NetworkLauncher.Instance.IsSessionRunning)
        {
            if (loadingSceneUI != null) loadingSceneUI.SetActive(false);
        }
    }
    // Worst case fallback
    private void HandleAllRoomsFull()
    {
        if (loadingSceneUI != null) loadingSceneUI.SetActive(false);
        if (allRoomsFullUI != null) allRoomsFullUI.SetActive(true);

        Invoke(nameof(ResetPortal), 3f);
    }
    private void ResetPortal()
    {
        if (allRoomsFullUI != null) allRoomsFullUI.SetActive(false);
        _travelStarted = false;
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color  = Color.cyan;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(Vector3.zero, detectionSize);
    }
}