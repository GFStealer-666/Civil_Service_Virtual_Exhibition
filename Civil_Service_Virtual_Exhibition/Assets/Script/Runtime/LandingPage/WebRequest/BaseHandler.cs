// BaseHandler.cs — shared logic every handler inherits
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public abstract class BaseHandler : MonoBehaviour
{
    [Header("Shared")]
    [SerializeField] protected ApiConfig    api;
    [SerializeField] protected StatusOverlay overlay;

    // ── Helpers handlers call ───────────────────────────────────

    /// <summary>
    /// Runs a POST, drives the overlay states, then calls back.
    /// onSuccess receives the raw JSON string.
    /// onError   receives the user-facing message.
    /// </summary>
    protected IEnumerator PostRequest(
        string          url,
        string          jsonBody,
        Action<string>  onSuccess,
        Action<string>  onError)
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

    /// <summary>Loads main scene after storing player data.</summary>
    protected void EnterMainScene(string name, string gender, string department, bool isGuest)
    {
        var data          = LocalPlayerData.Instance;
        data.PlayerName   = name;
        data.Organization = department;
        data.IsGuest      = isGuest;
        data.Gender       = gender == "female" ? PlayerGender.Female : PlayerGender.Male;

        overlay.ShowSuccess(() =>
            UnityEngine.SceneManagement.SceneManager.LoadScene(api.mainSceneName));
    }
}