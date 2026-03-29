using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PS_PanelController : MonoBehaviour
{
    [Header("Repository")]
    [SerializeField] private PS_CatalogRepository repository;

    [Header("Category Filter")]
    [SerializeField] private string targetCategory;

    [Header("Localized Header")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private string titleTh;
    [SerializeField] private string titleEn;

    [Header("Localized Summary")]
    [SerializeField] private TMP_Text totalCountText;
    [SerializeField] private string totalFormatTh = "ทั้งหมด {0} กิจกรรม";
    [SerializeField] private string totalFormatEn = "Total {0} activities";

    [Header("Localized Search Placeholder")]
    [SerializeField] private TMP_Text searchPlaceholderText;
    [SerializeField] private string searchPlaceholderTh = "ค้นหาชื่อกิจกรรมหรือหน่วยงาน";
    [SerializeField] private string searchPlaceholderEn = "Search activity or department";

    [Header("Status UI")]
    [SerializeField] private TMP_Text statusText;

    [Header("List UI")]
    [SerializeField] private Transform contentRoot;
    [SerializeField] private PS_ServiceItemView itemPrefab;
    [SerializeField] private GameObject loadingObject;
    [SerializeField] private GameObject emptyStateObject;

    private readonly List<PS_ServiceItemView> _spawnedItems = new List<PS_ServiceItemView>();

    private string _searchKeyword = string.Empty;
    private string _lastLocaleCode = string.Empty;

    private void OnEnable()
    {
        if (repository == null)
            repository = PS_CatalogRepository.Instance;

        if (repository == null)
        {
            SetStatus(IsEnglish() ? "PS_CatalogRepository is missing." : "ไม่พบ PS_CatalogRepository");
            ShowLoading(false);
            ShowEmpty(true);
            ApplyLocalizationOnly();
            return;
        }

        repository.OnDataLoaded += HandleRepositoryLoaded;
        repository.OnDataLoadFailed += HandleRepositoryLoadFailed;

        _lastLocaleCode = GetLocaleCode();

        repository.Initialize();
        RefreshView();
    }

    private void OnDisable()
    {
        if (repository == null)
            return;

        repository.OnDataLoaded -= HandleRepositoryLoaded;
        repository.OnDataLoadFailed -= HandleRepositoryLoadFailed;
    }

    private void Update()
    {
        string localeCode = GetLocaleCode();
        if (string.Equals(_lastLocaleCode, localeCode, StringComparison.OrdinalIgnoreCase))
            return;

        _lastLocaleCode = localeCode;
        RefreshView();
    }

    public void SetSearchKeyword(string keyword)
    {
        _searchKeyword = keyword ?? string.Empty;
        RebuildList();
        UpdateHeaderTexts();
    }

    public void SetCategory(string categoryName)
    {
        targetCategory = categoryName;
        RefreshView();
    }

    public void SetLocalizedTitle(string thaiTitle, string englishTitle)
    {
        titleTh = thaiTitle;
        titleEn = englishTitle;
        UpdateHeaderTexts();
    }

    public void RefreshFromRepository()
    {
        RefreshView();
    }

    private void HandleRepositoryLoaded()
    {
        RefreshView();
    }

    private void HandleRepositoryLoadFailed(string error)
    {
        ClearItems();
        UpdateHeaderTexts();
        ShowLoading(false);
        ShowEmpty(true);
        SetStatus(string.IsNullOrWhiteSpace(error)
            ? (IsEnglish() ? "Load failed." : "โหลดข้อมูลไม่สำเร็จ")
            : error);
    }

    private void RefreshView()
    {
        ApplyLocalizationOnly();

        if (repository == null)
            return;

        if (repository.IsLoading && !repository.HasData)
        {
            ClearItems();
            ShowLoading(true);
            ShowEmpty(false);
            SetStatus(string.Empty);
            UpdateHeaderTexts();
            return;
        }

        if (!repository.HasData)
        {
            ClearItems();
            ShowLoading(false);
            ShowEmpty(true);
            SetStatus(string.IsNullOrWhiteSpace(repository.LastError)
                ? (IsEnglish() ? "No data." : "ไม่มีข้อมูล")
                : repository.LastError);
            UpdateHeaderTexts();
            return;
        }

        ShowLoading(false);
        SetStatus(string.Empty);
        UpdateHeaderTexts();
        RebuildList();
    }

    private void RebuildList()
    {
        ClearItems();

        if (repository == null || !repository.HasData)
        {
            ShowEmpty(true);
            return;
        }

        List<PS_ServiceActivityDto> items = repository.GetActivitiesByCategory(targetCategory, _searchKeyword);

        if (items == null || items.Count == 0)
        {
            ShowEmpty(true);
            return;
        }

        ShowEmpty(false);

        for (int i = 0; i < items.Count; i++)
        {
            PS_ServiceActivityDto item = items[i];
            if (item == null)
                continue;

            PS_ServiceItemView view = Instantiate(itemPrefab, contentRoot);
            view.Bind(item, IsEnglish());
            _spawnedItems.Add(view);
        }
    }

    private void ApplyLocalizationOnly()
    {
        UpdateHeaderTexts();
        UpdateSearchPlaceholder();
    }

    private void UpdateHeaderTexts()
    {
        bool useEnglish = IsEnglish();

        if (titleText != null)
        {
            titleText.text = useEnglish
                ? Clean(titleEn, Clean(titleTh, "-"))
                : Clean(titleTh, Clean(titleEn, "-"));
        }

        if (totalCountText != null)
        {
            int total = repository != null ? repository.GetCategoryTotalCount(targetCategory) : 0;
            string format = useEnglish ? totalFormatEn : totalFormatTh;
            totalCountText.text = string.Format(format, total);
        }
    }

    private void UpdateSearchPlaceholder()
    {
        if (searchPlaceholderText == null)
            return;

        searchPlaceholderText.text = IsEnglish()
            ? Clean(searchPlaceholderEn, "Search activity or department")
            : Clean(searchPlaceholderTh, "ค้นหาชื่อกิจกรรมหรือหน่วยงาน");
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

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }

    private void ShowLoading(bool show)
    {
        if (loadingObject != null)
            loadingObject.SetActive(show);
    }

    private void ShowEmpty(bool show)
    {
        if (emptyStateObject != null)
            emptyStateObject.SetActive(show);
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