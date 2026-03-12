using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ServerStartupManager : MonoBehaviour
{
    [Header("Room Scenes to Load")]
    [SerializeField] private string[] roomScenes =
    {
        "MainScene",
        "DisplayRoomScene",
        "PlayRoomScene"
    };

    private void Start()
    {
#if UNITY_SERVER
        Debug.Log("[ServerStartupManager] Server starting — loading all rooms...");
        StartCoroutine(LoadAllRooms());
#endif
    }

    private IEnumerator LoadAllRooms()
    {
        foreach (var scene in roomScenes)
        {
            Debug.Log($"[ServerStartupManager] Loading room: {scene}");

            AsyncOperation op = SceneManager.LoadSceneAsync(
                scene,
                LoadSceneMode.Additive
            );

            yield return op;

            Debug.Log($"[ServerStartupManager] Room ready: {scene}");
        }

        Debug.Log("[ServerStartupManager] All rooms loaded. Server is ready.");
    }
}