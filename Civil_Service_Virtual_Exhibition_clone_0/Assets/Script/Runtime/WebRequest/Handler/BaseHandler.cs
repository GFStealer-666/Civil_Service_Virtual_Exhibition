using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SocialPlatforms;

public abstract class BaseHandler : MonoBehaviour
{
    [Header("Overlay")]
    [SerializeField] protected StatusOverlay overlay;
    protected ApiService Api => ApiService.Instance;
    [SerializeField] private RoomDefinition initialRoom;
    /// <summary>
    /// Runs a POST, drives the overlay states, then calls back.
    /// onSuccess receives the raw JSON string.
    /// onError   receives the user-facing message.
    /// </summary>
    protected IEnumerator PostRequest(
        string url,
        string jsonBody,
        Action<string> onSuccess,
        Action<string> onError,
        string bearerToken = null)
    {
        if (Api == null)
        {
            Debug.LogError("[BaseHandler] ApiService.Instance is null.");
            overlay?.ShowError("API service is not ready.");
            onError?.Invoke("ApiService.Instance is null.");
            yield break;
        }

        if (string.IsNullOrWhiteSpace(url))
        {
            Debug.LogError("[BaseHandler] Request URL is null or empty.");
            overlay?.ShowError("API URL is missing.");
            onError?.Invoke("Request URL is null or empty.");
            yield break;
        }
        overlay.ShowLoading();

        using UnityWebRequest req = Api.PostJson(url, jsonBody, bearerToken);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            overlay.ShowError("เชื่อมต่อเซิร์ฟเวอร์ไม่ได้ กรุณาลองใหม่");
            onError?.Invoke(req.error);
            yield break;
        }

        onSuccess?.Invoke(req.downloadHandler.text);
    }

    /// <summary>Loads initial room after storing player data.</summary>
    protected void EnterMainScene(PlayerData player)
    {
                
        var localData = LocalPlayerData.Instance;
        if (localData == null)
        {
            overlay.ShowError("LocalPlayerData is missing.");
            return;
        }

        localData.SetContact(player.email, player.phone);
        localData.PlayerName = string.IsNullOrWhiteSpace(player.characterName) 
            ? $"{player.firstName} {player.lastName}".Trim()
            : player.characterName;

        localData.Organization = player.department ?? "";
        localData.IsGuest = player.isAnonymous;
        localData.Gender = ParseGender(player.gender);   
        localData.PlayerID = player.id;
        localData.PlayerToken = player.token;
        localData.SetAuth(player.id, player.token);
        
        if (!localData.HasInitializedAppearance)
        {
            localData.RandomizeAppearance();
            localData.HasInitializedAppearance = true;
        }

        overlay.ShowSuccessNoDismiss(
            "เข้าสู่ระบบสำเร็จ",
            "กรุณารอสักครู่" , 
        () =>
        {
            
            StartCoroutine(StartNetworkFlow());
        });
    }

    private IEnumerator StartNetworkFlow()
    {
        if (NetworkLauncher.Instance == null)
        {
            Debug.LogError("[BaseHandler] NetworkLauncher.Instance is null.");
            overlay?.ShowError("Network system is not ready.");
            yield break;
        }

        if (Api == null)
        {
            Debug.LogError("[BaseHandler] ApiService.Instance is null.");
            overlay?.ShowError("API service is not ready.");
            yield break;
        }

        if (initialRoom == null)
        {
            Debug.LogError("[BaseHandler] InitialRoom is not assigned.");
            overlay?.ShowError("Initial room is not configured.");
            yield break;
        }

        var task = NetworkLauncher.Instance.JoinInitialRoom(initialRoom);

        while (!task.IsCompleted)
        {
            yield return null;
        }

        if (task.IsFaulted)
        {
            Debug.LogException(task.Exception);
            overlay?.ShowError("Failed to join room.");
            yield break;
        }

        if (!task.Result)
        {
            Debug.LogError("[BaseHandler] JoinInitialRoom returned false.");
            overlay?.ShowError("Room is full or unavailable.");
        }
    }
    private PlayerGender ParseGender(string gender)
    {
        if (string.IsNullOrWhiteSpace(gender))
            return PlayerGender.Male;

        return gender.Trim().ToLowerInvariant() == "female"
            ? PlayerGender.Female
            : PlayerGender.Male;
    }
}
