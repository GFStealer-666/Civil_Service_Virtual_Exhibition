using TMPro;
using UnityEngine;

public class ChatLineView : MonoBehaviour
{
    [SerializeField] private TMP_Text contentText;

    public void Bind(string senderName, string message)
    {
        if (contentText == null)
            return;

        senderName = string.IsNullOrWhiteSpace(senderName) ? "Guest" : senderName;
        message = string.IsNullOrWhiteSpace(message) ? string.Empty : message.Trim();

        contentText.text = $"<b>{senderName}:</b> {message}";
    }
}