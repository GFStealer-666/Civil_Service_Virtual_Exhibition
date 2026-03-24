using System;
using System.Collections.Generic;
[Serializable]
public class QuizApiResponseDto
{
    public bool success;
    public QuizApiDataDto data;
}

[Serializable]
public class QuizApiDataDto
{
    public int setNumber;
    public int totalQuestions;
    public QuizApiQuestionDto[] questions;
}

[Serializable]
public class QuizApiQuestionDto
{
    public int id;
    public string question;
    public QuizApiChoicesDto choices;
    public string answer;
}

[Serializable]
public class QuizApiChoicesDto
{
    public string a;
    public string b;
    public string c;
    public string d;
}

[Serializable]
public class QuizCacheEnvelope
{
    public string rawJson;
    public long savedAtUnixSeconds;
}

// Quiz Leaderboard 

[Serializable]
public class QuizSubmitRequestDto
{
    public int score;
    public int totalQuestions;
}

[Serializable]
public class QuizSubmitResponseDto
{
    public bool success;
    public string message;
}

[Serializable]
public class QuizLeaderboardResponseDto
{
    public bool success;
    public QuizLeaderboardDataDto data;
}

[Serializable]
public class QuizLeaderboardDataDto
{
    // Rename this field if your backend uses another name
    public LeaderboardEntryDto[] leaderboard;

    public int myRank;
    public int myScore;
}

[Serializable]
public class LeaderboardEntryDto
{
    public int rank;
    public string username;
    public int score;
}


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