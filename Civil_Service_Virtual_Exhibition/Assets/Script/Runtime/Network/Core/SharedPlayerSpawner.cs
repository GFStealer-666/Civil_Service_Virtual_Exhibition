using Fusion;
using Fusion.Sockets;
using UnityEngine;

public class SharedPlayerSpawner : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("Player Prefabs")]
    [SerializeField] private Player malePlayerPrefab;
    [SerializeField] private Player femalePlayerPrefab;

    [Header("Fallback")]
    [SerializeField] private Transform fallbackSpawn;

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        NetworkDisconnectOverlayController.Instance?.RegisterRunner(runner);
        if (player != runner.LocalPlayer)
            return;

        if (runner.GetPlayerObject(player) != null)
        {
            Debug.Log($"[SharedPlayerSpawner] Player object already exists for {player}.");
            return;
        }

        var local = LocalPlayerData.Instance;
        PlayerGender gender = local != null ? local.Gender : PlayerGender.Female;

        Player prefab = gender == PlayerGender.Female ? femalePlayerPrefab : malePlayerPrefab;
        if (prefab == null)
        {
            Debug.LogError("[SharedPlayerSpawner] Player prefab is missing.");
            return;
        }

        Transform spawn = FindSpawnPoint() ?? fallbackSpawn ?? transform;

        Player playerObject = runner.Spawn(
            prefab,
            spawn.position,
            spawn.rotation,
            player
        );

        if (playerObject == null)
        {
            Debug.LogError("[SharedPlayerSpawner] Runner.Spawn returned null.");
            return;
        }

        playerObject.CurrentRoom = runner.SessionInfo != null
            ? runner.SessionInfo.Name
            : "UnknownRoom";

        runner.SetPlayerObject(player, playerObject.Object);

        Debug.Log($"[SharedPlayerSpawner] Spawned local player in session '{playerObject.CurrentRoom}'.");
    }

    private Transform FindSpawnPoint()
    {
        SpawnPoint[] points = FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None);

        if (points == null || points.Length == 0)
        {
            Debug.LogWarning("[SharedPlayerSpawner] No SpawnPoint found. Using fallback.");
            return null;
        }

        return points[Random.Range(0, points.Length)].transform;
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        NetworkObject playerObject = runner.GetPlayerObject(player);
        if (playerObject != null)
        {
            runner.Despawn(playerObject);
        }
    }
#region Unused Callbacks
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, System.Collections.Generic.List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, System.Collections.Generic.Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, System.ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
#endregion
}