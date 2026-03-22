using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class AE_ProjectSelectionUI : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("Build")]
    [SerializeField] private RectTransform pageRoot;
    [SerializeField] private RectTransform dotRoot;
    [SerializeField] private RectTransform pagePrefab;
    [SerializeField] private ExhibitionProjectCardUI cardPrefab;
    [SerializeField] private PaginationDotUI dotPrefab;
    [SerializeField] private int itemsPerPage = 3;

    [Header("Input")]
    [SerializeField] private bool allowSwipe = true;
    [SerializeField] private float swipeThreshold = 80f;

    private readonly List<RectTransform> _pages = new List<RectTransform>();
    private readonly List<PaginationDotUI> _dots = new List<PaginationDotUI>();
    private readonly List<ExhibitionProjectData> _items = new List<ExhibitionProjectData>();

    private Vector2 _pointerDownPosition;
    private int _currentPageIndex;

    public event Action<ExhibitionProjectData> ItemSelected;

    public void SetItems(IReadOnlyList<ExhibitionProjectData> items)
    {
        _items.Clear();

        if (items != null)
            _items.AddRange(items);

        Rebuild();
    }

    public void SetPage(int pageIndex)
    {
        if (_pages.Count == 0)
            return;

        _currentPageIndex = Mathf.Clamp(pageIndex, 0, _pages.Count - 1);

        for (int i = 0; i < _pages.Count; i++)
            _pages[i].gameObject.SetActive(i == _currentPageIndex);

        for (int i = 0; i < _dots.Count; i++)
            _dots[i].SetSelected(i == _currentPageIndex);
    }

    public void NextPage()
    {
        if (_currentPageIndex >= _pages.Count - 1)
            return;

        SetPage(_currentPageIndex + 1);
    }

    public void PreviousPage()
    {
        if (_currentPageIndex <= 0)
            return;

        SetPage(_currentPageIndex - 1);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _pointerDownPosition = eventData.position;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!allowSwipe || _pages.Count <= 1)
            return;

        Vector2 delta = eventData.position - _pointerDownPosition;

        if (Mathf.Abs(delta.x) < swipeThreshold)
            return;

        if (Mathf.Abs(delta.x) < Mathf.Abs(delta.y))
            return;

        if (delta.x < 0f)
            NextPage();
        else
            PreviousPage();
    }

    private void Rebuild()
    {
        ClearGenerated();

        if (pageRoot == null || dotRoot == null || pagePrefab == null || cardPrefab == null || dotPrefab == null)
        {
            Debug.LogError("[PaginatedProjectSelectionUI] Missing references.");
            return;
        }

        if (_items.Count == 0)
        {
            _currentPageIndex = 0;
            return;
        }

        int safeItemsPerPage = Mathf.Max(1, itemsPerPage);
        int pageCount = Mathf.CeilToInt(_items.Count / (float)safeItemsPerPage);

        for (int pageIndex = 0; pageIndex < pageCount; pageIndex++)
        {
            RectTransform page = Instantiate(pagePrefab, pageRoot);
            page.name = $"Page_{pageIndex + 1}";
            page.gameObject.SetActive(true);
            _pages.Add(page);

            int start = pageIndex * safeItemsPerPage;
            int end = Mathf.Min(start + safeItemsPerPage, _items.Count);

            for (int itemIndex = start; itemIndex < end; itemIndex++)
            {
                ExhibitionProjectCardUI card = Instantiate(cardPrefab, page);
                card.name = $"Item_{itemIndex + 1}";
                card.Bind(_items[itemIndex], HandleItemClicked);
            }

            PaginationDotUI dot = Instantiate(dotPrefab, dotRoot);
            dot.name = $"Dot_{pageIndex + 1}";
            dot.Bind(pageIndex, SetPage);
            _dots.Add(dot);
        }

        SetPage(0);
    }

    private void HandleItemClicked(ExhibitionProjectData data)
    {
        ItemSelected?.Invoke(data);
        Debug.Log($"[PaginatedProjectSelectionUI] Clicked project: {data.Title}");
    }

    private void ClearGenerated()
    {
        for (int i = 0; i < _pages.Count; i++)
        {
            if (_pages[i] != null)
                Destroy(_pages[i].gameObject);
        }

        for (int i = 0; i < _dots.Count; i++)
        {
            if (_dots[i] != null)
                Destroy(_dots[i].gameObject);
        }

        _pages.Clear();
        _dots.Clear();
    }
}