using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

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

    [Header("Language")]
    [SerializeField] private LanguageMode languageMode = LanguageMode.Auto;

    [Header("Timing")]
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private float minDelayBetweenMessages = 6f;
    [SerializeField] private float maxDelayBetweenMessages = 14f;
    [SerializeField] private float messageLifetime = 20f;

    [Header("Floating")]
    [SerializeField] private float floatHeight = 0.2f;
    [SerializeField] private float floatSpeed = 1.2f;

    [Header("Fade")]
    [SerializeField] private float fadeDuration = 0.35f;

    [Header("Thai Messages")]
    [SerializeField] private List<string> thaiMessages = new List<string>
    {
        "มีอะไรให้ช่วยไหมคะ",
        "สนใจดูข้อมูลเพิ่มเติมไหมคะ",
        "ยินดีให้คำแนะนำค่ะ",
        "ต้องการความช่วยเหลือไหมคะ"
    };

    [Header("English Messages")]
    [SerializeField] private List<string> englishMessages = new List<string>
    {
        "Can I help you?",
        "Would you like more information?",
        "I am happy to assist you.",
        "Need any help?"
    };

    private Vector3 _baseWorldPosition;
    private float _nextMessageTime;
    private float _messageStartTime;
    private bool _isShowing;
    private string _lastMessage = string.Empty;
    private Coroutine _fadeRoutine;

    private void Awake()
    {
        if (messageRoot == null)
            messageRoot = transform;

        if (targetCamera == null)
            targetCamera = Camera.main;

        _baseWorldPosition = messageRoot.position;
        SetVisibleImmediate(false);
    }

    private void Start()
    {
        ScheduleNextMessage(playOnStart ? 0.5f : GetRandomDelay());
    }

    private void LateUpdate()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        FaceCamera();

        if (_isShowing)
        {
            UpdateFloating();
            UpdateLifetime();
        }
        else if (Time.time >= _nextMessageTime)
        {
            ShowRandomMessage();
        }
    }

    public void ShowRandomMessage()
    {
        List<string> source = GetMessageSource();
        if (source == null || source.Count == 0)
            return;

        string selected = PickRandomMessage(source);
        if (string.IsNullOrWhiteSpace(selected))
            return;

        _lastMessage = selected;
        _messageStartTime = Time.time;
        _isShowing = true;

        if (messageText != null)
            messageText.text = selected;

        _baseWorldPosition = messageRoot.position;
        StartFade(1f);
    }

    public void ShowMessage(string customMessage)
    {
        if (string.IsNullOrWhiteSpace(customMessage))
            return;

        _lastMessage = customMessage;
        _messageStartTime = Time.time;
        _isShowing = true;

        if (messageText != null)
            messageText.text = customMessage;

        _baseWorldPosition = messageRoot.position;
        StartFade(1f);
    }

    public void HideMessage()
    {
        if (!_isShowing)
            return;

        _isShowing = false;
        StartFade(0f);
        ScheduleNextMessage(GetRandomDelay());
    }

    private void UpdateFloating()
    {
        float offsetY = Mathf.Sin((Time.time - _messageStartTime) * floatSpeed) * floatHeight;
        messageRoot.position = _baseWorldPosition + Vector3.up * offsetY;
    }

    private void UpdateLifetime()
    {
        if (Time.time - _messageStartTime >= messageLifetime)
            HideMessage();
    }

    private void FaceCamera()
    {
        if (messageRoot == null || targetCamera == null)
            return;

        Vector3 direction = messageRoot.position - targetCamera.transform.position;
        if (direction.sqrMagnitude <= 0.0001f)
            return;

        //messageRoot.rotation = Quaternion.LookRotation(direction);
    }

    private List<string> GetMessageSource()
    {
        switch (languageMode)
        {
            case LanguageMode.Thai:
                return thaiMessages;

            case LanguageMode.English:
                return englishMessages;

            default:
                return IsThaiLanguage() ? thaiMessages : englishMessages;
        }
    }

    private bool IsThaiLanguage()
    {
        string localeCode = LocalizationService.CurrentLocaleCode;

        if (string.IsNullOrWhiteSpace(localeCode))
            return false;

        return localeCode.StartsWith("th", StringComparison.OrdinalIgnoreCase);
    }

    private string PickRandomMessage(List<string> source)
    {
        if (source.Count == 1)
            return source[0];

        string result = source[UnityEngine.Random.Range(0, source.Count)];

        int safeGuard = 0;
        while (result == _lastMessage && safeGuard < 10)
        {
            result = source[UnityEngine.Random.Range(0, source.Count)];
            safeGuard++;
        }

        return result;
    }

    private float GetRandomDelay()
    {
        return UnityEngine.Random.Range(minDelayBetweenMessages, maxDelayBetweenMessages);
    }

    private void ScheduleNextMessage(float delay)
    {
        _nextMessageTime = Time.time + Mathf.Max(0f, delay);
    }

    private void StartFade(float targetAlpha)
    {
        if (_fadeRoutine != null)
            StopCoroutine(_fadeRoutine);

        _fadeRoutine = StartCoroutine(FadeCanvas(targetAlpha, fadeDuration));
    }

    private void SetVisibleImmediate(bool visible)
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

    private IEnumerator FadeCanvas(float targetAlpha, float duration)
    {
        if (messageRoot != null && !messageRoot.gameObject.activeSelf)
            messageRoot.gameObject.SetActive(true);

        if (worldCanvas != null)
            worldCanvas.enabled = true;

        if (canvasGroup == null)
        {
            bool visibleWithoutCanvasGroup = targetAlpha > 0.001f;

            if (messageRoot != null)
                messageRoot.gameObject.SetActive(visibleWithoutCanvasGroup);

            if (worldCanvas != null)
                worldCanvas.enabled = visibleWithoutCanvasGroup;

            yield break;
        }

        float startAlpha = canvasGroup.alpha;
        float elapsed = 0f;

        if (targetAlpha > 0.001f)
        {
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = duration <= 0.0001f ? 1f : Mathf.Clamp01(elapsed / duration);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;

        bool visible = targetAlpha > 0.001f;
        canvasGroup.blocksRaycasts = visible;
        canvasGroup.interactable = visible;

        if (worldCanvas != null)
            worldCanvas.enabled = visible;

        if (messageRoot != null)
            messageRoot.gameObject.SetActive(visible);

        _fadeRoutine = null;
    }
}