using System;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

public class HOH_OfficerDetailUI : MonoBehaviour
{
    [Header("Linked Panels")]
    [SerializeField] private HOH_OfficerAdditionalDetailUI additionalDetailUI;

    [Header("Root")]
    [SerializeField] private GameObject root;
    [SerializeField] private Button closeButton;

    [Header("Text")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text roleText;
    [SerializeField] private TMP_Text organizationText;
    [SerializeField] private TMP_Text shortDescriptionText;

    [Header("Buttons")]
    [SerializeField] private Button moreInfoButton;
    [SerializeField] private TMP_Text moreInfoButtonLabel;
    [SerializeField] private Button narratorButton;
    [SerializeField] private TMP_Text narratorButtonLabel;
    [SerializeField] private GameObject narratorButtonRoot;
    [SerializeField] private bool narratorAvailable = true;

    [Header("Narration")]
    [SerializeField] private HOH_OfficerNarrationController narrationController;

    [Header("Config")]
    [SerializeField] private int shortDescriptionCharacterLimit = 150;

    [Header("Loader")]
    [SerializeField] private UniversalImageLoader photoLoader;

    private HOH_PersonDto _currentPerson;
    private HOH_UnitDto _currentUnit;
    private HOH_OfficerSelectionPanelController _previousSelection;

    public bool HasPerson => _currentPerson != null;

    private bool UseEnglish
    {
        get
        {
            var locale = LocalizationSettings.SelectedLocale;
            string code = locale != null ? locale.Identifier.Code : "th";
            return code.StartsWith("en", StringComparison.OrdinalIgnoreCase);
        }
    }

    private string NoDetailText => UseEnglish ? "No detail available" : "ไม่พบข้อมูลรายละเอียด";
    private string MoreInfoLabel => UseEnglish ? "Additional Details" : "รายละเอียดเพิ่มเติม";
    private string NarratorIdleLabel => UseEnglish ? "Narrator" : "ผู้บรรยายอวตาร";
    private string NarratorLoadingLabel => UseEnglish ? "Cancel" : "ยกเลิก";
    private string NarratorPlayingLabel => UseEnglish ? "Stop" : "หยุด";

    private void Awake()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(Hide);

        if (moreInfoButton != null)
            moreInfoButton.onClick.AddListener(HandleMoreInfoClicked);

        if (narratorButton != null)
            narratorButton.onClick.AddListener(HandleNarratorClicked);

        if (narrationController != null)
            narrationController.StateChanged += HandleNarrationStateChanged;

        RefreshButtons();
    }

    private void OnDestroy()
    {
        if (closeButton != null)
            closeButton.onClick.RemoveListener(Hide);

        if (moreInfoButton != null)
            moreInfoButton.onClick.RemoveListener(HandleMoreInfoClicked);

        if (narratorButton != null)
            narratorButton.onClick.RemoveListener(HandleNarratorClicked);

        if (narrationController != null)
            narrationController.StateChanged -= HandleNarrationStateChanged;
    }

    public void Show(HOH_PersonDto person, HOH_UnitDto unit, HOH_OfficerSelectionPanelController previousSelection)
    {
        _currentPerson = person;
        _currentUnit = unit;
        _previousSelection = previousSelection;

        narrationController?.BindPerson(_currentPerson);

        if (root != null)
            root.SetActive(true);

        BindText();
        BindPhoto();
        RefreshButtons();
    }

    public void Hide()
    {
        if (additionalDetailUI != null)
            additionalDetailUI.HideSilently();

        narrationController?.ClearTarget(true);

        _currentPerson = null;
        _currentUnit = null;

        ApplyEmptyState();

        if (root != null)
            root.SetActive(false);

        RefreshButtons();

        if (_previousSelection != null)
            _previousSelection.ReopenFromChild();
    }

    public void HideSilently()
    {
        narrationController?.ClearTarget(true);

        _currentPerson = null;
        _currentUnit = null;

        ApplyEmptyState();

        if (root != null)
            root.SetActive(false);

        RefreshButtons();
    }

    public void ReopenFromChild()
    {
        if (root != null)
            root.SetActive(true);

        RefreshButtons();
    }

    public void StopNarration()
    {
        narrationController?.StopMedia();
        RefreshButtons();
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

        if (shortDescriptionText != null)
        {
            shortDescriptionText.text = TruncateWithEllipsis(
                BuildShortDescription(_currentPerson),
                shortDescriptionCharacterLimit
            );
        }
    }

    private void BindPhoto()
    {
        if (photoLoader == null)
            return;

        if (_currentPerson == null)
        {
            photoLoader.Load(string.Empty);
            return;
        }

        photoLoader.Load(_currentPerson.photoUrl);
    }

    private void ApplyEmptyState()
    {
        if (titleText != null)
            titleText.text = string.Empty;

        if (roleText != null)
            roleText.text = string.Empty;

        if (organizationText != null)
            organizationText.text = string.Empty;

        if (shortDescriptionText != null)
            shortDescriptionText.text = string.Empty;

        if (photoLoader != null)
            photoLoader.Load(string.Empty);
    }

    private void HandleMoreInfoClicked()
    {
        if (_currentPerson == null)
            return;

        if (additionalDetailUI == null)
        {
            Debug.LogWarning("[HOH_OfficerDetailUI] AdditionalDetailUI is not assigned.");
            return;
        }

        narrationController?.StopMedia();

        if (root != null)
            root.SetActive(false);

        additionalDetailUI.Show(_currentPerson, _currentUnit, this);
    }

    private void HandleNarratorClicked()
    {
        if (!narratorAvailable || _currentPerson == null)
            return;

        if (narrationController == null)
        {
            Debug.LogWarning("[HOH_OfficerDetailUI] NarrationController is not assigned.");
            return;
        }

        narrationController.ToggleBoundPerson();
        RefreshButtons();
    }

    private void HandleNarrationStateChanged(MediaPlaybackState state)
    {
        RefreshButtons();
    }

    private void RefreshButtons()
    {
        if (moreInfoButton != null)
            moreInfoButton.interactable = _currentPerson != null;

        if (moreInfoButtonLabel != null)
            moreInfoButtonLabel.text = MoreInfoLabel;

        if (narratorButtonRoot != null)
            narratorButtonRoot.SetActive(narratorAvailable);

        if (narratorButton != null)
        {
            bool isLoading =
                narrationController != null &&
                narrationController.State == MediaPlaybackState.Loading;

            narratorButton.interactable =
                narratorAvailable &&
                narrationController != null &&
                _currentPerson != null &&
                !isLoading;
        }

        RefreshNarratorButtonLabel();
    }

    private void RefreshNarratorButtonLabel()
    {
        if (narratorButtonLabel == null)
            return;

        if (!narratorAvailable || narrationController == null || _currentPerson == null)
        {
            narratorButtonLabel.text = NarratorIdleLabel;
            return;
        }

        switch (narrationController.State)
        {
            case MediaPlaybackState.Loading:
                narratorButtonLabel.text = NarratorLoadingLabel;
                break;

            case MediaPlaybackState.Playing:
                narratorButtonLabel.text = NarratorPlayingLabel;
                break;

            default:
                narratorButtonLabel.text = NarratorIdleLabel;
                break;
        }
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
            return JoinNonEmpty(" ", FirstNotEmpty(person.positionEn, person.position), FirstNotEmpty(person.positionLevelEn, person.positionLevel));

        return JoinNonEmpty(" ", person.position, person.positionLevel);
    }

    private string BuildShortDescription(HOH_PersonDto person)
    {
        if (person == null)
            return string.Empty;

        if (UseEnglish)
        {
            return FirstNotEmpty(
                person.outstandingWorkEn,
                JoinNonEmpty(" ", person.educationEn, person.institutionEn),
                person.outstandingWork,
                JoinNonEmpty(" ", person.education, person.institution),
                NoDetailText
            );
        }

        return FirstNotEmpty(
            person.outstandingWork,
            JoinNonEmpty(" ", person.education, person.institution),
            person.outstandingWorkEn,
            JoinNonEmpty(" ", person.educationEn, person.institutionEn),
            NoDetailText
        );
    }

    private string TruncateWithEllipsis(string value, int maxCharacters)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        if (maxCharacters <= 0 || value.Length <= maxCharacters)
            return value;

        return value.Substring(0, maxCharacters).TrimEnd() + "...";
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