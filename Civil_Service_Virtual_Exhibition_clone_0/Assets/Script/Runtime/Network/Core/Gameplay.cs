using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class Gameplay : NetworkBehaviour
{
    [Header("Player Prefabs")]
    public Player MalePlayerPrefab;
    public Player FemalePlayerPrefab;

    [SerializeField] private List<Player> _spawnedPlayers = new(16);
    private readonly List<Transform> _recentSpawnPoints = new(4);

    public override void Spawned()
    {
        Debug.Log($"[Gameplay] Spawned | IsServer={Runner.IsServer} | GameMode={Runner.GameMode}");

#if UNITY_SERVER
        if (Runner.GameMode == GameMode.Server)
        {
            Application.targetFrameRate =
                TickRate.Resolve(Runner.Config.Simulation.TickRateSelection).Server;
            Debug.Log("[Gameplay] Dedicated server — skipping local player setup.");
            return;
        }
#endif
        if (Runner.GameMode == GameMode.Shared)
            throw new System.NotSupportedException("[Gameplay] Shared Mode is not supported.");
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;
        PlayerManager.UpdatePlayerConnections(Runner, SpawnPlayer, DespawnPlayer);
    }

    /// <summary>
    /// Called by PlayerManager when a PlayerRef has no Player object yet.
    /// For the local player we have gender immediately from LocalPlayerData.
    /// For remote players we spawn Male by default — OnProfileReceived will
    /// respawn with the correct prefab once the client's RPC arrives (usually
    /// within the same second, not 30 seconds).
    /// </summary>
    private void SpawnPlayer(PlayerRef playerRef)
    {
        PlayerGender gender = PlayerGender.Male;

        if (playerRef == Runner.LocalPlayer && LocalPlayerData.Instance != null)
            gender = LocalPlayerData.Instance.Gender;

        SpawnWithGender(playerRef, gender);
    }

    /// <summary>
    /// Called by PlayerProfile.RPC_SubmitProfile after the player is already
    /// spawned and has sent its real profile to the server.
    /// Only respawns if the prefab gender doesn't match the profile gender.
    /// </summary>
    public void OnProfileReceived(PlayerRef playerRef, string playerName, PlayerGender gender)
    {
        var netObj = Runner.GetPlayerObject(playerRef);
        if (netObj == null)
        {
            Debug.LogWarning($"[Gameplay] OnProfileReceived: no object for {playerRef} yet — ignoring.");
            return;
        }

        var profile = netObj.GetComponent<PlayerProfile>();
        if (profile == null) return;

        if (profile.Gender == gender)
        {
            Debug.Log($"[Gameplay] {playerRef} gender matches — no respawn needed.");
            return;
        }

        Debug.Log($"[Gameplay] Gender mismatch for {playerRef} — respawning as {gender}.");
        _spawnedPlayers.Remove(netObj.GetComponent<Player>());
        Runner.Despawn(netObj);
        SpawnWithGender(playerRef, gender);
    }

    private void SpawnWithGender(PlayerRef playerRef, PlayerGender gender)
    {
        if (!Runner.IsRunning) return;

        if (MalePlayerPrefab == null || FemalePlayerPrefab == null)
        {
            Debug.LogError("[Gameplay] Player prefabs not assigned.");
            return;
        }

        Player    prefab     = gender == PlayerGender.Female ? FemalePlayerPrefab : MalePlayerPrefab;
        Transform spawnPoint = GetSpawnPoint();

        Player player = Runner.Spawn(prefab, spawnPoint.position, spawnPoint.rotation, playerRef);
        if (player == null)
        {
            Debug.LogError($"[Gameplay] Spawn failed for {playerRef}");
            return;
        }

        Runner.SetPlayerObject(playerRef, player.Object);
        _spawnedPlayers.Add(player);
        Debug.Log($"[Gameplay] Spawned {playerRef} | Gender={gender}");
    }


    private void DespawnPlayer(PlayerRef playerRef, Player player)
    {
        Debug.Log($"[Gameplay] DespawnPlayer: {playerRef}");

        if (player != null)
        {
            _spawnedPlayers.Remove(player);
            Runner.Despawn(player.Object);
        }
    }


    private Transform GetSpawnPoint()
    {
        var spawnPoints = Runner.SimulationUnityScene.GetComponents<SpawnPoint>(false);

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("[Gameplay] No SpawnPoint found — using Gameplay transform.");
            return transform;
        }

        Transform spawnPoint = default;

        for (int i = 0, offset = Random.Range(0, spawnPoints.Length); i < spawnPoints.Length; i++)
        {
            spawnPoint = spawnPoints[(offset + i) % spawnPoints.Length].transform;
            if (!_recentSpawnPoints.Contains(spawnPoint))
                break;
        }

        _recentSpawnPoints.Add(spawnPoint);
        if (_recentSpawnPoints.Count > 3)
            _recentSpawnPoints.RemoveAt(0);

        return spawnPoint;
    }
}