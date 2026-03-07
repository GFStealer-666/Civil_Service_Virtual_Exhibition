using Fusion;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
public class Gameplay : NetworkBehaviour
{
    [Header("Player Prefabs")]
    public Player MalePlayerPrefab;
    public Player FemalePlayerPrefab;

    [Header("Fetch Settings")]
    [SerializeField] private float profileFetchTimeout = 3f;
    [SerializeField] private float mockFetchDelay = 1f;

    [SerializeField] private List<Player> _spawnedPlayers = new(16);
    private readonly Dictionary<PlayerRef, PendingPlayerProfile> _pendingProfiles = new();
    private readonly List<Transform> _recentSpawnPoints = new(4);

    private class PendingPlayerProfile
    {
        public PlayerRef PlayerRef;
        public bool IsFetchStarted;
        public bool IsFetchCompleted;
        public bool IsSpawned;
        public string PlayerName;
        public PlayerGender PlayerGender;
        public float FetchStartTime;
    }

    public override void Spawned()
    {
        Debug.Log($"[Gameplay] Spawned | IsServer={Runner.IsServer} | GameMode={Runner.GameMode}");
        if (Runner.IsServer)
        {
            Application.targetFrameRate =
                TickRate.Resolve(Runner.Config.Simulation.TickRateSelection).Server;
        }

        if (Runner.GameMode == GameMode.Shared)
        {
            throw new System.NotSupportedException(
                "[Gameplay] This doesn't support Shared Mode, please start the game as Server, Host or Client."
            );
        }

        Debug.Log($"[Gameplay] Spawned | Mode={Runner.Mode} | GameMode={Runner.GameMode} | IsServer={Runner.IsServer} | HasStateAuthority={HasStateAuthority}");
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
        {
            return;
        }
        _pendingProfiles[playerRef] = new PendingPlayerProfile
        {
            PlayerRef = playerRef,
            IsFetchStarted = false,
            IsFetchCompleted = false,
            IsSpawned = false,
            FetchStartTime = Time.time
        };

        Debug.Log($"[Gameplay] Registered pending player: {playerRef}");
    }

    private void ProcessPendingProfiles()
    {
        foreach (var keyValuePair in _pendingProfiles)
        {
            PendingPlayerProfile profile = keyValuePair.Value;

            if (profile.IsSpawned)
                continue;

            if (!profile.IsFetchStarted)
            {
                profile.IsFetchStarted = true;
                profile.FetchStartTime = Time.time;
                StartCoroutine(FetchPlayerProfileMock(profile));
                Debug.Log($"[Gameplay] Started profile fetch for {profile.PlayerRef}");
            }
            else if (profile.IsFetchCompleted)
            {
                SpawnResolvedPlayer(profile);
            }
            else if (Time.time - profile.FetchStartTime > profileFetchTimeout)
            {

                // default fallback when fetching current profile is failed

                profile.PlayerName = $"Guest_{profile.PlayerRef.PlayerId}";
                profile.PlayerGender = PlayerGender.Male;
                profile.IsFetchCompleted = true;

                Debug.LogWarning($"[Gameplay] Profile fetch timeout for {profile.PlayerRef}, using fallback profile.");
            }
        }
    }

    private IEnumerator FetchPlayerProfileMock(PendingPlayerProfile profile)
    {
        // Replace this later with real UnityWebRequest
        yield return new WaitForSeconds(mockFetchDelay);

        profile.PlayerName = $"User_{profile.PlayerRef.PlayerId}";
        profile.PlayerGender = Random.value > 0.5f ? PlayerGender.Male : PlayerGender.Female;
        profile.IsFetchCompleted = true;

        Debug.Log($"[Gameplay] Mock profile ready for {profile.PlayerRef} | Name={profile.PlayerName} | Gender={profile.PlayerGender}");
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
            Debug.LogError("[Gameplay] MalePlayerPrefab or FemalePlayerPrefab is null.");
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
        {
            _spawnedPlayers.Add(player);
        }

        PlayerProfile playerProfile = player.GetComponent<PlayerProfile>();
        if (playerProfile != null)
        {
            playerProfile.ApplyProfile(profile.PlayerName, profile.PlayerGender);
            profile.IsSpawned = true; // add this incase we going to need json log file later on
        }
        else
        {
            Debug.LogError($"[Gameplay] Spawned player {profile.PlayerRef} has no PlayerProfile component.");
        }

        _pendingProfiles.Remove(profile.PlayerRef);

        Debug.Log($"[Gameplay] Spawned player {profile.PlayerRef} | Name={profile.PlayerName} | Gender={profile.PlayerGender}");
    }

    private void DespawnPlayer(PlayerRef playerRef, Player player)
    {
        Debug.Log($"[Gameplay] DespawnPlayer called for {playerRef}");

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
        {
            _recentSpawnPoints.RemoveAt(0);
        }

        return spawnPoint;
    }
}