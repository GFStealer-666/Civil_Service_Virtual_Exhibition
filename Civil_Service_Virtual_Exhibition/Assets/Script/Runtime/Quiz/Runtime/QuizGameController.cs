using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization.Settings;

public class QuizGameController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private QuizGameConfigSO config;
    [SerializeField] private QuizQuestionRepository repository;
    [SerializeField] private QuizRepository quizApiRepository;
    [SerializeField] private QuizUIController ui;
    [SerializeField] private QuizLeaderboardController leaderboardController;
    [SerializeField] private StatusOverlay statusOverlay;

    [Header("Flow")]
    [SerializeField] private float nextQuestionDelay = 0.25f;

    [Header("Score")]
    [SerializeField] private int scorePerCorrectAnswer = 10;
    [SerializeField] private float remainingTimeMultiplier = 0.05f;

    [Header("Data Source")]
    [SerializeField] private bool useLocalFallbackOnly = false;

    [Header("Editor")]
    [SerializeField] private bool bypassCooldownForEditorTesting = false;

    private readonly List<QuizSessionQuestion> _sessionQuestions = new List<QuizSessionQuestion>();

    private int _currentQuestionIndex;
    private int _score;
    private int _maxScore;
    private int _correctCount;

    private bool _isPaused;
    private bool _questionActive;
    private bool _awaitingNextQuestion;
    private bool _sessionEnded;
    private bool _sessionStarted;
    private bool _isStartFlowRunning;
    private bool _isSubmitFlowRunning;

    private float _remainingSessionTime;

    private bool ShouldBypassCooldownForTesting()
    {
    #if UNITY_EDITOR
        return bypassCooldownForEditorTesting;
    #else
        return false;
    #endif
    }

    private void OnEnable()
    {
        if (ui == null)
            return;

        ui.StartClicked += HandleStartClicked;
        ui.ConfirmClicked += HandleConfirmClicked;
        ui.CloseClicked += HandleCloseClicked;
        ui.QuitConfirmed += HandleQuitConfirmed;
        ui.QuitCanceled += HandleQuitCanceled;
        ui.ResultBackClicked += HandleResultBackClicked;
    }

    private void OnDisable()
    {
        if (ui == null)
            return;

        ui.StartClicked -= HandleStartClicked;
        ui.ConfirmClicked -= HandleConfirmClicked;
        ui.CloseClicked -= HandleCloseClicked;
        ui.QuitConfirmed -= HandleQuitConfirmed;
        ui.QuitCanceled -= HandleQuitCanceled;
        ui.ResultBackClicked -= HandleResultBackClicked;
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
        if (_isStartFlowRunning || _sessionStarted || _isSubmitFlowRunning)
            return;

        StartCoroutine(BeginStartFlowRoutine());
    }

    private void HandleResultBackClicked()
    {
        _sessionEnded = true;
        _questionActive = false;
        _awaitingNextQuestion = false;
        _sessionStarted = false;
        _isPaused = false;

        if (ui != null)
        {
            ui.HideQuitConfirmation();
            ui.SetQuestionInteractable(false);
            ui.ShowStart();
        }

        leaderboardController?.RefreshLeaderboard();
    }

    private IEnumerator BeginStartFlowRoutine()
    {
        _isStartFlowRunning = true;

        if (ui != null)
            ui.SetStartInteractable(false);

        if (useLocalFallbackOnly)
        {
            _isStartFlowRunning = false;
            StartNewSession();
            yield break;
        }

        if (quizApiRepository == null)
        {
            _isStartFlowRunning = false;
            FailToStartQuiz(L("ไม่พบ Quiz API Repository", "Quiz API repository is missing."));
            yield break;
        }

        QuizCheckOperationResult checkResult = null;

        yield return quizApiRepository.CheckStatus(result =>
        {
            checkResult = result;
        });

        _isStartFlowRunning = false;

        if (checkResult == null)
        {
            FailToStartQuiz(L("ตรวจสอบสถานะควิซไม่สำเร็จ", "Failed to check quiz status."));
            yield break;
        }

        if (!checkResult.success || checkResult.response == null || checkResult.response.data == null)
        {
            string message = string.IsNullOrWhiteSpace(checkResult.message)
                ? L("ตรวจสอบสถานะควิซไม่สำเร็จ", "Failed to check quiz status.")
                : LocalizeQuizApiMessage(checkResult.message);

            ShowCannotStartMessage(
                L("ไม่สามารถเริ่มควิซได้", "Unable to start quiz"),
                message
            );

            ShowStartState();
            yield break;
        }

        QuizCheckDataDto checkData = checkResult.response.data;

        bool blockedByCooldown =
            !checkData.canPlay &&
            (checkData.playedToday || checkData.playedThisWeek);

        if (blockedByCooldown && !ShouldBypassCooldownForTesting())
        {
            string rawMessage = checkData.playedToday
                ? L(
                    "ร่วมกิจกรรมได้วันละ 1 ครั้ง กรุณาลองใหม่พรุ่งนี้",
                    "You can play once per day. Please try again tomorrow."
                )
                : (!string.IsNullOrWhiteSpace(checkResult.response.message)
                    ? checkResult.response.message
                    : L(
                        "คุณยังไม่สามารถเล่นควิซได้ในขณะนี้",
                        "You cannot play this quiz right now."
                    ));
            Debug.Log(rawMessage);
            string message = LocalizeQuizApiMessage(rawMessage);
            ShowCannotStartMessage(L("ไม่สามารถเริ่มควิซได้", "Unable to start quiz"), message);
            ShowStartState();
            yield break;
        }

        if (!checkData.canPlay && !blockedByCooldown)
        {
            string message = !string.IsNullOrWhiteSpace(checkResult.response.message)
                ? LocalizeQuizApiMessage(checkResult.response.message)
                : L(
                    "คุณยังไม่สามารถเล่นควิซได้ในขณะนี้",
                    "You cannot play this quiz right now."
                );
            Debug.Log(message);
            ShowCannotStartMessage(
                L("ไม่สามารถเริ่มควิซได้", "Unable to start quiz"),
                message
            );
            ShowStartState();
            yield break;
        }

        StartNewSession();
    }

    private void HandleCloseClicked()
    {
        if (_sessionStarted && !_sessionEnded)
        {
            _isPaused = true;

            if (ui != null)
            {
                ui.ShowQuitConfirmation(
                    T(LocalizationKeys.Quiz.QuitConfirmTitle, L("ออกจากควิซ?", "Exit quiz?")),
                    T(
                        LocalizationKeys.Quiz.QuitConfirmMessage,
                        L(
                            "หากออกจากควิซตอนนี้ ความคืบหน้าจะหายไป",
                            "If you leave the quiz now, your progress will be lost."
                        )
                    )
                );
            }

            return;
        }

        if (_isSubmitFlowRunning)
            return;

        if (ui != null)
            ui.SetQuestionInteractable(false);

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
        _isPaused = false;

        if (ui != null)
            ui.SetQuestionInteractable(true);
    }

    private void HandleConfirmClicked(int selectedChoiceIndex)
    {
        SubmitAnswer(selectedChoiceIndex);
    }

    private void StartNewSession()
    {
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
        _isPaused = false;

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
            List<QuizSessionQuestion> fallbackQuestions = QuizSessionBuilder.BuildFromFallback(config);

            if (fallbackQuestions == null || fallbackQuestions.Count == 0)
            {
                FailToStartQuiz(T(LocalizationKeys.Quiz.StartFailedNoQuestions, L("ไม่พบคำถามควิซ", "No quiz questions found.")));
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
            FailToStartQuiz(T(LocalizationKeys.Quiz.StartFailedLoadQuiz, L("โหลดควิซไม่สำเร็จ", "Unable to load quiz.")));
            yield break;
        }

        List<QuizSessionQuestion> preparedQuestions = PrepareSessionQuestions(loadedQuestions);

        if (preparedQuestions == null || preparedQuestions.Count == 0)
        {
            FailToStartQuiz(T(LocalizationKeys.Quiz.StartFailedNoQuestions, L("ไม่พบคำถามควิซ", "No quiz questions found.")));
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
        _isStartFlowRunning = false;

        ShowCannotStartMessage(
            T(LocalizationKeys.Quiz.StartFailedTitle, L("เริ่มควิซไม่สำเร็จ", "Failed to start quiz")),
            message
        );

        ShowStartState();
    }

    private void ShowCannotStartMessage(string title, string message)
    {
        if (statusOverlay == null)
            return;

        statusOverlay.ShowFailed(
            title,
            message,
            onDismissed: null,
            showBlocker: true
        );
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

        FinishSessionAndSubmit();
    }

    private void EndSession()
    {
        if (_sessionEnded)
            return;

        FinishSessionAndSubmit();
    }

    private void FinishSessionAndSubmit()
    {
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

        if (!_isSubmitFlowRunning && !useLocalFallbackOnly)
            StartCoroutine(SubmitFinishedSessionRoutine());
    }

    private IEnumerator SubmitFinishedSessionRoutine()
    {
        _isSubmitFlowRunning = true;

        if (quizApiRepository == null)
        {
            _isSubmitFlowRunning = false;
            yield break;
        }

        QuizSubmitOperationResult submitResult = null;

        yield return quizApiRepository.SubmitResult(
            _score,
            result =>
            {
                submitResult = result;
            });

        _isSubmitFlowRunning = false;

        if (submitResult == null)
        {
            ShowCannotStartMessage(
                L("ส่งคะแนนไม่สำเร็จ", "Failed to submit score"),
                L("การส่งคะแนนควิซไม่ส่งค่ากลับมา", "Quiz score submit returned no result.")
            );
            yield break;
        }

        if (!submitResult.success)
        {
            ShowCannotStartMessage(
                L("ส่งคะแนนไม่สำเร็จ", "Failed to submit score"),
                string.IsNullOrWhiteSpace(submitResult.message)
                    ? L("ไม่สามารถส่งคะแนนควิซได้", "Unable to submit quiz score.")
                    : submitResult.message
            );
            yield break;
        }

        leaderboardController?.RefreshLeaderboard();
    }

    private void QuitCurrentSession()
    {
        StopAllCoroutines();

        _sessionEnded = true;
        _questionActive = false;
        _awaitingNextQuestion = false;
        _sessionStarted = false;
        _isStartFlowRunning = false;
        _isSubmitFlowRunning = false;

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
                questionTextTh = original.questionTextTh,
                questionTextEn = original.questionTextEn,
                correctChoiceIndex = original.correctChoiceIndex,
                explanationTh = original.explanationTh,
                explanationEn = original.explanationEn
            };

            for (int c = 0; c < original.choices.Count; c++)
            {
                QuizSessionChoice originalChoice = original.choices[c];
                copy.choices.Add(new QuizSessionChoice
                {
                    textTh = originalChoice.textTh,
                    textEn = originalChoice.textEn,
                    isCorrect = originalChoice.isCorrect
                });
            }

            cloned.Add(copy);
        }

        if (config != null && config.shuffleQuestionOrder)
            QuizSessionBuilder.Shuffle(cloned);

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

    private bool IsEnglishLocale()
    {
        string code = LocalizationSettings.SelectedLocale != null
            ? LocalizationSettings.SelectedLocale.Identifier.Code
            : string.Empty;

        return !string.IsNullOrWhiteSpace(code) &&
               code.StartsWith("en", StringComparison.OrdinalIgnoreCase);
    }

    private string L(string th, string en)
    {
        return IsEnglishLocale() ? en : th;
    }

    private string T(string key, string fallback)
    {
        if (!LocalizationSettings.InitializationOperation.IsDone)
            return fallback;

        string value = LocalizationSettings.StringDatabase.GetLocalizedString(
            LocalizationKeys.Tables.Quiz,
            key
        );

        return string.IsNullOrEmpty(value) ? fallback : value;
    }
    private string LocalizeQuizApiMessage(string rawMessage)
    {
        if (string.IsNullOrWhiteSpace(rawMessage))
            return rawMessage;

        switch (rawMessage.Trim())
        {
            case "Anonymous users cannot participate":
                return L(
                    "ผู้ใช้แบบไม่ระบุตัวตนไม่สามารถเข้าร่วมกิจกรรมได้",
                    "Anonymous users cannot participate"
                );

            case "Unauthorized":
                return L(
                    "กรุณาเข้าสู่ระบบก่อนเข้าร่วมกิจกรรม",
                    "Please sign in before joining the activity."
                );

            default:
                return rawMessage;
        }
    }
}