using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

public class QuizUIOverlay : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject startPanel;
    [SerializeField] private GameObject questionPanel;
    [SerializeField] private GameObject resultPanel;

    [Header("Start")]
    [SerializeField] private Button startButton;

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

    public event Action StartClicked;
    public event Action<int> ConfirmClicked;
    public event Action CloseClicked;
    public event Action QuitConfirmed;
    public event Action QuitCanceled;

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
        {
            confirmButton.onClick.AddListener(HandleConfirmClicked);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(() =>
            {
                Debug.Log("[QuizUIOverlay] Close clicked");
                CloseClicked?.Invoke();
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
        RefreshLocalizedStaticTexts();
    }

    private void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
        RefreshLocalizedStaticTexts();
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }

    private void OnLocaleChanged(Locale _)
    {
        RefreshLocalizedStaticTexts();

        if (_hasQuestionCounter)
            RefreshQuestionCounter();

        if (_isShowingQuitPopup)
            RefreshQuitPopupTexts();
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

        _currentQuestionIndex = currentIndex;
        _totalQuestionCount = totalCount;
        _hasQuestionCounter = true;

        RefreshQuestionCounter();

        if (questionText != null)
        {
            questionText.text = question.questionText;
        }

        for (int i = 0; i < choiceViews.Count; i++)
        {
            if (i < question.choices.Count)
            {
                choiceViews[i].gameObject.SetActive(true);
                choiceViews[i].Bind(
                    i,
                    question.choices[i].text,
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
            {
                choiceView.SetInteractable(value);
            }
        }

        SetConfirmInteractable(value && _selectedChoiceIndex >= 0);

        if (closeButton != null)
        {
            closeButton.interactable = true;
        }
    }

    public void SetResult(int totalScore, int maxScore)
    {
        if (finalScoreText != null)
        {
            finalScoreText.text = $"{totalScore}/{maxScore}";
        }
    }

    public void SetResult(int totalScore, int correctCount, int totalQuestions)
    {
        if (finalScoreText != null)
        {
            finalScoreText.text = $"{totalScore}";
        }
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
        {
            timerText.gameObject.SetActive(visible);
        }
    }

    public void SetStartInteractable(bool visible)
    {
        if (startButton != null)
        {
            startButton.interactable = visible;
        }
    }

    public void ShowQuitConfirmation(string title, string message)
    {
        _lastQuitTitleFallback = string.IsNullOrWhiteSpace(title) ? "ออกจากควิซ?" : title;
        _lastQuitMessageFallback = string.IsNullOrWhiteSpace(message)
            ? "หากออกจากควิซตอนนี้ คุณจะไม่สามารถเล่นได้อีกเป็นเวลา 24 ชั่วโมง"
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
        {
            quitConfirmPanel.SetActive(false);
        }
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

    private void SetConfirmInteractable(bool value)
    {
        if (confirmButton != null)
        {
            confirmButton.interactable = value;
        }
    }

    private void RefreshLocalizedStaticTexts()
    {
        if (quitConfirmLeaveButtonText != null)
        {
            quitConfirmLeaveButtonText.text = T(
                LocalizationKeys.Quiz.QuitConfirmConfirmButton,
                "ตกลง"
            );
        }

        if (quitConfirmStayButtonText != null)
        {
            quitConfirmStayButtonText.text = T(
                LocalizationKeys.Quiz.QuitConfirmCancelButton,
                "ยกเลิก"
            );
        }
    }

    private void RefreshQuestionCounter()
    {
        if (questionCounterText == null)
            return;

        string format = T(
            LocalizationKeys.Quiz.PlayTotalQuestionsFormat,
            "จำนวนข้อทั้งหมด {0}/{1} ข้อ"
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
            questionCounterText.text = $"จำนวนข้อทั้งหมด {_currentQuestionIndex}/{_totalQuestionCount} ข้อ";
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

    private string T(string key, string fallback)
    {
        string value = LocalizationSettings.StringDatabase.GetLocalizedString(
            LocalizationKeys.Tables.Quiz,
            key
        );

        return string.IsNullOrEmpty(value) ? fallback : value;
    }
}