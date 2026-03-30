using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class QuizQuestionRepository : MonoBehaviour
{
    public static QuizQuestionRepository Instance { get; private set; }

    [Header("References")]
    [SerializeField] private QuizGameConfigSO fallbackConfig;

    [Header("Load")]
    [SerializeField] private bool preloadOnStart = true;
    [SerializeField] private bool dontDestroyOnLoad = true;

    [Header("Cache")]
    [SerializeField] private int refreshAfterHours = 24;

    private const string CachePlayerPrefsKey = "quiz_cache_envelope_v1";

    private ApiService Api => ApiService.Instance;

    public bool IsLoading { get; private set; }
    public bool HasData => _cachedQuestions != null && _cachedQuestions.Count > 0;
    public string LastError { get; private set; }

    public event Action OnQuestionsLoaded;
    public event Action<string> OnQuestionsLoadFailed;

    private readonly List<QuizSessionQuestion> _cachedQuestions = new List<QuizSessionQuestion>();
    private Coroutine _loadRoutine;

    public IReadOnlyList<QuizSessionQuestion> CachedQuestions => _cachedQuestions;

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

    public void Initialize(bool forceRefresh = false, string accessToken = null)
    {
        if (IsLoading)
            return;

        if (!forceRefresh && HasData)
        {
            Debug.Log("[QuizQuestionRepository] Using in-memory cached questions.");
            OnQuestionsLoaded?.Invoke();
            return;
        }

        if (_loadRoutine != null)
            StopCoroutine(_loadRoutine);

        _loadRoutine = StartCoroutine(LoadQuestionsRoutine(accessToken));
    }

    public void Refresh(string accessToken = null)
    {
        Initialize(true, accessToken);
    }

    public List<QuizSessionQuestion> GetQuestions()
    {
        return new List<QuizSessionQuestion>(_cachedQuestions);
    }

    private IEnumerator LoadQuestionsRoutine(string accessToken)
    {
        IsLoading = true;
        LastError = string.Empty;

        bool loaded = false;
        string failReason = "Failed to load any quiz source.";

        yield return StartCoroutine(LoadQuestions(
            questions =>
            {
                _cachedQuestions.Clear();
                if (questions != null)
                    _cachedQuestions.AddRange(questions);

                LastError = string.Empty;
                loaded = _cachedQuestions.Count > 0;

                if (!loaded)
                    failReason = "Quiz returned no valid questions.";
            },
            () =>
            {
                loaded = false;
                failReason = "Failed to load any quiz source.";
            },
            accessToken
        ));

        if (loaded)
        {
            Debug.Log($"[QuizQuestionRepository] Loaded {_cachedQuestions.Count} questions.");
            OnQuestionsLoaded?.Invoke();
        }
        else
        {
            _cachedQuestions.Clear();
            LastError = failReason;
            Debug.LogWarning($"[QuizQuestionRepository] Load failed: {LastError}");
            OnQuestionsLoadFailed?.Invoke(LastError);
        }

        IsLoading = false;
        _loadRoutine = null;
    }

    public IEnumerator LoadQuestions(
        Action<List<QuizSessionQuestion>> onSuccess,
        Action onFailedCompletely,
        string accessToken = null)
    {
        bool webSuccess = false;
        List<QuizSessionQuestion> webQuestions = null;

        if (Api != null && !string.IsNullOrWhiteSpace(Api.GetQuizUrl))
        {
            Debug.Log($"[QuizQuestionRepository] Downloading from: {Api.GetQuizUrl}");

            yield return FetchFromWeb(
                Api.GetQuizUrl,
                accessToken,
                questions =>
                {
                    webSuccess = true;
                    webQuestions = questions;
                });
        }
        else
        {
            Debug.LogWarning("[QuizQuestionRepository] ApiService or GetQuizUrl is missing.");
        }

        if (webSuccess && webQuestions != null && webQuestions.Count > 0)
        {
            Debug.Log("[QuizQuestionRepository] Using web quiz and updating cache.");
            onSuccess?.Invoke(webQuestions);
            yield break;
        }

        QuizCacheEnvelope cache = LoadCacheEnvelope();
        bool hasCache = cache != null && !string.IsNullOrWhiteSpace(cache.rawJson);
        bool cacheIsFresh = hasCache && !IsExpired(cache.savedAtUnixSeconds);

        if (cacheIsFresh)
        {
            List<QuizSessionQuestion> cachedQuestions = ConvertRawJsonToSessionQuestions(cache.rawJson);
            if (cachedQuestions != null && cachedQuestions.Count > 0)
            {
                Debug.Log("[QuizQuestionRepository] Web failed. Using fresh cached quiz.");
                onSuccess?.Invoke(cachedQuestions);
                yield break;
            }
        }

        if (hasCache)
        {
            List<QuizSessionQuestion> staleCachedQuestions = ConvertRawJsonToSessionQuestions(cache.rawJson);
            if (staleCachedQuestions != null && staleCachedQuestions.Count > 0)
            {
                Debug.Log("[QuizQuestionRepository] Web failed. Using stale cached quiz.");
                onSuccess?.Invoke(staleCachedQuestions);
                yield break;
            }
        }

        if (fallbackConfig != null)
        {
            List<QuizSessionQuestion> fallbackQuestions = QuizSessionBuilder.BuildFromFallback(fallbackConfig);
            if (fallbackQuestions != null && fallbackQuestions.Count > 0)
            {
                Debug.Log("[QuizQuestionRepository] No web or cache data. Using SO fallback.");
                onSuccess?.Invoke(fallbackQuestions);
                yield break;
            }
        }

        Debug.LogWarning("[QuizQuestionRepository] Failed to load any quiz source.");
        onFailedCompletely?.Invoke();
    }

    private IEnumerator FetchFromWeb(
        string url,
        string accessToken,
        Action<List<QuizSessionQuestion>> onSuccess)
    {
        if (Api == null)
        {
            Debug.LogWarning("[QuizQuestionRepository] ApiService.Instance is null.");
            yield break;
        }

        using UnityWebRequest request = Api.Get(url, accessToken);
        yield return request.SendWebRequest();

        long statusCode = request.responseCode;
        string rawJson = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;

        bool hasNetworkFailure =
            request.result == UnityWebRequest.Result.ConnectionError ||
            request.result == UnityWebRequest.Result.DataProcessingError;

        if (hasNetworkFailure)
        {
            Debug.LogWarning($"[QuizQuestionRepository] Quiz GET failed: {request.error}");
            yield break;
        }

        if (statusCode < 200 || statusCode >= 300)
        {
            Debug.LogWarning(
                $"[QuizQuestionRepository] Quiz GET failed. HTTP {statusCode}. Body: {rawJson}"
            );
            yield break;
        }

        if (string.IsNullOrWhiteSpace(rawJson))
        {
            Debug.LogWarning("[QuizQuestionRepository] Quiz GET returned empty JSON.");
            yield break;
        }

        List<QuizSessionQuestion> questions = ConvertRawJsonToSessionQuestions(rawJson);
        if (questions == null || questions.Count == 0)
        {
            Debug.LogWarning("[QuizQuestionRepository] Quiz GET JSON could not be converted.");
            yield break;
        }

        SaveCacheEnvelope(rawJson);
        onSuccess?.Invoke(questions);
    }

    private List<QuizSessionQuestion> ConvertRawJsonToSessionQuestions(string rawJson)
    {
        QuizCurrentResponseDto dto = null;

        try
        {
            dto = JsonUtility.FromJson<QuizCurrentResponseDto>(rawJson);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[QuizQuestionRepository] JSON parse error: {ex.Message}");
            return null;
        }

        if (dto == null)
        {
            Debug.LogWarning("[QuizQuestionRepository] DTO is null.");
            return null;
        }

        if (!dto.success)
        {
            Debug.LogWarning("[QuizQuestionRepository] API returned success = false.");
            return null;
        }

        if (dto.data == null || dto.data.questions == null || dto.data.questions.Length == 0)
        {
            Debug.LogWarning("[QuizQuestionRepository] No questions found in dto.data.questions.");
            return null;
        }

        List<QuizSessionQuestion> result = new List<QuizSessionQuestion>();

        for (int i = 0; i < dto.data.questions.Length; i++)
        {
            QuizQuestionDto source = dto.data.questions[i];

            if (source == null)
                continue;

            if (source.choices == null)
            {
                Debug.LogWarning($"[QuizQuestionRepository] Question {source.id} has null Thai choices.");
                continue;
            }

            string[] thChoices =
            {
                source.choices.a,
                source.choices.b,
                source.choices.c,
                source.choices.d
            };

            string[] enChoices =
            {
                source.choicesEn != null ? source.choicesEn.a : null,
                source.choicesEn != null ? source.choicesEn.b : null,
                source.choicesEn != null ? source.choicesEn.c : null,
                source.choicesEn != null ? source.choicesEn.d : null
            };

            int correctChoiceIndex = AnswerKeyToIndex(source.answer);

            if (correctChoiceIndex < 0 || correctChoiceIndex >= thChoices.Length)
            {
                Debug.LogWarning($"[QuizQuestionRepository] Question {source.id} has invalid answer key: {source.answer}");
                continue;
            }

            QuizSessionQuestion sessionQuestion = new QuizSessionQuestion
            {
                questionTextTh = source.question,
                questionTextEn = source.questionEn,
                correctChoiceIndex = correctChoiceIndex,
                explanationTh = string.Empty,
                explanationEn = string.Empty
            };

            for (int c = 0; c < thChoices.Length; c++)
            {
                sessionQuestion.choices.Add(new QuizSessionChoice
                {
                    textTh = thChoices[c],
                    textEn = enChoices[c],
                    isCorrect = c == correctChoiceIndex
                });
            }

            result.Add(sessionQuestion);
        }

        return result;
    }

    private bool IsExpired(long savedAtUnixSeconds)
    {
        long nowUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        long ageSeconds = nowUnixSeconds - savedAtUnixSeconds;
        long maxAgeSeconds = refreshAfterHours * 60L * 60L;
        return ageSeconds >= maxAgeSeconds;
    }

    private void SaveCacheEnvelope(string rawJson)
    {
        QuizCacheEnvelope envelope = new QuizCacheEnvelope
        {
            rawJson = rawJson,
            savedAtUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        string envelopeJson = JsonUtility.ToJson(envelope);
        PlayerPrefs.SetString(CachePlayerPrefsKey, envelopeJson);
        PlayerPrefs.Save();
    }

    private QuizCacheEnvelope LoadCacheEnvelope()
    {
        if (!PlayerPrefs.HasKey(CachePlayerPrefsKey))
            return null;

        string envelopeJson = PlayerPrefs.GetString(CachePlayerPrefsKey, "");
        if (string.IsNullOrWhiteSpace(envelopeJson))
            return null;

        try
        {
            return JsonUtility.FromJson<QuizCacheEnvelope>(envelopeJson);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[QuizQuestionRepository] Cache parse error: {ex.Message}");
            return null;
        }
    }

    private int AnswerKeyToIndex(string answerKey)
    {
        if (string.IsNullOrWhiteSpace(answerKey))
            return -1;

        switch (answerKey.Trim().ToLowerInvariant())
        {
            case "a":
                return 0;
            case "b":
                return 1;
            case "c":
                return 2;
            case "d":
                return 3;
            default:
                return -1;
        }
    }
}