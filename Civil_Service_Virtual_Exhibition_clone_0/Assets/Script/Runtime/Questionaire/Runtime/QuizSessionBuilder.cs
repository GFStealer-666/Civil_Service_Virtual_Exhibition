using System.Collections.Generic;
using UnityEngine;

public static class QuizSessionBuilder
{
    public static List<QuizSessionQuestion> BuildFromFallback(QuizGameConfigSO config)
    {
        List<QuizQuestionSO> source = new List<QuizQuestionSO>(config.fallbackQuestions);

        if (config.shuffleQuestionOrder)
        {
            Shuffle(source);
        }

        int count = Mathf.Min(config.questionsPerSession, source.Count);

        List<QuizSessionQuestion> result = new List<QuizSessionQuestion>(count);

        for (int i = 0; i < count; i++)
        {
            QuizQuestionSO so = source[i];

            List<QuizSessionChoice> runtimeChoices = new List<QuizSessionChoice>(4);
            for (int c = 0; c < so.choices.Length; c++)
            {
                runtimeChoices.Add(new QuizSessionChoice
                {
                    text = so.choices[c].text,
                    isCorrect = c == so.correctChoiceIndex
                });
            }

            if (config.shuffleChoiceOrder)
            {
                Shuffle(runtimeChoices);
            }

            int correctIndex = 0;
            for (int c = 0; c < runtimeChoices.Count; c++)
            {
                if (runtimeChoices[c].isCorrect)
                {
                    correctIndex = c;
                    break;
                }
            }

            QuizSessionQuestion question = new QuizSessionQuestion
            {
                questionText = so.questionText,
                choices = runtimeChoices,
                correctChoiceIndex = correctIndex,
                explanation = so.explanation
            };

            result.Add(question);
        }

        return result;
    }

    public static void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int swapIndex = Random.Range(0, i + 1);
            (list[i], list[swapIndex]) = (list[swapIndex], list[i]);
        }
    }
}