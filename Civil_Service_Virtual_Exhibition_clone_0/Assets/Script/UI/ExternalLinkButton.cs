using UnityEngine;

public class ExternalLinkButton : MonoBehaviour
{
    [SerializeField] private string url = "https://thaicivilserviceday.mhesi.go.th/privacy";

    public void OpenLink()
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            Debug.LogWarning("[ExternalLinkButton] URL is empty.");
            return;
        }

        Application.OpenURL(url);
    }
}