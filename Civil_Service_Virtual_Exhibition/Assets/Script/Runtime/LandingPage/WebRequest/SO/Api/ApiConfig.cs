using UnityEngine;

[CreateAssetMenu(fileName = "ApiConfig", menuName = "Game/API Config")]
public class ApiConfig : ScriptableObject
{
    [Header("Base")]
    public string baseUrl = "";

    [Header("Endpoints")]
    public string loginEndpoint = "/api/auth/login";
    public string registerEndpoint = "/api/game/register";
    public string anonymousEndpoint = "/anonymous";
    public string resetPasswordEndpoint = "/api/auth/reset-password";

    [Header("Quiz")]
    public string weeklyQuizEndpoint = "/api/quiz/weekly";
    [Header("Government Catalog")]
    public string governmentCatalogEndpoint = "/api/game/catalog";
    public string agencyexhibitionttseng = "/api/game/projects/:id/tts?lang=en";
    public string agencyexhibitionttsth = "/api/game/projects/:id/tts";
    [Header("Initial Room")]
    public RoomDefinition initialRoom;

    public string LoginUrl => baseUrl + loginEndpoint;
    public string RegisterUrl => baseUrl + registerEndpoint;
    public string AnonymousUrl => baseUrl + anonymousEndpoint;
    public string ResetPasswordUrl => baseUrl + resetPasswordEndpoint;
    public string WeeklyQuizUrl => baseUrl + weeklyQuizEndpoint;
    public string GovernmentCatalogUrl => baseUrl + governmentCatalogEndpoint;
    public string AgencyExhibitionTtsEng => baseUrl + agencyexhibitionttseng;
    public string AgencyExhibitionTtsTh => baseUrl + agencyexhibitionttsth;
}