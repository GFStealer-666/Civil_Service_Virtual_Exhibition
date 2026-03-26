using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[Serializable]
public class HOH_PanelSection
{
    public HOH_CategoryKind categoryKind;
    public GameObject panelRoot;
    public Transform contentRoot;
    public HOH_ItemView itemPrefab;
    public HOH_FilterGroup filterGroup;
    public HOH_SearchBar searchBar;
    public GameObject emptyStateRoot;
    public TextMeshProUGUI emptyStateText;

    [NonSerialized] public readonly HashSet<HOH_FilterOption> currentFilters = new();
    [NonSerialized] public string currentSearch = string.Empty;
    [NonSerialized] public readonly List<HOH_ItemView> spawnedItems = new();
}

public class HOH_PanelController : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject panelRoot;

    [Header("Data")]
    [SerializeField] private HOH_CatalogRepository repository;
    [SerializeField] private HOH_FilterResolver filterResolver;
    [SerializeField] private bool initializeRepositoryOnStart = true;

    [Header("Detail")]
    [SerializeField] private HOH_OfficerSelectionPanelController officerSelectionPanel;

    [Header("Display")]
    [SerializeField] private bool singleVisibleCategoryMode = true;
    [SerializeField] private HOH_CategoryKind defaultVisibleCategory = HOH_CategoryKind.Ministry;
    [SerializeField] private List<HOH_PanelSection> sections = new();

    public HOH_CategoryKind CurrentCategory { get; private set; } = HOH_CategoryKind.Unknown;
    public bool IsOpen => panelRoot != null ? panelRoot.activeInHierarchy : gameObject.activeInHierarchy;

    private bool _uiBound;

    private void Awake()
    {
        ResolveReferences();
        BindSectionUi();
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
        if (initializeRepositoryOnStart && repository != null && !repository.HasData && !repository.IsLoading)
            repository.Initialize();
    }

    private void OnDisable()
    {
        if (repository != null)
        {
            repository.OnDataLoaded -= HandleRepositoryLoaded;
            repository.OnDataLoadFailed -= HandleRepositoryFailed;
        }
    }

    public void Open(HOH_CategoryKind kind)
    {
        if (panelRoot != null)
            panelRoot.SetActive(true);

        CurrentCategory = kind;

        if (repository != null && !repository.HasData && !repository.IsLoading)
            repository.Initialize();

        ShowCategory(kind);
        RefreshCategory(kind);

        PlayerInput.PushUIBlock();
    }

    public void Close()
    {
        HideAllCategoryPanels();
        CurrentCategory = HOH_CategoryKind.Unknown;

        if (panelRoot != null)
            panelRoot.SetActive(false);

        PlayerInput.PopUIBlock();
    }

    public void ShowCategory(HOH_CategoryKind kind)
    {
        CurrentCategory = kind;

        for (int i = 0; i < sections.Count; i++)
        {
            HOH_PanelSection section = sections[i];
            if (section == null || section.panelRoot == null)
                continue;

            section.panelRoot.SetActive(section.categoryKind == kind);
        }
    }

    public void RefreshAllSections()
    {
        for (int i = 0; i < sections.Count; i++)
            RefreshSection(sections[i]);
    }

    public void RefreshCategory(HOH_CategoryKind categoryKind)
    {
        HOH_PanelSection section = GetSection(categoryKind);
        if (section == null)
            return;

        RefreshSection(section);
    }

    private void HandleRepositoryLoaded()
    {
        if (singleVisibleCategoryMode && CurrentCategory != HOH_CategoryKind.Unknown)
            RefreshCategory(CurrentCategory);
        else
            RefreshAllSections();
    }

    private void HandleRepositoryFailed(string error)
    {
        for (int i = 0; i < sections.Count; i++)
        {
            HOH_PanelSection section = sections[i];
            if (section == null)
                continue;

            ClearSectionItems(section);
            SetEmptyState(section, true, string.IsNullOrWhiteSpace(error) ? "โหลดข้อมูลไม่สำเร็จ" : error);
        }
    }

    private void RefreshSection(HOH_PanelSection section)
    {
        if (section == null)
            return;

        if (section.contentRoot == null || section.itemPrefab == null)
            return;

        ClearSectionItems(section);

        if (repository == null || !repository.HasData)
        {
            SetEmptyState(section, true, "ยังไม่มีข้อมูล");
            return;
        }

        List<HOH_UnitDto> sourceUnits = repository.GetUnitsByKind(section.categoryKind);
        if (sourceUnits == null || sourceUnits.Count == 0)
        {
            SetEmptyState(section, true, "ไม่พบข้อมูล");
            return;
        }

        int visibleCount = 0;

        for (int i = 0; i < sourceUnits.Count; i++)
        {
            HOH_UnitDto unit = sourceUnits[i];
            if (unit == null)
                continue;

            if (!MatchesSearch(unit, section.currentSearch))
                continue;

            if (!MatchesFilter(section.categoryKind, unit, section.currentFilters))
                continue;

            HOH_ItemView view = Instantiate(section.itemPrefab, section.contentRoot);
            view.gameObject.SetActive(true);
            view.Bind(unit, HandleUnitClicked);

            section.spawnedItems.Add(view);
            visibleCount++;
        }

        SetEmptyState(section, visibleCount == 0, "ไม่พบข้อมูล");
    }

    private bool MatchesFilter(HOH_CategoryKind categoryKind, HOH_UnitDto unit, IReadOnlyCollection<HOH_FilterOption> filters)
    {
        if (filters == null || filters.Count == 0)
            return true;

        if (filterResolver == null)
            return false;

        foreach (HOH_FilterOption filter in filters)
        {
            if (filterResolver.Matches(categoryKind, unit, filter))
                return true;
        }

        return false;
    }

    private bool MatchesSearch(HOH_UnitDto unit, string search)
    {
        if (string.IsNullOrWhiteSpace(search))
            return true;

        string normalizedSearch = search.Trim();

        return ContainsIgnoreCase(unit.unit, normalizedSearch)
            || ContainsIgnoreCase(unit.unitEn, normalizedSearch)
            || ContainsIgnoreCase(unit.ministry, normalizedSearch)
            || ContainsIgnoreCase(unit.ministryEn, normalizedSearch);
    }

    private bool ContainsIgnoreCase(string source, string value)
    {
        if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(value))
            return false;

        return source.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private void ClearSectionItems(HOH_PanelSection section)
    {
        if (section == null)
            return;

        for (int i = 0; i < section.spawnedItems.Count; i++)
        {
            if (section.spawnedItems[i] != null)
                Destroy(section.spawnedItems[i].gameObject);
        }

        section.spawnedItems.Clear();
    }

    private void SetEmptyState(HOH_PanelSection section, bool visible, string message)
    {
        if (section.emptyStateRoot != null)
            section.emptyStateRoot.SetActive(visible);

        if (section.emptyStateText != null)
            section.emptyStateText.text = message;
    }

    private void ResolveReferences()
    {
        if (repository == null)
            repository = FindObjectOfType<HOH_CatalogRepository>();

        if (filterResolver == null)
            filterResolver = FindObjectOfType<HOH_FilterResolver>();
    }

    private void BindSectionUi()
    {
        if (_uiBound)
            return;

        _uiBound = true;

        for (int i = 0; i < sections.Count; i++)
        {
            HOH_PanelSection section = sections[i];
            if (section == null)
                continue;

            HOH_PanelSection capturedSection = section;

            if (capturedSection.filterGroup != null)
            {
                capturedSection.currentFilters.Clear();

                foreach (HOH_FilterOption filter in capturedSection.filterGroup.CurrentFilters)
                    capturedSection.currentFilters.Add(filter);

                capturedSection.filterGroup.FiltersChanged += filters =>
                {
                    capturedSection.currentFilters.Clear();

                    if (filters != null)
                    {
                        foreach (HOH_FilterOption filter in filters)
                            capturedSection.currentFilters.Add(filter);
                    }

                    RefreshSection(capturedSection);
                };
            }

            if (capturedSection.searchBar != null)
            {
                capturedSection.currentSearch = capturedSection.searchBar.CurrentText;
                capturedSection.searchBar.SearchChanged += search =>
                {
                    capturedSection.currentSearch = search;
                    RefreshSection(capturedSection);
                };
            }
        }
    }

    private HOH_PanelSection GetSection(HOH_CategoryKind categoryKind)
    {
        for (int i = 0; i < sections.Count; i++)
        {
            HOH_PanelSection section = sections[i];
            if (section == null)
                continue;

            if (section.categoryKind == categoryKind)
                return section;
        }

        return null;
    }

    private void HandleUnitClicked(HOH_UnitDto unit)
    {
        if (unit == null)
            return;

        if (officerSelectionPanel == null)
        {
            Debug.LogWarning("[HOH_PanelController] OfficerSelectionPanel is not assigned.");
            return;
        }

        bool opened = officerSelectionPanel.Open(unit, this);
        if (!opened)
            return;

        HideAllCategoryPanels();
    }

    public void HideAllCategoryPanels()
    {
        for (int i = 0; i < sections.Count; i++)
        {
            HOH_PanelSection section = sections[i];
            if (section == null || section.panelRoot == null)
                continue;

            section.panelRoot.SetActive(false);
        }
    }

    public void RestoreCurrentCategory()
    {
        if (CurrentCategory == HOH_CategoryKind.Unknown)
            return;

        ShowCategory(CurrentCategory);
        RefreshCategory(CurrentCategory);
    }
}