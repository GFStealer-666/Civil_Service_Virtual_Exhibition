using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class LocalizedImageSwitcher : MonoBehaviour
{
    public enum LanguageMode
    {
        Auto,
        Thai,
        English
    }

    [Header("Target")]
    [SerializeField] private Image targetImage;

    [Header("Language")]
    [SerializeField] private LanguageMode languageMode = LanguageMode.Auto;

    [Header("Sprites")]
    [SerializeField] private Sprite thaiSprite;
    [SerializeField] private Sprite englishSprite;

    [Header("Options")]
    [SerializeField] private bool applyOnStart = true;
    [SerializeField] private bool preserveNativeSize = false;

    private void Awake()
    {
        if (targetImage == null)
            targetImage = GetComponent<Image>();
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
        if (applyOnStart)
            RefreshImage();
    }

    private void HandleLocaleChanged(Locale locale)
    {
        RefreshImage();
    }

    public void RefreshImage()
    {
        if (targetImage == null)
            return;

        Sprite selectedSprite = IsThaiLanguage() ? thaiSprite : englishSprite;

        if (selectedSprite == null)
            return;

        targetImage.sprite = selectedSprite;

        if (preserveNativeSize)
            targetImage.SetNativeSize();
    }

    public void SetLanguageMode(LanguageMode mode)
    {
        languageMode = mode;
        RefreshImage();
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
                Locale locale = LocalizationSettings.SelectedLocale;
                if (locale == null)
                    return true;

                string code = locale.Identifier.Code;
                return !string.IsNullOrEmpty(code) &&
                       code.StartsWith("th", StringComparison.OrdinalIgnoreCase);
        }
    }
}