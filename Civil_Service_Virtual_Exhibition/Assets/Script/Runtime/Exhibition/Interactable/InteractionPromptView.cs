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
    [SerializeField] private Button mobileButton;

    public Button MobileButton => mobileButton;

    private void Awake()
    {
        Hide();
    }
    public void Show(bool showMobile)
    {
        if (desktopRoot != null)
            desktopRoot.SetActive(!showMobile);

        if (mobileRoot != null)
            mobileRoot.SetActive(showMobile);
    }
    public void Hide()
    {
        if (desktopRoot != null)
            desktopRoot.SetActive(false);

        if (mobileRoot != null)
            mobileRoot.SetActive(false);
    }
}