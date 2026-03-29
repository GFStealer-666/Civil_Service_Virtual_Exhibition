using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Networking;
using UnityEngine.UI;

public class HOH_ItemView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button button;
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI unitNameText;
    [SerializeField] private TextMeshProUGUI personCountText;
    [SerializeField] private UniversalImageLoader iconLoader;

    [Header("Fallback")]
    [SerializeField] private Sprite fallbackSprite;

    private static readonly Dictionary<string, Sprite> SpriteCache = new();

    private Coroutine _loadRoutine;
    [SerializeField] private HOH_UnitDto _boundData;
    private string _pendingIconUrl;

    private bool UseEnglish
    {
        get
        {
            var locale = LocalizationSettings.SelectedLocale;
            string code = locale != null ? locale.Identifier.Code : "th";
            return code.StartsWith("en", StringComparison.OrdinalIgnoreCase);
        }
    }

    public void Bind(HOH_UnitDto data, Action<HOH_UnitDto> onClick = null)
    {
        _boundData = data;

        BindTexts(data);
        BindButton(onClick);
        BindIcon(data);
    }

    private void OnEnable()
    {
        if (_boundData != null)
            BindTexts(_boundData);

        TryStartPendingIconLoad();
    }

    private void OnDisable()
    {
        if (_loadRoutine != null)
        {
            StopCoroutine(_loadRoutine);
            _loadRoutine = null;
        }
    }

    private void BindTexts(HOH_UnitDto data)
    {
        if (unitNameText != null)
            unitNameText.text = GetUnitName(data);

        if (personCountText != null)
        {
            int count = data?.persons != null ? data.persons.Count : 0;
            personCountText.text = GetPersonCountText(count);
        }
    }

    private string GetUnitName(HOH_UnitDto data)
    {
        if (data == null)
            return string.Empty;

        if (UseEnglish)
            return FirstNotEmpty(data.unitEn, data.unit);

        return FirstNotEmpty(data.unit, data.unitEn);
    }

    private string GetPersonCountText(int count)
    {
        if (UseEnglish)
            return count == 1 ? "1 officer" : $"{count} officers";

        return $"จำนวน {count} ราย";
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

    private void BindButton(Action<HOH_UnitDto> onClick)
    {
        if (button == null)
            return;

        button.onClick.RemoveAllListeners();

        if (onClick != null)
            button.onClick.AddListener(() => onClick.Invoke(_boundData));
    }

    private void BindIcon(HOH_UnitDto data)
    {
        if (_loadRoutine != null)
        {
            StopCoroutine(_loadRoutine);
            _loadRoutine = null;
        }

        string url = data != null ? data.logoUrl : string.Empty;
        _pendingIconUrl = url;

        if (string.IsNullOrWhiteSpace(url))
        {
            SetFallbackSprite();

            if (iconLoader != null)
                iconLoader.ClearImage();

            return;
        }

        if (SpriteCache.TryGetValue(url, out Sprite cachedSprite) && cachedSprite != null)
        {
            if (iconImage != null)
                iconImage.sprite = cachedSprite;

            _pendingIconUrl = null;

            if (iconLoader != null && iconImage != null && iconLoader.gameObject == iconImage.gameObject)
                iconLoader.Load(url);

            return;
        }

        SetFallbackSprite();

        if (iconLoader != null)
        {
            iconLoader.Load(url);
            _pendingIconUrl = null;
            return;
        }

        TryStartPendingIconLoad();
    }

    private void TryStartPendingIconLoad()
    {
        if (!isActiveAndEnabled || !gameObject.activeInHierarchy)
            return;

        if (string.IsNullOrWhiteSpace(_pendingIconUrl))
            return;

        if (SpriteCache.TryGetValue(_pendingIconUrl, out Sprite cachedSprite) && cachedSprite != null)
        {
            if (iconImage != null)
                iconImage.sprite = cachedSprite;

            _pendingIconUrl = null;
            return;
        }

        if (iconLoader != null)
        {
            iconLoader.Load(_pendingIconUrl);
            _pendingIconUrl = null;
            return;
        }

        _loadRoutine = StartCoroutine(LoadSpriteRoutine(_pendingIconUrl));
    }

    private IEnumerator LoadSpriteRoutine(string url)
    {
        using UnityWebRequest req = UnityWebRequestTexture.GetTexture(url);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"[HOH_ItemView] Failed to load icon: {url} | {req.error}");

            if (req.downloadHandler != null)
                Debug.LogWarning($"[HOH_ItemView] DownloadHandler error: {req.downloadHandler.error}");

            _loadRoutine = null;
            yield break;
        }

        Texture2D texture = DownloadHandlerTexture.GetContent(req);
        if (texture == null)
        {
            Debug.LogWarning($"[HOH_ItemView] Texture is null: {url}");
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

        if (iconImage != null)
            iconImage.sprite = sprite;

        if (_pendingIconUrl == url)
            _pendingIconUrl = null;

        _loadRoutine = null;
    }

    private void SetFallbackSprite()
    {
        if (iconImage != null && fallbackSprite != null)
            iconImage.sprite = fallbackSprite;
    }
}