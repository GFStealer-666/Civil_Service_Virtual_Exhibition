using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuizGameController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private QuizGameConfigSO config;
    [SerializeField] private QuizQuestionRepository repository;
    [SerializeField] private QuizUIOverlay ui;
    [SerializeField] private QuizLeaderboardController leaderboardController;
    [SerializeField] private StatusOverlay statusOverlay;

    [Header("Flow")]
    [SerializeField] private float nextQuestionDelay = 0.75f;

    [Header("Score")]
    [SerializeField] private int scorePerCorrectAnswer = 10;
    [SerializeField] private float remainingTimeMultiplier = 0.05f;

    [Header("Data Source")]
    [SerializeField] private bool useLocalFallbackOnly = false;

    [Header("Cooldown")]
    [SerializeField] private int playCooldownHours = 24;
    [SerializeField] private string playerPrefsCooldownKey = "quiz_last_played_unix_seconds";

    [Header("Quit Confirm Text")]
    [SerializeField] private string quitConfirmTitleThai = "ออกจากควิซ?";
    [SerializeField] private string quitConfirmMessageThai =
        "หากออกจากควิซตอนนี้ คุณจะไม่สามารถเล่นได้อีกเป็นเวลา 24 ชั่วโมง";
    [SerializeField] private string cooldownBlockedTitleThai = "ไม่สามารถเริ่มควิซได้";
    [Header("Debug")]
    [SerializeField] private bool bypassCooldownForTesting = false;
    private readonly List<QuizSessionQuestion> _sessionQuestions = new List<QuizSessionQuestion>();

    private int _currentQuestionIndex;
    private int _score;
    private int _maxScore;
    private int _correctCount;
    private bool _isPaused;
    private float _remainingSessionTime;
    private bool _questionActive;
    private bool _awaitingNextQuestion;
    private bool _sessionEnded;
    private bool _sessionStarted;

    private void OnEnable()
    {
        if (ui == null) return;

        ui.StartClicked += HandleStartClicked;
        ui.ConfirmClicked += HandleConfirmClicked;
        ui.CloseClicked += HandleCloseClicked;
        ui.QuitConfirmed += HandleQuitConfirmed;
        ui.QuitCanceled += HandleQuitCanceled;
    }

    private void OnDisable()
    {
        if (ui == null) return;

        ui.StartClicked -= HandleStartClicked;
        ui.ConfirmClicked -= HandleConfirmClicked;
        ui.CloseClicked -= HandleCloseClicked;
        ui.QuitConfirmed -= HandleQuitConfirmed;
        ui.QuitCanceled -= HandleQuitCanceled;
    }

    private void Start()
    {
        //ShowStartState();
    }

    private void Update()
    {
        if (_sessionEnded || !_questionActive || _awaitingNextQuestion)
            return;

        if (_isPaused) 
            return;
            
        if (config == null || !config.isTimeLimited)
            return;

        _remainingSessionTime -= Time.deltaTime;

        if (ui != null)
            ui.UpdateTimer(_remainingSessionTime);

        if (_remainingSessionTime <= 0f)
        {
            _remainingSessionTime = 0f;

            if (ui != null)
                ui.UpdateTimer(_remainingSessionTime);

            ForceEndSessionBecauseTimeExpired();
        }
    }

    private void ShowStartState()
    {
        if (ui == null)
            return;

        ui.ShowStart();
        ui.HideQuitConfirmation();
        ui.SetStartInteractable(true);
    }

    private void HandleStartClicked()
    {
        Debug.Log("[QuizGameController] Start quiz clicked");

        if (IsCooldownActive(out TimeSpan remaining))
        {
            string remainingText = FormatTimeSpanThai(remaining);
            string reason = $"คุณเล่นควิซนี้ไปแล้ว กรุณารออีก {remainingText} ก่อนที่จะเล่นได้";

            Debug.Log($"[QuizGameController] Cooldown active. Remaining = {remainingText}");

            if (statusOverlay != null)
            {
                statusOverlay.ShowFailed(
                    cooldownBlockedTitleThai,
                    reason,
                    onDismissed: null,
                    showBlocker: true
                );
            }

            if (ui != null)
            {
                ui.ShowStart();
                ui.SetStartInteractable(true);
            }

            return;
        }

        MarkPlayerAsPlayedNow();
        StartNewSession();
    }

    private void HandleCloseClicked()
    {
        if (_sessionStarted && !_sessionEnded)
        {
            _isPaused = true;

            ui.ShowQuitConfirmation(
                quitConfirmTitleThai,
                quitConfirmMessageThai
            );
            return;
        }
        ui.SetQuestionInteractable(false);
        StopAllCoroutines();

        _sessionEnded = true;
        _questionActive = false;
        _awaitingNextQuestion = false;
        _sessionStarted = false;

        ShowStartState();
    }

    private void HandleQuitConfirmed()
    {

        _isPaused = false; 
        QuitCurrentSession();
    }

    private void HandleQuitCanceled()
    {
        Debug.Log("[QuizGameController] Player canceled quit.");
        _isPaused = false; 
        ui.SetQuestionInteractable(true);
    }

    private void HandleConfirmClicked(int selectedChoiceIndex)
    {
        SubmitAnswer(selectedChoiceIndex);
    }

    private void StartNewSession()
    {
        Debug.Log("[QuizGameController] StartNewSession");

        StopAllCoroutines();

        _sessionQuestions.Clear();
        _currentQuestionIndex = 0;
        _score = 0;
        _maxScore = 0;
        _correctCount = 0;

        _questionActive = false;
        _awaitingNextQuestion = false;
        _sessionEnded = false;
        _sessionStarted = true;

        _remainingSessionTime = config != null ? config.sessionTimeLimitSeconds : 0f;

        if (ui != null)
        {
            ui.HideQuitConfirmation();
            ui.SetTimerVisible(config != null && config.isTimeLimited);

            if (config != null && config.isTimeLimited)
                ui.UpdateTimer(_remainingSessionTime);
        }

        if (useLocalFallbackOnly)
        {
            Debug.Log("[QuizGameController] Using local fallback questions only.");

            List<QuizSessionQuestion> fallbackQuestions = QuizSessionBuilder.BuildFromFallback(config);

            if (fallbackQuestions == null || fallbackQuestions.Count == 0)
            {
                Debug.LogWarning("[QuizGameController] No local fallback questions available.");
                FailToStartQuiz("ไม่พบคำถามควิซ");
                return;
            }

            _sessionQuestions.AddRange(fallbackQuestions);

            if (ui != null)
            {
                ui.ShowQuestion();
                ShowCurrentQuestion();
            }
            return;
        }

        StartCoroutine(BeginSessionRoutine());
    }

    private IEnumerator BeginSessionRoutine()
    {
        bool loaded = false;
        List<QuizSessionQuestion> loadedQuestions = null;

        if (repository != null)
        {
            yield return repository.LoadQuestions(
                questions =>
                {
                    loadedQuestions = questions;
                    loaded = true;
                },
                () =>
                {
                    loaded = false;
                }
            );
        }

        if (!loaded || loadedQuestions == null || loadedQuestions.Count == 0)
        {
            Debug.LogWarning("[QuizGameController] Failed to prepare quiz session.");
            FailToStartQuiz("ไม่สามารถโหลดควิซได้");
            yield break;
        }

        List<QuizSessionQuestion> preparedQuestions = PrepareSessionQuestions(loadedQuestions);

        if (preparedQuestions == null || preparedQuestions.Count == 0)
        {
            Debug.LogWarning("[QuizGameController] No questions available after preparation.");
            FailToStartQuiz("ไม่พบคำถามควิซ");
            yield break;
        }

        _sessionQuestions.Clear();
        _sessionQuestions.AddRange(preparedQuestions);

        if (ui != null)
        {
            ui.ShowQuestion();
            ShowCurrentQuestion();
        }
    }

    private void FailToStartQuiz(string message)
    {
        _sessionEnded = true;
        _sessionStarted = false;
        _questionActive = false;
        _awaitingNextQuestion = false;

        if (statusOverlay != null)
        {
            statusOverlay.ShowFailed(
                "เริ่มควิซไม่สำเร็จ",
                message,
                onDismissed: null,
                showBlocker: true
            );
        }

        ShowStartState();
    }

    private void ShowCurrentQuestion()
    {
        if (_sessionEnded || ui == null)
            return;

        if (_currentQuestionIndex >= _sessionQuestions.Count)
        {
            EndSession();
            return;
        }

        QuizSessionQuestion question = _sessionQuestions[_currentQuestionIndex];

        ui.BindQuestion(question, _currentQuestionIndex + 1, _sessionQuestions.Count);
        ui.ResetToggle();

        _questionActive = true;
        _awaitingNextQuestion = false;

        ui.SetQuestionInteractable(true);
    }

    private void SubmitAnswer(int selectedChoiceIndex)
    {
        if (_sessionEnded || !_questionActive || _awaitingNextQuestion)
            return;

        _questionActive = false;
        _awaitingNextQuestion = true;

        if (ui != null)
            ui.SetQuestionInteractable(false);

        QuizSessionQuestion question = _sessionQuestions[_currentQuestionIndex];
        bool isCorrect = selectedChoiceIndex == question.correctChoiceIndex;

        if (isCorrect)
            _correctCount++;

        StartCoroutine(ProceedToNextQuestionAfterDelay());
    }

    private IEnumerator ProceedToNextQuestionAfterDelay()
    {
        yield return new WaitForSeconds(nextQuestionDelay);

        if (_sessionEnded)
            yield break;

        if (config != null && config.isTimeLimited && _remainingSessionTime <= 0f)
        {
            ForceEndSessionBecauseTimeExpired();
            yield break;
        }

        _currentQuestionIndex++;
        _awaitingNextQuestion = false;
        ShowCurrentQuestion();
    }

    private void ForceEndSessionBecauseTimeExpired()
    {
        if (_sessionEnded)
            return;

        _sessionEnded = true;
        _questionActive = false;
        _awaitingNextQuestion = false;
        _sessionStarted = false;

        _score = CalculateFinalScore();
        _maxScore = CalculateMaxScore();

        leaderboardController?.HandleQuizFinished(
            _correctCount,
            _sessionQuestions.Count,
            _score
        );

        if (ui != null)
        {
            ui.SetQuestionInteractable(false);
            ui.HideQuitConfirmation();
            ui.SetResult(_score, _maxScore);
            ui.ShowResult();
        }
    }

    private void EndSession()
    {
        if (_sessionEnded)
            return;

        _sessionEnded = true;
        _questionActive = false;
        _awaitingNextQuestion = false;
        _sessionStarted = false;

        _score = CalculateFinalScore();
        _maxScore = CalculateMaxScore();

        leaderboardController?.HandleQuizFinished(
            _correctCount,
            _sessionQuestions.Count,
            _score
        );

        if (ui != null)
        {
            ui.HideQuitConfirmation();
            ui.SetResult(_score, _maxScore);
            ui.ShowResult();
        }
    }

    private void QuitCurrentSession()
    {
        Debug.Log("[QuizGameController] Player quit the quiz mid-session.");

        StopAllCoroutines();

        _sessionEnded = true;
        _questionActive = false;
        _awaitingNextQuestion = false;
        _sessionStarted = false;

        if (ui != null)
        {
            ui.HideQuitConfirmation();
            ui.SetQuestionInteractable(false);
        }

        ShowStartState();
    }

    private List<QuizSessionQuestion> PrepareSessionQuestions(List<QuizSessionQuestion> source)
    {
        List<QuizSessionQuestion> cloned = new List<QuizSessionQuestion>();

        for (int i = 0; i < source.Count; i++)
        {
            QuizSessionQuestion original = source[i];

            QuizSessionQuestion copy = new QuizSessionQuestion
            {
                questionText = original.questionText,
                correctChoiceIndex = original.correctChoiceIndex,
                explanation = original.explanation
            };

            for (int c = 0; c < original.choices.Count; c++)
            {
                QuizSessionChoice originalChoice = original.choices[c];
                copy.choices.Add(new QuizSessionChoice
                {
                    text = originalChoice.text,
                    isCorrect = originalChoice.isCorrect
                });
            }

            cloned.Add(copy);
        }

        if (config != null && config.shuffleQuestionOrder)
        {
            QuizSessionBuilder.Shuffle(cloned);
        }

        int takeCount = config != null
            ? Mathf.Min(config.questionsPerSession, cloned.Count)
            : cloned.Count;

        List<QuizSessionQuestion> finalList = new List<QuizSessionQuestion>();

        for (int i = 0; i < takeCount; i++)
        {
            QuizSessionQuestion question = cloned[i];

            if (config != null && config.shuffleChoiceOrder)
            {
                QuizSessionBuilder.Shuffle(question.choices);

                for (int c = 0; c < question.choices.Count; c++)
                {
                    if (question.choices[c].isCorrect)
                    {
                        question.correctChoiceIndex = c;
                        break;
                    }
                }
            }

            finalList.Add(question);
        }

        return finalList;
    }

    private int CalculateFinalScore()
    {
        float remainingTimeBonus = 0f;

        if (config != null && config.isTimeLimited)
            remainingTimeBonus = Mathf.Max(0f, _remainingSessionTime) * remainingTimeMultiplier;

        float finalScore = (_correctCount * scorePerCorrectAnswer) + remainingTimeBonus;
        return Mathf.RoundToInt(finalScore);
    }

    private int CalculateMaxScore()
    {
        float maxTimeBonus =
            config != null && config.isTimeLimited
                ? config.sessionTimeLimitSeconds * remainingTimeMultiplier
                : 0f;

        float maxScore = (_sessionQuestions.Count * scorePerCorrectAnswer) + maxTimeBonus;
        return Mathf.RoundToInt(maxScore);
    }

    private bool IsCooldownActive(out TimeSpan remaining)
    {
        remaining = TimeSpan.Zero;

        if (bypassCooldownForTesting)
        {
            return false;
        }

        if (!PlayerPrefs.HasKey(playerPrefsCooldownKey))
            return false;

        string raw = PlayerPrefs.GetString(playerPrefsCooldownKey, string.Empty);
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        if (!long.TryParse(raw, out long lastPlayedUnix))
            return false;

        DateTimeOffset lastPlayedTime = DateTimeOffset.FromUnixTimeSeconds(lastPlayedUnix);
        DateTimeOffset nextAllowedTime = lastPlayedTime.AddHours(playCooldownHours);
        DateTimeOffset now = DateTimeOffset.UtcNow;

        if (now < nextAllowedTime)
        {
            remaining = nextAllowedTime - now;
            return true;
        }

        return false;
    }

    private void MarkPlayerAsPlayedNow()
    {
        long nowUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        PlayerPrefs.SetString(playerPrefsCooldownKey, nowUnix.ToString());
        PlayerPrefs.Save();

        Debug.Log($"[QuizGameController] Player marked as played at unix={nowUnix}");
    }

    private string FormatTimeSpanThai(TimeSpan time)
    {
        int totalHours = Mathf.Max(0, (int)time.TotalHours);
        int minutes = Mathf.Max(0, time.Minutes);
        int seconds = Mathf.Max(0, time.Seconds);

        if (totalHours > 0)
            return $"{totalHours} ชั่วโมง {minutes} นาที {seconds} วินาที";

        if (minutes > 0)
            return $"{minutes} นาที {seconds} วินาที";

        return $"{seconds} วินาที";
    }

    // Context Menu 

    [ContextMenu("Reset Cooldown")]
    private void ResetCooldown()
    {
        PlayerPrefs.DeleteKey(playerPrefsCooldownKey);
        PlayerPrefs.Save();

        Debug.Log("[QuizGameController] Cooldown reset");
    }
}