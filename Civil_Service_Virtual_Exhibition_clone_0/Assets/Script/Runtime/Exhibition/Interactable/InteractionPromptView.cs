using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InteractionPromptView : MonoBehaviour
{
    [Header("Desktop")]
    [SerializeField] private GameObject desktopRoot;
    [SerializeField] private TMP_Text desktopText;

    [Header("Mobile")]
    [SerializeField] private GameObject mobileRoot;
    [SerializeField] private TMP_Text mobileText;
    [SerializeField] private Button mobileButton;

    public Button MobileButton => mobileButton;

    private void Awake()
    {
        Hide();
    }

    public void Show(string prompt, bool showMobile)
    {
        if (desktopRoot != null)
            desktopRoot.SetActive(!showMobile);

        if (mobileRoot != null)
            mobileRoot.SetActive(showMobile);

        if (!showMobile && desktopText != null)
            desktopText.text = prompt;

        if (showMobile && mobileText != null)
            mobileText.text = prompt;
    }

    public void Hide()
    {
        if (desktopRoot != null)
            desktopRoot.SetActive(false);

        if (mobileRoot != null)
            mobileRoot.SetActive(false);
    }
}