using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public abstract class BaseHandler : MonoBehaviour
{
    [Header("Overlay")]
    [SerializeField] protected StatusOverlay overlay;

    [Header("Room")]
    [SerializeField] private RoomDefinition initialRoom;

    protected ApiService Api => ApiService.Instance;

    protected void ShowLoadingOverlay(
        string title = "กำลังโหลดข้อมูล",
        string subtitle = "กรุณารอสักครู่")
    {
        overlay?.ShowLoading(title, subtitle);
    }

    protected void ShowFailedOverlay(
        string subtitle,
        string title = "ดำเนินการไม่สำเร็จ",
        Action onDismissed = null)
    {
        overlay?.ShowFailed(title, subtitle, onDismissed);
    }

    protected void ShowSuccessOverlay(
        string title,
        string subtitle,
        bool autoDismiss = true,
        Action onDone = null)
    {
        overlay?.ShowSuccess(title, subtitle, autoDismiss, onDone);
    }

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
            ShowFailedOverlay("API service is not ready.");
            onError?.Invoke("ApiService.Instance is null.");
            yield break;
        }

        if (string.IsNullOrWhiteSpace(url))
        {
            Debug.LogError("[BaseHandler] Request URL is null or empty.");
            ShowFailedOverlay("API URL is missing.");
            onError?.Invoke("Request URL is null or empty.");
            yield break;
        }

        ShowLoadingOverlay();

        using UnityWebRequest req = Api.PostJson(url, jsonBody, bearerToken);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            ShowFailedOverlay("เชื่อมต่อเซิร์ฟเวอร์ไม่ได้ กรุณาลองใหม่");
            onError?.Invoke(req.error);
            yield break;
        }

        onSuccess?.Invoke(req.downloadHandler.text);
    }

    protected void EnterMainScene(LoginData  loginData)
    {
        if (loginData == null || loginData.player == null)
        {
            ShowFailedOverlay("Login data is missing.");
            return;
        }

        LocalPlayerData localData = LocalPlayerData.Instance;
        if (localData == null)
        {
            ShowFailedOverlay("LocalPlayerData is missing.");
            return;
        }

        PlayerData player = loginData.player;

        localData.SetContact(player.email, player.phone);

        localData.PlayerName = string.IsNullOrWhiteSpace(player.characterName)
            ? $"{player.firstName} {player.lastName}".Trim()
            : player.characterName;

        localData.Organization = player.department ?? string.Empty;
        localData.IsGuest = player.isAnonymous;
        localData.Gender = ParseGender(player.gender);
        localData.PlayerID = player.id;
        localData.PlayerToken = loginData.token;

        if (!localData.HasInitializedAppearance)
        {
            localData.RandomizeAppearance();
            localData.HasInitializedAppearance = true;
        }

        Debug.Log(
            $"[EnterMainScene] ID={localData.PlayerID}, Name={localData.PlayerName}, " +
            $"Gender={localData.Gender}, Org={localData.Organization}, Email={player.email}, Token={localData.PlayerToken}"
        );

        ShowSuccessOverlay(
            "เข้าสู่ระบบสำเร็จ",
            "กรุณารอสักครู่",
            false,
            () => StartCoroutine(StartNetworkFlow())
        );
    }

    private IEnumerator StartNetworkFlow()
    {
        if (NetworkLauncher.Instance == null)
        {
            Debug.LogError("[BaseHandler] NetworkLauncher.Instance is null.");
            ShowFailedOverlay("Network system is not ready.");
            yield break;
        }

        if (Api == null)
        {
            Debug.LogError("[BaseHandler] ApiService.Instance is null.");
            ShowFailedOverlay("API service is not ready.");
            yield break;
        }

        if (initialRoom == null)
        {
            Debug.LogError("[BaseHandler] InitialRoom is not assigned.");
            ShowFailedOverlay("Initial room is not configured.");
            yield break;
        }

        var task = NetworkLauncher.Instance.JoinInitialRoom(initialRoom);

        while (!task.IsCompleted)
            yield return null;

        if (task.IsFaulted)
        {
            Debug.LogException(task.Exception);
            ShowFailedOverlay("Failed to join room.");
            yield break;
        }

        if (!task.Result)
        {
            Debug.LogError("[BaseHandler] JoinInitialRoom returned false.");
            ShowFailedOverlay("Room is full or unavailable.");
            yield break;
        }

        overlay?.Hide();
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