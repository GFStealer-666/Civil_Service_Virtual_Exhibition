using Fusion;
using TMPro;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerSpawnLoadingUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject loadingRoot;
    [SerializeField] private Button disconnectButton;
    [Header("Status")]
    [SerializeField] private TextMeshProUGUI loadingText;
    [SerializeField] private TextMeshProUGUI disconnectText;

    [Header("Runner")]
    [SerializeField] private NetworkRunner runner;

    private bool _waitingForSpawn;
    private Coroutine _loadingAnimationCoroutine;

    private void Awake()
    {
        if (disconnectButton != null)
        {
            disconnectButton.onClick.AddListener(OnDisconnectPressed);
        }
            
    }

    private void Start()
    {
        ShowLoading();
    }

    private void Update()
    {
        // Auto-find runner if not assigned yet
        if (runner == null)
            runner = FindFirstObjectByType<NetworkRunner>();

        if (_waitingForSpawn == false)
            return;

        if (runner == null)
            return;

        if (runner.IsRunning == false)
            return;

        if (runner.LocalPlayer == PlayerRef.None)
            return;

        // This is the key check
        if (runner.TryGetPlayerObject(runner.LocalPlayer, out var playerObject) && playerObject != null)
        {
            HideLoading();
        }
        else
        {
            ShowLoading();
        }
    }
    public void ShowLoading()
    {
        _waitingForSpawn = true;

        if (loadingRoot != null)
        {
            loadingRoot.SetActive(true);
        }
            
        if (loadingText != null)
        {
            loadingText.gameObject.SetActive(true);
            disconnectText.gameObject.SetActive(false);
        }

        // Start loading animation if not already running
        if (_loadingAnimationCoroutine == null)
        {
            _loadingAnimationCoroutine = StartCoroutine(LoadingAnimation());
        }
    }

    public void HideLoading()
    {
        _waitingForSpawn = false;

        if (loadingRoot != null)
            loadingRoot.SetActive(false);

        // Stop loading animation
        if (_loadingAnimationCoroutine != null)
        {
            StopCoroutine(_loadingAnimationCoroutine);
            _loadingAnimationCoroutine = null;
        }
    }

    private IEnumerator LoadingAnimation()
    {
        string[] loadingStates = { "Loading", "Loading.", "Loading..", "Loading..." };
        int currentIndex = 0;

        while (true)
        {
            if (loadingText != null)
            {
                loadingText.text = loadingStates[currentIndex];
            }

            currentIndex = (currentIndex + 1) % loadingStates.Length;
            yield return new WaitForSeconds(0.5f);
        }
    }

    private async void OnDisconnectPressed()
    {
        if (runner == null)
        {
            return;
        }
            
        if (disconnectText != null)
        {
            loadingText.gameObject.SetActive(false);
            disconnectText.gameObject.SetActive(true);
        }
        

        await runner.Shutdown();
    }
}