using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

public class RoomPortal : MonoBehaviour
{
    [Header("Room Config")]
    [SerializeField] private RoomDefinition roomDefinition;

    [Header("UI")]
    [SerializeField] private GameObject promptUI;
    [SerializeField] private GameObject loadingSceneUI;
    [SerializeField] private GameObject roomFullUI;

    [Header("Detection")]
    [SerializeField] private Vector3 detectionSize = Vector3.one;
    [SerializeField] private LayerMask playerLayerMask;

    [Header("Behavior")]
    [SerializeField] private bool autoTravelOnEnter = true;
    [SerializeField] private float retryDelay = 0.5f;

    private bool _playerInRange;
    private bool _travelStarted;

    private void Update()
    {
        DetectPlayer();

        if (!_travelStarted && _playerInRange && autoTravelOnEnter)
        {
            _ = BeginTravelAsync();
        }
    }

    private void DetectPlayer()
    {
        Collider[] hits = Physics.OverlapBox(
            transform.position,
            detectionSize * 0.5f,
            transform.rotation,
            playerLayerMask
        );

        bool found = false;

        foreach (var hit in hits)
        {
            if (hit.transform.root.CompareTag("LocalPlayer"))
            {
                found = true;
                break;
            }
        }

        if (found != _playerInRange)
        {
            _playerInRange = found;

            if (promptUI != null)
                promptUI.SetActive(_playerInRange);
        }
    }

    private async Task BeginTravelAsync()
    {
        if (_travelStarted)
            return;

        _travelStarted = true;

        if (loadingSceneUI != null)
            loadingSceneUI.SetActive(true);

        if (roomFullUI != null)
            roomFullUI.SetActive(false);

        bool joined = await NetworkLauncher.Instance.JoinBestRoom(roomDefinition);

        if (!joined)
        {
            if (roomFullUI != null)
                roomFullUI.SetActive(true);
        }

        if (loadingSceneUI != null)
            loadingSceneUI.SetActive(false);

        StartCoroutine(ResetTravelFlag());
    }

    private IEnumerator ResetTravelFlag()
    {
        yield return new WaitForSeconds(retryDelay);
        _travelStarted = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(Vector3.zero, detectionSize);
    }
}