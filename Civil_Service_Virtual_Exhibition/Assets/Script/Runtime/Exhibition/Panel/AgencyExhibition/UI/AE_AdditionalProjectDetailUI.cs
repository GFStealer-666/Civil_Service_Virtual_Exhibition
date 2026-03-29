using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AE_AdditionalProjectDetailUI : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button closeButton;

    [Header("Content")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text agencyText;
    [SerializeField] private TMP_Text descriptionText;

    [Header("Optional")]
    [SerializeField] private ScrollRect scrollRect;

    private GovernmentProjectDto _currentProject;
    private string _currentAgencyName;

    public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

    private bool UseEnglish
    {
        get
        {
            string code = LocalizationService.CurrentLocaleCode;
            return !string.IsNullOrWhiteSpace(code) &&
                   code.StartsWith("en", System.StringComparison.OrdinalIgnoreCase);
        }
    }

    private void Awake()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(Hide);

        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    private void OnDestroy()
    {
        if (closeButton != null)
            closeButton.onClick.RemoveListener(Hide);
    }

    public void Show(GovernmentProjectDto project, string agencyName)
    {
        _currentProject = project;
        _currentAgencyName = agencyName;

        if (panelRoot != null)
            panelRoot.SetActive(true);

        RefreshUI();
        ResetScrollToTop();
    }

    public void Hide()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    public void Toggle(GovernmentProjectDto project, string agencyName)
    {
        if (IsOpen)
        {
            Hide();
            return;
        }

        Show(project, agencyName);
    }

    private void RefreshUI()
    {
        if (_currentProject == null)
        {
            ApplyEmptyState();
            return;
        }

        if (titleText != null)
            titleText.text = GetProjectTitle(_currentProject);

        if (agencyText != null)
            agencyText.text = _currentAgencyName ?? string.Empty;

        if (descriptionText != null)
            descriptionText.text = GetFullDescription(_currentProject);
    }

    private void ApplyEmptyState()
    {
        if (titleText != null)
            titleText.text = string.Empty;

        if (agencyText != null)
            agencyText.text = string.Empty;

        if (descriptionText != null)
            descriptionText.text = string.Empty;
    }

    private void ResetScrollToTop()
    {
        if (scrollRect == null)
            return;

        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 1f;
        scrollRect.horizontalNormalizedPosition = 0f;
    }

    private string GetProjectTitle(GovernmentProjectDto project)
    {
        if (project == null)
            return string.Empty;

        return UseEnglish
            ? FirstNotEmpty(project.nameEn, project.name)
            : FirstNotEmpty(project.name, project.nameEn);
    }

    private string GetFullDescription(GovernmentProjectDto project)
    {
        if (project == null)
            return string.Empty;

        return UseEnglish
            ? FirstNotEmpty(project.descriptionEn, project.description)
            : FirstNotEmpty(project.description, project.descriptionEn);
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
}