using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class QuizLeaderboardRepository : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ApiConfig apiConfig;

    public IEnumerator SubmitResult(
        int score,
        int totalQuestions,
        Action onSuccess,
        Action<string> onFailed)
    {
        if (apiConfig == null || string.IsNullOrWhiteSpace(apiConfig.QuizSubmitUrl))
        {
            onFailed?.Invoke("QuizSubmitUrl is missing in ApiConfig.");
            yield break;
        }

        QuizSubmitRequestDto bodyDto = new QuizSubmitRequestDto
        {
            score = score,
            totalQuestions = totalQuestions
        };

        string json = JsonUtility.ToJson(bodyDto);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        using UnityWebRequest req = new UnityWebRequest(apiConfig.QuizSubmitUrl, UnityWebRequest.kHttpVerbPOST);
        req.uploadHandler = new UploadHandlerRaw(bodyRaw);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            onFailed?.Invoke($"Submit failed: {req.error}");
            yield break;
        }

        string raw = req.downloadHandler.text;

        if (string.IsNullOrWhiteSpace(raw))
        {
            onSuccess?.Invoke();
            yield break;
        }

        QuizSubmitResponseDto dto = null;

        try
        {
            dto = JsonUtility.FromJson<QuizSubmitResponseDto>(raw);
        }
        catch
        {
            // If backend returns some other shape but HTTP is success,
            // we can still treat submission as success.
            onSuccess?.Invoke();
            yield break;
        }

        if (dto != null && !dto.success)
        {
            onFailed?.Invoke(string.IsNullOrWhiteSpace(dto.message) ? "Submit returned success = false." : dto.message);
            yield break;
        }

        onSuccess?.Invoke();
    }

    public IEnumerator LoadLeaderboard(
        Action<QuizLeaderboardDataDto> onSuccess,
        Action<string> onFailed)
    {
        if (apiConfig == null || string.IsNullOrWhiteSpace(apiConfig.GetQuizLeaderboardUrl))
        {
            onFailed?.Invoke("QuizLeaderboardUrl is missing in ApiConfig.");
            yield break;
        }

        using UnityWebRequest req = UnityWebRequest.Get(apiConfig.GetQuizLeaderboardUrl);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            onFailed?.Invoke($"Leaderboard GET failed: {req.error}");
            yield break;
        }

        string raw = req.downloadHandler.text;

        if (string.IsNullOrWhiteSpace(raw))
        {
            onFailed?.Invoke("Leaderboard GET returned empty response.");
            yield break;
        }

        QuizLeaderboardResponseDto dto = null;

        try
        {
            dto = JsonUtility.FromJson<QuizLeaderboardResponseDto>(raw);
        }
        catch (Exception ex)
        {
            onFailed?.Invoke($"Leaderboard parse error: {ex.Message}");
            yield break;
        }

        if (dto == null || !dto.success || dto.data == null)
        {
            onFailed?.Invoke("Leaderboard response is invalid.");
            yield break;
        }

        onSuccess?.Invoke(dto.data);
    }
}