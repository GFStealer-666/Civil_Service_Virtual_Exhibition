using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AppearanceSlotUI : MonoBehaviour
{
    [SerializeField] private Button   slotButton;
    [SerializeField] private Image    colorPreview;
    [SerializeField] private TMP_Text slotLabel;

    public AppearanceSlot           Slot { get; private set; }
    public Action<AppearanceSlotUI> OnSlotClicked;

    public void Initialize(AppearanceSlot slot, Color currentColor)
    {
        Slot = slot;

        slotLabel.text     = FormatSlotName(slot);
        currentColor.a     = 1f;  // ← was 255, Unity uses 0-1
        colorPreview.color = currentColor;

        slotButton.onClick.RemoveAllListeners();
        slotButton.onClick.AddListener(() => OnSlotClicked?.Invoke(this));
    }

    public void SetColor(Color color)
    {
        color.a            = 1f;
        colorPreview.color = color;
    }

    private string FormatSlotName(AppearanceSlot slot)
    {
        string name = slot.ToString();
        System.Text.StringBuilder sb = new();

        for (int i = 0; i < name.Length; i++)
        {
            if (i > 0 && char.IsUpper(name[i]))
                sb.Append(' ');
            sb.Append(name[i]);
        }

        return sb.ToString();
    }
}