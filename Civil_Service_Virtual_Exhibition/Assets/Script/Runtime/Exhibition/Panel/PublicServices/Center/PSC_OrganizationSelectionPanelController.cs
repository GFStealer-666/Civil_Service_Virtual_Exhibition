using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PSC_OrganizationSelectionPanelController : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private GameObject previousPageRoot;

    [Header("Data")]
    [SerializeField] private PSC_Repository repository;
    [SerializeField] private PSC_ServiceDetailPanelController serviceDetailPanel;

    [Header("Header")]
    [SerializeField] private Image ministryLogoImage;
    [SerializeField] private UniversalImageLoader ministryLogoLoader;
    [SerializeField] private Sprite fallbackMinistryLogo;
    [SerializeField] private TMP_Text welcomeTitleText;
    [SerializeField] private TMP_Text ministryNameText;
    [SerializeField] private TMP_Text organizationCountText;

    [Header("List")]
    [SerializeField] private Transform contentRoot;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private PSC_OrganizationItemView itemPrefab;

    [Header("Empty State")]
    [SerializeField] private GameObject emptyStateRoot;
    [SerializeField] private TMP_Text emptyStateText;
    [SerializeField] private string emptyMessage = "ไม่พบข้อมูล";

    [Header("Buttons")]
    [SerializeField] private Button closeButton;
    [SerializeField] private Button backButton;

    [Header("Display")]
    [SerializeField] private bool pushUiBlockOnOpen = true;
    [SerializeField] private string welcomeTextThai = "ยินดีต้อนรับสู่";
    [SerializeField] private string welcomeTextEnglish = "Welcome to";
    [SerializeField] private bool useEnglishText = false;

    public PSC_ServiceMinistryDto SelectedMinistry { get; private set; }
    public PSC_ServiceOrganizationDto SelectedOrganization { get; private set; }
    public bool IsOpen => panelRoot != null ? panelRoot.activeInHierarchy : gameObject.activeInHierarchy;

    public event Action<PSC_ServiceOrganizationDto> OrganizationSelected;
    public event Action Closed;

    private readonly List<PSC_OrganizationItemView> _spawnedItems = new();
    private bool _uiBound;

    private void Awake()
    {
        ResolveReferences();
        BindUi();
    }

    public void Open(PSC_ServiceMinistryDto ministry)
    {
        if (ministry == null)
        {
            Debug.LogWarning("[PSC_OrganizationSelectionPanelController] Ministry is null.");
            return;
        }

        SelectedMinistry = ministry;
        SelectedOrganization = null;

        if (previousPageRoot != null)
            previousPageRoot.SetActive(false);

        if (panelRoot != null)
            panelRoot.SetActive(true);

        RefreshHeader();
        RefreshList();
        ResetScrollToTop();

        if (pushUiBlockOnOpen)
            PlayerInput.PushUIBlock();
    }

    public void Close()
    {
        ClearItems();
        ClearHeader();

        SelectedOrganization = null;
        SelectedMinistry = null;

        if (panelRoot != null)
            panelRoot.SetActive(false);

        if (previousPageRoot != null)
            previousPageRoot.SetActive(true);

        if (pushUiBlockOnOpen)
            PlayerInput.PopUIBlock();

        Closed?.Invoke();
    }

    public void RefreshCurrent()
    {
        if (SelectedMinistry == null)
        {
            SetEmptyState(true, emptyMessage);
            return;
        }

        RefreshHeader();
        RefreshList();
    }

    private void HandleItemClicked(PSC_ServiceOrganizationDto organization)
    {
        if (organization == null)
            return;

        SelectedOrganization = organization;

        if (serviceDetailPanel != null)
        {
            serviceDetailPanel.Open(organization);
            return;
        }

        OrganizationSelected?.Invoke(organization);
    }

    private void RefreshHeader()
    {
        if (SelectedMinistry == null)
            return;

        if (welcomeTitleText != null)
            welcomeTitleText.text = useEnglishText ? welcomeTextEnglish : welcomeTextThai;

        if (ministryNameText != null)
            ministryNameText.text = GetMinistryDisplayName(SelectedMinistry);

        if (organizationCountText != null)
            organizationCountText.text = useEnglishText
                ? BuildEnglishCountText(SelectedMinistry.runtimeOrganizationCount)
                : BuildThaiCountText(SelectedMinistry.runtimeOrganizationCount);

        BindMinistryLogo(SelectedMinistry);
    }

    private void BindMinistryLogo(PSC_ServiceMinistryDto ministry)
    {
        string logoUrl = ministry != null ? ministry.ministryLogo : string.Empty;

        if (ministryLogoImage != null)
        {
            ministryLogoImage.sprite = fallbackMinistryLogo;
            ministryLogoImage.enabled = true;
        }

        if (ministryLogoLoader == null)
            return;

        if (string.IsNullOrWhiteSpace(logoUrl))
            return;

        ministryLogoLoader.Load(logoUrl);
    }

    private void RefreshList()
    {
        ClearItems();

        if (contentRoot == null || itemPrefab == null)
        {
            Debug.LogWarning("[PSC_OrganizationSelectionPanelController] ContentRoot or ItemPrefab is missing.");
            SetEmptyState(true, emptyMessage);
            return;
        }

        if (repository == null)
        {
            SetEmptyState(true, "Repository not found");
            return;
        }

        if (SelectedMinistry == null)
        {
            SetEmptyState(true, emptyMessage);
            return;
        }

        List<PSC_ServiceOrganizationDto> organizations =
            repository.GetOrganizationsByMinistry(SelectedMinistry.runtimeId);

        if (organizations == null || organizations.Count == 0)
        {
            SetEmptyState(true, emptyMessage);
            return;
        }

        for (int i = 0; i < organizations.Count; i++)
        {
            PSC_ServiceOrganizationDto organization = organizations[i];
            if (organization == null)
                continue;

            PSC_OrganizationItemView view = Instantiate(itemPrefab, contentRoot);
            view.gameObject.SetActive(true);
            view.Bind(organization, HandleItemClicked);

            _spawnedItems.Add(view);
        }

        SetEmptyState(_spawnedItems.Count == 0, emptyMessage);
    }

    private void ClearItems()
    {
        for (int i = 0; i < _spawnedItems.Count; i++)
        {
            if (_spawnedItems[i] != null)
                Destroy(_spawnedItems[i].gameObject);
        }

        _spawnedItems.Clear();
    }

    private void ClearHeader()
    {
        if (welcomeTitleText != null)
            welcomeTitleText.text = string.Empty;

        if (ministryNameText != null)
            ministryNameText.text = string.Empty;

        if (organizationCountText != null)
            organizationCountText.text = string.Empty;

        if (ministryLogoImage != null)
        {
            ministryLogoImage.sprite = fallbackMinistryLogo;
            ministryLogoImage.enabled = ministryLogoImage.sprite != null;
        }
    }

    private void SetEmptyState(bool visible, string message)
    {
        if (emptyStateRoot != null)
            emptyStateRoot.SetActive(visible);

        if (emptyStateText != null)
            emptyStateText.text = message;
    }

    private void ResetScrollToTop()
    {
        if (scrollRect == null)
            return;

        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 1f;
    }

    private string GetMinistryDisplayName(PSC_ServiceMinistryDto ministry)
    {
        if (ministry == null)
            return string.Empty;

        if (useEnglishText && !string.IsNullOrWhiteSpace(ministry.ministryEn))
            return ministry.ministryEn;

        return !string.IsNullOrWhiteSpace(ministry.ministry)
            ? ministry.ministry
            : ministry.ministryEn;
    }

    private string BuildThaiCountText(int count)
    {
        return $"จำนวน {count} หน่วยงาน";
    }

    private string BuildEnglishCountText(int count)
    {
        return count == 1 ? "1 organization" : $"{count} organizations";
    }

    private void ResolveReferences()
    {
        if (repository == null)
            repository = FindObjectOfType<PSC_Repository>();
    }

    private void BindUi()
    {
        if (_uiBound)
            return;

        _uiBound = true;

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        if (backButton != null)
            backButton.onClick.AddListener(Close);
    }
}