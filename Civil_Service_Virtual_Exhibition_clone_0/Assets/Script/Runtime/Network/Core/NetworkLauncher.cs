using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
[DefaultExecutionOrder(-1000)]
public class NetworkLauncher : MonoBehaviour
{
    public static NetworkLauncher Instance;

    [SerializeField] private NetworkRunner runnerPrefab; // ← drag your prefab here
    public bool IsSessionRunning => _runner != null && _runner.IsRunning;
    public NetworkEvents NetworkEvents { get; private set; }
    private NetworkRunner _runner;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public async Task StartSession(string scenePath)
    {
        // Fully shut down existing runner first
        if (_runner != null)
        {
            if (_runner.IsRunning)
                await _runner.Shutdown();

            Destroy(_runner.gameObject);
            _runner     = null;
            NetworkEvents = null;
        }

        // Fresh runner from prefab every time
        _runner = Instantiate(runnerPrefab);
        _runner.name = "NetworkRunner";
        DontDestroyOnLoad(_runner.gameObject);
        _runner.ProvideInput = true;
        NetworkEvents = _runner.GetComponent<NetworkEvents>();

        int buildIndex = SceneUtility.GetBuildIndexByScenePath(scenePath);
        if (buildIndex < 0)
        {
            Debug.LogError($"[NetworkLauncher] Scene not in Build Settings: {scenePath}");
            return;
        }

        // SessionName = scenePath means ALL players going to the same scene
        // land in the SAME session automatically — including players returning
        var result = await _runner.StartGame(new StartGameArgs
        {
            GameMode     = GameMode.Shared,
            SessionName  = scenePath,        // ← this is the room key
            Scene        = SceneRef.FromIndex(buildIndex),
            SceneManager = _runner.GetComponent<NetworkSceneManagerDefault>()
        });

        if (!result.Ok)
            Debug.LogError($"[NetworkLauncher] StartGame failed: {result.ShutdownReason}");
        else
            Debug.Log($"[NetworkLauncher] Joined session: {scenePath}");
    }
}