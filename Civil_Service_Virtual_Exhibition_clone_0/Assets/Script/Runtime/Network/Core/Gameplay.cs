using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class Gameplay : NetworkBehaviour
{
    [Header("Player Prefabs")]
    public Player MalePlayerPrefab;
    public Player FemalePlayerPrefab;

    private readonly List<Transform> _recentSpawnPoints = new(4);

    public override void Spawned()
    {
        if (Runner.GetPlayerObject(Runner.LocalPlayer) != null)
            return;

        SpawnLocalPlayer();
        
    }

    private void SpawnLocalPlayer()
    {
        // Add this guard — tells you exactly what's missing
        if (MalePlayerPrefab == null || FemalePlayerPrefab == null)
        {
            Debug.LogError("[Gameplay] Player prefabs not assigned in Inspector!");
            return;
        }

        var local  = LocalPlayerData.Instance;
        PlayerGender gender     = local != null ? local.Gender : PlayerGender.Female;
        Player       prefab     = gender == PlayerGender.Female ? FemalePlayerPrefab : MalePlayerPrefab;
        Transform    spawnPoint = GetSpawnPoint();

        Player player = Runner.Spawn(prefab, spawnPoint.position, spawnPoint.rotation, Runner.LocalPlayer);
        if (player == null) { Debug.LogError("[Gameplay] Spawn failed."); return; }

        Runner.SetPlayerObject(Runner.LocalPlayer, player.Object);
        Debug.Log($"[Gameplay] Spawned local player | Gender={gender}");
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
            if (!_recentSpawnPoints.Contains(spawnPoint)) break;
        }

        _recentSpawnPoints.Add(spawnPoint);
        if (_recentSpawnPoints.Count > 3) _recentSpawnPoints.RemoveAt(0);

        return spawnPoint;
    }
}