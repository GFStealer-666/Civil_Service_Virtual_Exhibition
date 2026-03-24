using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "QuizQuestion",
    menuName = "Minigame/Quiz/Question"
)]
public class QuizQuestionSO : ScriptableObject
{
    [TextArea(2, 5)]
    public string questionText;

    public ChoiceData[] choices = new ChoiceData[4];

    [Range(0, 3)]
    public int correctChoiceIndex;

    [TextArea(2, 5)]
    public string explanation;

    [Serializable]
    public struct ChoiceData
    {
        public string text;
    }
}