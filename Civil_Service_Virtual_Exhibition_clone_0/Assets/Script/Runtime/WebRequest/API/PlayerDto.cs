[System.Serializable] public class LoginRequestBody { public string email, password; }

[System.Serializable] public class RegisterRequestBody
{
    public string email;
    public string characterName;
    public string password;
    public string firstName;
    public string lastName;
    public string department;       
    public string phone;
    public string gender;          
}
[System.Serializable]
public class LoginData
{
    public PlayerData player;
    public string token;
    public string expiresAt;
}

[System.Serializable] public class ForgotPasswordRequestBody { public string email; }
[System.Serializable]
public class BaseResponse
{
    public bool success;
    public string message;
    public string error;

    public string DisplayMessage
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(message))
                return message;

            if (!string.IsNullOrWhiteSpace(error))
                return error;

            return string.Empty;
        }
    }
}
[System.Serializable]
public class LoginResponse : BaseResponse
{
    public LoginData data;
}
[System.Serializable]
public class PlayerData
{
    public string id;
    public string email;
    public string characterName;
    public string firstName;
    public string lastName;
    public string department;
    public string phone;
    public string gender;
    public bool isAnonymous;
}
[System.Serializable]
public class RegisterResponse : BaseResponse { }


