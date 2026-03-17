using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class Gameplay : NetworkBehaviour
{
    [Header("Player Prefabs")]
    public Player MalePlayerPrefab;
    public Player FemalePlayerPrefab;
    [Header("Start Scene")]
    [SerializeField] private RoomDefinition  _startingRoom;
    private readonly List<Transform> _recentSpawnPoints = new(4);

    public override void Spawned()
    {
        Debug.Log("[Gameplay] Scene gameplay bootstrap ready.");
    }

    private void SpawnLocalPlayer()
    {
        if (MalePlayerPrefab == null || FemalePlayerPrefab == null)
        {
            Debug.LogError("[Gameplay] Player prefabs not assigned!");
            return;
        }

        var local = LocalPlayerData.Instance;

        if (local == null)
        {
            Debug.LogWarning("[Gameplay] LocalPlayerData missing, using default values");
        }

        PlayerGender gender = local != null
            ? local.Gender
            : PlayerGender.Female;

        Player prefab = gender == PlayerGender.Female
            ? FemalePlayerPrefab
            : MalePlayerPrefab;

        // Default room 
        string startRoom = _startingRoom.roomName;

        Transform spawnPoint = GetSpawnPointByRoom(startRoom);

        Player player = Runner.Spawn(
            prefab,
            spawnPoint.position,
            spawnPoint.rotation,
            Runner.LocalPlayer
        );

        if (player == null)
        {
            Debug.LogError("[Gameplay] Spawn failed.");
            return;
        }

        player.CurrentRoom = startRoom;

        Runner.SetPlayerObject(Runner.LocalPlayer, player.Object);

        Debug.Log($"[Gameplay] Spawned in room: {startRoom}");
    }

    private Transform GetSpawnPointByRoom(string roomName)
    {
        var spawnPoints = Runner.SimulationUnityScene.GetComponents<SpawnPoint>(false);

        foreach (var sp in spawnPoints)
        {
            if (sp.GetRoomName() == roomName)
                return sp.transform;
        }

        Debug.LogWarning($"No spawn point for room: {roomName}");

        return spawnPoints.Length > 0
            ? spawnPoints[0].transform
            : transform;
    }
}