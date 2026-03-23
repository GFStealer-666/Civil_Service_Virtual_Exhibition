using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;


// fetch down webrequest and translate data to local data 
public class QuizQuestionRepository : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ApiConfig apiConfig;
    [SerializeField] private QuizGameConfigSO fallbackConfig;

    [Header("Cache")]
    [SerializeField] private int refreshAfterHours = 24;

    private const string CachePlayerPrefsKey = "quiz_cache_envelope_v1";

    public IEnumerator LoadQuestions(
        Action<List<QuizSessionQuestion>> onSuccess,
        Action onFailedCompletely)
    {
        QuizCacheEnvelope cache = LoadCacheEnvelope();

        bool hasCache = cache != null && !string.IsNullOrWhiteSpace(cache.rawJson);
        bool cacheIsFresh = hasCache && !IsExpired(cache.savedAtUnixSeconds);

        if (cacheIsFresh)
        {
            List<QuizSessionQuestion> cachedQuestions = ConvertRawJsonToSessionQuestions(cache.rawJson);
            if (cachedQuestions != null && cachedQuestions.Count > 0)
            {
                Debug.Log("[QuizQuestionRepository] Using fresh cached quiz.");
                onSuccess?.Invoke(cachedQuestions);
                yield break;
            }
        }

        bool webSuccess = false;
        List<QuizSessionQuestion> webQuestions = null;

        if (apiConfig != null && !string.IsNullOrWhiteSpace(apiConfig.GetQuizUrl))
        {
            yield return FetchFromWeb(
                apiConfig.GetQuizUrl,
                questions =>
                {
                    webSuccess = true;
                    webQuestions = questions;
                });
        }

        if (webSuccess && webQuestions != null && webQuestions.Count > 0)
        {
            Debug.Log("[QuizQuestionRepository] Using web quiz and updating cache.");
            onSuccess?.Invoke(webQuestions);
            yield break;
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
                Debug.Log("[QuizQuestionRepository] No web/cache data. Using SO fallback.");
                onSuccess?.Invoke(fallbackQuestions);
                yield break;
            }
        }

        Debug.LogWarning("[QuizQuestionRepository] Failed to load any quiz source.");
        onFailedCompletely?.Invoke();
    }

    private IEnumerator FetchFromWeb(string url, Action<List<QuizSessionQuestion>> onSuccess)
    {
        using UnityWebRequest req = UnityWebRequest.Get(url);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"[QuizQuestionRepository] Quiz GET failed: {req.error}");
            yield break;
        }

        string rawJson = req.downloadHandler.text;

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
        QuizApiResponseDto dto = null;

        try
        {
            dto = JsonUtility.FromJson<QuizApiResponseDto>(rawJson);
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
            QuizApiQuestionDto source = dto.data.questions[i];

            if (source == null)
                continue;

            if (source.choices == null)
            {
                Debug.LogWarning($"[QuizQuestionRepository] Question {source.id} has null choices.");
                continue;
            }

            List<string> orderedChoices = new List<string>
            {
                source.choices.a,
                source.choices.b,
                source.choices.c,
                source.choices.d
            };

            int correctChoiceIndex = AnswerKeyToIndex(source.answer);

            if (correctChoiceIndex < 0 || correctChoiceIndex >= orderedChoices.Count)
            {
                Debug.LogWarning($"[QuizQuestionRepository] Question {source.id} has invalid answer key: {source.answer}");
                continue;
            }

            QuizSessionQuestion sessionQuestion = new QuizSessionQuestion
            {
                questionText = source.question,
                correctChoiceIndex = correctChoiceIndex,
                explanation = string.Empty
            };

            for (int c = 0; c < orderedChoices.Count; c++)
            {
                sessionQuestion.choices.Add(new QuizSessionChoice
                {
                    text = orderedChoices[c],
                    isCorrect = c == correctChoiceIndex
                });
            }

            result.Add(sessionQuestion);
        }

        return result;
    }
    // check if current local data is expired or not
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
            case "a": return 0;
            case "b": return 1;
            case "c": return 2;
            case "d": return 3;
            default: return -1;
        }
    }
}