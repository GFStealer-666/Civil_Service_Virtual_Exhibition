using System;
using System.Collections.Generic;

[Serializable]
public class QuizCurrentResponseDto
{
    public bool success;
    public QuizCurrentDataDto data;
}

[Serializable]
public class QuizCurrentDataDto
{
    public int setNumber;
    public int totalQuestions;
    public QuizQuestionDto[] questions;
}

[Serializable]
public class QuizQuestionDto
{
    public int id;
    public string question;
    public QuizChoicesDto choices;
    public string answer;
}

[Serializable]
public class QuizChoicesDto
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

[Serializable]
public class QuizSubmitRequestDto
{
    public int score;
}

[Serializable]
public class QuizSubmitResponseDto
{
    public bool success;
    public QuizSubmitDataDto data;
}

[Serializable]
public class QuizSubmitDataDto
{
    public int setNumber;
    public int score;
    public int maxScore;
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
    public LeaderboardEntryDto[] top10;
    public LeaderboardEntryDto player;
}

[Serializable]
public class LeaderboardEntryDto
{
    public int rank;
    public string characterName;
    public int totalScore;
    public string lastPlayed;
}

[Serializable]
public class QuizSessionQuestion
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

[Serializable]
public class QuizMeResponseDto
{
    public bool success;
    public QuizMeDataDto data;
}

[Serializable]
public class QuizMeDataDto
{
    public int totalScore;
    public int setsCompleted;
    public int maxScore;
    public int rank;
    public int totalPlayers;
}
