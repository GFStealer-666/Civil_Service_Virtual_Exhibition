using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RoomPortal : MonoBehaviour
{
    [Header("Destination")]
    [SerializeField] private string destinationScene;

    [Header("UI")]
    [SerializeField] private GameObject promptUI;
    [SerializeField] private GameObject loadingSceneUI;
    [Header("Detection")]
    [SerializeField] private Vector3   detectionSize = Vector3.one;
    [SerializeField] private LayerMask playerLayerMask;

    private bool _playerInRange = false;
    private bool _travelStarted = false;

    private void Update()
    {
        if (_travelStarted) return;

        DetectPlayer();

        if (_playerInRange)
            BeginTravel();
    }

    private void DetectPlayer()
    {
        Collider[] hits = Physics.OverlapBox(
            transform.position, detectionSize * 0.5f,
            transform.rotation, playerLayerMask);

        bool found = false;
        foreach (var hit in hits)
            if (hit.transform.root.CompareTag("LocalPlayer")) { found = true; break; }

        if (found && !_playerInRange)
        {
            Debug.Log($" [RoomPortal] {this.name} detect player");
            _playerInRange = true;
            if (promptUI != null) promptUI.SetActive(true);
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
            Debug.LogWarning("[RoomPortal] destinationScene not set.");
            return;
        }
        loadingSceneUI.SetActive(true);
        _travelStarted = true;
        if (promptUI  != null) promptUI.SetActive(false);

        await NetworkLauncher.Instance.StartSession(destinationScene);

        _travelStarted = false;
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color  = Color.cyan;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(Vector3.zero, detectionSize);
    }
}