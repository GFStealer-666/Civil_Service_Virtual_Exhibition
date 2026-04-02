using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
public class DeleteAccountPanelController : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject panelRoot;

    [Header("Buttons")]
    [SerializeField] private Button openButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button confirmDeleteButton;
    [SerializeField] private Button cancelButton;

    [Header("Texts")]
    [SerializeField] private TMP_Text headerText;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private TMP_Text confirmDeleteButtonText;
    [SerializeField] private TMP_Text cancelButtonText;

    [Header("Overlay")]
    [SerializeField] private StatusOverlay statusOverlay;


    private bool _busy;

    private void Awake()
    {
        if (openButton != null)
            openButton.onClick.AddListener(Open);

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        if (cancelButton != null)
            cancelButton.onClick.AddListener(Close);

        if (confirmDeleteButton != null)
            confirmDeleteButton.onClick.AddListener(OnConfirmDeletePressed);

        ApplyLocalization();
        HideImmediate();
    }

    private void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += HandleLocaleChanged;
        ApplyLocalization();
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= HandleLocaleChanged;
    }

    private void OnDestroy()
    {
        if (openButton != null)
            openButton.onClick.RemoveListener(Open);

        if (closeButton != null)
            closeButton.onClick.RemoveListener(Close);

        if (cancelButton != null)
            cancelButton.onClick.RemoveListener(Close);

        if (confirmDeleteButton != null)
            confirmDeleteButton.onClick.RemoveListener(OnConfirmDeletePressed);
    }

    private void HandleLocaleChanged(UnityEngine.Localization.Locale locale)
    {
        ApplyLocalization();
    }

    public void Open()
    {
        if (_busy)
            return;

        ApplyLocalization();

        if (panelRoot != null)
            panelRoot.SetActive(true);
    }

    public void Close()
    {
        if (_busy)
            return;

        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    public void HideImmediate()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    private void ApplyLocalization()
    {
        bool isThai = IsThaiLanguage();

        if (headerText != null)
            headerText.text = isThai ? "ตั้งค่า" : "Settings";

        if (titleText != null)
            titleText.text = isThai
                ? "ยืนยันการลบบัญชีผู้ใช้"
                : "Confirm Account Deletion";

        if (messageText != null)
            messageText.text = isThai
                ? "หากคุณลบบัญชีนี้แล้ว\nจะไม่สามารถกู้ข้อมูลกลับคืนมาได้"
                : "If you delete this account,\nyou will not be able to recover the data.";

        if (confirmDeleteButtonText != null)
            confirmDeleteButtonText.text = isThai ? "ตกลง" : "Confirm";

        if (cancelButtonText != null)
            cancelButtonText.text = isThai ? "ยกเลิก" : "Cancel";
    }

    private bool IsThaiLanguage()
    {
        var locale = LocalizationSettings.SelectedLocale;
        if (locale == null)
            return true;

        string code = locale.Identifier.Code;
        return !string.IsNullOrEmpty(code) &&
               code.StartsWith("th", StringComparison.OrdinalIgnoreCase);
    }

    private void OnConfirmDeletePressed()
    {
        if (_busy)
            return;

        if (IsGuestUser())
        {
            HideImmediate();

            if (statusOverlay != null)
            {
                statusOverlay.ShowFailed(
                    IsThaiLanguage() ? "ไม่พบสิทธิ์การเข้าใช้งาน" : "Access Denied",
                    IsThaiLanguage()
                        ? "ไม่สามารถลบบัญชีได้เนื่องจากคุณกำลังใช้งานในโหมดผู้เยี่ยมชม"
                        : "Account deletion is not available for guest users.",
                    onDismissed: null,
                    showBlocker: false
                );
            }

            return;
        }

        string token = GetAccessToken();
        if (string.IsNullOrWhiteSpace(token))
        {
            HideImmediate();
            if (statusOverlay != null)
            {
                statusOverlay.ShowFailed(
                    IsThaiLanguage() ? "ไม่พบสิทธิ์การเข้าใช้งาน" : "Missing Authorization",
                    IsThaiLanguage()
                        ? "ไม่พบโทเคนสำหรับลบบัญชี"
                        : "No access token was found for account deletion.",
                    onDismissed: null,
                    showBlocker: false
                );
            }

            return;
        }
        
        StartCoroutine(DeleteAccountRoutine(token));
    }

    private IEnumerator DeleteAccountRoutine(string accessToken)
    {
        _busy = true;
        SetInteractable(false);

        if (statusOverlay != null)
        {
            statusOverlay.ShowLoading(
                IsThaiLanguage() ? "กำลังลบบัญชี" : "Deleting Account",
                IsThaiLanguage() ? "กรุณารอสักครู่" : "Please wait",
                showBlocker: true,
                cancelable: false,
                cancelButtonLabel: IsThaiLanguage() ? "ยกเลิก" : "Cancel",
                onCancel: null,
                animateDots: true
            );
        }

        if (ApiService.Instance == null)
        {
            HandleDeleteFailed(IsThaiLanguage() ? "ไม่พบ ApiService" : "ApiService was not found.");
            yield break;
        }

        using UnityWebRequest request = ApiService.Instance.Delete(ApiService.Instance.DeleteAccountUrl, accessToken);
        yield return request.SendWebRequest();

        bool success =
            request.result == UnityWebRequest.Result.Success &&
            request.responseCode >= 200 &&
            request.responseCode < 300;

        string responseText = request.downloadHandler != null
            ? request.downloadHandler.text
            : string.Empty;

        if (success)
        {
            HandleDeleteSuccess();
            yield break;
        }

        string errorMessage = ExtractErrorMessage(request, responseText);
        HandleDeleteFailed(errorMessage);

        
    }

    private void HandleDeleteSuccess()
    {
        ClearSavedSession();

        if (statusOverlay != null)
        {
            statusOverlay.ShowSuccess(
                IsThaiLanguage() ? "ลบบัญชีสำเร็จ" : "Account Deleted",
                IsThaiLanguage()
                    ? "บัญชีของคุณถูกลบเรียบร้อยแล้ว"
                    : "Your account has been deleted successfully.",
                autoDismiss: true,
                onDone: null,
                animateDots: false,
                showBlocker: true
            );
        }

        HideImmediate();
        _busy = false;
        SetInteractable(true);
        
        SceneManager.LoadScene("LandingPage");
        // redirect scene here if needed
    }
    
    private void HandleDeleteFailed(string apiMessage)
    {
        if (statusOverlay != null)
        {
            statusOverlay.ShowFailed(
                IsThaiLanguage() ? "ลบบัญชีไม่สำเร็จ" : "Delete Failed",
                string.IsNullOrWhiteSpace(apiMessage)
                    ? (IsThaiLanguage()
                        ? "ไม่สามารถลบบัญชีได้ในขณะนี้"
                        : "Unable to delete account right now.")
                    : apiMessage,
                onDismissed: null,
                showBlocker: false
            );
        }

        _busy = false;
        SetInteractable(true);
    }

    private void SetInteractable(bool value)
    {
        if (openButton != null)
            openButton.interactable = value;

        if (closeButton != null)
            closeButton.interactable = value;

        if (confirmDeleteButton != null)
            confirmDeleteButton.interactable = value;

        if (cancelButton != null)
            cancelButton.interactable = value;
    }

    private string ExtractErrorMessage(UnityWebRequest request, string responseText)
    {
        if (!string.IsNullOrWhiteSpace(responseText))
            return responseText;

        if (!string.IsNullOrWhiteSpace(request.error))
            return request.error;

        return IsThaiLanguage()
            ? "เกิดข้อผิดพลาดที่ไม่ทราบสาเหตุ"
            : "Unknown error.";
    }

    private bool IsGuestUser()
    {
        return LocalPlayerData.Instance != null && LocalPlayerData.Instance.IsGuest;
    }

    private string GetAccessToken()
    {
        if (LocalPlayerData.Instance == null)
            return string.Empty;

        return string.IsNullOrWhiteSpace(LocalPlayerData.Instance.PlayerToken)
            ? string.Empty
            : LocalPlayerData.Instance.PlayerToken.Trim();
    }

    private void ClearSavedSession()
    {
        if (LocalPlayerData.Instance != null)
            LocalPlayerData.Instance.ClearForSignOut();
    }
}