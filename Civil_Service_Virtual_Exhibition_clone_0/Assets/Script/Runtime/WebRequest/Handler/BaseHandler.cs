using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Localization.Settings;
public abstract class BaseHandler : MonoBehaviour
{
    [Header("Overlay")]
    [SerializeField] protected StatusOverlay overlay;

    [Header("Room")]
    [SerializeField] private RoomDefinition initialRoom;

    protected ApiService Api => ApiService.Instance;
    protected bool IsThaiLanguage()
    {
        var locale = LocalizationSettings.SelectedLocale;
        if (locale == null)
            return true;

        string code = locale.Identifier.Code;
        return !string.IsNullOrEmpty(code) &&
            code.StartsWith("th", StringComparison.OrdinalIgnoreCase);
    }

    protected string L(string thai, string english)
    {
        return IsThaiLanguage() ? thai : english;
    }
    protected void ShowLoadingOverlay(
    string title = null,
    string subtitle = null)
    {
        overlay?.ShowLoading(
            title ?? L("กำลังโหลดข้อมูล", "Loading"),
            subtitle ?? L("กรุณารอสักครู่", "Please wait a moment")
        );
    }

    protected void ShowFailedOverlay(
    string subtitle,
    string title = null,
    Action onDismissed = null)
    {
        overlay?.ShowFailed(
            title ?? L("ดำเนินการไม่สำเร็จ", "Action failed"),
            subtitle,
            onDismissed
        );
    }

    protected void ShowSuccessOverlay(
        string title,
        string subtitle,
        bool autoDismiss = false,
        Action onDone = null,
        bool animateDots = true,
        bool showBlock = true)
    {
        overlay?.ShowSuccess(title, subtitle, autoDismiss, onDone, animateDots , showBlock);
    }

    protected void ShowSuccessOverlayWaiting(
        string title,
        string subtitle,
        bool autoDismiss = false,
        Action onDone = null,
        bool animateDots = true,
        bool showBlock = true)
    {
        overlay?.ShowSuccessWaiting(title, subtitle, true);
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
            ShowFailedOverlay(L("ระบบ API ยังไม่พร้อมใช้งาน", "API service is not ready."));
            onError?.Invoke("ApiService.Instance is null.");
            yield break;
        }

        if (string.IsNullOrWhiteSpace(url))
        {
            Debug.LogError("[BaseHandler] Request URL is null or empty.");
            ShowFailedOverlay(L("ไม่พบ URL ของ API", "API URL is missing."));
            onError?.Invoke("Request URL is null or empty.");
            yield break;
        }

        ShowLoadingOverlay();

        using UnityWebRequest req = Api.PostJson(url, jsonBody, bearerToken);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            ShowFailedOverlay(
                L("เชื่อมต่อเซิร์ฟเวอร์ไม่ได้ กรุณาลองใหม่",
                "Cannot connect to the server. Please try again.")
            );
            onError?.Invoke(req.error);
            yield break;
        }

        onSuccess?.Invoke(req.downloadHandler.text);
    }

    protected void EnterMainScene(LoginData loginData)
    {
        if (loginData == null || loginData.player == null)
        {
            ShowFailedOverlay(L("ข้อมูลการล็อกอินหายไป", "Login data is missing."));
            return;
        }

        LocalPlayerData localData = LocalPlayerData.Instance;
        if (localData == null)
        {
            ShowFailedOverlay(L("ไม่พบ LocalPlayerData", "LocalPlayerData is missing."));
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

        ShowSuccessOverlayWaiting(
            L("เข้าสู่ระบบสำเร็จ", "Login successful"),
            L("กรุณารอสักครู่", "Please wait a moment")
        );
        StartCoroutine(StartNetworkFlow());
    }

    private IEnumerator StartNetworkFlow()
    {
        Debug.Log("[BaseHandler] StartNetworkFlow called.");

        if (NetworkLauncher.Instance == null)
        {
            Debug.LogError("[BaseHandler] NetworkLauncher.Instance is null.");
            ShowFailedOverlay(L("ระบบเครือข่ายยังไม่พร้อมใช้งาน", "Network system is not ready."));
            yield break;
        }

        if (Api == null)
        {
            Debug.LogError("[BaseHandler] ApiService.Instance is null.");
            ShowFailedOverlay(L("ระบบ API ยังไม่พร้อมใช้งาน", "API service is not ready."));
            yield break;
        }

        if (initialRoom == null)
        {
            Debug.LogError("[BaseHandler] InitialRoom is not assigned.");
            ShowFailedOverlay(L("ยังไม่ได้ตั้งค่า Initial Room", "Initial room is not configured."));
            yield break;
        }

        Debug.Log($"[BaseHandler] Joining room: {initialRoom.name}");

        var task = NetworkLauncher.Instance.JoinInitialRoom(initialRoom);

        while (!task.IsCompleted)
            yield return null;

        Debug.Log("[BaseHandler] JoinInitialRoom completed.");

        if (task.IsFaulted)
        {
            Debug.LogException(task.Exception);
            ShowFailedOverlay(L("เข้าห้องไม่สำเร็จ", "Failed to join room."));
            yield break;
        }

        Debug.Log($"[BaseHandler] JoinInitialRoom result: {task.Result}");

        if (!task.Result)
        {
            Debug.LogError("[BaseHandler] JoinInitialRoom returned false.");
            ShowFailedOverlay(L("ห้องเต็มหรือไม่พร้อมใช้งาน", "Room is full or unavailable."));
            yield break;
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