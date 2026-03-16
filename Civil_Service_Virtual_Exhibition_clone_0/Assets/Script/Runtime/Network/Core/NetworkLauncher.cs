using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using System;

[DefaultExecutionOrder(-1000)]
public class NetworkLauncher : MonoBehaviour
{
    public static NetworkLauncher Instance;

    [SerializeField] private NetworkRunner       runnerPrefab;
    [SerializeField] private RoomCapacityConfig  capacityConfig;  

    public bool IsSessionRunning => _runner != null && _runner.IsRunning;
    public NetworkEvents NetworkEvents { get; private set; }

    public event Action<string> OnSessionJoined;
    public event Action         OnSessionFailed;

    private NetworkRunner _runner;
    private const int MaxOverflowRooms = 10;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public async Task StartSession(string scenePath)
    {
        int maxPlayers = capacityConfig != null
            ? capacityConfig.GetMaxPlayers(scenePath)
            : 0;

        for (int attempt = 1; attempt <= MaxOverflowRooms; attempt++)
        {
            string sessionName = attempt == 1
                ? scenePath
                : $"{scenePath}_{attempt}";

            bool joined = await TryJoinSession(scenePath, sessionName, maxPlayers);

            if (joined)
            {
                Debug.Log($"[NetworkLauncher] Joined '{sessionName}' (slot {attempt})");
                OnSessionJoined?.Invoke(sessionName);
                return;
            }

            Debug.Log($"[NetworkLauncher] Slot {attempt} full, trying next...");
        }

        Debug.LogError("[NetworkLauncher] All overflow slots full.");
        OnSessionFailed?.Invoke();
    }

    private async Task<bool> TryJoinSession(string scenePath, string sessionName, int maxPlayers)
    {
        await ShutdownRunner();

        _runner = Instantiate(runnerPrefab);
        _runner.name = "NetworkRunner";
        DontDestroyOnLoad(_runner.gameObject);
        _runner.ProvideInput = true;
        NetworkEvents = _runner.GetComponent<NetworkEvents>();

        int buildIndex = GetBuildIndexByName(scenePath);
        if (buildIndex < 0)
        {
            Debug.LogError($"[NetworkLauncher] Scene not in Build Settings: {scenePath}");
            await ShutdownRunner();
            return false;
        }

        var args = new StartGameArgs
        {
            GameMode     = GameMode.Shared,
            SessionName  = sessionName,
            Scene        = SceneRef.FromIndex(buildIndex),
            SceneManager = _runner.GetComponent<NetworkSceneManagerDefault>()
        };

        if (maxPlayers > 0)
            args.PlayerCount = maxPlayers;

        var result = await _runner.StartGame(args);

        if (result.Ok) return true;

        bool isFull = result.ShutdownReason == ShutdownReason.GameNotFound
                   || result.ShutdownReason == ShutdownReason.DisconnectedByPluginLogic;

        if (!isFull)
        {
            Debug.LogError($"[NetworkLauncher] Unexpected failure: {result.ShutdownReason}");
        }
            

        await ShutdownRunner();
        return false;
    }

    private async Task ShutdownRunner()
    {
        if (_runner == null) return;
        if (_runner.IsRunning) await _runner.Shutdown();
        Destroy(_runner.gameObject);
        _runner       = null;
        NetworkEvents = null;
    }
    private static int GetBuildIndexByName(string sceneName)
    {
        int count = SceneManager.sceneCountInBuildSettings;
        for (int i = 0; i < count; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            string name = System.IO.Path.GetFileNameWithoutExtension(path);
            if (name == sceneName) return i;
        }
        return -1;
    }
}