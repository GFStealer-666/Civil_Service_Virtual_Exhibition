using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "QuizGameConfig",
    menuName = "Minigame/Quiz/Game Config"
)]
public class QuizGameConfigSO : ScriptableObject
{
    [Header("Session")]
    [Min(1)] public int questionsPerSession = 10;

    [Header("Timer")]
    public bool isTimeLimited = true;
    [Min(1f)] public float sessionTimeLimitSeconds = 240f;

    [Header("Behavior")]
    public bool shuffleQuestionOrder = true;
    public bool shuffleChoiceOrder = true;

    [Header("Scoring")]
    public int scorePerCorrectAnswer = 100;

    [Header("Fallback Questions")]
    public List<QuizQuestionSO> fallbackQuestions = new List<QuizQuestionSO>();
}