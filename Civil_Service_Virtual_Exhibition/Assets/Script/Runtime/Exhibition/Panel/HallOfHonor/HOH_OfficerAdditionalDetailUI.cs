using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HOH_OfficerAdditionalDetailUI : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject root;
    [SerializeField] private Button closeButton;

    [Header("Text")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text roleText;
    [SerializeField] private TMP_Text organizationText;
    [SerializeField] private TMP_Text bodyText;
    private HOH_OfficerDetailUI _previousDetail;
    private HOH_PersonDto _currentPerson;
    private HOH_UnitDto _currentUnit;

    private void Awake()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(Hide);
    }

    public void Show(HOH_PersonDto person, HOH_UnitDto unit, HOH_OfficerDetailUI previousDetail)
    {
        _currentPerson = person;
        _currentUnit = unit;
        _previousDetail = previousDetail;

        if (root != null)
            root.SetActive(true);

        BindText();
    }

    public void Hide()
    {
        _currentPerson = null;
        _currentUnit = null;

        if (titleText != null)
            titleText.text = string.Empty;

        if (roleText != null)
            roleText.text = string.Empty;

        if (organizationText != null)
            organizationText.text = string.Empty;

        if (bodyText != null)
            bodyText.text = string.Empty;

        if (root != null)
            root.SetActive(false);

        if (_previousDetail != null)
            _previousDetail.ReopenFromChild();
    }
    private void BindText()
    {
        if (_currentPerson == null)
        {
            ApplyEmptyState();
            return;
        }

        if (titleText != null)
            titleText.text = BuildFullName(_currentPerson);

        if (roleText != null)
            roleText.text = BuildRoleText(_currentPerson);

        if (organizationText != null)
            organizationText.text = BuildOrganizationLine(_currentPerson, _currentUnit);

        if (bodyText != null)
            bodyText.text = BuildLongDescription(_currentPerson, _currentUnit);
    }

    private void ApplyEmptyState()
    {
        if (titleText != null)
            titleText.text = string.Empty;

        if (roleText != null)
            roleText.text = string.Empty;

        if (organizationText != null)
            organizationText.text = string.Empty;

        if (bodyText != null)
            bodyText.text = "ไม่พบข้อมูลเพิ่มเติม";
    }

    private string BuildFullName(HOH_PersonDto person)
    {
        if (person == null)
            return string.Empty;

        string prefix = !string.IsNullOrWhiteSpace(person.prefixOther) ? person.prefixOther : person.prefix;
        return $"{prefix} {person.firstName} {person.lastName}".Trim();
    }

    private string BuildOrganizationLine(HOH_PersonDto person, HOH_UnitDto unit)
    {
        string ministry = FirstNotEmpty(person?.ministry, unit?.ministry);
        string department = FirstNotEmpty(person?.department, unit?.unit);
        string division = person?.division;

        return JoinNonEmpty("\n",
            JoinNonEmpty(" , ", ministry, department),
            division
        );
    }
    private string BuildRoleText(HOH_PersonDto person)
    {
        if (person == null)
            return string.Empty;

        return JoinNonEmpty(" ", person.position, person.positionLevel);
    }
    private string BuildLongDescription(HOH_PersonDto person, HOH_UnitDto unit)
    {
        string outstandingWork = FirstNotEmpty(person?.outstandingWork, "ไม่พบข้อมูลผลงานเพิ่มเติม");
        string education = person?.education;
        string institution = person?.institution;
        string serviceDuration = person?.serviceDuration;

        string educationBlock = JoinNonEmpty(" ", education, institution);
        string serviceBlock = string.IsNullOrWhiteSpace(serviceDuration)
            ? string.Empty
            : $"ระยะเวลารับราชการ {serviceDuration}";

        return JoinNonEmpty("\n\n",
            outstandingWork,
            educationBlock,
            serviceBlock
        );
    }
    public void HideSilently()
    {
        _currentPerson = null;
        _currentUnit = null;

        if (titleText != null)
            titleText.text = string.Empty;

        if (roleText != null)
            roleText.text = string.Empty;

        if (organizationText != null)
            organizationText.text = string.Empty;

        if (bodyText != null)
            bodyText.text = string.Empty;

        if (root != null)
            root.SetActive(false);
    }
    private string FirstNotEmpty(params string[] values)
    {
        if (values == null)
            return string.Empty;

        for (int i = 0; i < values.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(values[i]))
                return values[i];
        }

        return string.Empty;
    }

    private string JoinNonEmpty(string separator, params string[] values)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();

        if (values == null)
            return string.Empty;

        for (int i = 0; i < values.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(values[i]))
                continue;

            if (builder.Length > 0)
                builder.Append(separator);

            builder.Append(values[i].Trim());
        }

        return builder.ToString();
    }
}