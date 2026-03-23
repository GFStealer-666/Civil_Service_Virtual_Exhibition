using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class QuizLeaderboardController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private QuizLeaderboardRepository repository;

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
        if (!_hasSessionResult || repository == null)
            yield break;

        bool isDone = false;
        string errorMessage = null;

        // Submit correctCount instead of finalUiScore
        // because your API body example is like:
        // { "score": 8, "totalQuestions": 10 }
        yield return repository.SubmitResult(
            _lastSubmittedCorrectCount,
            _lastSubmittedTotalQuestions,
            () =>
            {
                isDone = true;
            },
            error =>
            {
                errorMessage = error;
                isDone = true;
            });

        if (!isDone)
            yield break;

        if (!string.IsNullOrWhiteSpace(errorMessage))
        {
            Debug.LogWarning("[QuizLeaderboardController] Submit failed: " + errorMessage);
        }
    }

    private IEnumerator RefreshLeaderboardRoutine()
    {
        _isRefreshing = true;

        SetStatus("Loading...");
        ClearSpawned();

        QuizLeaderboardDataDto loadedData = null;
        string errorMessage = null;

        if (repository == null)
        {
            SetStatus("Leaderboard repository is missing.");
            _isRefreshing = false;
            yield break;
        }

        yield return repository.LoadLeaderboard(
            data => loadedData = data,
            error => errorMessage = error
        );

        if (loadedData == null)
        {
            SetStatus(string.IsNullOrWhiteSpace(errorMessage)
                ? "Failed to load leaderboard."
                : errorMessage);

            _isRefreshing = false;
            yield break;
        }

        BuildLeaderboard(loadedData);
        _isRefreshing = false;
    }

    private void BuildLeaderboard(QuizLeaderboardDataDto data)
    {
        SetStatus(string.Empty);

        if (data != null && data.leaderboard != null && data.leaderboard.Length > 0)
        {
            Array.Sort(data.leaderboard, CompareByRank);

            for (int i = 0; i < data.leaderboard.Length; i++)
            {
                LeaderboardEntryDto entry = data.leaderboard[i];
                if (entry == null)
                    continue;

                SpawnEntry(entry);
            }
        }

        UpdateMySummary(data);
    }

    private static int CompareByRank(LeaderboardEntryDto a, LeaderboardEntryDto b)
    {
        if (a == null && b == null) return 0;
        if (a == null) return 1;
        if (b == null) return -1;

        return a.rank.CompareTo(b.rank);
    }

    private void SpawnEntry(LeaderboardEntryDto entry)
    {
        if (entry == null)
            return;

        LeaderboardEntryView prefab;
        Transform parent;

        if (!TryGetPrefabAndParent(entry.rank, out prefab, out parent))
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

    private void UpdateMySummary(QuizLeaderboardDataDto data)
    {
        if (mySummaryText == null)
            return;

        int shownMyScore = (data != null && data.myScore > 0)
            ? data.myScore
            : _lastFinalUiScore;

        string rankText = (data != null && data.myRank > 0)
            ? "อันดับที่ " + data.myRank
            : "-";

        mySummaryText.text = "คะแนนของคุณ: " + shownMyScore + " คะแนน อันดับของคุณ: " + rankText;
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