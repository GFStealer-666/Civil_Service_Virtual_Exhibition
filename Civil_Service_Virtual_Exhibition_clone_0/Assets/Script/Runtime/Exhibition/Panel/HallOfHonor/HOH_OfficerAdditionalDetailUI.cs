using System;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
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

    private bool UseEnglish
    {
        get
        {
            var locale = LocalizationSettings.SelectedLocale;
            string code = locale != null ? locale.Identifier.Code : "th";
            return code.StartsWith("en", StringComparison.OrdinalIgnoreCase);
        }
    }

    private string NoAdditionalInfoText => UseEnglish ? "No additional information available" : "ไม่พบข้อมูลเพิ่มเติม";
    private string NoOutstandingWorkText => UseEnglish ? "No additional work information available" : "ไม่พบข้อมูลผลงานเพิ่มเติม";
    private string ServiceDurationLabel => UseEnglish ? "Years of service" : "ระยะเวลารับราชการ";

    private void Awake()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(Hide);
    }

    private void OnDestroy()
    {
        if (closeButton != null)
            closeButton.onClick.RemoveListener(Hide);
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
            bodyText.text = NoAdditionalInfoText;
    }

    private string BuildFullName(HOH_PersonDto person)
    {
        if (person == null)
            return string.Empty;

        if (UseEnglish)
        {
            string prefix = FirstNotEmpty(person.prefixEn, person.prefixOther, person.prefix);
            string firstName = FirstNotEmpty(person.firstNameEn, person.firstName);
            string lastName = FirstNotEmpty(person.lastNameEn, person.lastName);
            return JoinNonEmpty(" ", prefix, firstName, lastName);
        }

        string thaiPrefix = !string.IsNullOrWhiteSpace(person.prefixOther) ? person.prefixOther : person.prefix;
        return JoinNonEmpty(" ", thaiPrefix, person.firstName, person.lastName);
    }

    private string BuildOrganizationLine(HOH_PersonDto person, HOH_UnitDto unit)
    {
        string ministry = UseEnglish
            ? FirstNotEmpty(person?.ministryEn, unit?.ministryEn, person?.ministry, unit?.ministry)
            : FirstNotEmpty(person?.ministry, unit?.ministry, person?.ministryEn, unit?.ministryEn);

        string department = UseEnglish
            ? FirstNotEmpty(person?.departmentEn, unit?.unitEn, person?.department, unit?.unit)
            : FirstNotEmpty(person?.department, unit?.unit, person?.departmentEn, unit?.unitEn);

        string division = UseEnglish
            ? FirstNotEmpty(person?.divisionEn, person?.division)
            : FirstNotEmpty(person?.division, person?.divisionEn);

        return JoinNonEmpty(
            "\n",
            JoinNonEmpty(" , ", ministry, department),
            division
        );
    }

    private string BuildRoleText(HOH_PersonDto person)
    {
        if (person == null)
            return string.Empty;

        if (UseEnglish)
        {
            return JoinNonEmpty(
                " ",
                FirstNotEmpty(person.positionEn, person.position),
                FirstNotEmpty(person.positionLevelEn, person.positionLevel)
            );
        }

        return JoinNonEmpty(" ", person.position, person.positionLevel);
    }

    private string BuildLongDescription(HOH_PersonDto person, HOH_UnitDto unit)
    {
        if (person == null)
            return NoAdditionalInfoText;

        string outstandingWork = UseEnglish
            ? FirstNotEmpty(person.outstandingWorkEn, person.outstandingWork, NoOutstandingWorkText)
            : FirstNotEmpty(person.outstandingWork, person.outstandingWorkEn, NoOutstandingWorkText);

        string education = UseEnglish
            ? FirstNotEmpty(person.educationEn, person.education)
            : FirstNotEmpty(person.education, person.educationEn);

        string institution = UseEnglish
            ? FirstNotEmpty(person.institutionEn, person.institution)
            : FirstNotEmpty(person.institution, person.institutionEn);

        string serviceDuration = UseEnglish
            ? FirstNotEmpty(person.serviceDurationEn, person.serviceDuration)
            : FirstNotEmpty(person.serviceDuration, person.serviceDurationEn);

        string educationBlock = JoinNonEmpty(" ", education, institution);
        string serviceBlock = string.IsNullOrWhiteSpace(serviceDuration)
            ? string.Empty
            : $"{ServiceDurationLabel} {serviceDuration}";

        return JoinNonEmpty(
            "\n\n",
            outstandingWork,
            educationBlock,
            serviceBlock
        );
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