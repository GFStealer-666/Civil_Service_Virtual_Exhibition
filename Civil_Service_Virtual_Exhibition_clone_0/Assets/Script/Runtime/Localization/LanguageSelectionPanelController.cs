using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class LanguageSelectionPanelController : MonoBehaviour
{
    private const string HasChosenLanguageKey = "app.language.has_chosen";

    [Header("Root")]
    [SerializeField] private GameObject panelRoot;

    [Header("Flow")]
    [SerializeField] private GameObject[] rootsToShowAfterSelection;
    [SerializeField] private GameObject[] rootsToHideAfterSelection;

    [Header("Buttons")]
    [SerializeField] private Button thaiButton;
    [SerializeField] private Button englishButton;

    [Header("Behavior")]
    [SerializeField] private bool showEveryLaunch = true;
    [SerializeField] private bool closePanelAfterSelection = true;
    [SerializeField] private string thaiLocaleCode = "th";
    [SerializeField] private string englishLocaleCode = "en";

    private bool _isApplyingLanguage;

    private IEnumerator Start()
    {
        yield return LocalizationSettings.InitializationOperation;

        if (thaiButton != null)
        {
            thaiButton.onClick.RemoveListener(OnThaiClicked);
            thaiButton.onClick.AddListener(OnThaiClicked);
        }

        if (englishButton != null)
        {
            englishButton.onClick.RemoveListener(OnEnglishClicked);
            englishButton.onClick.AddListener(OnEnglishClicked);
        }

        bool hasChosenBefore = PlayerPrefs.GetInt(HasChosenLanguageKey, 0) == 1;
        bool shouldShow = showEveryLaunch || !hasChosenBefore;

        if (panelRoot != null)
            panelRoot.SetActive(shouldShow);

        SetObjectsActive(rootsToShowAfterSelection, !shouldShow);
        SetObjectsActive(rootsToHideAfterSelection, shouldShow);
    }

    private void OnDestroy()
    {
        if (thaiButton != null)
            thaiButton.onClick.RemoveListener(OnThaiClicked);

        if (englishButton != null)
            englishButton.onClick.RemoveListener(OnEnglishClicked);
    }

    private void OnThaiClicked()
    {
        if (_isApplyingLanguage)
            return;

        StartCoroutine(ApplyLanguageRoutine(thaiLocaleCode));
    }

    private void OnEnglishClicked()
    {
        if (_isApplyingLanguage)
            return;

        StartCoroutine(ApplyLanguageRoutine(englishLocaleCode));
    }

    private IEnumerator ApplyLanguageRoutine(string localeCode)
    {
        _isApplyingLanguage = true;
        SetButtonsInteractable(false);

        yield return LocalizationSettings.InitializationOperation;

        Locale locale = LocalizationSettings.AvailableLocales.GetLocale(localeCode);
        if (locale == null)
        {
            Debug.LogError($"[LanguageSelectionPanelController] Locale not found: {localeCode}");
            SetButtonsInteractable(true);
            _isApplyingLanguage = false;
            yield break;
        }

        LocalizationSettings.SelectedLocale = locale;
        PlayerPrefs.SetInt(HasChosenLanguageKey, 1);
        PlayerPrefs.Save();

        yield return null;

        if (closePanelAfterSelection && panelRoot != null)
            panelRoot.SetActive(false);

        SetObjectsActive(rootsToShowAfterSelection, true);
        SetObjectsActive(rootsToHideAfterSelection, false);

        _isApplyingLanguage = false;
    }

    private void SetButtonsInteractable(bool interactable)
    {
        if (thaiButton != null)
            thaiButton.interactable = interactable;

        if (englishButton != null)
            englishButton.interactable = interactable;
    }

    private static void SetObjectsActive(GameObject[] objects, bool active)
    {
        if (objects == null)
            return;

        for (int i = 0; i < objects.Length; i++)
        {
            if (objects[i] != null)
                objects[i].SetActive(active);
        }
    }

    [ContextMenu("Reset Language Choice")]
    public void ResetLanguageChoice()
    {
        PlayerPrefs.DeleteKey(HasChosenLanguageKey);
        PlayerPrefs.Save();
    }
}