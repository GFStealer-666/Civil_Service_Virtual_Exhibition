using System;
using System.Threading.Tasks;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-1000)]
public class NetworkLauncher : MonoBehaviour
{
    public static NetworkLauncher Instance;

    [Header("Runner")]
    [SerializeField] private NetworkRunner runnerPrefab;

    [Header("Debug")]
    [SerializeField] private bool verboseLogs = true;

    public NetworkRunner Runner => _runner;
    public bool IsRunning => _runner != null && _runner.IsRunning;
    public NetworkEvents NetworkEvents { get; private set; }

    public event Action<string, string> OnRoomJoined; // sceneName, sessionName
    public event Action<string> OnRoomJoinFailed;     // reason

    private NetworkRunner _runner;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Join or create the first available instance:
    /// scene_0, scene_1, scene_2 ...
    /// Oldest available = lowest index first.
    /// </summary>
    public async Task<bool> JoinBestRoom(RoomDefinition roomDefinition)
    {
        if (roomDefinition == null)
        {
            Fail("RoomDefinition is null.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(roomDefinition.sceneName))
        {
            Fail("RoomDefinition.sceneName is empty.");
            return false;
        }

        int sceneBuildIndex = GetBuildIndexByName(roomDefinition.sceneName);
        if (sceneBuildIndex < 0)
        {
            Fail($"Scene '{roomDefinition.sceneName}' is not in Build Settings.");
            return false;
        }

        int maxInstances = Mathf.Max(1, roomDefinition.maxSubRooms);

        for (int index = 0; index < maxInstances; index++)
        {
            string sessionName = BuildSessionName(roomDefinition.sceneName, index);

            bool joined = await TryJoinOrCreateRoom(
                sceneName: roomDefinition.sceneName,
                sceneBuildIndex: sceneBuildIndex,
                sessionName: sessionName,
                maxPlayers: Mathf.Max(1, roomDefinition.maxPlayersPerRoom)
            );

            if (joined)
            {
                if (verboseLogs)
                    Debug.Log($"[NetworkLauncher] Joined room instance '{sessionName}'.");

                OnRoomJoined?.Invoke(roomDefinition.sceneName, sessionName);
                return true;
            }
        }

        Fail($"All instances are full for room '{roomDefinition.sceneName}'.");
        return false;
    }

    /// <summary>
    /// Used for the first entry after login, e.g. MainLobby.
    /// </summary>
    public async Task<bool> JoinInitialRoom(RoomDefinition roomDefinition)
    {
        return await JoinBestRoom(roomDefinition);
    }

    private async Task<bool> TryJoinOrCreateRoom(string sceneName, int sceneBuildIndex, string sessionName, int maxPlayers)
    {
        await ShutdownRunner();

        _runner = Instantiate(runnerPrefab);
        _runner.name = $"NetworkRunner_{sessionName}";
        _runner.ProvideInput = true;
        DontDestroyOnLoad(_runner.gameObject);

        NetworkEvents = _runner.GetComponent<NetworkEvents>();
        var sceneManager = _runner.GetComponent<NetworkSceneManagerDefault>();

        if (sceneManager == null)
        {
            Fail("NetworkSceneManagerDefault is missing on runner prefab.");
            await ShutdownRunner();
            return false;
        }

        var args = new StartGameArgs
        {
            GameMode = GameMode.Shared,
            SessionName = sessionName,
            Scene = SceneRef.FromIndex(sceneBuildIndex),
            SceneManager = sceneManager,
            PlayerCount = maxPlayers,
            IsVisible = true,
            IsOpen = true
        };

        if (verboseLogs)
            Debug.Log($"[NetworkLauncher] StartGame => Scene='{sceneName}', Session='{sessionName}', Capacity={maxPlayers}");

        var result = await _runner.StartGame(args);

        if (result.Ok)
            return true;

        // For your flow, failed join/create here is treated as "try next slot".
        if (verboseLogs)
            Debug.LogWarning($"[NetworkLauncher] Failed '{sessionName}' => {result.ShutdownReason}");

        await ShutdownRunner();
        return false;
    }

    public async Task ShutdownCurrentRoom()
    {
        await ShutdownRunner();
    }

    private async Task ShutdownRunner()
    {
        if (_runner == null)
            return;

        try
        {
            if (_runner.IsRunning)
                await _runner.Shutdown();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }

        if (_runner != null)
            Destroy(_runner.gameObject);

        _runner = null;
        NetworkEvents = null;
    }

    public static string BuildSessionName(string sceneName, int index)
    {
        return $"{sceneName}_{index}";
    }

    private static int GetBuildIndexByName(string sceneName)
    {
        int count = SceneManager.sceneCountInBuildSettings;

        for (int i = 0; i < count; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            string name = System.IO.Path.GetFileNameWithoutExtension(path);

            if (name == sceneName)
                return i;
        }

        return -1;
    }

    private void Fail(string message)
    {
        Debug.LogError($"[NetworkLauncher] {message}");
        OnRoomJoinFailed?.Invoke(message);
    }
}