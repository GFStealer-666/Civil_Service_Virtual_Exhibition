using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public abstract class BaseHandler : MonoBehaviour
{
    [Header("Shared")]
    [SerializeField] protected ApiConfig api;
    [SerializeField] protected StatusOverlay overlay;

    /// <summary>
    /// Runs a POST, drives the overlay states, then calls back.
    /// onSuccess receives the raw JSON string.
    /// onError   receives the user-facing message.
    /// </summary>
    protected IEnumerator PostRequest(
        string url,
        string jsonBody,
        Action<string> onSuccess,
        Action<string> onError)
    {
        overlay.ShowLoading();

        using var req = APIHelper.PostJson(url, jsonBody);
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
    protected void EnterMainScene(string name, string gender, string department, bool isGuest)
    {
        var data = LocalPlayerData.Instance;
        data.PlayerName = name;
        data.Organization = department;
        data.IsGuest = isGuest;
        data.Gender = gender == "female" ? PlayerGender.Female : PlayerGender.Male;
        data.RandomizeAppearance();

        overlay.ShowSuccess(() =>
        {
            StartCoroutine(StartNetworkFlow());
        });
    }

    private IEnumerator StartNetworkFlow()
    {
        if (NetworkLauncher.Instance == null)
        {
            Debug.LogError("[BaseHandler] NetworkLauncher.Instance is null.");
            overlay.ShowError("Network system is not ready.");
            yield break;
        }

        if (api == null)
        {
            Debug.LogError("[BaseHandler] ApiConfig is not assigned.");
            overlay.ShowError("API config is missing.");
            yield break;
        }

        if (api.initialRoom == null)
        {
            Debug.LogError("[BaseHandler] ApiConfig.initialRoom is not assigned.");
            overlay.ShowError("Initial room is not configured.");
            yield break;
        }

        var task = NetworkLauncher.Instance.JoinInitialRoom(api.initialRoom);

        while (!task.IsCompleted)
            yield return null;

        if (task.IsFaulted)
        {
            Debug.LogException(task.Exception);
            overlay.ShowError("Failed to join room.");
            yield break;
        }

        if (!task.Result)
        {
            Debug.LogError("[BaseHandler] JoinInitialRoom returned false.");
            overlay.ShowError("Room is full or unavailable.");
            yield break;
        }
    }
}