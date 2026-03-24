using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HOH_OfficerSelectionPanelController : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("Root")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button closeButton;    
    [SerializeField] private HOH_OfficerDetailUI officerDetailUI;
    [Header("Header")]
    [SerializeField] private Image unitLogoImage;
    [SerializeField] private TextMeshProUGUI unitNameText;
    [SerializeField] private TextMeshProUGUI subtitleText;

    [Header("Build")]
    [SerializeField] private RectTransform pageRoot;
    [SerializeField] private RectTransform dotRoot;
    [SerializeField] private RectTransform pagePrefab;
    [SerializeField] private HOH_OfficerCardUI officerCardPrefab;
    [SerializeField] private PaginationDotUI dotPrefab;
    [SerializeField] private int itemsPerPage = 3;

    [Header("Input")]
    [SerializeField] private bool allowSwipe = true;
    [SerializeField] private float swipeThreshold = 80f;

    [Header("Empty State")]
    [SerializeField] private GameObject emptyStateRoot;
    [SerializeField] private TextMeshProUGUI emptyStateText;
    [Header("Loader")]
    [SerializeField] private UniversalImageLoader photoLoader;

    private readonly List<RectTransform> _pages = new();
    private readonly List<PaginationDotUI> _dots = new();
    private readonly List<HOH_PersonDto> _items = new();

    private Vector2 _pointerDownPosition;
    private int _currentPageIndex;
    private HOH_UnitDto _currentUnit;
    private HOH_PanelController _owner;
    public bool IsOpen => panelRoot != null && panelRoot.activeSelf;
    public HOH_UnitDto CurrentUnit => _currentUnit;

    public event Action<HOH_PersonDto> OfficerSelected;

    private void Awake()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    public bool Open(HOH_UnitDto unit, HOH_PanelController owner)
    {
        _currentUnit = unit;
        _owner = owner;

        if (panelRoot == null)
        {
            Debug.LogWarning("[HOH_OfficerSelectionPanelController] panelRoot is not assigned.");
            return false;
        }

        panelRoot.SetActive(true);

        Canvas.ForceUpdateCanvases();

        BindHeader(unit);
        SetItems(unit != null ? unit.persons : null);

        Canvas.ForceUpdateCanvases();

        if (pageRoot != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(pageRoot);

        Debug.Log(
            $"[HOH_OfficerSelectionPanelController] Open success | " +
            $"activeSelf={panelRoot.activeSelf} | activeInHierarchy={panelRoot.activeInHierarchy}"
        );

        return true;
    }

    public void Close()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);

        if (_owner != null)
            _owner.RestoreCurrentCategory();
    }

    public void SetItems(IReadOnlyList<HOH_PersonDto> items)
    {
        _items.Clear();

        if (items != null)
            _items.AddRange(items);

        Rebuild();
    }
    public void ReopenFromChild()
    {
        if (panelRoot != null)
            panelRoot.SetActive(true);
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

    private void BindHeader(HOH_UnitDto unit)
    {

        if (unitNameText != null)
            unitNameText.text = unit != null ? unit.unit : string.Empty;

        if (subtitleText != null)
        {
            int count = unit?.persons != null ? unit.persons.Count : 0;
            subtitleText.text = count > 0
                ? "ข้าราชการดีเด่นของหน่วยงาน"
                : "ไม่พบข้อมูลบุคลากร";
        }

        if (unitLogoImage != null)
            unitLogoImage.gameObject.SetActive(true);

        if (photoLoader != null)
        photoLoader.Load(unit != null ? unit.logoUrl : string.Empty);
    }

    private void Rebuild()
    {
        ClearGenerated();

        if (pageRoot == null || dotRoot == null || pagePrefab == null || officerCardPrefab == null || dotPrefab == null)
        {
            Debug.LogError("[HOH_OfficerSelectionPanelController] Missing references.");
            return;
        }

        bool hasItems = _items.Count > 0;

        if (emptyStateRoot != null)
            emptyStateRoot.SetActive(!hasItems);

        if (emptyStateText != null && !hasItems)
            emptyStateText.text = "ไม่พบข้อมูลบุคลากร";

        if (!hasItems)
        {
            _currentPageIndex = 0;
            return;
        }

        int safeItemsPerPage = Mathf.Max(1, itemsPerPage);
        int pageCount = Mathf.CeilToInt(_items.Count / (float)safeItemsPerPage);

        if (dotRoot != null)
        dotRoot.gameObject.SetActive(pageCount > 1);
        
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
                HOH_OfficerCardUI card = Instantiate(officerCardPrefab, page);
                card.name = $"Item_{itemIndex + 1}";
                card.Bind(_items[itemIndex], HandleOfficerClicked);
            }

            if (pageCount > 1)
            {
                PaginationDotUI dot = Instantiate(dotPrefab, dotRoot);
                dot.name = $"Dot_{pageIndex + 1}";
                dot.Bind(pageIndex, SetPage);
                _dots.Add(dot);
            }
        }

        _currentPageIndex = 0;
        SetPage(_currentPageIndex);

        Canvas.ForceUpdateCanvases();

        if (pageRoot != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(pageRoot);
    }

    private void HandleOfficerClicked(HOH_PersonDto person)
    {
        OfficerSelected?.Invoke(person);

        if (officerDetailUI == null)
        {
            Debug.LogWarning("[HOH_OfficerSelectionPanelController] OfficerDetailUI is not assigned.");
            return;
        }

        if (panelRoot != null)
            panelRoot.SetActive(false);

        officerDetailUI.Show(person, _currentUnit, this);
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