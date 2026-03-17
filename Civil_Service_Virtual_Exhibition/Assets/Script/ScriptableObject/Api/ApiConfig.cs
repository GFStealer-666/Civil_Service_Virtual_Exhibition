// ApiConfig.cs — single source of truth for all API settings
using UnityEngine;

[CreateAssetMenu(fileName = "ApiConfig", menuName = "Game/API Config")]
public class ApiConfig : ScriptableObject
{
    [Header("Base")]
    public string baseUrl = "";

    [Header("Endpoints")]
    public string loginEndpoint          = "/api/auth/login";
    public string registerEndpoint       = "/api/game/register";
    public string anonymousEndpoint      = "/anonymous";
    public string resetPasswordEndpoint  = "/api/auth/reset-password";

    [Header("Initial Room")]
    public RoomDefinition initialRoom;

    public string LoginUrl         => baseUrl + loginEndpoint;
    public string RegisterUrl      => baseUrl + registerEndpoint;
    public string AnonymousUrl     => baseUrl + anonymousEndpoint;
    public string ResetPasswordUrl => baseUrl + resetPasswordEndpoint;
}