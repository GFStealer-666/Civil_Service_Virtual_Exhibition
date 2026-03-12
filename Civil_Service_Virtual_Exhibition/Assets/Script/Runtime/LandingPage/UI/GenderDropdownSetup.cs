using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(TMP_Dropdown))]
public class GenderDropdownSetup : MonoBehaviour
{

    private TMP_Dropdown dropdown;
    private const int PLACEHOLDER_INDEX = 0;

    private void Awake()
    {
        dropdown = GetComponent<TMP_Dropdown>();
        dropdown.ClearOptions();

        dropdown.AddOptions(new List<string>
        {
            "ชาย",
            "หญิง",
        });

        dropdown.value = PLACEHOLDER_INDEX;
        dropdown.RefreshShownValue();

        UpdateLabelColor(PLACEHOLDER_INDEX);
        dropdown.onValueChanged.AddListener(OnValueChanged);
    }

    private void OnValueChanged(int index)
    {
        UpdateLabelColor(index);
    }

    private void UpdateLabelColor(int index)
    {
        if (dropdown.captionText == null) return;
    }

    public bool IsValidSelection() => dropdown.value != PLACEHOLDER_INDEX;
    public string GetSelectedGender() => IsValidSelection() ? dropdown.options[dropdown.value].text : null;

    private void OnDestroy()
    {
        dropdown.onValueChanged.RemoveAllListeners();
    }
}