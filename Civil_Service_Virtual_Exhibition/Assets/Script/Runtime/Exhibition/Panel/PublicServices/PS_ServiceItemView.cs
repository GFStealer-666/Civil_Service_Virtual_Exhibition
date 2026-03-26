using TMPro;
using UnityEngine;

public class PS_ServiceItemView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text departmentText;
    [SerializeField] private TMP_Text dateText;
    [SerializeField] private TMP_Text placesText;
    public void Bind(PS_ServiceActivityDto data, bool useEnglish)
    {
        if (data == null)
            return;

        if (titleText != null)
            titleText.text = Clean(useEnglish ? data.activityNameEn : data.activityName, "-");

        if (departmentText != null)
            departmentText.text = Clean(useEnglish ? data.departmentEn : data.department, "-");

        if (dateText != null)
            dateText.text = Clean(useEnglish ? data.activityDateEn : data.activityDate, useEnglish ? "Not specified" : "ไม่ระบุ");
        
        if(placesText != null)
        {
            placesText.text = "ไม่ระบุ";
        }
    }

    private string Clean(string value, string fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
            return fallback;

        return value.Replace("\r", " ").Replace("\n", " ").Trim();
    }
}