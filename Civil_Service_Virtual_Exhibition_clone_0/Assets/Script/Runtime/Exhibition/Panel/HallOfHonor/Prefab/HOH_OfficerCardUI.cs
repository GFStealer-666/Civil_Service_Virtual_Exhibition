using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Networking;
using UnityEngine.UI;

public class HOH_OfficerCardUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button button;
    [SerializeField] private Image photoImage;
    [SerializeField] private TextMeshProUGUI officerNameText;
    [SerializeField] private UniversalImageLoader photoLoader;

    [Header("Fallback")]
    [SerializeField] private Sprite fallbackPhoto;

    private static readonly Dictionary<string, Sprite> SpriteCache = new();

    private Coroutine _loadRoutine;
    private HOH_PersonDto _boundData;
    private string _pendingPhotoUrl;

    private bool UseEnglish
    {
        get
        {
            var locale = LocalizationSettings.SelectedLocale;
            string code = locale != null ? locale.Identifier.Code : "th";
            return code.StartsWith("en", StringComparison.OrdinalIgnoreCase);
        }
    }

    public void Bind(HOH_PersonDto data, Action<HOH_PersonDto> onClick)
    {
        _boundData = data;

        BindTexts(data);
        BindButton(onClick);
        BindPhoto(data);

        if (photoLoader != null)
            photoLoader.Load(data != null ? data.photoUrl : string.Empty);
    }

    private void OnEnable()
    {
        if (_boundData != null)
            BindTexts(_boundData);

        TryStartPendingPhotoLoad();
    }

    private void OnDisable()
    {
        if (_loadRoutine != null)
        {
            StopCoroutine(_loadRoutine);
            _loadRoutine = null;
        }
    }

    private void BindTexts(HOH_PersonDto data)
    {
        if (officerNameText != null)
            officerNameText.text = BuildOfficerName(data);
    }

    private void BindButton(Action<HOH_PersonDto> onClick)
    {
        if (button == null)
            return;

        button.onClick.RemoveAllListeners();

        if (onClick != null)
            button.onClick.AddListener(() => onClick.Invoke(_boundData));
    }

    private void BindPhoto(HOH_PersonDto data)
    {
        if (photoImage == null)
            return;

        if (_loadRoutine != null)
        {
            StopCoroutine(_loadRoutine);
            _loadRoutine = null;
        }

        string url = data != null ? data.photoUrl : string.Empty;
        _pendingPhotoUrl = url;

        if (string.IsNullOrWhiteSpace(url))
        {
            SetFallbackPhoto();
            return;
        }

        if (SpriteCache.TryGetValue(url, out Sprite cachedSprite) && cachedSprite != null)
        {
            photoImage.sprite = cachedSprite;
            _pendingPhotoUrl = null;
            return;
        }

        SetFallbackPhoto();
        TryStartPendingPhotoLoad();
    }

    private void TryStartPendingPhotoLoad()
    {
        if (!isActiveAndEnabled || !gameObject.activeInHierarchy)
            return;

        if (string.IsNullOrWhiteSpace(_pendingPhotoUrl))
            return;

        if (SpriteCache.TryGetValue(_pendingPhotoUrl, out Sprite cachedSprite) && cachedSprite != null)
        {
            if (photoImage != null)
                photoImage.sprite = cachedSprite;

            _pendingPhotoUrl = null;
            return;
        }

        _loadRoutine = StartCoroutine(LoadPhotoRoutine(_pendingPhotoUrl));
    }

    private IEnumerator LoadPhotoRoutine(string url)
    {
        using UnityWebRequest req = UnityWebRequestTexture.GetTexture(url);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"[HOH_OfficerCardUI] Failed to load officer photo: {url} | {req.error}");
            _loadRoutine = null;
            yield break;
        }

        Texture2D texture = DownloadHandlerTexture.GetContent(req);
        if (texture == null)
        {
            _loadRoutine = null;
            yield break;
        }

        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f)
        );

        if (!SpriteCache.ContainsKey(url))
            SpriteCache.Add(url, sprite);

        if (photoImage != null)
            photoImage.sprite = sprite;

        if (_pendingPhotoUrl == url)
            _pendingPhotoUrl = null;

        _loadRoutine = null;
    }

    private void SetFallbackPhoto()
    {
        if (photoImage != null && fallbackPhoto != null)
            photoImage.sprite = fallbackPhoto;
    }

    private string BuildOfficerName(HOH_PersonDto data)
    {
        if (data == null)
            return string.Empty;

        if (UseEnglish)
        {
            string prefix = FirstNotEmpty(data.prefixEn, data.prefixOther, data.prefix);
            string firstName = FirstNotEmpty(data.firstNameEn, data.firstName);
            string lastName = FirstNotEmpty(data.lastNameEn, data.lastName);
            return JoinNonEmpty(" ", prefix, firstName, lastName);
        }

        string thaiPrefix = !string.IsNullOrWhiteSpace(data.prefixOther) ? data.prefixOther : data.prefix;
        return JoinNonEmpty(" ", thaiPrefix, data.firstName, data.lastName);
    }

    private string FirstNotEmpty(params string[] values)
    {
        if (values == null)
            return string.Empty;

        for (int i = 0; i < values.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(values[i]))
                return values[i];
        }

        return string.Empty;
    }

    private string JoinNonEmpty(string separator, params string[] values)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();

        if (values == null)
            return string.Empty;

        for (int i = 0; i < values.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(values[i]))
                continue;

            if (builder.Length > 0)
                builder.Append(separator);

            builder.Append(values[i].Trim());
        }

        return builder.ToString();
    }
}