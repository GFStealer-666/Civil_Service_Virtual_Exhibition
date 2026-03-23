using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PS_PanelController : MonoBehaviour
{
    [Header("Repository")]
    [SerializeField] private PS_ServiceRepository repository;

    [Header("Category")]
    [SerializeField] private string targetCategory;
    [SerializeField] private bool useEnglish = false;

    [Header("Header UI")]
    [SerializeField] private TMP_Text categoryTitleText;
    [SerializeField] private TMP_Text totalCountText;
    [SerializeField] private TMP_Text statusText;

    [Header("List UI")]
    [SerializeField] private Transform contentRoot;
    [SerializeField] private PS_ServiceItemView itemPrefab;
    [SerializeField] private GameObject loadingObject;
    [SerializeField] private GameObject emptyStateObject;

    private readonly List<PS_ServiceItemView> _spawnedItems = new List<PS_ServiceItemView>();

    private string _searchKeyword = string.Empty;

    private void OnEnable()
    {
        if (repository == null)
            repository = PS_ServiceRepository.Instance;

        if (repository == null)
        {
            SetStatus("PS_ServiceRepository is missing.");
            ShowLoading(false);
            ShowEmpty(true);
            return;
        }

        repository.OnDataLoaded += HandleRepositoryLoaded;
        repository.OnDataLoadFailed += HandleRepositoryLoadFailed;

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

    public void SetSearchKeyword(string keyword)
    {
        _searchKeyword = keyword ?? string.Empty;
        RebuildList();
    }

    public void SetCategory(string categoryName)
    {
        targetCategory = categoryName;
        RefreshView();
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
        UpdateHeader();
        ShowLoading(false);
        ShowEmpty(true);
        SetStatus(string.IsNullOrWhiteSpace(error) ? "Load failed." : error);
    }

    private void RefreshView()
    {
        if (repository == null)
            return;

        UpdateHeader();

        if (repository.IsLoading && !repository.HasData)
        {
            ClearItems();
            ShowLoading(true);
            ShowEmpty(false);
            SetStatus(string.Empty);
            return;
        }

        if (!repository.HasData)
        {
            ClearItems();
            ShowLoading(false);
            ShowEmpty(true);
            SetStatus(string.IsNullOrWhiteSpace(repository.LastError) ? "No data." : repository.LastError);
            return;
        }

        ShowLoading(false);
        SetStatus(string.Empty);
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
            view.Bind(item, useEnglish);
            _spawnedItems.Add(view);
        }
    }

    private void UpdateHeader()
    {
        if (repository == null)
            return;

        string displayName = repository.GetCategoryDisplayName(targetCategory, useEnglish);
        int total = repository.GetCategoryTotalCount(targetCategory);

        if (categoryTitleText != null)
            categoryTitleText.text = displayName;

        if (totalCountText != null)
            totalCountText.text = useEnglish
                ? $"Total {total} activities"
                : $"ทั้งหมด {total} กิจกรรม";
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
}