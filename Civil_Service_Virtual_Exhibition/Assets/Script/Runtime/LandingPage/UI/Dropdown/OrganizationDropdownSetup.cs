using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;


[RequireComponent(typeof(TMP_Dropdown))]
public class OrganizationDropdownSetup : MonoBehaviour
{
    [Serializable]
    public class OrganizationOption
    {
        public string value;
        public string thaiLabel;
        public string englishLabel;
    }

    [Header("Options")]
    [SerializeField] private List<OrganizationOption> options = new();

    [Header("Colors")]
    [SerializeField] private Color placeholderColor = new Color(0.35f, 0.35f, 0.35f, 1f);
    [SerializeField] private Color selectedColor = new Color(0.15f, 0.15f, 0.15f, 1f);

    private TMP_Dropdown _dropdown;
    private int _selectedIndex = 0;

    public bool IsValidSelection()
    {
        return _dropdown != null && _dropdown.value > 0;
    }

    public string GetSelectedOrganization()
    {
        if (options == null || options.Count == 0)
            return string.Empty;

        int index = _dropdown != null ? _dropdown.value : _selectedIndex;
        if (index < 0 || index >= options.Count)
            return string.Empty;

        return options[index].value;
    }

    private void Awake()
    {
        _dropdown = GetComponent<TMP_Dropdown>();

        if (options == null || options.Count == 0)
            options = CreateDefaultOptions();

        _dropdown.onValueChanged.AddListener(OnValueChanged);
    }

    private void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
        RebuildOptions();
    }

    private void Start()
    {
        RebuildOptions();
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }

    private void OnDestroy()
    {
        if (_dropdown != null)
            _dropdown.onValueChanged.RemoveListener(OnValueChanged);
    }

    private void OnLocaleChanged(Locale _)
    {
        RebuildOptions();
    }

    private void OnValueChanged(int index)
    {
        _selectedIndex = Mathf.Clamp(index, 0, Mathf.Max(0, options.Count - 1));
        UpdateLabelColor();
    }

    private void RebuildOptions()
    {
        if (_dropdown == null)
            return;

        if (options == null || options.Count == 0)
            options = CreateDefaultOptions();

        _selectedIndex = Mathf.Clamp(_selectedIndex, 0, Mathf.Max(0, options.Count - 1));

        bool isThai = IsThaiLanguage();
        List<string> displayOptions = new List<string>(options.Count);

        for (int i = 0; i < options.Count; i++)
        {
            displayOptions.Add(isThai ? options[i].thaiLabel : options[i].englishLabel);
        }

        _dropdown.ClearOptions();
        _dropdown.AddOptions(displayOptions);
        _dropdown.value = _selectedIndex;
        _dropdown.RefreshShownValue();

        UpdateLabelColor();
    }

    private void UpdateLabelColor()
    {
        if (_dropdown == null || _dropdown.captionText == null)
            return;

        _dropdown.captionText.color = _dropdown.value == 0
            ? placeholderColor
            : selectedColor;
    }

    private static bool IsThaiLanguage()
    {
        Locale locale = LocalizationSettings.SelectedLocale;
        if (locale == null)
            return true;

        string code = locale.Identifier.Code;
        return !string.IsNullOrEmpty(code) &&
               code.StartsWith("th", StringComparison.OrdinalIgnoreCase);
    }

    private static List<OrganizationOption> CreateDefaultOptions()
    {
        return new List<OrganizationOption>
        {
            new OrganizationOption
            {
                value = "",
                thaiLabel = "กรุณาเลือกหน่วยงาน",
                englishLabel = "Please select organization"
            },
            new OrganizationOption
            {
                value = "สำนักงานปลัดสำนักนายกรัฐมนตรี",
                thaiLabel = "สำนักงานปลัดสำนักนายกรัฐมนตรี",
                englishLabel = "Office of the Permanent Secretary"
            },
            new OrganizationOption
            {
                value = "กระทรวงมหาดไทย",
                thaiLabel = "กระทรวงมหาดไทย",
                englishLabel = "Ministry of Interior"
            },
            new OrganizationOption
            {
                value = "กรมพัฒนาที่ดิน",
                thaiLabel = "กรมพัฒนาที่ดิน",
                englishLabel = "Land Development Department"
            },
            new OrganizationOption
            {
                value = "กระทรวงแรงงาน",
                thaiLabel = "กระทรวงแรงงาน",
                englishLabel = "Ministry of Labour"
            },
            new OrganizationOption
            {
                value = "สำนักงานปลัดกระทรวงพาณิชย์",
                thaiLabel = "สำนักงานปลัดกระทรวงพาณิชย์",
                englishLabel = "Ministry of Commerce"
            },
            new OrganizationOption
            {
                value = "สำนักงานปลัดกระทรวงยุติธรรม",
                thaiLabel = "สำนักงานปลัดกระทรวงยุติธรรม",
                englishLabel = "Ministry of Justice"
            },
            new OrganizationOption
            {
                value = "สำนักงานคณะกรรมการนโยบายรัฐวิสาหกิจ",
                thaiLabel = "สำนักงานคณะกรรมการนโยบายรัฐวิสาหกิจ",
                englishLabel = "State Enterprise Policy Office"
            },
            new OrganizationOption
            {
                value = "กรมสรรพสามิต",
                thaiLabel = "กรมสรรพสามิต",
                englishLabel = "Excise Department"
            },
            new OrganizationOption
            {
                value = "กระทรวงทรัพยากรธรรมชาติและสิ่งแวดล้อม",
                thaiLabel = "กระทรวงทรัพยากรธรรมชาติและสิ่งแวดล้อม",
                englishLabel = "Ministry of Natural Resources and Environment"
            },
            new OrganizationOption
            {
                value = "กองบริหารทรัพยากรบุคคลกระทรวงการคลัง",
                thaiLabel = "กองบริหารทรัพยากรบุคคลกระทรวงการคลัง",
                englishLabel = "Ministry of Finance"
            },
            new OrganizationOption
            {
                value = "สำนักงานปลัดกระทรวงวัฒนธรรม",
                thaiLabel = "สำนักงานปลัดกระทรวงวัฒนธรรม",
                englishLabel = "Ministry of Culture"
            },
            new OrganizationOption
            {
                value = "สำนักงานปลัดกระทรวงดิจิทัลเพื่อเศรษฐกิจและสังคม",
                thaiLabel = "สำนักงานปลัดกระทรวงดิจิทัลเพื่อเศรษฐกิจและสังคม",
                englishLabel = "Ministry of Digital Economy and Society"
            },
            new OrganizationOption
            {
                value = "สำนักงานปลัดกระทรวงการพัฒนาสังคมและความมั่นคงของมนุษย์",
                thaiLabel = "สำนักงานปลัดกระทรวงการพัฒนาสังคมและความมั่นคงของมนุษย์",
                englishLabel = "Ministry of Social Development and Human Security"
            },
            new OrganizationOption
            {
                value = "สำนักงานปลัดกระทรวงการท่องเที่ยวและกีฬา",
                thaiLabel = "สำนักงานปลัดกระทรวงการท่องเที่ยวและกีฬา",
                englishLabel = "Ministry of Tourism and Sports"
            },
            new OrganizationOption
            {
                value = "กรมการกงสุล กระทรวงการต่างประเทศ",
                thaiLabel = "กรมการกงสุล กระทรวงการต่างประเทศ",
                englishLabel = "Ministry of Foreign Affairs"
            },
            new OrganizationOption
            {
                value = "สำนักงานปลัดกระทรวงคมนาคม",
                thaiLabel = "สำนักงานปลัดกระทรวงคมนาคม",
                englishLabel = "Ministry of Transport"
            },
            new OrganizationOption
            {
                value = "สำนักงานปลัดกระทรวงพลังงาน",
                thaiLabel = "สำนักงานปลัดกระทรวงพลังงาน",
                englishLabel = "Ministry of Energy"
            },
            new OrganizationOption
            {
                value = "สำนักงานบริหารหนี้สาธารณะ",
                thaiLabel = "สำนักงานบริหารหนี้สาธารณะ",
                englishLabel = "Public Debt Management Office"
            },
            new OrganizationOption
            {
                value = "กรมสารนิเทศ",
                thaiLabel = "กรมสารนิเทศ",
                englishLabel = "Department of Information"
            },
            new OrganizationOption
            {
                value = "กระทรวงศึกษาธิการ",
                thaiLabel = "กระทรวงศึกษาธิการ",
                englishLabel = "Ministry of Education"
            },
            new OrganizationOption
            {
                value = "พนักงานบริษัทเอกชน",
                thaiLabel = "พนักงานบริษัทเอกชน",
                englishLabel = "Private Company Employee"
            },
            new OrganizationOption
            {
                value = "อื่นๆ",
                thaiLabel = "อื่นๆ",
                englishLabel = "Other"
            }
        };
    }
}