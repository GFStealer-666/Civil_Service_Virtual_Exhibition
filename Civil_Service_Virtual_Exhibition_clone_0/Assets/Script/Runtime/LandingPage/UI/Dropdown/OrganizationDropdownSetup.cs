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
        dropdown.value = 0;
        dropdown.RefreshShownValue();

        // Grey out placeholder label
        UpdateLabelColor(dropdown);
        dropdown.onValueChanged.AddListener(_ => UpdateLabelColor(dropdown));
    }

        private void UpdateLabelColor(TMP_Dropdown dropdown)
    {
        if (dropdown.captionText == null) return;
        dropdown.captionText.color = dropdown.value == 0
            ? new Color(0.35f, 0.35f, 0.35f, 1f)  
            : new Color(0.15f, 0.15f, 0.15f, 1f);
    }

    private static List<string> DefaultOrganizations() => new()
    {
        "กรุณาเลือกหน่วยงาน",   // placeholder index 0
        "สำนักงานปลัดสำนักนายกรัฐมนตรี",
        "กระทรวงมหาดไทย",
        "กรมพัฒนาที่ดิน",
        "กระทรวงแรงงาน",
        "สำนักงานปลัดกระทรวงพาณิชย์",
        "สำนักงานปลัดกระทรวงยุติธรรม",
        "สำนักงานคณะกรรมการนโยบายรัฐวิสาหกิจ",
        "กรมสรรพสามิต",
        "กระทรวงทรัพยากรธรรมชาติและสิ่งแวดล้อม",
        "กองบริหารทรัพยากรบุคคลกระทรวงการคลัง",
        "สำนักงานปลัดกระทรวงวัฒนธรรม",
        "สำนักงานปลัดกระทรวงดิจิทัลเพื่อเศรษฐกิจและสังคม",
        "สำนักงานปลัดกระทรวงการพัฒนาสังคมและความมั่นคงของมนุษย์",
        "สำนักงานปลัดกระทรวงการท่องเที่ยวและกีฬา",
        "กรมการกงสุล กระทรวงการต่างประเทศ",
        "สำนักงานปลัดกระทรวงคมนาคม",
        "สำนักงานปลัดกระทรวงพลังงาน",
        "สำนักงานบริหารหนี้สาธารณะ",
        "กรมสารนิเทศ",
        "กระทรวงศึกษาธิการ",
    };
}