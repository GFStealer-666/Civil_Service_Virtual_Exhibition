using System;

[Serializable]
public class QuizApiResponseDto
{
    public string version;
    public string generatedAtUtc;
    public QuizApiQuestionDto[] questions;
}

[Serializable]
public class QuizApiQuestionDto
{
    public string id;
    public string questionText;
    public string[] choices;
    public int correctChoiceIndex;
    public string explanation;
}


[Serializable]
public class QuizCacheEnvelope
{
    public string rawJson;
    public long savedAtUnixSeconds;
}