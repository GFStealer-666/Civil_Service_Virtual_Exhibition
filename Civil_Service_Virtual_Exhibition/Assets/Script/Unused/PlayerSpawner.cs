using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class PlayerSpawner : SimulationBehaviour, IPlayerJoined, IPlayerLeft
{
    [SerializeField] private NetworkPrefabRef playerPrefab;
    [SerializeField] private Transform[] spawnPoints;

    private readonly Dictionary<PlayerRef, NetworkObject> _spawned = new();

    public void PlayerJoined(PlayerRef player)
    {
        if (player != Runner.LocalPlayer) return;

        Vector3 spawnPos = Vector3.zero;
        Quaternion spawnRot = Quaternion.identity;

        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            int index = Random.Range(0, spawnPoints.Length);
            if (spawnPoints[index] != null)
            {
                spawnPos = spawnPoints[index].position;
                spawnRot = spawnPoints[index].rotation;
            }
        }

        Debug.Log($"[PlayerSpawner] Spawning {player} at {spawnPos}");

        var obj = Runner.Spawn(
            playerPrefab,
            spawnPos,
            spawnRot,
            inputAuthority: player,
            onBeforeSpawned: (runner, networkObj) =>
            {
                var cc = networkObj.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;

                networkObj.transform.SetPositionAndRotation(spawnPos, spawnRot);

                if (cc != null) cc.enabled = true;
            }
        );

        Runner.SetPlayerObject(player, obj);
        _spawned[player] = obj;
    }

    public void PlayerLeft(PlayerRef player)
    {
        if (player != Runner.LocalPlayer) return;

        if (_spawned.TryGetValue(player, out var obj))
        {
            Runner.Despawn(obj);
            _spawned.Remove(player);
        }
    }
}