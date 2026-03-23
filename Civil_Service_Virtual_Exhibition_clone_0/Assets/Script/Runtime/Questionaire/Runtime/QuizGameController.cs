using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class QuizGameController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private QuizGameConfigSO config;
    [SerializeField] private QuizQuestionRepository repository;
    [SerializeField] private QuizUIOverlay ui;
    [SerializeField] private QuizLeaderboardController leaderboardController;
    [Header("Flow")]
    [SerializeField] private float nextQuestionDelay = 0.75f;
    [Header("Score")]
    [SerializeField] private int scorePerCorrectAnswer = 10;
    [SerializeField] private float remainingTimeMultiplier = 0.05f;
    [Header("Data Source")]
    [SerializeField] private bool useLocalFallbackOnly = false;
    private List<QuizSessionQuestion> _sessionQuestions = new List<QuizSessionQuestion>();
    private int _currentQuestionIndex;
    private int _score;
    private int _maxScore;
    private int _correctCount;

    private float _remainingSessionTime;
    private bool _questionActive;
    private bool _awaitingNextQuestion;
    private bool _sessionEnded;

    private void OnEnable()
    {
        if (ui == null) return;

        ui.StartClicked += HandleStartClicked;
        ui.ConfirmClicked += HandleConfirmClicked;
        ui.CloseClicked += HandleCloseClicked;
    }

    private void OnDisable()
    {
        if (ui == null) return;

        ui.StartClicked -= HandleStartClicked;
        ui.ConfirmClicked -= HandleConfirmClicked;
        ui.CloseClicked -= HandleCloseClicked;
    }

    private void Start()
    {
        if (ui != null)
        {
            // ui.ShowStart();
        }
    }

    private void Update()
    {
        if (_sessionEnded || !_questionActive || _awaitingNextQuestion)
        {
            return;
        }

        if (!config.isTimeLimited)
        {
            return;
        }

        _remainingSessionTime -= Time.deltaTime;
        ui.UpdateTimer(_remainingSessionTime);

        if (_remainingSessionTime <= 0f)
        {
            _remainingSessionTime = 0f;
            ui.UpdateTimer(_remainingSessionTime);
            ForceEndSessionBecauseTimeExpired();
        }
    }

    private void HandleStartClicked()
    {
        Debug.Log("[QuizGameController] Start quiz clicked");
        StartNewSession();
    }

    private void HandleCloseClicked()
    {
        _sessionEnded = true;
        _questionActive = false;
        _awaitingNextQuestion = false;

        ui.ShowStart();
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
        _correctCount = 0;
        _questionActive = false;
        _awaitingNextQuestion = false;
        _sessionEnded = false;

        _remainingSessionTime = config.sessionTimeLimitSeconds;

        ui.SetTimerVisible(config.isTimeLimited);
        if (config.isTimeLimited)
        {
            ui.UpdateTimer(_remainingSessionTime);
        }

        if (useLocalFallbackOnly)
        {
            Debug.Log("[QuizGameController] Using local fallback questions only.");
            _sessionQuestions = QuizSessionBuilder.BuildFromFallback(config);

            if (_sessionQuestions == null || _sessionQuestions.Count == 0)
            {
                Debug.LogWarning("[QuizGameController] No local fallback questions available.");
                return;
            }

            ui.ShowQuestion();
            ShowCurrentQuestion();
            return;
        }

        StartCoroutine(BeginSessionRoutine());
    }
    private IEnumerator BeginSessionRoutine()
    {
        bool loaded = false;
        List<QuizSessionQuestion> loadedQuestions = null;

        if(repository != null)
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
            yield break;
        }

        _sessionQuestions = PrepareSessionQuestions(loadedQuestions);

        if (_sessionQuestions == null || _sessionQuestions.Count == 0)
        {
            Debug.LogWarning("[QuizGameController] No questions available after preparation.");
            yield break;
        }
        ui.ShowQuestion();
        ShowCurrentQuestion();
    }
    private void ShowCurrentQuestion()
    {
        if (_sessionEnded)
        {
            return;
        }

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
        {
            return;
        }

        _questionActive = false;
        _awaitingNextQuestion = true;

        ui.SetQuestionInteractable(false);

        QuizSessionQuestion question = _sessionQuestions[_currentQuestionIndex];
        bool isCorrect = selectedChoiceIndex == question.correctChoiceIndex;

        if (isCorrect)
        {
            _correctCount++;
        }

        StartCoroutine(ProceedToNextQuestionAfterDelay());
    }
    private IEnumerator ProceedToNextQuestionAfterDelay()
    {
        yield return new WaitForSeconds(nextQuestionDelay);

        if (_sessionEnded)
        {
            yield break;
        }

        if (config.isTimeLimited && _remainingSessionTime <= 0f)
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
        {
            return;
        }

        _sessionEnded = true;
        _questionActive = false;
        _awaitingNextQuestion = false;

        _score = CalculateFinalScore();
        _maxScore = CalculateMaxScore();

        leaderboardController?.HandleQuizFinished(
            _correctCount,
            _sessionQuestions.Count,
            _score
        );

        ui.SetQuestionInteractable(false);
        ui.SetResult(_score, _maxScore);
        ui.ShowResult();
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

        if (config.shuffleQuestionOrder)
        {
            QuizSessionBuilder.Shuffle(cloned);
        }

        int takeCount = Mathf.Min(config.questionsPerSession, cloned.Count);
        List<QuizSessionQuestion> finalList = new List<QuizSessionQuestion>();

        for (int i = 0; i < takeCount; i++)
        {
            QuizSessionQuestion question = cloned[i];

            if (config.shuffleChoiceOrder)
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

        if (config.isTimeLimited)
        {
            remainingTimeBonus = Mathf.Max(0f, _remainingSessionTime) * remainingTimeMultiplier;
        }

        float finalScore = (_correctCount * scorePerCorrectAnswer) + remainingTimeBonus;
        return Mathf.RoundToInt(finalScore);
    }

    private int CalculateMaxScore()
    {
        float maxTimeBonus = config.isTimeLimited
            ? config.sessionTimeLimitSeconds * remainingTimeMultiplier
            : 0f;

        float maxScore = (_sessionQuestions.Count * scorePerCorrectAnswer) + maxTimeBonus;
        return Mathf.RoundToInt(maxScore);
    }

    private void EndSession()
    {
        if (_sessionEnded)
        {
            return;
        }

        _sessionEnded = true;
        _questionActive = false;
        _awaitingNextQuestion = false;

        _score = CalculateFinalScore();
        _maxScore = CalculateMaxScore();

        leaderboardController?.HandleQuizFinished(
            _correctCount,
            _sessionQuestions.Count,
            _score
        );

        ui.SetResult(_score, _maxScore);
        ui.ShowResult();
    }
}