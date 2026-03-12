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
    [SerializeField] private float profileFetchTimeout = 30f;

    [SerializeField] private List<Player> _spawnedPlayers = new(16);
    private readonly List<PlayerRef> _profilesToRemove = new(16);
    private readonly Dictionary<PlayerRef, PendingPlayerProfile> _pendingProfiles = new();
    private readonly List<Transform> _recentSpawnPoints = new(4);

    private class PendingPlayerProfile
    {
        public PlayerRef    PlayerRef;
        public bool         IsFetchStarted;
        public bool         IsFetchCompleted;
        public bool         IsSpawned;
        public string       PlayerName;
        public PlayerGender PlayerGender;
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    public override void Spawned()
    {
        Debug.Log($"[Gameplay] Spawned | IsServer={Runner.IsServer} | GameMode={Runner.GameMode}");

#if UNITY_SERVER
        if (Runner.GameMode == GameMode.Server)
        {
            Application.targetFrameRate =
                TickRate.Resolve(Runner.Config.Simulation.TickRateSelection).Server;
            Debug.Log("[Gameplay] Running as dedicated server — skipping local player setup.");
            return;
        }
#endif

        if (Runner.GameMode == GameMode.Shared)
            throw new System.NotSupportedException("[Gameplay] Shared Mode is not supported.");
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        PlayerManager.UpdatePlayerConnections(Runner, RegisterPendingPlayer, DespawnPlayer);
        ProcessPendingProfiles();
    }

    // ── Pending player registration ───────────────────────────────────────────

    private void RegisterPendingPlayer(PlayerRef playerRef)
    {
        if (_pendingProfiles.ContainsKey(playerRef)) return;

        var profile = new PendingPlayerProfile
        {
            PlayerRef        = playerRef,
            IsFetchStarted   = false,
            IsFetchCompleted = false,
            IsSpawned        = false,
        };

        // ✅ Host's local player: read directly, no RPC needed
        if (playerRef == Runner.LocalPlayer && LocalPlayerData.Instance != null)
        {
            profile.PlayerName       = LocalPlayerData.Instance.PlayerName;
            profile.PlayerGender     = LocalPlayerData.Instance.Gender;
            profile.IsFetchCompleted = true;
            Debug.Log($"[Gameplay] Local player profile pre-loaded: {profile.PlayerName} | {profile.PlayerGender}");
        }

        _pendingProfiles[playerRef] = profile;
        Debug.Log($"[Gameplay] Registered pending player: {playerRef}");
    }
    private void ProcessPendingProfiles()
    {
        _profilesToRemove.Clear();

        foreach (var kvp in _pendingProfiles)
        {
            PendingPlayerProfile profile = kvp.Value;

            if (profile.IsSpawned) continue;

            if (!profile.IsFetchStarted)
            {
                profile.IsFetchStarted = true;
                StartCoroutine(WaitForProfileRPC(profile));
                Debug.Log($"[Gameplay] Waiting for profile RPC: {profile.PlayerRef}");
            }
            else if (profile.IsFetchCompleted)
            {
                _profilesToRemove.Add(kvp.Key);
            }
        }

        foreach (var playerRef in _profilesToRemove)
        {
            if (_pendingProfiles.TryGetValue(playerRef, out var profile))
                SpawnResolvedPlayer(profile);
        }
    }

    public void OnProfileReceived(PlayerRef playerRef, string playerName, PlayerGender gender)
    {
        if (_pendingProfiles.TryGetValue(playerRef, out var profile))
        {
            profile.PlayerName       = playerName;
            profile.PlayerGender     = gender;
            profile.IsFetchCompleted = true;
            Debug.Log($"[Gameplay] Profile received | {playerRef} | Name={playerName} | Gender={gender}");
            return;
        }

        // Already spawned — check for gender mismatch and respawn if needed
        SpawnPlayerWithCorrectGender(playerRef, playerName, gender);
    }

    private IEnumerator WaitForProfileRPC(PendingPlayerProfile profile)
    {
        // Already resolved (e.g. local player on Host)
        if (profile.IsFetchCompleted)
            yield break;

        float elapsed = 0f;
        while (!profile.IsFetchCompleted && elapsed < profileFetchTimeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (!profile.IsFetchCompleted)
        {
            Debug.LogWarning($"[Gameplay] Profile fetch timed out for {profile.PlayerRef} — using fallback.");
            profile.PlayerName       = $"Player_{profile.PlayerRef.PlayerId}";
            profile.PlayerGender     = PlayerGender.Male;
            profile.IsFetchCompleted = true;
        }
    }
    // ── Spawn / Despawn ───────────────────────────────────────────────────────

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
        Player    prefab     = profile.PlayerGender == PlayerGender.Female
            ? FemalePlayerPrefab
            : MalePlayerPrefab;
        Debug.Log(prefab.name);
        Player player = Runner.Spawn(prefab, spawnPoint.position, spawnPoint.rotation, profile.PlayerRef);

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

        Debug.Log($"[Gameplay] Spawned {profile.PlayerRef} | Name={profile.PlayerName} | Gender={profile.PlayerGender}");
    }

    private void SpawnPlayerWithCorrectGender(PlayerRef playerRef, string playerName, PlayerGender gender)
    {
        var existing = Runner.GetPlayerObject(playerRef);
        if (existing == null) return;

        Player existingPlayer = existing.GetComponent<Player>();
        if (existingPlayer == null) return;

        var existingProfile = existing.GetComponent<PlayerProfile>();
        if (existingProfile == null) return;

        bool genderMismatch = existingProfile.Gender != gender;
        if (!genderMismatch)
        {
            Debug.Log($"[Gameplay] Gender matches for {playerRef} — no respawn needed");
            return;
        }

        Debug.Log($"[Gameplay] Gender mismatch — respawning {playerRef} as {gender}");

        Transform spawnPoint = GetSpawnPoint();
        Player    prefab     = gender == PlayerGender.Female ? FemalePlayerPrefab : MalePlayerPrefab;

        _spawnedPlayers.Remove(existingPlayer);
        Runner.Despawn(existing);

        Player newPlayer = Runner.Spawn(prefab, spawnPoint.position, spawnPoint.rotation, playerRef);
        if (newPlayer == null)
        {
            Debug.LogError($"[Gameplay] Respawn failed for {playerRef}");
            return;
        }

        Runner.SetPlayerObject(playerRef, newPlayer.Object);
        _spawnedPlayers.Add(newPlayer);

        Debug.Log($"[Gameplay] Respawned {playerRef} | Name={playerName} | Gender={gender}");
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