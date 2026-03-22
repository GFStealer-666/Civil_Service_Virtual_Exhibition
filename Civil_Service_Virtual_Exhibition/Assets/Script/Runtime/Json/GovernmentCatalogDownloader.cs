using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class GovernmentCatalogDownloader : MonoBehaviour
{
    public static GovernmentCatalogDownloader Instance { get; private set; }

    public bool IsDownloading { get; private set; }
    public string LastError { get; private set; }
    public ApiConfig apiConfig;
    private Coroutine _downloadRoutine;

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
    void Start()
    {
        GovernmentCatalogDownloader.EnsureExists().Download(
            apiConfig,
            dto =>
            {
                GovernmentCatalogStore.EnsureExists().SetData(dto);
            },
            error =>
            {
                Debug.LogError(error);
            }
        );
        
    }
    public static GovernmentCatalogDownloader EnsureExists()
    {
        if (Instance != null)
            return Instance;

        GameObject go = new GameObject(nameof(GovernmentCatalogDownloader));
        return go.AddComponent<GovernmentCatalogDownloader>();
    }

    public void Download(ApiConfig api, Action<GovernmentCatalogResponseDto> onSuccess, Action<string> onFail, string accessToken = null)
    {   
        if (api == null)
        {
            onFail?.Invoke("ApiConfig is null.");
            return;
        }
        Debug.Log($"[GovernmentCatalogDownloader] {api.governmentCatalogEndpoint}");
        if (string.IsNullOrWhiteSpace(api.GovernmentCatalogUrl))
        {
            onFail?.Invoke("GovernmentCatalogUrl is empty.");
            return;
        }

        if (_downloadRoutine != null)
            StopCoroutine(_downloadRoutine);

        _downloadRoutine = StartCoroutine(DownloadRoutine(api.GovernmentCatalogUrl, accessToken, onSuccess, onFail));
    }

    private IEnumerator DownloadRoutine(
        string url,
        string accessToken,
        Action<GovernmentCatalogResponseDto> onSuccess,
        Action<string> onFail)
    {
        Debug.Log("[Project Catalog] Catalog start downloading.");
        IsDownloading = true;
        LastError = null;

        using UnityWebRequest req = UnityWebRequest.Get(url);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Accept", "application/json");

        if (!string.IsNullOrWhiteSpace(accessToken))
            req.SetRequestHeader("Authorization", $"Bearer {accessToken}");

        yield return req.SendWebRequest();

        IsDownloading = false;

        if (req.result != UnityWebRequest.Result.Success)
        {
            LastError = $"Download failed: {req.error}";
            onFail?.Invoke(LastError);
            yield break;
        }

        GovernmentCatalogResponseDto dto;

        try
        {
            dto = JsonUtility.FromJson<GovernmentCatalogResponseDto>(req.downloadHandler.text);
        }
        catch (Exception ex)
        {
            LastError = $"Parse failed: {ex.Message}";
            onFail?.Invoke(LastError);
            yield break;
        }

        if (dto == null)
        {
            LastError = "Parsed dto is null.";
            onFail?.Invoke(LastError);
            yield break;
        }

        onSuccess?.Invoke(dto);
    }
}