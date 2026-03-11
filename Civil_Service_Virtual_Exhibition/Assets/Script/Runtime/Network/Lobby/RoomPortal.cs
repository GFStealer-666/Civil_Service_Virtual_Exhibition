using UnityEngine;

public class RoomPortal : MonoBehaviour
{
    [Header("Destination")]
    [SerializeField] private string destinationScene;

    [Header("UI")]
    [SerializeField] private GameObject promptUI;
    [SerializeField] private GameObject loadingScene;

    [Header("Detection")]
    [SerializeField] private Vector3 detectionSize = Vector3.one;
    [SerializeField] private LayerMask playerLayerMask;  

    private bool _playerInRange = false;

    private void Update()
    {
        // Poll for overlap every frame on Agent layer
        Collider[] hits = Physics.OverlapBox(
            transform.position,
            detectionSize * 0.5f,
            transform.rotation,
            playerLayerMask
        );

        bool found = false;
        foreach (var hit in hits)
        {
            //Debug.Log(hit.tag);
            if (hit.transform.root.CompareTag("LocalPlayer"))
            {
                Debug.Log("Found player");
                found = true;
                break;
            }
        }

        // Entered range
        if (found && !_playerInRange)
        {
            _playerInRange = true;
            if (promptUI != null) promptUI.SetActive(true);
        }

        // Left range
        if (!found && _playerInRange)
        {
            _playerInRange = false;
            if (promptUI != null) promptUI.SetActive(false);
        }

        // Travel on E press
        if (_playerInRange)
        {
            if (!string.IsNullOrEmpty(destinationScene))
            {
                loadingScene.gameObject.SetActive(true);
                RoomTravelManager.Instance.TravelToRoom(destinationScene);
            }
                
            else
                Debug.LogWarning("[RoomPortal] destinationScene is not set.");
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(Vector3.zero, detectionSize);
    }
}