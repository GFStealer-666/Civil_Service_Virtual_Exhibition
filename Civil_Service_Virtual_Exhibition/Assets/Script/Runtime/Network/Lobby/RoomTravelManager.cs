using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RoomTravelManager : MonoBehaviour
{
    public static RoomTravelManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void TravelToRoom(string sceneName)
    {
        StartCoroutine(DoTravel(sceneName));
    }

    private IEnumerator DoTravel(string sceneName)
    {
        Debug.Log($"[RoomTravelManager] Travelling to {sceneName}");

        List<NetworkRunner> runners = new List<NetworkRunner>(NetworkRunner.Instances);

        foreach (var runner in runners)
        {
            if (runner != null && runner.IsRunning)
            {
                Debug.Log($"[RoomTravelManager] Shutting down runner: {runner.name}");
                runner.Shutdown();
            }
        }

        // Wait for all runners to finish shutting down
        bool allShutdown = false;
        while (!allShutdown)
        {
            allShutdown = true;

            // Copy again here too — same reason
            List<NetworkRunner> remaining = new List<NetworkRunner>(NetworkRunner.Instances);
            foreach (var runner in remaining)
            {
                if (runner != null && runner.IsRunning)
                {
                    allShutdown = false;
                    break;
                }
            }

            yield return null;
        }

        Debug.Log($"[RoomTravelManager] All runners shutdown. Loading: {sceneName}");
        SceneManager.LoadScene(sceneName);
    }
}