using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

public class QuizUIController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject startPanel;
    [SerializeField] private GameObject questionPanel;
    [SerializeField] private GameObject resultPanel;

    [Header("Start")]
    [SerializeField] private Button startButton;

    [Header("Main Page Optional")]
    [SerializeField] private TMP_Text phase1ScoreText;
    [SerializeField] private TMP_Text phase2ScoreText;
    [SerializeField] private TMP_Text phase3ScoreText;
    [SerializeField] private TMP_Text phase4ScoreText;

    [Header("Question")]
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text questionCounterText;
    [SerializeField] private TMP_Text questionText;
    [SerializeField] private ToggleGroup toggleGroup;
    [SerializeField] private List<QuizChoiceToggleView> choiceViews = new List<QuizChoiceToggleView>(4);
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button closeButton;

    [Header("Result")]
    [SerializeField] private TMP_Text finalScoreText;
    [SerializeField] private Button resultBackButton;

    [Header("Quit Confirm")]
    [SerializeField] private GameObject quitConfirmPanel;
    [SerializeField] private TMP_Text quitConfirmTitleText;
    [SerializeField] private TMP_Text quitConfirmMessageText;
    [SerializeField] private Button quitConfirmLeaveButton;
    [SerializeField] private Button quitConfirmStayButton;
    [SerializeField] private TMP_Text quitConfirmLeaveButtonText;
    [SerializeField] private TMP_Text quitConfirmStayButtonText;

    private int _selectedChoiceIndex = -1;

    private int _currentQuestionIndex;
    private int _totalQuestionCount;
    private bool _hasQuestionCounter;

    private string _lastQuitTitleFallback;
    private string _lastQuitMessageFallback;
    private bool _isShowingQuitPopup;

    private QuizSessionQuestion _currentQuestion;

    public event Action StartClicked;
    public event Action<int> ConfirmClicked;
    public event Action CloseClicked;
    public event Action QuitConfirmed;
    public event Action QuitCanceled;
    public event Action ResultBackClicked;

    private void Awake()
    {
        if (startButton != null)
        {
            startButton.onClick.AddListener(() =>
            {
                Debug.Log("[QuizUIOverlay] Start quiz clicked");
                StartClicked?.Invoke();
            });
        }

        if (confirmButton != null)
            confirmButton.onClick.AddListener(HandleConfirmClicked);

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(() =>
            {
                Debug.Log("[QuizUIOverlay] Close clicked");
                CloseClicked?.Invoke();
            });
        }

        if (resultBackButton != null)
        {
            resultBackButton.onClick.AddListener(() =>
            {
                Debug.Log("[QuizUIOverlay] Result back clicked");
                ResultBackClicked?.Invoke();
            });
        }

        if (quitConfirmLeaveButton != null)
        {
            quitConfirmLeaveButton.onClick.AddListener(() =>
            {
                Debug.Log("[QuizUIOverlay] Quit confirmed");
                HideQuitConfirmation();
                QuitConfirmed?.Invoke();
            });
        }

        if (quitConfirmStayButton != null)
        {
            quitConfirmStayButton.onClick.AddListener(() =>
            {
                Debug.Log("[QuizUIOverlay] Quit canceled");
                HideQuitConfirmation();
                QuitCanceled?.Invoke();
            });
        }

        SetConfirmInteractable(false);
        HideQuitConfirmation();
        SetPhaseScores(null, null, null, null);
    }

    private void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
        StartCoroutine(RefreshWhenLocalizationReady());
    }

    private IEnumerator RefreshWhenLocalizationReady()
    {
        yield return LocalizationSettings.InitializationOperation;

        RefreshLocalizedStaticTexts();

        if (_hasQuestionCounter)
            RefreshQuestionCounter();

        if (_isShowingQuitPopup)
            RefreshQuitPopupTexts();

        RefreshCurrentQuestionTexts();
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }

    private void OnLocaleChanged(Locale _)
    {
        StartCoroutine(RefreshWhenLocalizationReady());
    }

    public void ShowStart()
    {
        Debug.Log("[QuizUIOverlay] ShowStart called");

        if (startPanel != null)
            startPanel.SetActive(true);

        if (questionPanel != null)
            questionPanel.SetActive(false);

        if (resultPanel != null)
            resultPanel.SetActive(false);

        HideQuitConfirmation();
    }

    public void ShowQuestion()
    {
        Debug.Log("[QuizUIOverlay] ShowQuestion called");

        if (startPanel != null)
            startPanel.SetActive(false);

        if (questionPanel != null)
        {
            questionPanel.SetActive(true);
            questionPanel.transform.SetAsLastSibling();
        }

        if (resultPanel != null)
            resultPanel.SetActive(false);

        HideQuitConfirmation();
    }

    public void ShowResult()
    {
        Debug.Log("[QuizUIOverlay] ShowResult called");

        if (startPanel != null)
            startPanel.SetActive(false);

        if (questionPanel != null)
            questionPanel.SetActive(false);

        if (resultPanel != null)
            resultPanel.SetActive(true);

        HideQuitConfirmation();
    }

    public void CloseAll()
    {
        Debug.Log("[QuizUIOverlay] CloseAll called");

        if (startPanel != null)
            startPanel.SetActive(false);

        if (questionPanel != null)
            questionPanel.SetActive(false);

        if (resultPanel != null)
            resultPanel.SetActive(false);

        HideQuitConfirmation();
    }

    public void BindQuestion(QuizSessionQuestion question, int currentIndex, int totalCount)
    {
        _selectedChoiceIndex = -1;
        SetConfirmInteractable(false);

        _currentQuestion = question;
        _currentQuestionIndex = currentIndex;
        _totalQuestionCount = totalCount;
        _hasQuestionCounter = true;

        RefreshQuestionCounter();
        RefreshCurrentQuestionTexts();
    }

    public void UpdateTimer(float remainingSeconds)
    {
        if (timerText == null)
            return;

        int seconds = Mathf.Max(0, Mathf.FloorToInt(remainingSeconds));
        timerText.text = seconds.ToString();
    }

    public void SetQuestionInteractable(bool value)
    {
        foreach (QuizChoiceToggleView choiceView in choiceViews)
        {
            if (choiceView != null && choiceView.gameObject.activeSelf)
                choiceView.SetInteractable(value);
        }

        SetConfirmInteractable(value && _selectedChoiceIndex >= 0);

        if (closeButton != null)
            closeButton.interactable = true;
    }

   public void SetResult(float totalScore, float maxScore)
    {
        if (finalScoreText != null)
            finalScoreText.text = $"{totalScore:F3}/{maxScore}";
    }
    public void SetResult(int totalScore, int correctCount, int totalQuestions)
    {
        if (finalScoreText != null)
            finalScoreText.text = $"{totalScore}";
    }

    public void SetPhaseScores(float? set1, float? set2, float? set3, float? set4)
    {
        SetSinglePhaseScore(phase1ScoreText, set1);
        SetSinglePhaseScore(phase2ScoreText, set2);
        SetSinglePhaseScore(phase3ScoreText, set3);
        SetSinglePhaseScore(phase4ScoreText, set4);
    }

    public void ResetToggle()
    {
        if (toggleGroup != null)
            toggleGroup.SetAllTogglesOff();

        _selectedChoiceIndex = -1;
        SetConfirmInteractable(false);
    }

    public void SetTimerVisible(bool visible)
    {
        if (timerText != null)
            timerText.gameObject.SetActive(visible);
    }

    public void SetStartInteractable(bool visible)
    {
        if (startButton != null)
            startButton.interactable = visible;
    }

    public void ShowQuitConfirmation(string title, string message)
    {
        _lastQuitTitleFallback = string.IsNullOrWhiteSpace(title)
            ? L("ออกจากควิซ?", "Exit quiz?")
            : title;

        _lastQuitMessageFallback = string.IsNullOrWhiteSpace(message)
            ? L(
                "หากออกจากควิซตอนนี้ ความคืบหน้าจะหายไป",
                "If you leave the quiz now, your progress will be lost."
            )
            : message;

        _isShowingQuitPopup = true;

        RefreshQuitPopupTexts();

        if (quitConfirmPanel != null)
        {
            quitConfirmPanel.SetActive(true);
            quitConfirmPanel.transform.SetAsLastSibling();
        }
    }

    public void HideQuitConfirmation()
    {
        _isShowingQuitPopup = false;

        if (quitConfirmPanel != null)
            quitConfirmPanel.SetActive(false);
    }

    private void HandleChoiceToggleChanged(int choiceIndex, bool isOn)
    {
        if (!isOn)
            return;

        _selectedChoiceIndex = choiceIndex;
        SetConfirmInteractable(true);
    }

    private void HandleConfirmClicked()
    {
        if (_selectedChoiceIndex < 0)
            return;

        ConfirmClicked?.Invoke(_selectedChoiceIndex);
    }

    private void RefreshCurrentQuestionTexts()
    {
        if (_currentQuestion == null)
            return;

        bool useEnglish = IsEnglishLocale();

        if (questionText != null)
            questionText.text = _currentQuestion.GetQuestionText(useEnglish);

        for (int i = 0; i < choiceViews.Count; i++)
        {
            if (i < _currentQuestion.choices.Count)
            {
                choiceViews[i].gameObject.SetActive(true);
                choiceViews[i].Bind(
                    i,
                    _currentQuestion.choices[i].GetText(useEnglish),
                    toggleGroup,
                    HandleChoiceToggleChanged
                );
            }
            else
            {
                choiceViews[i].gameObject.SetActive(false);
            }
        }
    }

    private void SetConfirmInteractable(bool value)
    {
        if (confirmButton != null)
            confirmButton.interactable = value;
    }

    private void SetSinglePhaseScore(TMP_Text target, float? value)
    {
        if (target == null)
            return;

        target.text = value.HasValue ? value.Value.ToString("F3") : "0";
    }

    private void RefreshLocalizedStaticTexts()
    {
        if (quitConfirmLeaveButtonText != null)
        {
            quitConfirmLeaveButtonText.text = T(
                LocalizationKeys.Quiz.QuitConfirmConfirmButton,
                L("ตกลง", "Confirm")
            );
        }

        if (quitConfirmStayButtonText != null)
        {
            quitConfirmStayButtonText.text = T(
                LocalizationKeys.Quiz.QuitConfirmCancelButton,
                L("ยกเลิก", "Cancel")
            );
        }
    }

    private void RefreshQuestionCounter()
    {
        if (questionCounterText == null)
            return;

        string format = T(
            LocalizationKeys.Quiz.PlayTotalQuestionsFormat,
            L("จำนวนข้อทั้งหมด {0}/{1} ข้อ", "Questions {0}/{1}")
        );

        try
        {
            questionCounterText.text = string.Format(
                format,
                _currentQuestionIndex,
                _totalQuestionCount
            );
        }
        catch (FormatException)
        {
            questionCounterText.text = string.Format(
                L("จำนวนข้อทั้งหมด {0}/{1} ข้อ", "Questions {0}/{1}"),
                _currentQuestionIndex,
                _totalQuestionCount
            );
        }
    }

    private void RefreshQuitPopupTexts()
    {
        if (quitConfirmTitleText != null)
        {
            quitConfirmTitleText.text = T(
                LocalizationKeys.Quiz.QuitConfirmTitle,
                _lastQuitTitleFallback
            );
        }

        if (quitConfirmMessageText != null)
        {
            quitConfirmMessageText.text = T(
                LocalizationKeys.Quiz.QuitConfirmMessage,
                _lastQuitMessageFallback
            );
        }
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
}