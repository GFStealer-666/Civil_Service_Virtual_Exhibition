using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class SettingsPanelPresenter : MonoBehaviour
{
    [Header("User Info")]
    [SerializeField] private TMP_Text emailText;
    [SerializeField] private TMP_Text phoneText;

    [Header("Audio")]
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider effectSlider;

    private bool _isBinding;

    private void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
        RefreshView();
        RegisterEvents();
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
        UnregisterEvents();
    }

    private void OnLocaleChanged(Locale _)
    {
        RefreshView();
    }

    public void RefreshView()
    {
        var local = LocalPlayerData.Instance;
        if (local == null) return;

        _isBinding = true;

        string emailPrefix = GetLocalizedText(
            LocalizationKeys.CentralHub.EmailPrefix,
            "อีเมล :"
        );

        string phonePrefix = GetLocalizedText(
            LocalizationKeys.CentralHub.PhonePrefix,
            "เบอร์ติดต่อ :"
        );

        if (emailText != null)
            emailText.text = $"{emailPrefix} {local.Email}";

        if (phoneText != null)
            phoneText.text = $"{phonePrefix} {local.PhoneNumber}";

        if (bgmSlider != null)
            bgmSlider.value = local.BgmVolume;

        if (effectSlider != null)
            effectSlider.value = local.EffectVolume;

        _isBinding = false;
    }

    private void RegisterEvents()
    {
        if (bgmSlider != null)
            bgmSlider.onValueChanged.AddListener(OnBgmSliderChanged);

        if (effectSlider != null)
            effectSlider.onValueChanged.AddListener(OnEffectSliderChanged);
    }

    private void UnregisterEvents()
    {
        if (bgmSlider != null)
            bgmSlider.onValueChanged.RemoveListener(OnBgmSliderChanged);

        if (effectSlider != null)
            effectSlider.onValueChanged.RemoveListener(OnEffectSliderChanged);
    }

    private void OnBgmSliderChanged(float value)
    {
        if (_isBinding) return;
        if (ExhibitionAudioManager.Instance == null) return;

        ExhibitionAudioManager.Instance.SetBgmVolume(value);
    }

    private void OnEffectSliderChanged(float value)
    {
        if (_isBinding) return;
        if (ExhibitionAudioManager.Instance == null) return;

        ExhibitionAudioManager.Instance.SetEffectVolume(value);
    }
    private string GetLocalizedText(string key, string fallback)
    {
        string value = LocalizationSettings.StringDatabase.GetLocalizedString(
            LocalizationKeys.Tables.CentralHub,
            key
        );

        return string.IsNullOrEmpty(value) ? fallback : value;
    }
}