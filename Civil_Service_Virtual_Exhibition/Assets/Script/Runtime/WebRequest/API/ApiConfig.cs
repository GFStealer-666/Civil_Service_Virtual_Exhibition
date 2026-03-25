using UnityEngine;
using UnityEngine.Networking;

[CreateAssetMenu(fileName = "ApiConfig", menuName = "Game/API Config")]
public class ApiConfig : ScriptableObject
{
    [Header("Base")]
    [SerializeField] private string baseUrl = "";

    [Header("Auth")]
    [SerializeField] private string loginEndpoint = "/api/auth/login";
    [SerializeField] private string registerEndpoint = "/api/game/register";
    [SerializeField] private string anonymousEndpoint = "/api/game/anonymous";
    [SerializeField] private string resetPasswordEndpoint = "/api/auth/reset-password";

    [Header("Quiz")]
    [SerializeField] private string getQuizEndpoint = "/api/quiz/weekly";
    [SerializeField] private string submitQuizEndpoint = "/api/game/quiz/submit";
    [SerializeField] private string getQuizLeaderboardEndpoint = "/api/game/quiz/leaderboard";
    [SerializeField] private string getQuizLeaderboardMe = "/api/game/quiz/me";
    [Header("AgencyExhibition")]
    [SerializeField] private string governmentCatalogEndpoint = "/api/game/submissions/all";
    [Header("Public Service")]
    [SerializeField] private string publicServiceUrl ="/api/public/activities/all";
    [Header("Hall of Frame")]
    [SerializeField] private string hallofHonorUrl = "/api/public/civil-servants/all";
    [Header("TTS")]
    [SerializeField] private string agencyExhibitionTtsEngEndpoint = "/api/game/projects/:id/tts?lang=en";
    [SerializeField] private string agencyExhibitionTtsThEndpoint = "/api/game/projects/:id/tts";
    [SerializeField] private string hallofHonorTtsEngEndpoint = "/api/public/civil-servants/:id/tts?lang=en";
    [SerializeField] private string hallofHonorTtsThEndPoint = "/api/public/civil-servants/:id/tts";

    public string BaseUrl => NormalizeBaseUrl(baseUrl);

    public string LoginUrl => Build(loginEndpoint);
    public string RegisterUrl => Build(registerEndpoint);
    public string AnonymousUrl => Build(anonymousEndpoint);
    public string ResetPasswordUrl => Build(resetPasswordEndpoint);

    public string GetQuizUrl => Build(getQuizEndpoint);
    public string QuizSubmitUrl => Build(submitQuizEndpoint);
    public string GetQuizLeaderboardUrl => Build(getQuizLeaderboardEndpoint);
    public string GetQuizMe => Build(getQuizLeaderboardMe);
    public string GovernmentCatalogUrl => Build(governmentCatalogEndpoint);
    public string PublicServiceUrl => Build(publicServiceUrl);
    public string HallofHonorUrl => Build(hallofHonorUrl);
    public string GetAgencyExhibitionTtsEngUrl(string projectId)
    {
        return BuildTemplate(agencyExhibitionTtsEngEndpoint, projectId);
    }

    public string GetAgencyExhibitionTtsThUrl(string projectId)
    {
        return BuildTemplate(agencyExhibitionTtsThEndpoint, projectId);
    }
    public string GetHallOfHonorTtsEngUrl(string officerId)
    {
        return BuildTemplate(hallofHonorTtsEngEndpoint, officerId);
    }

    public string GetHallOfHonorTtsThUrl(string officerId)
    {
        return BuildTemplate(hallofHonorTtsThEndPoint, officerId);
    }
    public string GetAgencyExhibitionTtsUrl(string projectId, SystemLanguage language)
    {
        return language == SystemLanguage.English
            ? GetAgencyExhibitionTtsEngUrl(projectId)
            : GetAgencyExhibitionTtsThUrl(projectId);
    }

    private string Build(string endpoint)
    {
        string root = BaseUrl;
        string normalizedEndpoint = NormalizeEndpoint(endpoint);

        if (string.IsNullOrWhiteSpace(root))
        {
            Debug.LogWarning($"[{name}] Base URL is empty.");
            return normalizedEndpoint;
        }

        return $"{root}{normalizedEndpoint}";
    }

    private string BuildTemplate(string template, string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            Debug.LogWarning($"[{name}] Template endpoint requires id, but id is null or empty.");
            return Build(template);
        }

        string escapedId = UnityWebRequest.EscapeURL(id);
        string resolved = (template ?? string.Empty).Replace(":id", escapedId);
        return Build(resolved);
    }

    private static string NormalizeBaseUrl(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return value.Trim().TrimEnd('/');
    }

    private static string NormalizeEndpoint(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        value = value.Trim();
        return value.StartsWith("/") ? value : "/" + value;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        baseUrl = NormalizeBaseUrl(baseUrl);

        loginEndpoint = NormalizeEndpoint(loginEndpoint);
        registerEndpoint = NormalizeEndpoint(registerEndpoint);
        anonymousEndpoint = NormalizeEndpoint(anonymousEndpoint);
        resetPasswordEndpoint = NormalizeEndpoint(resetPasswordEndpoint);

        getQuizEndpoint = NormalizeEndpoint(getQuizEndpoint);
        submitQuizEndpoint = NormalizeEndpoint(submitQuizEndpoint);
        getQuizLeaderboardEndpoint = NormalizeEndpoint(getQuizLeaderboardEndpoint);

        governmentCatalogEndpoint = NormalizeEndpoint(governmentCatalogEndpoint);
        agencyExhibitionTtsEngEndpoint = NormalizeEndpoint(agencyExhibitionTtsEngEndpoint);
        agencyExhibitionTtsThEndpoint = NormalizeEndpoint(agencyExhibitionTtsThEndpoint);
    }
#endif
}