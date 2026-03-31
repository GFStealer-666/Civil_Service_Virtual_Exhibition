using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Localization.Settings;

public static class LocalizationService
{
    public static string CurrentLocaleCode
    {
        get
        {
            var locale = LocalizationSettings.SelectedLocale;
            return locale != null ? locale.Identifier.Code : "en";
        }
    }

    public static void SetLocale(string code)
    {
        var locale = LocalizationSettings.AvailableLocales.GetLocale(code);
        if (locale == null)
        {
            Debug.LogWarning($"[LocalizationService] Locale not found: {code}");
            return;
        }

        LocalizationSettings.SelectedLocale = locale;
        PlayerPrefs.SetString("locale", code);
        PlayerPrefs.Save();
    }

    public static IEnumerator Initialize()
    {
        yield return LocalizationSettings.InitializationOperation;

        string savedCode = PlayerPrefs.GetString("locale", "en");
        var locale = LocalizationSettings.AvailableLocales.GetLocale(savedCode);

        if (locale != null)
            LocalizationSettings.SelectedLocale = locale;
    }

    public static IEnumerator GetRoutine(string table, string key, Action<string> onCompleted)
    {
        yield return LocalizationSettings.InitializationOperation;

        var handle = LocalizationSettings.StringDatabase.GetLocalizedStringAsync(table, key);
        yield return handle;

        onCompleted?.Invoke(handle.Result);
    }

    public static IEnumerator GetRoutine(string key, Action<string> onCompleted)
    {
        yield return LocalizationSettings.InitializationOperation;

        string table = ResolveDefaultTable(key);
        var handle = LocalizationSettings.StringDatabase.GetLocalizedStringAsync(table, key);
        yield return handle;

        onCompleted?.Invoke(handle.Result);
    }

    private static string ResolveDefaultTable(string key)
    {
        if (key.StartsWith("landing."))
            return "UI_Landing";

        if (key.StartsWith("error."))
            return "UI_Error";

        return "UI_Common";
    }
}