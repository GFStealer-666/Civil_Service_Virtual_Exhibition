using System.Text;
using UnityEngine;
using UnityEngine.Networking;

[DefaultExecutionOrder(-900)]
public class ApiService : MonoBehaviour
{
    public static ApiService Instance { get; private set; }

    [Header("Config")]
    [SerializeField] private ApiConfig config;

    public ApiConfig Config => config;

    public string BaseUrl => config != null ? config.BaseUrl : string.Empty;

    public string LoginUrl => config != null ? config.LoginUrl : string.Empty;
    public string RegisterUrl => config != null ? config.RegisterUrl : string.Empty;
    public string AnonymousUrl => config != null ? config.AnonymousUrl : string.Empty;
    public string ResetPasswordUrl => config != null ? config.ResetPasswordUrl : string.Empty;

    public string GetQuizUrl => config != null ? config.GetQuizUrl : string.Empty;
    public string QuizSubmitUrl => config != null ? config.QuizSubmitUrl : string.Empty;
    public string GetQuizLeaderboardUrl => config != null ? config.GetQuizLeaderboardUrl : string.Empty;

    public string GovernmentCatalogUrl => config != null ? config.GovernmentCatalogUrl : string.Empty;
    public string PublicServiceUrl => config != null ? config.PublicServiceUrl : string.Empty;
    public string HallOfHonorUrl => config != null ? config.HallofHonorUrl : string.Empty;
    public string GetQuizMe => config != null ? config.GetQuizMe : string.Empty;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (config == null)
            Debug.LogError("[ApiService] ApiConfig is not assigned.");
    }

    public string GetAgencyExhibitionTtsEngUrl(string projectId)
    {
        return config != null ? config.GetAgencyExhibitionTtsEngUrl(projectId) : string.Empty;
    }

    public string GetAgencyExhibitionTtsThUrl(string projectId)
    {
        return config != null ? config.GetAgencyExhibitionTtsThUrl(projectId) : string.Empty;
    }

    public string GetAgencyExhibitionTtsUrl(string projectId, SystemLanguage language)
    {
        return config != null ? config.GetAgencyExhibitionTtsUrl(projectId, language) : string.Empty;
    }

    public string GetHallOfHonorTtsEngUrl(string officerId)
    {
        return config != null ? config.GetHallOfHonorTtsEngUrl(officerId) : string.Empty;
    }

    public string GetHallOfHonorTtsThUrl(string officerId)
    {
        return config != null ? config.GetHallOfHonorTtsThUrl(officerId) : string.Empty;
    }

    public string GetHallOfHonorTtsUrl(string officerId, SystemLanguage language)
    {
        return language == SystemLanguage.English
            ? GetHallOfHonorTtsEngUrl(officerId)
            : GetHallOfHonorTtsThUrl(officerId);
    }

    public UnityWebRequest Get(string url, string bearerToken = null)
    {
        UnityWebRequest request = UnityWebRequest.Get(url);
        ApplyCommonHeaders(request, bearerToken);
        return request;
    }

    public UnityWebRequest PostJson(string url, string jsonBody, string bearerToken = null)
    {
        return CreateJsonRequest(UnityWebRequest.kHttpVerbPOST, url, jsonBody, bearerToken);
    }

    public UnityWebRequest PutJson(string url, string jsonBody, string bearerToken = null)
    {
        return CreateJsonRequest(UnityWebRequest.kHttpVerbPUT, url, jsonBody, bearerToken);
    }

    public UnityWebRequest Delete(string url, string bearerToken = null)
    {
        UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbDELETE);
        request.downloadHandler = new DownloadHandlerBuffer();
        ApplyCommonHeaders(request, bearerToken);
        return request;
    }

    public UnityWebRequest GetTexture(string url, string bearerToken = null)
    {
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
        ApplyCommonHeaders(request, bearerToken);
        return request;
    }

    public UnityWebRequest GetAudioClip(string url, AudioType audioType, string bearerToken = null)
    {
        UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(url, audioType);

        ApplyCommonHeaders(request, bearerToken);
        request.SetRequestHeader("Accept", "audio/mpeg,audio/*,*/*");

        DownloadHandlerAudioClip downloadHandler = request.downloadHandler as DownloadHandlerAudioClip;
        if (downloadHandler != null)
            downloadHandler.streamAudio = false;

        return request;
    }

    private UnityWebRequest CreateJsonRequest(string method, string url, string jsonBody, string bearerToken)
    {
        UnityWebRequest request = new UnityWebRequest(url, method);
        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonBody ?? "{}"));
        request.downloadHandler = new DownloadHandlerBuffer();

        ApplyCommonHeaders(request, bearerToken);
        request.SetRequestHeader("Content-Type", "application/json");

        return request;
    }
    
    public string ResolveUrl(string rawUrl)
    {
        if (string.IsNullOrWhiteSpace(rawUrl))
            return string.Empty;

        string trimmed = rawUrl.Trim();

        if (System.Uri.TryCreate(trimmed, System.UriKind.Absolute, out System.Uri absoluteUri))
            return absoluteUri.ToString();

        string baseUrl = BaseUrl;
        if (string.IsNullOrWhiteSpace(baseUrl))
            return trimmed;

        if (!trimmed.StartsWith("/"))
            trimmed = "/" + trimmed;

        return baseUrl.TrimEnd('/') + trimmed;
    }

    private void ApplyCommonHeaders(UnityWebRequest request, string bearerToken)
    {
        if (request == null)
            return;

        request.SetRequestHeader("Accept", "application/json");

        if (!string.IsNullOrWhiteSpace(bearerToken))
            request.SetRequestHeader("Authorization", $"Bearer {bearerToken}");
    }
}