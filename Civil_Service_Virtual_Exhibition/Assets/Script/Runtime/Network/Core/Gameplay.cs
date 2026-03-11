using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class Gameplay : NetworkBehaviour
{
    [Header("Player Prefabs")]
    public Player MalePlayerPrefab;
    public Player FemalePlayerPrefab;

    [Header("Fetch Settings")]
    [SerializeField] private float profileFetchTimeout = 5f;

    [SerializeField] private List<Player> _spawnedPlayers = new(16);
    private readonly List<PlayerRef> _profilesToRemove = new(16);
    private readonly Dictionary<PlayerRef, PendingPlayerProfile> _pendingProfiles = new();
    private readonly List<Transform> _recentSpawnPoints = new(4);

    private class PendingPlayerProfile
    {
        public PlayerRef  PlayerRef;
        public bool       IsFetchStarted;
        public bool       IsFetchCompleted;
        public bool       IsSpawned;
        public string     PlayerName;
        public PlayerGender PlayerGender;
        public float      FetchStartTime;
    }

    public override void Spawned()
    {
        Debug.Log($"[Gameplay] Spawned | IsServer={Runner.IsServer} | GameMode={Runner.GameMode}");

#if UNITY_SERVER
        // Dedicated server: set tick rate and return — no local player
        if (Runner.GameMode == GameMode.Server)
        {
            Application.targetFrameRate =
                TickRate.Resolve(Runner.Config.Simulation.TickRateSelection).Server;
            Debug.Log("[Gameplay] Running as dedicated server — skipping local player setup.");
            return;
        }
#endif

        if (Runner.GameMode == GameMode.Shared)
        {
            throw new System.NotSupportedException(
                "[Gameplay] Shared Mode is not supported. Use Server, Host, or Client."
            );
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority)
            return;

        PlayerManager.UpdatePlayerConnections(Runner, RegisterPendingPlayer, DespawnPlayer);
        ProcessPendingProfiles();
    }

    private void RegisterPendingPlayer(PlayerRef playerRef)
    {
        if (_pendingProfiles.ContainsKey(playerRef))
            return;

        _pendingProfiles[playerRef] = new PendingPlayerProfile
        {
            PlayerRef        = playerRef,
            IsFetchStarted   = false,
            IsFetchCompleted = false,
            IsSpawned        = false,
            FetchStartTime   = Time.time
        };

        Debug.Log($"[Gameplay] Registered pending player: {playerRef}");
    }

    private void ProcessPendingProfiles()
    {
        _profilesToRemove.Clear();

        foreach (var kvp in _pendingProfiles)
        {
            PendingPlayerProfile profile = kvp.Value;

            if (profile.IsSpawned)
                continue;

            if (!profile.IsFetchStarted)
            {
                profile.IsFetchStarted = true;
                profile.FetchStartTime = Time.time;
                StartCoroutine(WaitForProfileRPC(profile));
                Debug.Log($"[Gameplay] Waiting for profile RPC: {profile.PlayerRef}");
            }
            else if (profile.IsFetchCompleted)
            {
                // Don't remove here — collect it first
                _profilesToRemove.Add(kvp.Key);
            }
            else if (Time.time - profile.FetchStartTime > profileFetchTimeout)
            {
                profile.PlayerName       = $"Guest_{profile.PlayerRef.PlayerId}";
                profile.PlayerGender     = PlayerGender.Male;
                profile.IsFetchCompleted = true;
                Debug.LogWarning($"[Gameplay] Profile timeout for {profile.PlayerRef}");
            }
        }

        // Safe to modify now — iteration is finished
        foreach (var playerRef in _profilesToRemove)
        {
            if (_pendingProfiles.TryGetValue(playerRef, out var profile))
                SpawnResolvedPlayer(profile);
        }
    }

    /// Waits for the client's RPC_SubmitProfile to arrive and set ProfileReady on
    /// the player object. Profile data is NOT read here — it comes via the RPC.
    /// This coroutine just spawns a temporary placeholder so the player object exists
    /// for the RPC to land on, then the profile is applied via the RPC itself.

    private IEnumerator WaitForProfileRPC(PendingPlayerProfile profile)
    {
        // Spawn with a default profile first so the NetworkObject exists
        // The RPC will update it once the client sends their data
        profile.PlayerName   = $"Guest_{profile.PlayerRef.PlayerId}";
        profile.PlayerGender = PlayerGender.Male;

        // Short wait to give the RPC a chance to arrive before first spawn
        float waited = 0f;
        while (waited < 1.5f)
        {
            waited += Runner.DeltaTime;
            yield return null;
        }

        profile.IsFetchCompleted = true;
    }

    private void SpawnResolvedPlayer(PendingPlayerProfile profile)
    {
        if (!Runner.IsRunning)
        {
            Debug.LogWarning($"[Gameplay] Runner not running, cannot spawn {profile.PlayerRef}");
            return;
        }

        if (MalePlayerPrefab == null || FemalePlayerPrefab == null)
        {
            Debug.LogError("[Gameplay] Player prefabs not assigned.");
            return;
        }

        Transform spawnPoint = GetSpawnPoint();

        Player prefab = profile.PlayerGender == PlayerGender.Female
            ? FemalePlayerPrefab
            : MalePlayerPrefab;

        Player player = Runner.Spawn(
            prefab,
            spawnPoint.position,
            spawnPoint.rotation,
            profile.PlayerRef
        );

        if (player == null)
        {
            Debug.LogError($"[Gameplay] Spawn failed for {profile.PlayerRef}");
            return;
        }

        Runner.SetPlayerObject(profile.PlayerRef, player.Object);

        if (!_spawnedPlayers.Contains(player))
            _spawnedPlayers.Add(player);

        profile.IsSpawned = true;
        _pendingProfiles.Remove(profile.PlayerRef);

        // PlayerProfile data is set by RPC_SubmitProfile in PlayerProfile.cs


        Debug.Log($"[Gameplay] Spawned player {profile.PlayerRef} | Name={profile.PlayerName}");
    }

    private void DespawnPlayer(PlayerRef playerRef, Player player)
    {
        Debug.Log($"[Gameplay] DespawnPlayer: {playerRef}");

        if (_pendingProfiles.TryGetValue(playerRef, out var profile))
        {
            profile.IsSpawned = false;
            _pendingProfiles.Remove(playerRef);
        }

        if (player != null)
        {
            _spawnedPlayers.Remove(player);
            Runner.Despawn(player.Object);
        }
    }

    private Transform GetSpawnPoint()
    {
        Transform spawnPoint = default;

        var spawnPoints = Runner.SimulationUnityScene.GetComponents<SpawnPoint>(false);
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("[Gameplay] No SpawnPoint found. Using Gameplay transform.");
            return transform;
        }

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