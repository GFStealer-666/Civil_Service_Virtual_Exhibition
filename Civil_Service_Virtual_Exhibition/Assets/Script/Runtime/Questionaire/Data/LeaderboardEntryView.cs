using TMPro;
using UnityEngine;

public class LeaderboardEntryView : MonoBehaviour
{
    [SerializeField] private TMP_Text rankText;
    [SerializeField] private TMP_Text usernameText;
    [SerializeField] private TMP_Text scoreText;

    public void Bind(LeaderboardEntryDto data)
    {
        if (data == null) return;

        if (rankText != null)
            rankText.text = data.rank.ToString();

        if (usernameText != null)
            usernameText.text = string.IsNullOrWhiteSpace(data.username) ? "-" : data.username;

        if (scoreText != null)
            scoreText.text = $"{data.score} คะแนน";
    }
}