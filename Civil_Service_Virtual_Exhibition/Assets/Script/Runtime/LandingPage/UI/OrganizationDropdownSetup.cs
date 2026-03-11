using System.Collections.Generic;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(TMP_Dropdown))]
public class OrganizationDropdownSetup : MonoBehaviour
{
    // Populate this list in the Inspector, or let the default list below be used.
    [SerializeField] private List<string> organizationOptions = new();

    private void Awake()
    {
        var dropdown = GetComponent<TMP_Dropdown>();
        dropdown.ClearOptions();

        if (organizationOptions.Count == 0)
            organizationOptions = DefaultOrganizations();

        dropdown.AddOptions(organizationOptions);
        dropdown.RefreshShownValue();
    }

    private static List<string> DefaultOrganizations() => new()
    {
        "กรุณาเลือกหน่วยงาน",          //  placeholder
        "กระทรวงศึกษาธิการ",
        "กระทรวงสาธารณสุข",
        "กระทรวงการคลัง",
        "กระทรวงมหาดไทย",
        "กระทรวงเกษตรและสหกรณ์",
        "กระทรวงยุติธรรม",
        "กระทรวงดิจิทัลเพื่อเศรษฐกิจและสังคม",
        "กระทรวงการต่างประเทศ",
        "กระทรวงกลาโหม",
        "กระทรวงการท่องเที่ยวและกีฬา",
        "กระทรวงพลังงาน",
        "กระทรวงพาณิชย์",
        "กระทรวงแรงงาน",
        "กระทรวงวิทยาศาสตร์และเทคโนโลยี",
        "กระทรวงอุตสาหกรรม",
        "สำนักนายกรัฐมนตรี",
        "หน่วยงานอื่น ๆ",
    };
}