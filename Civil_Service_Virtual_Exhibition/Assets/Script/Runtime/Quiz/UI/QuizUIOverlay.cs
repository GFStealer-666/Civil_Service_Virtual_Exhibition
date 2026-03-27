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
    [SerializeField] private Button closeButton;

    [Header("Result")]
    [SerializeField] private TMP_Text finalScoreText;

    [Header("Quit Confirm")]
    [SerializeField] private GameObject quitConfirmPanel;
    [SerializeField] private TMP_Text quitConfirmTitleText;
    [SerializeField] private TMP_Text quitConfirmMessageText;
    [SerializeField] private Button quitConfirmLeaveButton;
    [SerializeField] private Button quitConfirmStayButton;

    private int _selectedChoiceIndex = -1;

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
            finalScoreText.text = $"Score: {totalScore}";
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
        if (quitConfirmTitleText != null)
            quitConfirmTitleText.text = title ?? string.Empty;

        if (quitConfirmMessageText != null)
            quitConfirmMessageText.text = message ?? string.Empty;

        if (quitConfirmPanel != null)
        {
            quitConfirmPanel.SetActive(true);
            quitConfirmPanel.transform.SetAsLastSibling();
        }
    }

    public void HideQuitConfirmation()
    {
        if (quitConfirmPanel != null)
        {
            quitConfirmPanel.SetActive(false);
        }
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
}