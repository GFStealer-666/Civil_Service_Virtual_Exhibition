using TMPro;
using UnityEngine;
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
    [SerializeField] private Button narratorButton;
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

        return JoinNonEmpty(" ", person.position, person.positionLevel);
    }

    private string BuildShortDescription(HOH_PersonDto person)
    {
        if (person == null)
            return string.Empty;

        return FirstNotEmpty(
            person.outstandingWork,
            JoinNonEmpty(" ", person.education, person.institution),
            "ไม่พบข้อมูลรายละเอียด"
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