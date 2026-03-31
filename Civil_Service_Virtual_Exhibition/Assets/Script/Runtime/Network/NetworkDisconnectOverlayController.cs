using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class NetworkDisconnectOverlayController : MonoBehaviour, INetworkRunnerCallbacks
{
    public static NetworkDisconnectOverlayController Instance { get; private set; }

    [Header("Overlay")]
    [SerializeField] private GameObject overlayRoot;
    [SerializeField] private Button confirmButton;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text messageText;

    [Header("Scene")]
    [SerializeField] private string loginSceneName = "Login";

    [Header("Runner")]
    [SerializeField] private bool autoFindRunner = true;
    [SerializeField] private bool dontDestroyOnLoad = true;

    [Header("Localization")]
    [SerializeField] private SystemLanguage fallbackLanguage = SystemLanguage.English;

    [TextArea(2, 4)]
    [SerializeField] private string titleThai = "การเชื่อมต่อขาดหาย";

    [TextArea(2, 4)]
    [SerializeField] private string messageThai = "กรุณาตรวจสอบอินเตอร์เน็ตของท่าน";

    [TextArea(2, 4)]
    [SerializeField] private string titleEnglish = "Connection lost.";

    [TextArea(2, 4)]
    [SerializeField] private string messageEnglish = "Please check your internet connection.";
    [SerializeField] private TMP_Text buttonText;

    [SerializeField] private string thaiText = "ตกลง";
    [SerializeField] private string englishText = "OK";
    private NetworkRunner _runner;
    private bool _overlayShown;
    private bool _isReturningToLogin;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (dontDestroyOnLoad)
            DontDestroyOnLoad(gameObject);

        if (overlayRoot != null)
            overlayRoot.SetActive(false);

        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnConfirmPressed);
    }

    private void Start()
    {
        if (autoFindRunner)
            TryAttachRunner();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (confirmButton != null)
            confirmButton.onClick.RemoveListener(OnConfirmPressed);

        DetachRunner();

        if (Instance == this)
            Instance = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (autoFindRunner && (_runner == null || !_runner))
            TryAttachRunner();
    }

    public void RegisterRunner(NetworkRunner runner)
    {
        if (runner == null)
            return;

        if (_runner == runner)
            return;

        DetachRunner();

        _runner = runner;
        _runner.AddCallbacks(this);
    }

    public void TryAttachRunner()
    {
        NetworkRunner foundRunner = FindFirstObjectByType<NetworkRunner>();
        if (foundRunner != null)
            RegisterRunner(foundRunner);
    }

    private void DetachRunner()
    {
        if (_runner != null && _runner)
            _runner.RemoveCallbacks(this);

        _runner = null;
    }

    private void ShowOverlay()
    {
        if (_overlayShown)
            return;

        _overlayShown = true;
        ApplyLocalizedTexts();

        if (overlayRoot != null)
            overlayRoot.SetActive(true);
    }

    private void ApplyLocalizedTexts()
    {
        SystemLanguage language = GetCurrentLanguage();

        bool isThai = language == SystemLanguage.Thai;

        if (titleText != null)
            titleText.text = isThai ? titleThai : titleEnglish;

        if (messageText != null)
            messageText.text = isThai ? messageThai : messageEnglish;

        if (buttonText != null)
            buttonText.text = isThai ? thaiText : englishText;
    }

    private SystemLanguage GetCurrentLanguage()
    {
        if (Application.systemLanguage == SystemLanguage.Unknown)
            return fallbackLanguage;

        return Application.systemLanguage;
    }

    private void OnConfirmPressed()
    {
        if (_isReturningToLogin)
            return;

        _isReturningToLogin = true;
        ReturnToLogin();
    }

    private async void ReturnToLogin()
    {
        try
        {
            if (_runner != null && _runner)
            {
                try
                {
                    await _runner.Shutdown();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[NetworkDisconnectOverlayController] Runner shutdown failed: {ex.Message}");
                }
            }

            ResetEverything();

            if (overlayRoot != null)
                overlayRoot.SetActive(false);

            _overlayShown = false;
            _isReturningToLogin = false;

            SceneManager.LoadScene(loginSceneName);
        }
        catch (Exception ex)
        {
            _isReturningToLogin = false;
            Debug.LogError($"[NetworkDisconnectOverlayController] ReturnToLogin failed: {ex}");
        }
    }

    private void ResetEverything()
    {
        if (LocalPlayerData.Instance != null)
        {
            LocalPlayerData.Instance.PlayerID = string.Empty;
            LocalPlayerData.Instance.PlayerToken = string.Empty;
            LocalPlayerData.Instance.PlayerName = "Guest1";
            LocalPlayerData.Instance.Organization = string.Empty;
            LocalPlayerData.Instance.Email = string.Empty;
            LocalPlayerData.Instance.PhoneNumber = string.Empty;
            LocalPlayerData.Instance.IsGuest = false;
        }

        PlayerPrefs.DeleteKey("player_token");
        PlayerPrefs.DeleteKey("player_id");
        PlayerPrefs.DeleteKey("player_name");
        PlayerPrefs.DeleteKey("player_email");
        PlayerPrefs.DeleteKey("player_phone");
        PlayerPrefs.Save();

        Time.timeScale = 1f;
    }

    private void TriggerDisconnected()
    {
        ShowOverlay();
    }

    public void ForceShowDisconnectOverlay()
    {
        TriggerDisconnected();
    }

    public void OnConnectedToServer(NetworkRunner runner)
    {
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        Debug.LogWarning($"[NetworkDisconnectOverlayController] Disconnected: {reason}");
        TriggerDisconnected();
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        if (_isReturningToLogin)
            return;

        Debug.LogWarning($"[NetworkDisconnectOverlayController] Shutdown: {shutdownReason}");
        TriggerDisconnected();
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        Debug.LogWarning($"[NetworkDisconnectOverlayController] Connect failed: {reason}");
        TriggerDisconnected();
    }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {
        Debug.LogWarning("[NetworkDisconnectOverlayController] Host migration triggered.");
        TriggerDisconnected();
    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
    }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
    {
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
    }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
    {
    }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
    {
    }

    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
    {
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
    {
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
    }

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
    }

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
    }
}