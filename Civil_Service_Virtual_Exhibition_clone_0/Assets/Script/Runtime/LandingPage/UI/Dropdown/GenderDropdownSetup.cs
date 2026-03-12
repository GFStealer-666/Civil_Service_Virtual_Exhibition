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
            "เลือกเพศ",
            "ชาย",        
            "หญิง",       
        });

        dropdown.value = PLACEHOLDER_INDEX;
        dropdown.RefreshShownValue();

        UpdateLabelColor(PLACEHOLDER_INDEX);
        dropdown.onValueChanged.AddListener(OnValueChanged);
    }

    private void OnValueChanged(int index) => UpdateLabelColor(index);

    private void UpdateLabelColor(int index)
    {
        if (dropdown.captionText == null) return;
        dropdown.captionText.color = index == PLACEHOLDER_INDEX
            ? new Color(0.35f, 0.35f, 0.35f, 1f)  
            : new Color(0.15f, 0.15f, 0.15f, 1f);
    }

    public bool IsValidSelection()    => dropdown.value != PLACEHOLDER_INDEX;
    public string GetSelectedGender() => dropdown.value == 1 ? "male" : "female";

    private void OnDestroy() => dropdown.onValueChanged.RemoveAllListeners();
}