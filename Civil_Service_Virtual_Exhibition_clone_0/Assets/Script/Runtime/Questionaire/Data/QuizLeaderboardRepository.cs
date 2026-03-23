using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class QuizLeaderboardRepository : MonoBehaviour
{
    private ApiService Api => ApiService.Instance;

    public IEnumerator SubmitResult(
        int score,
        int totalQuestions,
        Action onSuccess,
        Action<string> onFailed,
        string accessToken = null)
    {
        if (Api == null)
        {
            onFailed?.Invoke("ApiService.Instance is null.");
            yield break;
        }

        if (string.IsNullOrWhiteSpace(Api.QuizSubmitUrl))
        {
            onFailed?.Invoke("QuizSubmitUrl is missing.");
            yield break;
        }

        QuizSubmitRequestDto bodyDto = new QuizSubmitRequestDto
        {
            score = score,
            totalQuestions = totalQuestions
        };

        string json = JsonUtility.ToJson(bodyDto);

        using UnityWebRequest request = Api.PostJson(Api.QuizSubmitUrl, json, accessToken);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            onFailed?.Invoke($"Submit failed: {request.error}");
            yield break;
        }

        string raw = request.downloadHandler.text;

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
            onSuccess?.Invoke();
            yield break;
        }

        if (dto != null && !dto.success)
        {
            onFailed?.Invoke(
                string.IsNullOrWhiteSpace(dto.message)
                    ? "Submit returned success = false."
                    : dto.message
            );
            yield break;
        }

        onSuccess?.Invoke();
    }

    public IEnumerator LoadLeaderboard(
        Action<QuizLeaderboardDataDto> onSuccess,
        Action<string> onFailed,
        string accessToken = null)
    {
        if (Api == null)
        {
            onFailed?.Invoke("ApiService.Instance is null.");
            yield break;
        }

        if (string.IsNullOrWhiteSpace(Api.GetQuizLeaderboardUrl))
        {
            onFailed?.Invoke("QuizLeaderboardUrl is missing.");
            yield break;
        }

        using UnityWebRequest request = Api.Get(Api.GetQuizLeaderboardUrl, accessToken);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            onFailed?.Invoke($"Leaderboard GET failed: {request.error}");
            yield break;
        }

        string raw = request.downloadHandler.text;

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