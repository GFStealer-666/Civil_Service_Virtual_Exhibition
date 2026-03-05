using Fusion;
using UnityEngine;
using System.Collections.Generic;

public struct PlayerData : INetworkStruct
{
    [Networked, Capacity(24)]
    public string PlayerName { get => default; set{} }  
    public PlayerRef PlayerRef;
    public bool IsConnected;
}

public class Gameplay : NetworkBehaviour
{
    public Player PlayerPrefab;

    [Networked][Capacity(32)][HideInInspector]
	public NetworkDictionary<PlayerRef, PlayerData> PlayerData { get; }

    [SerializeField] private List<Player> _spawnedPlayers = new(16);
	private List<PlayerRef> _pendingPlayers = new(16);
	[SerializeField] private List<PlayerData> _tempPlayerData = new(16);
	private List<Transform> _recentSpawnPoints = new(4);


    public override void Spawned()
    {
        if (Runner.Mode == SimulationModes.Server)
        {
            Application.targetFrameRate = TickRate.Resolve(Runner.Config.Simulation.TickRateSelection).Server;
        }

        if (Runner.GameMode == GameMode.Shared)
        {
            throw new System.NotSupportedException("[Gameplay] This doesn't support Shared Mode, please start the game as Server, Host or Client.");
        }

        Debug.Log($"[Gameplay] Spawned with Runner.Mode={Runner.Mode}, Runner.GameMode={Runner.GameMode}");
    }

    public override void FixedUpdateNetwork()
    {
        if (HasStateAuthority == false) return;

        PlayerManager.UpdatePlayerConnections(Runner, SpawnPlayer, DespawnPlayer);
    }

    private void SpawnPlayer(PlayerRef playerRef)
    {
        if (PlayerData.TryGet(playerRef, out var playerData) == false)
        {
            playerData = new PlayerData();
            playerData.PlayerRef = playerRef;
            playerData.IsConnected = false;
        }

        if (playerData.IsConnected == true)
            return;

        Debug.LogWarning($"[Gameplay] {playerRef} connected.");

        playerData.IsConnected = true;

        PlayerData.Set(playerRef, playerData);

        var spawnPoint = GetSpawnPoint();
        var player = Runner.Spawn(PlayerPrefab, spawnPoint.position, spawnPoint.rotation, playerRef);

        // Set player instance as PlayerObject so we can easily get it from other locations.
        Runner.SetPlayerObject(playerRef, player.Object);

    }

    private void DespawnPlayer(PlayerRef playerRef, Player player)
    {
        if (PlayerData.TryGet(playerRef, out var playerData) == true)
        {
            if (playerData.IsConnected == true)
            {
                Debug.LogWarning($"[Gameplay] {playerRef} disconnected.");
            }

            playerData.IsConnected = false;
            PlayerData.Set(playerRef, playerData);
        }

        Runner.Despawn(player.Object);
    }

    private Transform GetSpawnPoint()
    {
        Transform spawnPoint = default;

        var spawnPoints = Runner.SimulationUnityScene.GetComponents<SpawnPoint>(false);
        for (int i = 0, offset = Random.Range(0, spawnPoints.Length); i < spawnPoints.Length; i++)
        {
            spawnPoint = spawnPoints[(offset + i) % spawnPoints.Length].transform;

            if (_recentSpawnPoints.Contains(spawnPoint) == false)
                break;
        }

        // Add spawn point to list of recently used spawn points.
        _recentSpawnPoints.Add(spawnPoint);


        if (_recentSpawnPoints.Count > 3)
        {
            _recentSpawnPoints.RemoveAt(0);
        }

        return spawnPoint;
    }
}

