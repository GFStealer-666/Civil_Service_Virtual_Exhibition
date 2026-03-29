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
    [SerializeField] private GameObject serviceDetailPanelRoot;

    [Header("Ministry List")]
    [SerializeField] private Transform ministryContentRoot;
    [SerializeField] private PSC_MinistryItemView ministryItemPrefab;

    [Header("Ministry Filter")]
    [SerializeField] private PSC_FilterGroup filterGroup;
    [SerializeField] private TMP_InputField searchInputField;

    [Header("Search Placeholder")]
    [SerializeField] private TMP_Text searchPlaceholderText;
    [SerializeField] private string searchPlaceholderTh = "ค้นหาชื่อหน่วยงาน";
    [SerializeField] private string searchPlaceholderEn = "Search ministry name";

    [Header("Empty State")]
    [SerializeField] private GameObject emptyStateRoot;
    [SerializeField] private TextMeshProUGUI emptyStateText;
    [SerializeField] private string emptyNoDataTh = "ยังไม่มีข้อมูล";
    [SerializeField] private string emptyNoDataEn = "No data available";
    [SerializeField] private string emptyNotFoundTh = "ไม่พบข้อมูล";
    [SerializeField] private string emptyNotFoundEn = "No results found";
    [SerializeField] private string loadFailedTh = "โหลดข้อมูลไม่สำเร็จ";
    [SerializeField] private string loadFailedEn = "Failed to load data";

    public PSCPanelStage CurrentStage { get; private set; } = PSCPanelStage.Ministries;
    public PSC_ServiceMinistryDto SelectedMinistry { get; private set; }
    public PSC_ServiceOrganizationDto SelectedOrganization { get; private set; }

    private readonly HashSet<PSC_MinistryCategory> _currentFilters = new();
    private readonly List<PSC_MinistryItemView> _spawnedItems = new();
    private string _currentSearch = string.Empty;
    private bool _uiBound;
    private string _lastLocaleCode = string.Empty;

    private void Awake()
    {
        ResolveReferences();
        BindUi();
    }

    private void OnEnable()
    {
        if (repository != null)
        {
            repository.OnDataLoaded += HandleRepositoryLoaded;
            repository.OnDataLoadFailed += HandleRepositoryFailed;
        }

        _lastLocaleCode = GetLocaleCode();
        ApplyStaticLocalization();
    }

    private void Start()
    {
        if (initializeRepositoryOnStart && repository != null && !repository.HasData && !repository.IsLoading)
            repository.Initialize();
    }

    private void Update()
    {
        string localeCode = GetLocaleCode();
        if (string.Equals(_lastLocaleCode, localeCode, StringComparison.OrdinalIgnoreCase))
            return;

        _lastLocaleCode = localeCode;
        ApplyStaticLocalization();
        RefreshCurrentStage();
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
        ApplyStaticLocalization();
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
        SetEmptyState(true, string.IsNullOrWhiteSpace(error) ? GetLoadFailedText() : error);
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
            SetEmptyState(true, GetNoDataText());
            return;
        }

        List<PSC_ServiceMinistryDto> source = repository.GetMinistries();
        if (source == null || source.Count == 0)
        {
            SetEmptyState(true, GetNotFoundText());
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

        SetEmptyState(visibleCount == 0, GetNotFoundText());
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

    private void ApplyStaticLocalization()
    {
        if (searchPlaceholderText != null)
        {
            searchPlaceholderText.text = IsEnglish()
                ? Clean(searchPlaceholderEn, "Search ministry name")
                : Clean(searchPlaceholderTh, "ค้นหาชื่อหน่วยงาน");
        }
    }

    private string GetNoDataText()
    {
        return IsEnglish()
            ? Clean(emptyNoDataEn, "No data available")
            : Clean(emptyNoDataTh, "ยังไม่มีข้อมูล");
    }

    private string GetNotFoundText()
    {
        return IsEnglish()
            ? Clean(emptyNotFoundEn, "No results found")
            : Clean(emptyNotFoundTh, "ไม่พบข้อมูล");
    }

    private string GetLoadFailedText()
    {
        return IsEnglish()
            ? Clean(loadFailedEn, "Failed to load data")
            : Clean(loadFailedTh, "โหลดข้อมูลไม่สำเร็จ");
    }

    private static bool IsEnglish()
    {
        return GetLocaleCode().StartsWith("en", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetLocaleCode()
    {
        string code = LocalizationService.CurrentLocaleCode;
        return string.IsNullOrWhiteSpace(code) ? "th" : code;
    }

    private static string Clean(string value, string fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
            return fallback;

        return value.Replace("\r", " ").Replace("\n", " ").Trim();
    }
}