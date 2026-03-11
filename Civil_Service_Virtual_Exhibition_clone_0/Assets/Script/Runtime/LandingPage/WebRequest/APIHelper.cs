using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Thin wrapper around UnityWebRequest for JSON API calls.
/// </summary>
public static class APIHelper
{
    /// <summary>POST with application/json body. Caller must dispose (using).</summary>
    public static UnityWebRequest PostJson(string url, string jsonBody)
    {
        var req             = new UnityWebRequest(url, "POST");
        req.uploadHandler   = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(jsonBody));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        req.SetRequestHeader("Accept",       "application/json");
        return req;
    }

    /// <summary>GET request. Caller must dispose (using).</summary>
    public static UnityWebRequest Get(string url)
    {
        var req             = UnityWebRequest.Get(url);
        req.SetRequestHeader("Accept", "application/json");
        return req;
    }
}

[System.Serializable]
public class LoginRequestBody
{
    public string email;
    public string password;
}

[System.Serializable]
public class RegisterRequestBody
{
    public string email;
    public string characterName;
    public string password;
    public string firstName;
    public string lastName;
    public string phone;
    public string organization;
    public string gender;
}


[System.Serializable]
public class LoginResponse
{
    public bool        success;
    public string      message;
    public UserPayload data;
}

[System.Serializable]
public class RegisterResponse
{
    public bool   success;
    public string message;
}

[System.Serializable]
public class UserPayload
{
    public string characterName;
    public string gender;
    public string organization;
    public string token;          // store if you need auth headers later
}