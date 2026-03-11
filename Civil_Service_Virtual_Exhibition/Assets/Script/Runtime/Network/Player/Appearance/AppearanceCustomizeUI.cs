using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class AppearanceCustomizeUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject panel;

    [Header("Slot List")]
    [SerializeField] private Transform       slotContainer;
    [SerializeField] private AppearanceSlotUI slotPrefab;

    [Header("Color Picker")]
    [SerializeField] private ColorPickerPanel colorPickerPanel;

    [Header("Slot Count Label")]
    [SerializeField] private TMP_Text slotCountLabel;

    [Header("Debug")]
    [SerializeField] private PlayerProfile _localProfile;
    [SerializeField] private AppearanceSlotMapping _mapping;
    [SerializeField] private AppearanceSlotUI _activeSlot;
    private PlayerAppearance _appearance;
    private readonly List<AppearanceSlotUI> _generatedSlots = new();

    private void Awake()
    {
        colorPickerPanel.OnColorConfirmed = OnColorConfirmed;
        colorPickerPanel.OnCancelled      = OnPickerCancelled; // now takes Color
    }

    public void Initialize(PlayerProfile profile, AppearanceSlotMapping mapping, PlayerAppearance appearance)
    {
        _localProfile = profile;
        _mapping      = mapping;
        _appearance   = appearance; // store reference

        GenerateSlotEntries();
        panel.SetActive(false);
    }

    private void GenerateSlotEntries()
    {
        foreach (var slot in _generatedSlots)
            Destroy(slot.gameObject);
        _generatedSlots.Clear();

        int visibleCount = 0;

        foreach (AppearanceSlot slot in System.Enum.GetValues(typeof(AppearanceSlot)))
        {
            string[] meshNames = _mapping.GetMeshNames(slot);
            if (meshNames == null || meshNames.Length == 0)
                continue;

            AppearanceSlotUI entry = Instantiate(slotPrefab, slotContainer);

            // Read directly from material — always correct regardless of network timing
            Color currentColor = _appearance != null
                ? _appearance.GetMaterialColor(slot)
                : Color.white;

            entry.Initialize(slot, currentColor);
            entry.OnSlotClicked = OnSlotClicked;

            _generatedSlots.Add(entry);
            visibleCount++;
        }

        slotCountLabel.text = $"{visibleCount} customizable slots";
        Debug.Log($"[AppearanceCustomizeUI] Generated {visibleCount} slot entries.");
    }

    public void RefreshSlotColors()
    {
        if (_appearance == null) return;

        foreach (var slotUI in _generatedSlots)
        {
            // Always read from material — source of truth for visual state
            Color color = _appearance.GetMaterialColor(slotUI.Slot);
            slotUI.SetColor(color);
        }
    }

    private void OnSlotClicked(AppearanceSlotUI slotUI)
    {
        _activeSlot = slotUI;
        Color current = _localProfile.GetColor(slotUI.Slot);
        colorPickerPanel.Open(slotUI.Slot, current);
        Debug.Log($"[AppearanceCustomizeUI] Slot clicked: {slotUI.Slot}");
    }

    // Live drag — update visuals, keep _activeSlot alive
    private void OnColorPreview(Color color)
    {
        if (_activeSlot == null) return;

        _activeSlot.SetColor(color);
        _localProfile?.ChangeColor(_activeSlot.Slot, color);
    }

    // Confirm button — finalize then clear
    private void OnColorConfirmed(Color color)
    {
        if (_activeSlot == null) return;

        _activeSlot.SetColor(color);
        _localProfile?.ChangeColor(_activeSlot.Slot, color);
        _activeSlot = null;
    }

    private void OnPickerCancelled(Color originalColor)
    {
        if (_activeSlot == null) return;

        _activeSlot.SetColor(originalColor);

        _localProfile?.ChangeColor(_activeSlot.Slot, originalColor);

        _activeSlot = null;
    }

    public void TogglePanel()
    {
        panel.SetActive(!panel.activeSelf);

        if (panel.activeSelf)
            RefreshSlotColors();
    }

}