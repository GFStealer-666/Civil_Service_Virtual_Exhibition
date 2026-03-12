using UnityEngine;

public class GameStartup : MonoBehaviour
{
    async void Start()
    {
        // If a session is already running (came from portal travel),
        // Fusion already loaded this scene — don't start another session.
        if (NetworkLauncher.Instance.IsSessionRunning)
        {
            Debug.Log("[GameStartup] Session already running — skipping StartSession.");
            return;
        }

        // Only reaches here on first launch from login screen
        await NetworkLauncher.Instance.StartSession("Assets/Scenes/MainLobby.unity");
    }
}