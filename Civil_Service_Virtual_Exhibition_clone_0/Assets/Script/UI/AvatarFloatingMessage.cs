using System;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class AvatarFloatingMessage : MonoBehaviour
{
    public enum LanguageMode
    {
        Auto,
        Thai,
        English
    }

    [Header("References")]
    [SerializeField] private Transform messageRoot;
    [SerializeField] private Canvas worldCanvas;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Camera targetCamera;

    [Header("Timing")]
    [SerializeField] private bool playOnStart = true;

    [Header("Floating")]
    [SerializeField] private bool allowFloating = false;
    [SerializeField] private float floatHeight = 0.2f;
    [SerializeField] private float floatSpeed = 1.2f;

    [Header("Localization")]
    [SerializeField] private LanguageMode languageMode = LanguageMode.Auto;

    [Header("Message")]
    [TextArea]
    [SerializeField] private string singleMessage = "Can I help you?";

    [TextArea]
    [SerializeField] private string singleMessageThai = "มีอะไรให้ช่วยไหม";

    [TextArea]
    [SerializeField] private string singleMessageEnglish = "Can I help you?";

    private Vector3 _baseWorldPosition;
    private bool _isShowing;

    private string _currentThaiMessage = string.Empty;
    private string _currentEnglishMessage = string.Empty;
    private bool _useLocalizedPair;
    private bool _suppressLocaleRefresh;

    private void Awake()
    {
        if (messageRoot == null)
            messageRoot = transform;

        if (targetCamera == null)
            targetCamera = Camera.main;

        _baseWorldPosition = messageRoot.position;
        SetVisible(false);
    }

    private void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += HandleLocaleChanged;
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= HandleLocaleChanged;
    }

    private void Start()
    {
        if (playOnStart)
            ShowLocalizedSingleMessage();
    }

    private void LateUpdate()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        FaceCamera();

        if (_isShowing && allowFloating)
            UpdateFloating();
    }

    private void HandleLocaleChanged(Locale locale)
    {
        if (_suppressLocaleRefresh)
            return;

        RefreshVisibleMessage();
    }

    public void ShowLocalizedSingleMessage()
    {
        _useLocalizedPair = true;
        _currentThaiMessage = string.IsNullOrWhiteSpace(singleMessageThai) ? singleMessage : singleMessageThai;
        _currentEnglishMessage = string.IsNullOrWhiteSpace(singleMessageEnglish) ? singleMessage : singleMessageEnglish;

        ApplyCurrentLocalizedMessage();
        ShowInternal();
    }

    public void ShowMessage(string customMessage)
    {
        if (string.IsNullOrWhiteSpace(customMessage))
            return;

        _useLocalizedPair = false;
        _currentThaiMessage = string.Empty;
        _currentEnglishMessage = string.Empty;

        if (messageText != null)
            messageText.text = customMessage;

        ShowInternal();
    }

    public void ShowMessage(string thaiMessage, string englishMessage)
    {
        _useLocalizedPair = true;
        _currentThaiMessage = thaiMessage;
        _currentEnglishMessage = englishMessage;

        ApplyCurrentLocalizedMessage();
        ShowInternal();
    }

    public void HideMessage()
    {
        if (!_isShowing)
            return;

        _isShowing = false;
        SetVisible(false);
    }

    public void RefreshVisibleMessage()
    {
        if (!_isShowing)
            return;

        if (!_useLocalizedPair)
            return;

        ApplyCurrentLocalizedMessage();
    }

    private void ApplyCurrentLocalizedMessage()
    {
        string localizedMessage = GetLocalizedMessage(_currentThaiMessage, _currentEnglishMessage);

        if (string.IsNullOrWhiteSpace(localizedMessage))
            return;

        if (messageText != null)
            messageText.text = localizedMessage;
    }

    private void ShowInternal()
    {
        _isShowing = true;
        _baseWorldPosition = messageRoot.position;
        SetVisible(true);
    }

    private void UpdateFloating()
    {
        float offsetY = Mathf.Sin(Time.time * floatSpeed) * floatHeight;
        messageRoot.position = _baseWorldPosition + Vector3.up * offsetY;
    }

    private void FaceCamera()
    {
        if (messageRoot == null || targetCamera == null)
            return;

        Vector3 direction = messageRoot.position - targetCamera.transform.position;
        if (direction.sqrMagnitude <= 0.0001f)
            return;

        // messageRoot.rotation = Quaternion.LookRotation(direction);
    }

    private string GetLocalizedMessage(string thaiMessage, string englishMessage)
    {
        bool useThai = IsThaiLanguage();

        if (useThai)
        {
            if (!string.IsNullOrWhiteSpace(thaiMessage))
                return thaiMessage;

            if (!string.IsNullOrWhiteSpace(singleMessageThai))
                return singleMessageThai;

            if (!string.IsNullOrWhiteSpace(singleMessage))
                return singleMessage;

            return englishMessage ?? string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(englishMessage))
            return englishMessage;

        if (!string.IsNullOrWhiteSpace(singleMessageEnglish))
            return singleMessageEnglish;

        if (!string.IsNullOrWhiteSpace(singleMessage))
            return singleMessage;

        return thaiMessage ?? string.Empty;
    }

    private bool IsThaiLanguage()
    {
        switch (languageMode)
        {
            case LanguageMode.Thai:
                return true;

            case LanguageMode.English:
                return false;

            default:
                var locale = LocalizationSettings.SelectedLocale;
                if (locale == null)
                    return true;

                string code = locale.Identifier.Code;
                return !string.IsNullOrEmpty(code) &&
                       code.StartsWith("th", StringComparison.OrdinalIgnoreCase);
        }
    }

    private void SetVisible(bool visible)
    {
        if (messageRoot != null)
            messageRoot.gameObject.SetActive(visible);

        if (worldCanvas != null)
            worldCanvas.enabled = visible;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.blocksRaycasts = visible;
            canvasGroup.interactable = visible;
        }
    }
}