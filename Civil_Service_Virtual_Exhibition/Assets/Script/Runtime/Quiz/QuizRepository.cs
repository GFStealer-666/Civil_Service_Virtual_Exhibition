using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System.Globalization;
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
public class QuizBasicMessageResponseDto
{
    public bool success;
    public string message;
}

[Serializable]
public class QuizCheckOperationResult
{
    public bool success;
    public QuizRequestErrorType errorType;
    public long statusCode;
    public string message;
    public string rawBody;
    public QuizCheckResponseDto response;
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

[Serializable]
public class QuizMeOperationResult
{
    public bool success;
    public QuizRequestErrorType errorType;
    public long statusCode;
    public string message;
    public string rawBody;
    public QuizMeResponseDto response;
}

public class QuizRepository : MonoBehaviour
{
    public static QuizRepository Instance { get; private set; }

    [Header("Load")]
    [SerializeField] private bool preloadOnStart = true;
    [SerializeField] private bool dontDestroyOnLoad = true;

    public bool IsLoading { get; private set; }
    public bool HasData => _cachedCurrentQuiz != null && _cachedCurrentQuiz.data != null;
    public string LastError { get; private set; }

    public event Action OnCurrentQuizLoaded;
    public event Action<string> OnCurrentQuizLoadFailed;

    private ApiService Api => ApiService.Instance;

    private QuizCurrentResponseDto _cachedCurrentQuiz;
    private Coroutine _loadRoutine;

    public QuizCurrentResponseDto GetCachedCurrentQuiz()
    {
        return _cachedCurrentQuiz;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (dontDestroyOnLoad)
            DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (preloadOnStart)
            Initialize();
    }

    public void Initialize(bool forceRefresh = false)
    {
        if (IsLoading)
            return;

        if (!forceRefresh && HasData)
        {
            Debug.Log("[QuizRepository] Using cached current quiz.");
            OnCurrentQuizLoaded?.Invoke();
            return;
        }

        if (_loadRoutine != null)
            StopCoroutine(_loadRoutine);

        _loadRoutine = StartCoroutine(LoadCurrentQuizRoutine());
    }

    public void Refresh()
    {
        Initialize(true);
    }

    private IEnumerator LoadCurrentQuizRoutine()
    {
        IsLoading = true;
        LastError = string.Empty;

        yield return StartCoroutine(LoadCurrentQuiz(result =>
        {
            if (result.success && result.response != null && result.response.data != null)
            {
                _cachedCurrentQuiz = result.response;
                LastError = string.Empty;

                Debug.Log("[QuizRepository] Current quiz loaded successfully.");
                OnCurrentQuizLoaded?.Invoke();
            }
            else
            {
                _cachedCurrentQuiz = null;
                LastError = string.IsNullOrWhiteSpace(result.message)
                    ? "Failed to load current quiz."
                    : result.message;

                Debug.LogError($"[QuizRepository] Current quiz load failed: {LastError}");
                OnCurrentQuizLoadFailed?.Invoke(LastError);
            }
        }));

        IsLoading = false;
        _loadRoutine = null;
    }

    public IEnumerator LoadCurrentQuiz(Action<QuizLoadOperationResult> onCompleted)
    {
        Debug.Log("[QuizRepository] LoadCurrentQuiz called.");

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

        Debug.Log($"[QuizRepository] Downloading current quiz from: {Api.GetQuizUrl}");

        using UnityWebRequest request = Api.Get(Api.GetQuizUrl);
        yield return request.SendWebRequest();

        long statusCode = request.responseCode;
        string rawBody = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;

        if (HasNetworkFailure(request))
        {
            CompleteQuizLoad(
                result,
                false,
                QuizRequestErrorType.NetworkError,
                statusCode,
                ResolveTransportMessage(request.error, rawBody, "Failed to load quiz."),
                rawBody,
                null,
                onCompleted
            );
            yield break;
        }

        if (!IsSuccessStatusCode(statusCode))
        {
            CompleteQuizLoad(
                result,
                false,
                QuizRequestErrorType.HttpError,
                statusCode,
                ResolveTransportMessage(request.error, rawBody, "Failed to load quiz."),
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
                ResolveBackendMessage(dto.message, rawBody, "Quiz response is invalid."),
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

    public IEnumerator CheckStatus(
        Action<QuizCheckOperationResult> onCompleted,
        string accessTokenOverride = null,
        bool requireToken = true)
    {
        QuizCheckOperationResult result = new QuizCheckOperationResult();

        if (Api == null)
        {
            CompleteCheck(result, false, QuizRequestErrorType.MissingApiService, 0, "ApiService.Instance is null.", null, null, onCompleted);
            yield break;
        }

        if (string.IsNullOrWhiteSpace(Api.CheckQuizStatusUrl))
        {
            CompleteCheck(result, false, QuizRequestErrorType.MissingUrl, 0, "CheckQuizStatusUrl is missing.", null, null, onCompleted);
            yield break;
        }

        string token = ResolveToken(accessTokenOverride);

        if (requireToken && string.IsNullOrWhiteSpace(token))
        {
            CompleteCheck(result, false, QuizRequestErrorType.MissingToken, 0, "Quiz check requires token, but no token was found.", null, null, onCompleted);
            yield break;
        }

        using UnityWebRequest request = Api.Get(Api.CheckQuizStatusUrl, token);
        yield return request.SendWebRequest();

        long statusCode = request.responseCode;
        string rawBody = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;

        if (HasNetworkFailure(request))
        {
            CompleteCheck(result, false, QuizRequestErrorType.NetworkError, statusCode, ResolveTransportMessage(request.error, rawBody, "Failed to check quiz status."), rawBody, null, onCompleted);
            yield break;
        }

        if (!IsSuccessStatusCode(statusCode))
        {
            CompleteCheck(result, false, QuizRequestErrorType.HttpError, statusCode, ResolveTransportMessage(request.error, rawBody, "Failed to check quiz status."), rawBody, null, onCompleted);
            yield break;
        }

        if (string.IsNullOrWhiteSpace(rawBody))
        {
            CompleteCheck(result, false, QuizRequestErrorType.EmptyResponse, statusCode, "Quiz check API returned empty response.", rawBody, null, onCompleted);
            yield break;
        }

        QuizCheckResponseDto dto = null;

        try
        {
            dto = JsonUtility.FromJson<QuizCheckResponseDto>(rawBody);
        }
        catch (Exception ex)
        {
            CompleteCheck(result, false, QuizRequestErrorType.ParseError, statusCode, $"Quiz check parse error: {ex.Message}", rawBody, null, onCompleted);
            yield break;
        }

        if (dto == null)
        {
            CompleteCheck(result, false, QuizRequestErrorType.ParseError, statusCode, "Quiz check response parsed to null.", rawBody, null, onCompleted);
            yield break;
        }

        if (!dto.success || dto.data == null)
        {
            CompleteCheck(result, false, QuizRequestErrorType.BackendRejected, statusCode, ResolveBackendMessage(dto.message, rawBody, "Quiz check response is invalid."), rawBody, dto, onCompleted);
            yield break;
        }

        CompleteCheck(result, true, QuizRequestErrorType.None, statusCode, "Quiz check loaded successfully.", rawBody, dto, onCompleted);
    }

    public IEnumerator SubmitResult(
    float score,
    Action<QuizSubmitOperationResult> onCompleted,
    string accessTokenOverride = null,
    bool requireToken = true)
    {
        QuizSubmitOperationResult result = new QuizSubmitOperationResult();

        if (Api == null)
        {
            CompleteSubmit(result, false, QuizRequestErrorType.MissingApiService, 0, "ApiService.Instance is null.", null, null, onCompleted);
            yield break;
        }

        if (string.IsNullOrWhiteSpace(Api.QuizSubmitUrl))
        {
            CompleteSubmit(result, false, QuizRequestErrorType.MissingUrl, 0, "QuizSubmitUrl is missing.", null, null, onCompleted);
            yield break;
        }

        string token = ResolveToken(accessTokenOverride);

        if (requireToken && string.IsNullOrWhiteSpace(token))
        {
            CompleteSubmit(result, false, QuizRequestErrorType.MissingToken, 0, "Quiz submit requires token, but no token was found.", null, null, onCompleted);
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

        if (HasNetworkFailure(request))
        {
            CompleteSubmit(result, false, QuizRequestErrorType.NetworkError, statusCode, ResolveTransportMessage(request.error, rawBody, "Failed to submit quiz score."), rawBody, null, onCompleted);
            yield break;
        }

        if (!IsSuccessStatusCode(statusCode))
        {
            CompleteSubmit(result, false, QuizRequestErrorType.HttpError, statusCode, ResolveTransportMessage(request.error, rawBody, "Failed to submit quiz score."), rawBody, null, onCompleted);
            yield break;
        }

        if (string.IsNullOrWhiteSpace(rawBody))
        {
            CompleteSubmit(result, false, QuizRequestErrorType.EmptyResponse, statusCode, "Submit API returned empty response.", rawBody, null, onCompleted);
            yield break;
        }

        QuizSubmitResponseDto dto = null;

        try
        {
            dto = JsonUtility.FromJson<QuizSubmitResponseDto>(rawBody);
        }
        catch (Exception ex)
        {
            CompleteSubmit(result, false, QuizRequestErrorType.ParseError, statusCode, $"Submit parse error: {ex.Message}", rawBody, null, onCompleted);
            yield break;
        }

        if (dto == null)
        {
            CompleteSubmit(result, false, QuizRequestErrorType.ParseError, statusCode, "Submit response parsed to null.", rawBody, null, onCompleted);
            yield break;
        }

        if (!dto.success || dto.data == null)
        {
            CompleteSubmit(result, false, QuizRequestErrorType.BackendRejected, statusCode, ResolveBackendMessage(dto.message, rawBody, "Submit response is invalid."), rawBody, dto, onCompleted);
            yield break;
        }

        CompleteSubmit(result, true, QuizRequestErrorType.None, statusCode, "Submit success.", rawBody, dto, onCompleted);
    }

    public IEnumerator LoadLeaderboard(
        Action<QuizLeaderboardOperationResult> onCompleted,
        string accessTokenOverride = null,
        bool requireToken = false)
    {
        QuizLeaderboardOperationResult result = new QuizLeaderboardOperationResult();

        if (Api == null)
        {
            CompleteLeaderboard(result, false, QuizRequestErrorType.MissingApiService, 0, "ApiService.Instance is null.", null, null, onCompleted);
            yield break;
        }

        if (string.IsNullOrWhiteSpace(Api.GetQuizLeaderboardUrl))
        {
            CompleteLeaderboard(result, false, QuizRequestErrorType.MissingUrl, 0, "GetQuizLeaderboardUrl is missing.", null, null, onCompleted);
            yield break;
        }

        string token = ResolveToken(accessTokenOverride);

        if (requireToken && string.IsNullOrWhiteSpace(token))
        {
            CompleteLeaderboard(result, false, QuizRequestErrorType.MissingToken, 0, "Quiz leaderboard requires token, but no token was found.", null, null, onCompleted);
            yield break;
        }

        using UnityWebRequest request = Api.Get(Api.GetQuizLeaderboardUrl, token);
        yield return request.SendWebRequest();

        long statusCode = request.responseCode;
        string rawBody = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;

        if (HasNetworkFailure(request))
        {
            CompleteLeaderboard(result, false, QuizRequestErrorType.NetworkError, statusCode, ResolveTransportMessage(request.error, rawBody, "Failed to load leaderboard."), rawBody, null, onCompleted);
            yield break;
        }

        if (!IsSuccessStatusCode(statusCode))
        {
            CompleteLeaderboard(result, false, QuizRequestErrorType.HttpError, statusCode, ResolveTransportMessage(request.error, rawBody, "Failed to load leaderboard."), rawBody, null, onCompleted);
            yield break;
        }

        if (string.IsNullOrWhiteSpace(rawBody))
        {
            CompleteLeaderboard(result, false, QuizRequestErrorType.EmptyResponse, statusCode, "Leaderboard API returned empty response.", rawBody, null, onCompleted);
            yield break;
        }

        QuizLeaderboardResponseDto dto = null;

        try
        {
            dto = JsonUtility.FromJson<QuizLeaderboardResponseDto>(rawBody);
        }
        catch (Exception ex)
        {
            CompleteLeaderboard(result, false, QuizRequestErrorType.ParseError, statusCode, $"Leaderboard parse error: {ex.Message}", rawBody, null, onCompleted);
            yield break;
        }

        if (dto == null)
        {
            CompleteLeaderboard(result, false, QuizRequestErrorType.ParseError, statusCode, "Leaderboard response parsed to null.", rawBody, null, onCompleted);
            yield break;
        }

        PopulateLeaderboardOptionalFields(dto, rawBody);

        if (!dto.success || dto.data == null)
        {
            CompleteLeaderboard(result, false, QuizRequestErrorType.BackendRejected, statusCode, ResolveBackendMessage(dto.message, rawBody, "Leaderboard response is invalid."), rawBody, dto, onCompleted);
            yield break;
        }

        CompleteLeaderboard(result, true, QuizRequestErrorType.None, statusCode, "Leaderboard loaded successfully.", rawBody, dto, onCompleted);
    }

    public IEnumerator LoadMe(
        Action<QuizMeOperationResult> onCompleted,
        string accessTokenOverride = null,
        bool requireToken = true)
    {
        QuizMeOperationResult result = new QuizMeOperationResult();

        if (Api == null)
        {
            CompleteMe(result, false, QuizRequestErrorType.MissingApiService, 0, "ApiService.Instance is null.", null, null, onCompleted);
            yield break;
        }

        if (string.IsNullOrWhiteSpace(Api.GetQuizMe))
        {
            CompleteMe(result, false, QuizRequestErrorType.MissingUrl, 0, "GetQuizMe is missing.", null, null, onCompleted);
            yield break;
        }

        string token = ResolveToken(accessTokenOverride);

        if (requireToken && string.IsNullOrWhiteSpace(token))
        {
            CompleteMe(result, false, QuizRequestErrorType.MissingToken, 0, "Quiz me requires token, but no token was found.", null, null, onCompleted);
            yield break;
        }

        using UnityWebRequest request = Api.Get(Api.GetQuizMe, token);
        yield return request.SendWebRequest();

        long statusCode = request.responseCode;
        string rawBody = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;

        if (HasNetworkFailure(request))
        {
            CompleteMe(result, false, QuizRequestErrorType.NetworkError, statusCode, ResolveTransportMessage(request.error, rawBody, "Failed to load quiz stats."), rawBody, null, onCompleted);
            yield break;
        }

        if (!IsSuccessStatusCode(statusCode))
        {
            CompleteMe(result, false, QuizRequestErrorType.HttpError, statusCode, ResolveTransportMessage(request.error, rawBody, "Failed to load quiz stats."), rawBody, null, onCompleted);
            yield break;
        }

        if (string.IsNullOrWhiteSpace(rawBody))
        {
            CompleteMe(result, false, QuizRequestErrorType.EmptyResponse, statusCode, "Quiz me API returned empty response.", rawBody, null, onCompleted);
            yield break;
        }

        QuizMeResponseDto dto = null;

        try
        {
            dto = JsonUtility.FromJson<QuizMeResponseDto>(rawBody);
        }
        catch (Exception ex)
        {
            CompleteMe(result, false, QuizRequestErrorType.ParseError, statusCode, $"Quiz me parse error: {ex.Message}", rawBody, null, onCompleted);
            yield break;
        }

        if (dto == null)
        {
            CompleteMe(result, false, QuizRequestErrorType.ParseError, statusCode, "Quiz me response parsed to null.", rawBody, null, onCompleted);
            yield break;
        }

        PopulateMeOptionalFields(dto, rawBody);

        if (!dto.success || dto.data == null)
        {
            CompleteMe(result, false, QuizRequestErrorType.BackendRejected, statusCode, ResolveBackendMessage(dto.message, rawBody, "Quiz me response is invalid."), rawBody, dto, onCompleted);
            yield break;
        }

        CompleteMe(result, true, QuizRequestErrorType.None, statusCode, "Quiz me loaded successfully.", rawBody, dto, onCompleted);
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

    private static bool HasNetworkFailure(UnityWebRequest request)
    {
        return request.result == UnityWebRequest.Result.ConnectionError ||
               request.result == UnityWebRequest.Result.DataProcessingError;
    }

    private static bool IsSuccessStatusCode(long statusCode)
    {
        return statusCode >= 200 && statusCode < 300;
    }

    private string ResolveTransportMessage(string requestError, string rawBody, string fallback)
    {
        string bodyMessage = ExtractMessageFromRawBody(rawBody);

        if (!string.IsNullOrWhiteSpace(bodyMessage))
            return bodyMessage;

        if (!string.IsNullOrWhiteSpace(requestError))
            return requestError;

        if (!string.IsNullOrWhiteSpace(rawBody))
            return rawBody;

        return fallback;
    }

    private string ResolveBackendMessage(string dtoMessage, string rawBody, string fallback)
    {
        if (!string.IsNullOrWhiteSpace(dtoMessage))
            return dtoMessage;

        string bodyMessage = ExtractMessageFromRawBody(rawBody);
        if (!string.IsNullOrWhiteSpace(bodyMessage))
            return bodyMessage;

        return fallback;
    }

    private string ExtractMessageFromRawBody(string rawBody)
    {
        if (string.IsNullOrWhiteSpace(rawBody))
            return null;

        try
        {
            QuizBasicMessageResponseDto dto = JsonUtility.FromJson<QuizBasicMessageResponseDto>(rawBody);
            return dto != null ? dto.message : null;
        }
        catch
        {
            return null;
        }
    }

    private void PopulateLeaderboardOptionalFields(QuizLeaderboardResponseDto dto, string rawBody)
    {
        if (dto == null || dto.data == null || dto.data.player == null || string.IsNullOrWhiteSpace(rawBody))
            return;

        string playerJson = ExtractObjectJson(rawBody, "\"player\"");
        if (string.IsNullOrWhiteSpace(playerJson))
            return;

        dto.data.player.setScores = ParseSetScores(playerJson);
    }

    private void PopulateMeOptionalFields(QuizMeResponseDto dto, string rawBody)
    {
        if (dto == null || dto.data == null || string.IsNullOrWhiteSpace(rawBody))
            return;

        string dataJson = ExtractObjectJson(rawBody, "\"data\"");
        if (string.IsNullOrWhiteSpace(dataJson))
            return;

        if (TryReadNullableFloatProperty(dataJson, "\"totalScore\"", out float totalScore, out bool hasTotalScore))
        {
            dto.data.hasTotalScore = hasTotalScore;
            if (hasTotalScore)
                dto.data.totalScore = totalScore;
        }

        if (TryReadNullableIntProperty(dataJson, "\"rank\"", out int rank, out bool hasRank))
        {
            dto.data.hasRank = hasRank;
            if (hasRank)
                dto.data.rank = rank;
        }

        dto.data.setScores = ParseSetScores(dataJson);
    }

    private QuizSetScoresDto ParseSetScores(string sourceJson)
    {
        QuizSetScoresDto result = new QuizSetScoresDto();

        if (string.IsNullOrWhiteSpace(sourceJson))
            return result;

        string setScoresJson = ExtractObjectJson(sourceJson, "\"setScores\"");
        if (string.IsNullOrWhiteSpace(setScoresJson))
            return result;

        if (TryReadNullableFloatProperty(setScoresJson, "\"1\"", out float set1, out bool hasSet1) && hasSet1)
            result.set1 = set1;

        if (TryReadNullableFloatProperty(setScoresJson, "\"2\"", out float set2, out bool hasSet2) && hasSet2)
            result.set2 = set2;

        if (TryReadNullableFloatProperty(setScoresJson, "\"3\"", out float set3, out bool hasSet3) && hasSet3)
            result.set3 = set3;

        if (TryReadNullableFloatProperty(setScoresJson, "\"4\"", out float set4, out bool hasSet4) && hasSet4)
            result.set4 = set4;

        return result;
    }

    private bool TryReadNullableFloatProperty(
    string sourceJson,
    string propertyName,
    out float value,
    out bool hasValue)
    {
        value = 0f;
        hasValue = false;

        if (string.IsNullOrWhiteSpace(sourceJson) || string.IsNullOrWhiteSpace(propertyName))
            return false;

        int propertyIndex = sourceJson.IndexOf(propertyName, StringComparison.Ordinal);
        if (propertyIndex < 0)
            return false;

        int colonIndex = sourceJson.IndexOf(':', propertyIndex);
        if (colonIndex < 0)
            return false;

        int cursor = colonIndex + 1;
        while (cursor < sourceJson.Length && char.IsWhiteSpace(sourceJson[cursor]))
            cursor++;

        if (cursor >= sourceJson.Length)
            return false;

        if (sourceJson.IndexOf("null", cursor, StringComparison.Ordinal) == cursor)
        {
            hasValue = false;
            return true;
        }

        int start = cursor;

        if (sourceJson[cursor] == '-')
            cursor++;

        bool hasDigit = false;

        while (cursor < sourceJson.Length && char.IsDigit(sourceJson[cursor]))
        {
            hasDigit = true;
            cursor++;
        }

        if (cursor < sourceJson.Length && sourceJson[cursor] == '.')
        {
            cursor++;

            while (cursor < sourceJson.Length && char.IsDigit(sourceJson[cursor]))
            {
                hasDigit = true;
                cursor++;
            }
        }

        if (!hasDigit)
            return false;

        string numberText = sourceJson.Substring(start, cursor - start);

        if (!float.TryParse(numberText, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
            return false;

        hasValue = true;
        return true;
    }
    private bool TryReadNullableIntProperty(
        string sourceJson,
        string propertyName,
        out int value,
        out bool hasValue)
    {
        value = 0;
        hasValue = false;

        if (string.IsNullOrWhiteSpace(sourceJson) || string.IsNullOrWhiteSpace(propertyName))
            return false;

        int propertyIndex = sourceJson.IndexOf(propertyName, StringComparison.Ordinal);
        if (propertyIndex < 0)
            return false;

        int colonIndex = sourceJson.IndexOf(':', propertyIndex);
        if (colonIndex < 0)
            return false;

        int cursor = colonIndex + 1;
        while (cursor < sourceJson.Length && char.IsWhiteSpace(sourceJson[cursor]))
            cursor++;

        if (cursor >= sourceJson.Length)
            return false;

        if (sourceJson.IndexOf("null", cursor, StringComparison.Ordinal) == cursor)
        {
            hasValue = false;
            return true;
        }

        int start = cursor;
        if (sourceJson[cursor] == '-')
            cursor++;

        while (cursor < sourceJson.Length && char.IsDigit(sourceJson[cursor]))
            cursor++;

        if (cursor <= start)
            return false;

        string numberText = sourceJson.Substring(start, cursor - start);
        if (!int.TryParse(numberText, out value))
            return false;

        hasValue = true;
        return true;
    }
    private string ExtractObjectJson(string sourceJson, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(sourceJson) || string.IsNullOrWhiteSpace(propertyName))
            return null;

        int propertyIndex = sourceJson.IndexOf(propertyName, StringComparison.Ordinal);
        if (propertyIndex < 0)
            return null;

        int colonIndex = sourceJson.IndexOf(':', propertyIndex);
        if (colonIndex < 0)
            return null;

        int startIndex = -1;
        for (int i = colonIndex + 1; i < sourceJson.Length; i++)
        {
            char ch = sourceJson[i];

            if (char.IsWhiteSpace(ch))
                continue;

            if (ch == '{')
            {
                startIndex = i;
                break;
            }

            return null;
        }

        if (startIndex < 0)
            return null;

        int depth = 0;

        for (int i = startIndex; i < sourceJson.Length; i++)
        {
            if (sourceJson[i] == '{')
                depth++;

            if (sourceJson[i] == '}')
            {
                depth--;

                if (depth == 0)
                    return sourceJson.Substring(startIndex, i - startIndex + 1);
            }
        }

        return null;
    }

    private void CompleteCheck(
        QuizCheckOperationResult result,
        bool success,
        QuizRequestErrorType errorType,
        long statusCode,
        string message,
        string rawBody,
        QuizCheckResponseDto response,
        Action<QuizCheckOperationResult> onCompleted)
    {
        result.success = success;
        result.errorType = errorType;
        result.statusCode = statusCode;
        result.message = message;
        result.rawBody = rawBody;
        result.response = response;

        Debug.Log(
            $"[QuizRepository][Check] success={result.success}, " +
            $"errorType={result.errorType}, " +
            $"statusCode={result.statusCode}, " +
            $"message={result.message}"
        );

        onCompleted?.Invoke(result);
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

    private void CompleteMe(
        QuizMeOperationResult result,
        bool success,
        QuizRequestErrorType errorType,
        long statusCode,
        string message,
        string rawBody,
        QuizMeResponseDto response,
        Action<QuizMeOperationResult> onCompleted)
    {
        result.success = success;
        result.errorType = errorType;
        result.statusCode = statusCode;
        result.message = message;
        result.rawBody = rawBody;
        result.response = response;

        Debug.Log(
            $"[QuizRepository][Me] success={result.success}, " +
            $"errorType={result.errorType}, " +
            $"statusCode={result.statusCode}, " +
            $"message={result.message}"
        );

        onCompleted?.Invoke(result);
    }
}