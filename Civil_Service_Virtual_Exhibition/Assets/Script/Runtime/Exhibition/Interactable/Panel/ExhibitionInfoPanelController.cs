using TMPro;
using UnityEngine;

public class ExhibitionInfoPanelController : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject panelRoot;

    [Header("Content")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;

    [Header("Optional")]
    [SerializeField] private GameObject mobileCanvasToHide;
    [SerializeField] private bool unlockCursorOnOpen = true;

    public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

    private void Awake()
    {
        Close();
    }

    public void Open(string title, string body)
    {
        if (titleText != null)
            titleText.text = title;

        if (bodyText != null)
            bodyText.text = body;

        if (panelRoot != null)
            panelRoot.SetActive(true);

        if (mobileCanvasToHide != null)
            mobileCanvasToHide.SetActive(false);

        PlayerInput.GameplayInputBlocked = true;

        if (unlockCursorOnOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public void Close()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);

        if (mobileCanvasToHide != null)
            mobileCanvasToHide.SetActive(true);

        PlayerInput.GameplayInputBlocked = false;

        if (!InputModeResolver.UseMobileInput())
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private bool UseMobileInput()
    {
        if (MobileInputState.Instance != null)
            return MobileInputState.Instance.UseMobileInput;

        return Application.isMobilePlatform;
    }
}