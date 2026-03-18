using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
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

    [Header("Result")]
    [SerializeField] private TMP_Text finalScoreText;
    [SerializeField] private TMP_Text finalSummaryText;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button closeButton;

    private int _selectedChoiceIndex = -1;

    public event Action StartClicked;
    public event Action<int> ConfirmClicked;
    public event Action RetryClicked;
    public event Action CloseClicked;

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

        if (retryButton != null)
        {
            retryButton.onClick.AddListener(() => RetryClicked?.Invoke());
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(() => CloseClicked?.Invoke());
        }

        SetConfirmInteractable(false);
    }

    public void ShowStart()
    {
        Debug.Log("[QuizUIOverlay] ShowStart called");

        if (startPanel != null)
        {
            startPanel.SetActive(true);
            Debug.Log($"[QuizUIOverlay] startPanel active = {startPanel.activeSelf}");
        }

        if (questionPanel != null)
        {
            questionPanel.SetActive(false);
            Debug.Log($"[QuizUIOverlay] questionPanel active = {questionPanel.activeSelf}");
        }

        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
            Debug.Log($"[QuizUIOverlay] resultPanel active = {resultPanel.activeSelf}");
        }
    }

    public void ShowQuestion()
    {
        Debug.Log("[QuizUIOverlay] ShowQuestion called");

        if (startPanel != null)
        {
            startPanel.SetActive(false);
            Debug.Log($"[QuizUIOverlay] startPanel active = {startPanel.activeSelf}");
        }

        if (questionPanel != null)
        {
            questionPanel.SetActive(true);
            Debug.Log($"[QuizUIOverlay] questionPanel active = {questionPanel.activeSelf}");
            questionPanel.transform.SetAsLastSibling();
        }

        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
            Debug.Log($"[QuizUIOverlay] resultPanel active = {resultPanel.activeSelf}");
        }
    }

    public void ShowResult()
    {
        Debug.Log("[QuizUIOverlay] ShowResult called");

        if (startPanel != null)
        {
            startPanel.SetActive(false);
            Debug.Log($"[QuizUIOverlay] startPanel active = {startPanel.activeSelf}");
        }

        if (questionPanel != null)
        {
            questionPanel.SetActive(false);
            Debug.Log($"[QuizUIOverlay] questionPanel active = {questionPanel.activeSelf}");
        }

        if (resultPanel != null)
        {
            resultPanel.SetActive(true);
            Debug.Log($"[QuizUIOverlay] resultPanel active = {resultPanel.activeSelf}");
        }
    }

    public void BindQuestion(QuizSessionQuestion question, int currentIndex, int totalCount)
    {
        _selectedChoiceIndex = -1;
        SetConfirmInteractable(false);

        if (questionCounterText != null)
        {
            questionCounterText.text = $"จำนวนข้อทั้งหมด <b>{currentIndex}/{totalCount}</b> ข้อ";
        }

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
        if (timerText == null) return;

        // Display only whole seconds (drop milliseconds)
        int seconds = Mathf.Max(0, Mathf.FloorToInt(remainingSeconds));
        timerText.text = seconds.ToString();
    }

    public void SetQuestionInteractable(bool value)
    {
        foreach (QuizChoiceToggleView choiceView in choiceViews)
        {
            if (choiceView.gameObject.activeSelf)
            {
                choiceView.SetInteractable(value);
            }
        }

        SetConfirmInteractable(value && _selectedChoiceIndex >= 0);
    }

    public void SetResult(int totalScore, int correctCount, int totalQuestions)
    {
        if (finalScoreText != null)
        {
            finalScoreText.text = $"Score: {totalScore}";
        }

        if (finalSummaryText != null)
        {
            finalSummaryText.text = $"Correct {correctCount}/{totalQuestions}";
        }
    }
    public void ResetToggle()
    {
        toggleGroup.SetAllTogglesOff();
    }
    private void HandleChoiceToggleChanged(int choiceIndex, bool isOn)
    {
        if (!isOn) return;

        _selectedChoiceIndex = choiceIndex;
        SetConfirmInteractable(true);
    }

    private void HandleConfirmClicked()
    {
        if (_selectedChoiceIndex < 0)
        {
            return;
        }

        ConfirmClicked?.Invoke(_selectedChoiceIndex);
    }

    private void SetConfirmInteractable(bool value)
    {
        if (confirmButton != null)
        {
            confirmButton.interactable = value;
        }
    }
    public void SetTimerVisible(bool visible)
    {
        if (timerText != null)
        {
            timerText.gameObject.SetActive(visible);
        }
    }
}