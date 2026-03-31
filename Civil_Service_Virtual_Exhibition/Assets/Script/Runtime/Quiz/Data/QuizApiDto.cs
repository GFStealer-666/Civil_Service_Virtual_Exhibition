using System;
using System.Collections.Generic;

[Serializable]
public class QuizCurrentResponseDto
{
    public bool success;
    public string message;
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
    public string questionEn;
    public QuizChoicesDto choices;
    public QuizChoicesDto choicesEn;
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
public class QuizCheckResponseDto
{
    public bool success;
    public string message;
    public QuizCheckDataDto data;
}

[Serializable]
public class QuizCheckDataDto
{
    public int setNumber;
    public bool canPlay;
    public bool playedToday;
    public bool playedThisWeek;
    public QuizPlaySessionDto[] sessions;
}

[Serializable]
public class QuizSubmitRequestDto
{
    public float score;
}

[Serializable]
public class QuizSubmitResponseDto
{
    public bool success;
    public string message;
    public QuizSubmitDataDto data;
}

[Serializable]
public class QuizSubmitDataDto
{
    public int setNumber;
    public float score;
    public QuizPlaySessionDto[] sessions;
}

[Serializable]
public class QuizLeaderboardResponseDto
{
    public bool success;
    public string message;
    public QuizLeaderboardDataDto data;
}

[Serializable]
public class QuizLeaderboardDataDto
{
    public LeaderboardEntryDto[] top10;
    public QuizLeaderboardPlayerDto player;
}

[Serializable]
public class LeaderboardEntryDto
{
    public int rank;
    public string characterName;
    public float totalScore;
    public string lastPlayed;
}

[Serializable]
public class QuizLeaderboardPlayerDto
{
    public int rank;
    public float totalScore;
    public QuizSetScoresDto setScores;
}

[Serializable]
public class QuizSessionQuestion
{
    public string questionTextTh;
    public string questionTextEn;
    public List<QuizSessionChoice> choices = new List<QuizSessionChoice>();
    public int correctChoiceIndex;
    public string explanationTh;
    public string explanationEn;

    public string GetQuestionText(bool useEnglish)
    {
        string preferred = useEnglish ? questionTextEn : questionTextTh;
        string fallback = useEnglish ? questionTextTh : questionTextEn;
        return string.IsNullOrWhiteSpace(preferred) ? fallback ?? string.Empty : preferred;
    }

    public string GetExplanationText(bool useEnglish)
    {
        string preferred = useEnglish ? explanationEn : explanationTh;
        string fallback = useEnglish ? explanationTh : explanationEn;
        return string.IsNullOrWhiteSpace(preferred) ? fallback ?? string.Empty : preferred;
    }
}

[Serializable]
public class QuizSessionChoice
{
    public string textTh;
    public string textEn;
    public bool isCorrect;

    public string GetText(bool useEnglish)
    {
        string preferred = useEnglish ? textEn : textTh;
        string fallback = useEnglish ? textTh : textEn;
        return string.IsNullOrWhiteSpace(preferred) ? fallback ?? string.Empty : preferred;
    }
}

[Serializable]
public class QuizMeResponseDto
{
    public bool success;
    public string message;
    public QuizMeDataDto data;
}

[Serializable]
public class QuizMeDataDto
{
    public float totalScore;
    public int setsCompleted;
    public int rank;
    public int totalPlayers;
    public int currentSetNumber;
    public QuizSetScoresDto setScores;
    public QuizPlaySessionDto[] sessions;
    public float? set1Score => setScores != null ? setScores.set1 : null;
    public float? set2Score => setScores != null ? setScores.set2 : null;
    public float? set3Score => setScores != null ? setScores.set3 : null;
    public float? set4Score => setScores != null ? setScores.set4 : null;

    public bool hasTotalScore;
    public bool hasRank;
}

[Serializable]
public class QuizPlaySessionDto
{
    public float score;
    public string playedAt;
}

[Serializable]
public class QuizSetScoresDto
{
    public float set1 = 0;
    public float set2 = 0;
    public float set3 = 0;
    public float set4 = 0;

    public bool HasSet1 => set1 >= 0;
    public bool HasSet2 => set2 >= 0;
    public bool HasSet3 => set3 >= 0;
    public bool HasSet4 => set4 >= 0;
}