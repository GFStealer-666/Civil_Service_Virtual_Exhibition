using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public enum QuizRequestErrorType
{
    None,
    MissingApiService,
    MissingUrl,
    MissingToken,
    NetworkError,
    HttpError,
    ParseError,
    BackendRejected,
    EmptyResponse
}

[Serializable]
public class QuizSubmitOperationResult
{
    public bool success;
    public QuizRequestErrorType errorType;
    public long statusCode;
    public string message;
    public string rawBody;
    public QuizSubmitResponseDto response;
}

[Serializable]
public class QuizLoadOperationResult
{
    public bool success;
    public QuizRequestErrorType errorType;
    public long statusCode;
    public string message;
    public string rawBody;
    public QuizCurrentResponseDto response;
}

[Serializable]
public class QuizLeaderboardOperationResult
{
    public bool success;
    public QuizRequestErrorType errorType;
    public long statusCode;
    public string message;
    public string rawBody;
    public QuizLeaderboardResponseDto response;
}

public class QuizRepository : MonoBehaviour
{
    private ApiService Api => ApiService.Instance;

    public IEnumerator LoadCurrentQuiz(Action<QuizLoadOperationResult> onCompleted)
    {
        QuizLoadOperationResult result = new QuizLoadOperationResult();

        if (Api == null)
        {
            CompleteQuizLoad(
                result,
                false,
                QuizRequestErrorType.MissingApiService,
                0,
                "ApiService.Instance is null.",
                null,
                null,
                onCompleted
            );
            yield break;
        }

        if (string.IsNullOrWhiteSpace(Api.GetQuizUrl))
        {
            CompleteQuizLoad(
                result,
                false,
                QuizRequestErrorType.MissingUrl,
                0,
                "GetQuizUrl is missing.",
                null,
                null,
                onCompleted
            );
            yield break;
        }

        using UnityWebRequest request = Api.Get(Api.GetQuizUrl);
        yield return request.SendWebRequest();

        long statusCode = request.responseCode;
        string rawBody = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;

        bool hasNetworkFailure =
            request.result == UnityWebRequest.Result.ConnectionError ||
            request.result == UnityWebRequest.Result.DataProcessingError;

        if (hasNetworkFailure)
        {
            CompleteQuizLoad(
                result,
                false,
                QuizRequestErrorType.NetworkError,
                statusCode,
                request.error,
                rawBody,
                null,
                onCompleted
            );
            yield break;
        }

        if (statusCode < 200 || statusCode >= 300)
        {
            CompleteQuizLoad(
                result,
                false,
                QuizRequestErrorType.HttpError,
                statusCode,
                string.IsNullOrWhiteSpace(rawBody) ? request.error : rawBody,
                rawBody,
                null,
                onCompleted
            );
            yield break;
        }

        if (string.IsNullOrWhiteSpace(rawBody))
        {
            CompleteQuizLoad(
                result,
                false,
                QuizRequestErrorType.EmptyResponse,
                statusCode,
                "Quiz API returned empty response.",
                rawBody,
                null,
                onCompleted
            );
            yield break;
        }

        QuizCurrentResponseDto dto = null;

        try
        {
            dto = JsonUtility.FromJson<QuizCurrentResponseDto>(rawBody);
        }
        catch (Exception ex)
        {
            CompleteQuizLoad(
                result,
                false,
                QuizRequestErrorType.ParseError,
                statusCode,
                $"Quiz parse error: {ex.Message}",
                rawBody,
                null,
                onCompleted
            );
            yield break;
        }

        if (dto == null)
        {
            CompleteQuizLoad(
                result,
                false,
                QuizRequestErrorType.ParseError,
                statusCode,
                "Quiz response parsed to null.",
                rawBody,
                null,
                onCompleted
            );
            yield break;
        }

        if (!dto.success || dto.data == null)
        {
            CompleteQuizLoad(
                result,
                false,
                QuizRequestErrorType.BackendRejected,
                statusCode,
                "Quiz response is invalid.",
                rawBody,
                dto,
                onCompleted
            );
            yield break;
        }

        CompleteQuizLoad(
            result,
            true,
            QuizRequestErrorType.None,
            statusCode,
            "Quiz loaded successfully.",
            rawBody,
            dto,
            onCompleted
        );
    }

    public IEnumerator SubmitResult(
        int score,
        Action<QuizSubmitOperationResult> onCompleted,
        string accessTokenOverride = null,
        bool requireToken = true)
    {
        QuizSubmitOperationResult result = new QuizSubmitOperationResult();

        if (Api == null)
        {
            CompleteSubmit(
                result,
                false,
                QuizRequestErrorType.MissingApiService,
                0,
                "ApiService.Instance is null.",
                null,
                null,
                onCompleted
            );
            yield break;
        }

        if (string.IsNullOrWhiteSpace(Api.QuizSubmitUrl))
        {
            CompleteSubmit(
                result,
                false,
                QuizRequestErrorType.MissingUrl,
                0,
                "QuizSubmitUrl is missing.",
                null,
                null,
                onCompleted
            );
            yield break;
        }

        string token = ResolveToken(accessTokenOverride);

        if (requireToken && string.IsNullOrWhiteSpace(token))
        {
            CompleteSubmit(
                result,
                false,
                QuizRequestErrorType.MissingToken,
                0,
                "Quiz submit requires token, but no token was found.",
                null,
                null,
                onCompleted
            );
            yield break;
        }

        QuizSubmitRequestDto bodyDto = new QuizSubmitRequestDto
        {
            score = score
        };

        string json = JsonUtility.ToJson(bodyDto);

        using UnityWebRequest request = Api.PostJson(Api.QuizSubmitUrl, json, token);
        yield return request.SendWebRequest();

        long statusCode = request.responseCode;
        string rawBody = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;

        bool hasNetworkFailure =
            request.result == UnityWebRequest.Result.ConnectionError ||
            request.result == UnityWebRequest.Result.DataProcessingError;

        if (hasNetworkFailure)
        {
            CompleteSubmit(
                result,
                false,
                QuizRequestErrorType.NetworkError,
                statusCode,
                request.error,
                rawBody,
                null,
                onCompleted
            );
            yield break;
        }

        if (statusCode < 200 || statusCode >= 300)
        {
            CompleteSubmit(
                result,
                false,
                QuizRequestErrorType.HttpError,
                statusCode,
                string.IsNullOrWhiteSpace(rawBody) ? request.error : rawBody,
                rawBody,
                null,
                onCompleted
            );
            yield break;
        }

        if (string.IsNullOrWhiteSpace(rawBody))
        {
            CompleteSubmit(
                result,
                false,
                QuizRequestErrorType.EmptyResponse,
                statusCode,
                "Submit API returned empty response.",
                rawBody,
                null,
                onCompleted
            );
            yield break;
        }

        QuizSubmitResponseDto dto = null;

        try
        {
            dto = JsonUtility.FromJson<QuizSubmitResponseDto>(rawBody);
        }
        catch (Exception ex)
        {
            CompleteSubmit(
                result,
                false,
                QuizRequestErrorType.ParseError,
                statusCode,
                $"Submit parse error: {ex.Message}",
                rawBody,
                null,
                onCompleted
            );
            yield break;
        }

        if (dto == null)
        {
            CompleteSubmit(
                result,
                false,
                QuizRequestErrorType.ParseError,
                statusCode,
                "Submit response parsed to null.",
                rawBody,
                null,
                onCompleted
            );
            yield break;
        }

        if (!dto.success || dto.data == null)
        {
            CompleteSubmit(
                result,
                false,
                QuizRequestErrorType.BackendRejected,
                statusCode,
                "Submit response is invalid.",
                rawBody,
                dto,
                onCompleted
            );
            yield break;
        }

        CompleteSubmit(
            result,
            true,
            QuizRequestErrorType.None,
            statusCode,
            "Submit success.",
            rawBody,
            dto,
            onCompleted
        );
    }

    public IEnumerator LoadLeaderboard(Action<QuizLeaderboardOperationResult> onCompleted)
    {
        QuizLeaderboardOperationResult result = new QuizLeaderboardOperationResult();

        if (Api == null)
        {
            CompleteLeaderboard(
                result,
                false,
                QuizRequestErrorType.MissingApiService,
                0,
                "ApiService.Instance is null.",
                null,
                null,
                onCompleted
            );
            yield break;
        }

        if (string.IsNullOrWhiteSpace(Api.GetQuizLeaderboardUrl))
        {
            CompleteLeaderboard(
                result,
                false,
                QuizRequestErrorType.MissingUrl,
                0,
                "GetQuizLeaderboardUrl is missing.",
                null,
                null,
                onCompleted
            );
            yield break;
        }

        using UnityWebRequest request = Api.Get(Api.GetQuizLeaderboardUrl);
        yield return request.SendWebRequest();

        long statusCode = request.responseCode;
        string rawBody = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;

        bool hasNetworkFailure =
            request.result == UnityWebRequest.Result.ConnectionError ||
            request.result == UnityWebRequest.Result.DataProcessingError;

        if (hasNetworkFailure)
        {
            CompleteLeaderboard(
                result,
                false,
                QuizRequestErrorType.NetworkError,
                statusCode,
                request.error,
                rawBody,
                null,
                onCompleted
            );
            yield break;
        }

        if (statusCode < 200 || statusCode >= 300)
        {
            CompleteLeaderboard(
                result,
                false,
                QuizRequestErrorType.HttpError,
                statusCode,
                string.IsNullOrWhiteSpace(rawBody) ? request.error : rawBody,
                rawBody,
                null,
                onCompleted
            );
            yield break;
        }

        if (string.IsNullOrWhiteSpace(rawBody))
        {
            CompleteLeaderboard(
                result,
                false,
                QuizRequestErrorType.EmptyResponse,
                statusCode,
                "Leaderboard API returned empty response.",
                rawBody,
                null,
                onCompleted
            );
            yield break;
        }

        QuizLeaderboardResponseDto dto = null;

        try
        {
            dto = JsonUtility.FromJson<QuizLeaderboardResponseDto>(rawBody);
        }
        catch (Exception ex)
        {
            CompleteLeaderboard(
                result,
                false,
                QuizRequestErrorType.ParseError,
                statusCode,
                $"Leaderboard parse error: {ex.Message}",
                rawBody,
                null,
                onCompleted
            );
            yield break;
        }

        if (dto == null)
        {
            CompleteLeaderboard(
                result,
                false,
                QuizRequestErrorType.ParseError,
                statusCode,
                "Leaderboard response parsed to null.",
                rawBody,
                null,
                onCompleted
            );
            yield break;
        }

        if (!dto.success || dto.data == null)
        {
            CompleteLeaderboard(
                result,
                false,
                QuizRequestErrorType.BackendRejected,
                statusCode,
                "Leaderboard response is invalid.",
                rawBody,
                dto,
                onCompleted
            );
            yield break;
        }

        CompleteLeaderboard(
            result,
            true,
            QuizRequestErrorType.None,
            statusCode,
            "Leaderboard loaded successfully.",
            rawBody,
            dto,
            onCompleted
        );
    }

    private string ResolveToken(string overrideToken)
    {
        if (!string.IsNullOrWhiteSpace(overrideToken))
            return overrideToken.Trim();

        if (LocalPlayerData.Instance != null &&
            !string.IsNullOrWhiteSpace(LocalPlayerData.Instance.PlayerToken))
        {
            return LocalPlayerData.Instance.PlayerToken.Trim();
        }

        return null;
    }

    private void CompleteSubmit(
        QuizSubmitOperationResult result,
        bool success,
        QuizRequestErrorType errorType,
        long statusCode,
        string message,
        string rawBody,
        QuizSubmitResponseDto response,
        Action<QuizSubmitOperationResult> onCompleted)
    {
        result.success = success;
        result.errorType = errorType;
        result.statusCode = statusCode;
        result.message = message;
        result.rawBody = rawBody;
        result.response = response;

        Debug.Log(
            $"[QuizRepository][Submit] success={result.success}, " +
            $"errorType={result.errorType}, " +
            $"statusCode={result.statusCode}, " +
            $"message={result.message}"
        );

        onCompleted?.Invoke(result);
    }

    private void CompleteQuizLoad(
        QuizLoadOperationResult result,
        bool success,
        QuizRequestErrorType errorType,
        long statusCode,
        string message,
        string rawBody,
        QuizCurrentResponseDto response,
        Action<QuizLoadOperationResult> onCompleted)
    {
        result.success = success;
        result.errorType = errorType;
        result.statusCode = statusCode;
        result.message = message;
        result.rawBody = rawBody;
        result.response = response;

        Debug.Log(
            $"[QuizRepository][LoadQuiz] success={result.success}, " +
            $"errorType={result.errorType}, " +
            $"statusCode={result.statusCode}, " +
            $"message={result.message}"
        );

        onCompleted?.Invoke(result);
    }

    private void CompleteLeaderboard(
        QuizLeaderboardOperationResult result,
        bool success,
        QuizRequestErrorType errorType,
        long statusCode,
        string message,
        string rawBody,
        QuizLeaderboardResponseDto response,
        Action<QuizLeaderboardOperationResult> onCompleted)
    {
        result.success = success;
        result.errorType = errorType;
        result.statusCode = statusCode;
        result.message = message;
        result.rawBody = rawBody;
        result.response = response;

        Debug.Log(
            $"[QuizRepository][Leaderboard] success={result.success}, " +
            $"errorType={result.errorType}, " +
            $"statusCode={result.statusCode}, " +
            $"message={result.message}"
        );

        onCompleted?.Invoke(result);
    }
}