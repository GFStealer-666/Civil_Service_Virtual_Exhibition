using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public enum PSCPanelStage
{
    Ministries = 0,
    Organizations = 1,
    ServiceDetail = 2
}

public class PSC_PanelController : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject panelRoot;

    [Header("Data")]
    [SerializeField] private PSC_Repository repository;
    [SerializeField] private PSC_FilterResolver filterResolver;
    [SerializeField] private bool initializeRepositoryOnStart = true;

    [Header("Panels")]
    [SerializeField] private GameObject ministryPanelRoot;
    [SerializeField] private GameObject organizationPanelRoot;
    [SerializeField] private PSC_OrganizationSelectionPanelController organizationSelectionPanel;
    [SerializeField] private PSC_ServiceDetailPanelController serviceDetailPanel;
    [SerializeField] private GameObject serviceDetailPanelRoot;

    [Header("Ministry List")]
    [SerializeField] private Transform ministryContentRoot;
    [SerializeField] private PSC_MinistryItemView ministryItemPrefab;

    [Header("Ministry Filter")]
    [SerializeField] private PSC_FilterGroup filterGroup;
    [SerializeField] private TMP_InputField searchInputField;

    [Header("Empty State")]
    [SerializeField] private GameObject emptyStateRoot;
    [SerializeField] private TextMeshProUGUI emptyStateText;

    public PSCPanelStage CurrentStage { get; private set; } = PSCPanelStage.Ministries;
    public PSC_ServiceMinistryDto SelectedMinistry { get; private set; }
    public PSC_ServiceOrganizationDto SelectedOrganization { get; private set; }

    private readonly HashSet<PSC_MinistryCategory> _currentFilters = new();
    private readonly List<PSC_MinistryItemView> _spawnedItems = new();
    private string _currentSearch = string.Empty;
    private bool _uiBound;

    private void Awake()
    {
        ResolveReferences();
        BindUi();
        //ShowStage(PSCPanelStage.Ministries);
    }

    private void OnEnable()
    {
        if (repository != null)
        {
            repository.OnDataLoaded += HandleRepositoryLoaded;
            repository.OnDataLoadFailed += HandleRepositoryFailed;
        }
    }

    private void Start()
    {
        // if (initializeRepositoryOnStart && repository != null && !repository.HasData && !repository.IsLoading)
        //     repository.Initialize();
    }

    private void OnDisable()
    {
        if (repository != null)
        {
            repository.OnDataLoaded -= HandleRepositoryLoaded;
            repository.OnDataLoadFailed -= HandleRepositoryFailed;
        }
    }

    public void Open()
    {
        if (panelRoot != null)
            panelRoot.SetActive(true);

        if (repository != null && !repository.HasData && !repository.IsLoading)
            repository.Initialize();

        ShowStage(PSCPanelStage.Ministries);
        RefreshCurrentStage();

        PlayerInput.PushUIBlock();
    }

    public void Close()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);

        PlayerInput.PopUIBlock();
    }

    public void OpenOrganizations(PSC_ServiceMinistryDto ministry)
    {
        if (ministry == null)
            return;

        SelectedMinistry = ministry;
        SelectedOrganization = null;

        ShowStage(PSCPanelStage.Organizations);
    }

    public void BackToMinistries()
    {
        SelectedOrganization = null;
        ShowStage(PSCPanelStage.Ministries);
        RefreshMinistryPanel();
    }

    public void OpenServiceDetail(PSC_ServiceOrganizationDto organization)
    {
        if (organization == null)
            return;

        SelectedOrganization = organization;
        ShowStage(PSCPanelStage.ServiceDetail);
    }

    public void BackToOrganizations()
    {
        ShowStage(PSCPanelStage.Organizations);
    }

    public List<PSC_ServiceOrganizationDto> GetSelectedMinistryOrganizations()
    {
        if (SelectedMinistry == null)
            return new List<PSC_ServiceOrganizationDto>();

        return repository != null
            ? repository.GetOrganizationsByMinistry(SelectedMinistry.runtimeId)
            : new List<PSC_ServiceOrganizationDto>();
    }

    public List<PSC_ServiceItemDto> GetSelectedOrganizationServices()
    {
        if (SelectedOrganization == null)
            return new List<PSC_ServiceItemDto>();

        return repository != null
            ? repository.GetServicesByOrganization(SelectedOrganization.runtimeId)
            : new List<PSC_ServiceItemDto>();
    }

    private void HandleRepositoryLoaded()
    {
        RefreshCurrentStage();
    }

    private void HandleRepositoryFailed(string error)
    {
        ClearItems();
        SetEmptyState(true, string.IsNullOrWhiteSpace(error) ? "โหลดข้อมูลไม่สำเร็จ" : error);
    }

    private void RefreshCurrentStage()
    {
        switch (CurrentStage)
        {
            case PSCPanelStage.Ministries:
                RefreshMinistryPanel();
                break;
        }
    }

    private void RefreshMinistryPanel()
    {
        if (ministryContentRoot == null || ministryItemPrefab == null)
            return;

        ClearItems();

        if (repository == null || !repository.HasData)
        {
            SetEmptyState(true, "ยังไม่มีข้อมูล");
            return;
        }

        List<PSC_ServiceMinistryDto> source = repository.GetMinistries();
        if (source == null || source.Count == 0)
        {
            SetEmptyState(true, "ไม่พบข้อมูล");
            return;
        }

        int visibleCount = 0;

        for (int i = 0; i < source.Count; i++)
        {
            PSC_ServiceMinistryDto ministry = source[i];
            if (ministry == null)
                continue;

            if (!MatchesSearch(ministry, _currentSearch))
                continue;

            if (!MatchesFilter(ministry, _currentFilters))
                continue;

            PSC_MinistryItemView view = Instantiate(ministryItemPrefab, ministryContentRoot);
            view.gameObject.SetActive(true);
            view.Bind(ministry, HandleMinistryClicked);

            _spawnedItems.Add(view);
            visibleCount++;
        }

        SetEmptyState(visibleCount == 0, "ไม่พบข้อมูล");
    }

    private bool MatchesFilter(PSC_ServiceMinistryDto ministry, IReadOnlyCollection<PSC_MinistryCategory> filters)
    {
        if (filters == null || filters.Count == 0)
            return true;

        if (filterResolver == null)
            return false;

        foreach (PSC_MinistryCategory filter in filters)
        {
            if (filterResolver.Matches(ministry, filter))
                return true;
        }

        return false;
    }

    private bool MatchesSearch(PSC_ServiceMinistryDto ministry, string search)
    {
        if (ministry == null)
            return false;

        if (string.IsNullOrWhiteSpace(search))
            return true;

        string normalized = search.Trim();

        return ContainsIgnoreCase(ministry.ministry, normalized)
            || ContainsIgnoreCase(ministry.ministryEn, normalized)
            || ContainsIgnoreCase(ministry.ministryType, normalized)
            || ContainsIgnoreCase(ministry.ministryTypeEn, normalized)
            || ContainsIgnoreCase(ministry.runtimeSearchBlob, normalized);
    }

    private bool ContainsIgnoreCase(string source, string value)
    {
        if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(value))
            return false;

        return source.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private void HandleMinistryClicked(PSC_ServiceMinistryDto ministry)
    {
        if (organizationSelectionPanel != null)
        {
            organizationSelectionPanel.Open(ministry);
            return;
        }

        OpenOrganizations(ministry);
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

    private void SetEmptyState(bool visible, string message)
    {
        if (emptyStateRoot != null)
            emptyStateRoot.SetActive(visible);

        if (emptyStateText != null)
            emptyStateText.text = message;
    }

    private void ResolveReferences()
    {
        if (repository == null)
            repository = FindObjectOfType<PSC_Repository>();

        if (filterResolver == null)
            filterResolver = FindObjectOfType<PSC_FilterResolver>();
    }

    private void BindUi()
    {
        if (_uiBound)
            return;

        _uiBound = true;

        if (filterGroup != null)
        {
            _currentFilters.Clear();

            foreach (PSC_MinistryCategory filter in filterGroup.CurrentFilters)
                _currentFilters.Add(filter);

            filterGroup.FiltersChanged += filters =>
            {
                _currentFilters.Clear();

                if (filters != null)
                {
                    foreach (PSC_MinistryCategory filter in filters)
                        _currentFilters.Add(filter);
                }

                RefreshMinistryPanel();
            };
        }

        if (searchInputField != null)
        {
            _currentSearch = searchInputField.text;
            searchInputField.onValueChanged.AddListener(search =>
            {
                _currentSearch = search;
                RefreshMinistryPanel();
            });
        }
    }

    private void ShowStage(PSCPanelStage stage)
    {
        CurrentStage = stage;

        if (ministryPanelRoot != null)
            ministryPanelRoot.SetActive(stage == PSCPanelStage.Ministries);

        if (organizationPanelRoot != null)
            organizationPanelRoot.SetActive(stage == PSCPanelStage.Organizations);

        if (serviceDetailPanelRoot != null)
            serviceDetailPanelRoot.SetActive(stage == PSCPanelStage.ServiceDetail);
    }
}