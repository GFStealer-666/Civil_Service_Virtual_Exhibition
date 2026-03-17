using UnityEngine;

public class GameStartup : MonoBehaviour
{
    [SerializeField] private RoomDefinition roomDefinition;
    async void Start()
    {
        // Only for Editor testing
#if UNITY_EDITOR

        if (NetworkLauncher.Instance == null)
        {
            Debug.LogError("NetworkLauncher missing in scene");
            return;
        }

        if (NetworkLauncher.Instance.IsRunning)
        {
            Debug.Log("[GameStartup] Session already running — skip.");
            return;
        }

        Debug.Log("[GameStartup] Starting session (Editor mode)");

        await NetworkLauncher.Instance.JoinInitialRoom(roomDefinition);

#endif
    }
}