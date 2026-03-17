using System.Threading.Tasks;
using UnityEngine;

public class RoomPortal : MonoBehaviour
{
    [Header("Room Config")]
    [SerializeField] private RoomDefinition roomDefinition;

    [Header("Detection")]
    [SerializeField] private Vector3 detectionSize = new Vector3(2f, 2f, 2f);
    [SerializeField] private LayerMask playerLayerMask;

    [Header("Travel UI")]
    [SerializeField] private GameObject loadingSceneUI;
    [SerializeField] private GameObject roomFullUI;

    [Header("Behavior")]
    [SerializeField] private float retryDelay = 0.5f;

    private bool _travelStarted;
    private float _nextAllowedTravelTime;

    public RoomDefinition RoomDefinition => roomDefinition;

    public bool IsLocalPlayerInRange()
    {
        Collider[] hits = Physics.OverlapBox(
            transform.position,
            detectionSize * 0.5f,
            transform.rotation,
            playerLayerMask
        );

        foreach (var hit in hits)
        {
            if (hit.transform.root.CompareTag("LocalPlayer"))
                return true;
        }

        return false;
    }

    public async Task<bool> TryTravelAsync()
    {
        if (_travelStarted)
            return false;

        if (Time.time < _nextAllowedTravelTime)
            return false;

        _travelStarted = true;

        if (loadingSceneUI != null)
            loadingSceneUI.SetActive(true);

        if (roomFullUI != null)
            roomFullUI.SetActive(false);

        bool joined = await NetworkLauncher.Instance.JoinBestRoom(roomDefinition);

        if (!joined && roomFullUI != null)
            roomFullUI.SetActive(true);

        if (loadingSceneUI != null)
            loadingSceneUI.SetActive(false);

        _travelStarted = false;
        _nextAllowedTravelTime = Time.time + retryDelay;

        return joined;
    }

    public Vector3 GetWorldPosition()
    {
        return transform.position;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(Vector3.zero, detectionSize);
    }
}