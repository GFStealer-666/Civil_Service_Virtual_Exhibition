using TMPro;
using UnityEngine;

public class PortalPromptView : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text promptText;

    public void Show(string text)
    {
        if (root != null && !root.activeSelf)
            root.SetActive(true);

        if (promptText != null)
            promptText.text = text;
    }

    public void Hide()
    {
        if (root != null && root.activeSelf)
            root.SetActive(false);
    }
}