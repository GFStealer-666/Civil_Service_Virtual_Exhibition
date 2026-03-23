using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
        RefreshView();
        RegisterEvents();
    }

    private void OnDisable()
    {
        UnregisterEvents();
    }

    public void RefreshView()
    {
        var local = LocalPlayerData.Instance;
        if (local == null) return;

        _isBinding = true;

        if (emailText != null)
            emailText.text = $"อีเมล : {local.Email}";

        if (phoneText != null)
            phoneText.text = $"เบอร์ติดต่อ : {local.PhoneNumber}";

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
}