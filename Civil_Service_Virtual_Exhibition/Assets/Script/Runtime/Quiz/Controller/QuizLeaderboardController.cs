using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

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

    private void Start()
    {
        RefreshLeaderboard();
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

        SetStatus("Loading...");
        ClearSpawned();

        if (repository == null)
        {
            SetStatus("Quiz repository is missing.");
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

        if (leaderboardResult == null)
        {
            SetStatus("Failed to load leaderboard.");
            UpdateMySummary(null);
            _isRefreshing = false;
            yield break;
        }

        if (!leaderboardResult.success || leaderboardResult.response == null || leaderboardResult.response.data == null)
        {
            SetStatus(string.IsNullOrWhiteSpace(leaderboardResult.message)
                ? "Failed to load leaderboard."
                : leaderboardResult.message);

            UpdateMySummary(meResult != null && meResult.success && meResult.response != null
                ? meResult.response.data
                : null);

            _isRefreshing = false;
            yield break;
        }

        BuildLeaderboard(leaderboardResult.response.data);

        UpdateMySummary(meResult != null && meResult.success && meResult.response != null
            ? meResult.response.data
            : null);

        _isRefreshing = false;
    }

    private void BuildLeaderboard(QuizLeaderboardDataDto data)
    {
        SetStatus(string.Empty);

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

        if (data != null)
        {
            mySummaryText.text =
                "ชื่อ: " + playerName +
                " | คะแนนของคุณ: " + data.totalScore +
                " คะแนน | อันดับของคุณ: " + data.rank;

            return;
        }

        mySummaryText.text =
            "ชื่อ: " + playerName +
            " | คะแนนของคุณ: " + _lastFinalUiScore +
            " คะแนน | อันดับของคุณ: -";
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

    private void SetStatus(string text)
    {
        if (statusText == null)
            return;

        statusText.text = text;
        statusText.gameObject.SetActive(!string.IsNullOrWhiteSpace(text));
    }
}