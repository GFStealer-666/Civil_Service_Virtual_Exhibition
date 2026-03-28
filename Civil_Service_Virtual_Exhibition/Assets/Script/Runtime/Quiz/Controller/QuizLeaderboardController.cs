using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class QuizLeaderboardController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private QuizRepository repository;

    [Header("Panel")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TMP_Text mySummaryText;
    [SerializeField] private TMP_Text statusText;

    [Header("Spawn Parents")]
    [SerializeField] private Transform top3Container;
    [SerializeField] private Transform top4To10Container;

    [Header("Prefabs")]
    [SerializeField] private LeaderboardEntryView top1Prefab;
    [SerializeField] private LeaderboardEntryView top2Prefab;
    [SerializeField] private LeaderboardEntryView top3Prefab;
    [SerializeField] private LeaderboardEntryView top4To10RowPrefab;

    private readonly List<GameObject> _spawnedObjects = new List<GameObject>();

    private int _lastSubmittedCorrectCount;
    private int _lastSubmittedTotalQuestions;
    private int _lastFinalUiScore;

    private bool _hasSessionResult;
    private bool _isRefreshing;

    private QuizMeDataDto _cachedMeData;

    private string _lastLocalizedStatusKey;
    private string _lastLocalizedStatusFallback;
    private string _lastRawStatusText;

    private void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
        UpdateMySummary(_cachedMeData);
        RefreshStatusText();
    }

    private void Start()
    {
        RefreshLeaderboard();
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }

    private void OnLocaleChanged(Locale _)
    {
        UpdateMySummary(_cachedMeData);
        RefreshStatusText();
    }

    public void HandleQuizFinished(int correctCount, int totalQuestions, int finalUiScore)
    {
        _lastSubmittedCorrectCount = correctCount;
        _lastSubmittedTotalQuestions = totalQuestions;
        _lastFinalUiScore = finalUiScore;
        _hasSessionResult = true;

        StartCoroutine(SubmitLatestResultRoutine());
    }

    public void OpenLeaderboard()
    {
        if (panelRoot != null)
            panelRoot.SetActive(true);

        RefreshLeaderboard();
    }

    public void CloseLeaderboard()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    public void RefreshLeaderboard()
    {
        if (_isRefreshing)
            return;

        StartCoroutine(RefreshLeaderboardRoutine());
    }

    private IEnumerator SubmitLatestResultRoutine()
    {
        if (!_hasSessionResult)
            yield break;

        if (repository == null)
        {
            Debug.LogWarning("[QuizLeaderboardController] QuizRepository is missing.");
            yield break;
        }

        QuizSubmitOperationResult submitResult = null;

        yield return repository.SubmitResult(
            _lastFinalUiScore,
            result =>
            {
                submitResult = result;
            });

        if (submitResult == null)
        {
            Debug.LogWarning("[QuizLeaderboardController] Submit result is null.");
            yield break;
        }

        if (!submitResult.success)
        {
            Debug.LogWarning(
                $"[QuizLeaderboardController] Submit failed | type={submitResult.errorType} | code={submitResult.statusCode} | message={submitResult.message}"
            );
            yield break;
        }

        Debug.Log("[QuizLeaderboardController] Submit success.");

        RefreshLeaderboard();
    }

    private IEnumerator RefreshLeaderboardRoutine()
    {
        _isRefreshing = true;

        SetStatusLocalized(
            LocalizationKeys.Quiz.LeaderboardLoading,
            "Loading..."
        );

        ClearSpawned();

        if (repository == null)
        {
            SetStatusLocalized(
                LocalizationKeys.Quiz.LeaderboardRepositoryMissing,
                "Quiz repository is missing."
            );

            _isRefreshing = false;
            yield break;
        }

        QuizLeaderboardOperationResult leaderboardResult = null;
        QuizMeOperationResult meResult = null;

        yield return repository.LoadLeaderboard(result =>
        {
            leaderboardResult = result;
        });

        yield return repository.LoadMe(result =>
        {
            meResult = result;
        });

        _cachedMeData = meResult != null && meResult.success && meResult.response != null
            ? meResult.response.data
            : null;

        if (leaderboardResult == null)
        {
            SetStatusLocalized(
                LocalizationKeys.Quiz.LeaderboardLoadFailed,
                "Failed to load leaderboard."
            );

            UpdateMySummary(_cachedMeData);
            _isRefreshing = false;
            yield break;
        }

        if (!leaderboardResult.success || leaderboardResult.response == null || leaderboardResult.response.data == null)
        {
            if (string.IsNullOrWhiteSpace(leaderboardResult.message))
            {
                SetStatusLocalized(
                    LocalizationKeys.Quiz.LeaderboardLoadFailed,
                    "Failed to load leaderboard."
                );
            }
            else
            {
                SetStatusRaw(leaderboardResult.message);
            }

            UpdateMySummary(_cachedMeData);
            _isRefreshing = false;
            yield break;
        }

        BuildLeaderboard(leaderboardResult.response.data);
        UpdateMySummary(_cachedMeData);

        _isRefreshing = false;
    }

    private void BuildLeaderboard(QuizLeaderboardDataDto data)
    {
        SetStatusRaw(string.Empty);

        if (data != null && data.top10 != null && data.top10.Length > 0)
        {
            Array.Sort(data.top10, CompareByRank);

            for (int i = 0; i < data.top10.Length; i++)
            {
                LeaderboardEntryDto entry = data.top10[i];
                if (entry == null)
                    continue;

                SpawnEntry(entry);
            }
        }
    }

    private static int CompareByRank(LeaderboardEntryDto a, LeaderboardEntryDto b)
    {
        if (a == null && b == null)
            return 0;

        if (a == null)
            return 1;

        if (b == null)
            return -1;

        return a.rank.CompareTo(b.rank);
    }

    private void SpawnEntry(LeaderboardEntryDto entry)
    {
        if (entry == null)
            return;

        if (!TryGetPrefabAndParent(entry.rank, out LeaderboardEntryView prefab, out Transform parent))
            return;

        LeaderboardEntryView view = Instantiate(prefab, parent);
        view.Bind(entry);
        _spawnedObjects.Add(view.gameObject);
    }

    private bool TryGetPrefabAndParent(
        int rank,
        out LeaderboardEntryView prefab,
        out Transform parent)
    {
        prefab = null;
        parent = null;

        switch (rank)
        {
            case 1:
                prefab = top1Prefab;
                parent = top3Container;
                return true;

            case 2:
                prefab = top2Prefab;
                parent = top3Container;
                return true;

            case 3:
                prefab = top3Prefab;
                parent = top3Container;
                return true;

            default:
                if (rank >= 4 && rank <= 10)
                {
                    prefab = top4To10RowPrefab;
                    parent = top4To10Container;
                    return true;
                }

                return false;
        }
    }

    private void UpdateMySummary(QuizMeDataDto data)
    {
        if (mySummaryText == null)
            return;

        string playerName =
            LocalPlayerData.Instance != null &&
            !string.IsNullOrWhiteSpace(LocalPlayerData.Instance.PlayerName)
                ? LocalPlayerData.Instance.PlayerName
                : "-";

        int score = data != null ? data.totalScore : _lastFinalUiScore;
        string rankText = data != null ? data.rank.ToString() : "-";

        mySummaryText.text = F(
            LocalizationKeys.Quiz.LeaderboardMyScoreFormat,
            "คะแนนของคุณ: {0} คะแนน | อันดับของคุณ: {1}",
            score,
            rankText
        );
    }

    private void ClearSpawned()
    {
        for (int i = 0; i < _spawnedObjects.Count; i++)
        {
            if (_spawnedObjects[i] != null)
                Destroy(_spawnedObjects[i]);
        }

        _spawnedObjects.Clear();
    }

    private void SetStatusLocalized(string key, string fallback)
    {
        _lastLocalizedStatusKey = key;
        _lastLocalizedStatusFallback = fallback;
        _lastRawStatusText = null;

        ApplyStatusText(T(key, fallback));
    }

    private void SetStatusRaw(string text)
    {
        _lastLocalizedStatusKey = null;
        _lastLocalizedStatusFallback = null;
        _lastRawStatusText = text;

        ApplyStatusText(text);
    }

    private void RefreshStatusText()
    {
        if (!string.IsNullOrEmpty(_lastLocalizedStatusKey))
        {
            ApplyStatusText(T(_lastLocalizedStatusKey, _lastLocalizedStatusFallback));
            return;
        }

        ApplyStatusText(_lastRawStatusText);
    }

    private void ApplyStatusText(string text)
    {
        if (statusText == null)
            return;

        statusText.text = text;
        statusText.gameObject.SetActive(!string.IsNullOrWhiteSpace(text));
    }

    private string T(string key, string fallback)
    {
        string value = LocalizationSettings.StringDatabase.GetLocalizedString(
            LocalizationKeys.Tables.Quiz,
            key
        );

        return string.IsNullOrEmpty(value) ? fallback : value;
    }

    private string F(string key, string fallback, params object[] args)
    {
        string format = T(key, fallback);

        try
        {
            return string.Format(format, args);
        }
        catch (FormatException)
        {
            return fallback;
        }
    }
}