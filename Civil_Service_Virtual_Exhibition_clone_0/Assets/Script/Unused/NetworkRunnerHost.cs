using UnityEngine;
using UnityEngine.SceneManagement;
using Fusion;

[DefaultExecutionOrder(-1000)]
public class NetworkRunnerHost : MonoBehaviour
{
    public NetworkRunner Runner { get; private set; }

    [Header("Session")]
    [SerializeField] private string sessionName = "exhibition-room";
    [SerializeField] private GameMode gameMode = GameMode.Shared;

    [Header("Callbacks")]
    [SerializeField] FusionInputProvider inputProvider;

    private NetworkSceneManagerDefault _sceneMgr;

    private async void Awake()
    {
        Runner = GetComponent<NetworkRunner>();
        if (Runner == null)
            Runner = gameObject.AddComponent<NetworkRunner>();

        Runner.ProvideInput = true;

        _sceneMgr = GetComponent<NetworkSceneManagerDefault>();
        if (_sceneMgr == null)
            _sceneMgr = gameObject.AddComponent<NetworkSceneManagerDefault>();

        if (inputProvider != null)
        {
            inputProvider.RegisterWithRunner(Runner);
        }

        var scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex);

        var result = await Runner.StartGame(new StartGameArgs
        {
            GameMode = gameMode,
            SessionName = sessionName,
            Scene = scene,
            SceneManager = _sceneMgr
        });

        Debug.Log($"[NetworkRunnerHost] StartGame result: {result.Ok}");
        if (!result.Ok)
            Debug.LogError($"[NetworkRunnerHost] StartGame failed: {result.ShutdownReason}");
    }
}