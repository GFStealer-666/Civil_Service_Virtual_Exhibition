using System.Collections.Generic;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(TMP_Dropdown))]
public class GenderDropdownSetup : MonoBehaviour
{
    private void Awake()
    {
        var dropdown = GetComponent<TMP_Dropdown>();
        dropdown.ClearOptions();
        dropdown.AddOptions(new List<string>
        {
            "ชาย",              // → Gender.Male   (default)
            "หญิง",             // → Gender.Female
            "ไม่ระบุ",          // → Gender.Male   (fallback)
            "หลากหลายทางเพศ",   // → Gender.Male   (fallback)
        });
        dropdown.value = 0;
        dropdown.RefreshShownValue();
    }
}