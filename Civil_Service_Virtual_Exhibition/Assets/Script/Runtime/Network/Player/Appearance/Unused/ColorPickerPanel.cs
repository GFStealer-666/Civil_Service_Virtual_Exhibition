using System;
using UnityEngine;
using UnityEngine.UI;

public class ColorPickerPanel : MonoBehaviour
{
    [SerializeField] private UI_ColourPicker colourPicker;
    [SerializeField] private Button          confirmButton;
    [SerializeField] private Button          cancelButton;

    public Action<Color> OnColorConfirmed;
    public Action<Color> OnCancelled;      // now passes original color back

    private bool           _initialized = false;
    private AppearanceSlot _currentSlot;
    private Color          _pendingColor;
    private Color          _originalColor; // snapshot on Open()

    private void EnsureInitialized()
    {
        if (_initialized) return;

        colourPicker.OnColourChanged.AddListener(OnColourChanged);
        confirmButton.onClick.AddListener(OnConfirmPressed);
        cancelButton.onClick.AddListener(OnCancelPressed);

        _initialized = true;
    }

    public void Open(AppearanceSlot slot, Color currentColor)
    {
        EnsureInitialized();

        _currentSlot   = slot;
        _pendingColor  = currentColor;
        _originalColor = currentColor; // snapshot before any changes

        gameObject.SetActive(true);
        colourPicker.SetNewColour(currentColor);
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    private void OnColourChanged(Color color)
    {
        // Store pending but do NOT invoke anything
        // No live preview — player must confirm first
        _pendingColor = color;
    }

    public void OnConfirmPressed()
    {
        OnColorConfirmed?.Invoke(_pendingColor);
        Debug.Log($"[ColorPickerPanel] Confirmed: {_pendingColor}");
        Close();
    }

    public void OnCancelPressed()
    {
        // Pass original color back so UI and character snap back
        OnCancelled?.Invoke(_originalColor);
        Debug.Log($"[ColorPickerPanel] Cancelled — restoring: {_originalColor}");
        Close();
    }
}