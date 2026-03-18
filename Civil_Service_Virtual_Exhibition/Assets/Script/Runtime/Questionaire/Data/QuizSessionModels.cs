using System;
using System.Collections.Generic;

[Serializable]
public class QuizSessionQuestion // each question 
{
    public string questionText;
    public List<QuizSessionChoice> choices = new List<QuizSessionChoice>();
    public int correctChoiceIndex;
    public string explanation;
}

[Serializable]
public class QuizSessionChoice
{
    public string text;
    public bool isCorrect;
}