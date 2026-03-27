using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.UI;

public class AE_ProjectSelectionUI : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("Header")]
    [SerializeField] private TMP_Text agencyTitleText;
    [SerializeField] private TMP_Text subtitleText;
    [SerializeField] private Image ministryLogoImage;
    [SerializeField] private Image agencyLogoImage;
    [SerializeField] private Sprite defaultMinistryLogo;
    [SerializeField] private Sprite defaultAgencyLogo;

    [Header("Build")]
    [SerializeField] private RectTransform pageRoot;
    [SerializeField] private RectTransform dotRoot;
    [SerializeField] private RectTransform pagePrefab;
    [SerializeField] private AE_ProjectCardUI cardPrefab;
    [SerializeField] private PaginationDotUI dotPrefab;
    [SerializeField] private int itemsPerPage = 3;

    [Header("Input")]
    [SerializeField] private bool allowSwipe = true;
    [SerializeField] private float swipeThreshold = 80f;

    private static readonly Dictionary<string, Sprite> SpriteCache = new Dictionary<string, Sprite>();

    private readonly List<RectTransform> _pages = new List<RectTransform>();
    private readonly List<PaginationDotUI> _dots = new List<PaginationDotUI>();
    private readonly List<ExhibitionProjectData> _items = new List<ExhibitionProjectData>();

    private Vector2 _pointerDownPosition;
    private int _currentPageIndex;

    private Coroutine _ministryLogoLoadRoutine;
    private Coroutine _agencyLogoLoadRoutine;

    public event Action<ExhibitionProjectData> ItemSelected;

    public void BindSelection(ExhibitionAgencyData agencyData, IReadOnlyList<ExhibitionProjectData> items, string ministryLogoUrl)
    {
        BindHeader(agencyData, ministryLogoUrl);
        SetItems(items);
    }

    public void BindHeader(ExhibitionAgencyData agencyData, string ministryLogoUrl)
    {
        if (agencyTitleText != null)
            agencyTitleText.text = agencyData != null ? agencyData.Title : string.Empty;

        if (subtitleText != null)
            subtitleText.text = "โครงการภายใต้การดูแลของหน่วยงาน";

        SetMinistryLogo(ministryLogoUrl);
        SetAgencyLogo(agencyData != null ? agencyData.LogoUrl : string.Empty);
    }

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

    private void OnDestroy()
    {
        StopRoutine(ref _ministryLogoLoadRoutine);
        StopRoutine(ref _agencyLogoLoadRoutine);
        ClearGenerated();
    }

    private void Rebuild()
    {
        ClearGenerated();

        if (pageRoot == null || dotRoot == null || pagePrefab == null || cardPrefab == null || dotPrefab == null)
        {
            Debug.LogError("[AE_ProjectSelectionUI] Missing references.");
            return;
        }

        if (_items.Count == 0)
        {
            _currentPageIndex = 0;

            if (dotRoot != null)
                dotRoot.gameObject.SetActive(false);

            return;
        }

        int safeItemsPerPage = Mathf.Max(1, itemsPerPage);
        int pageCount = Mathf.CeilToInt(_items.Count / (float)safeItemsPerPage);
        bool shouldShowDots = pageCount > 1;

        if (dotRoot != null)
            dotRoot.gameObject.SetActive(shouldShowDots);

        for (int pageIndex = 0; pageIndex < pageCount; pageIndex++)
        {
            RectTransform page = Instantiate(pagePrefab, pageRoot);
            page.name = "Page_" + (pageIndex + 1);
            page.gameObject.SetActive(false);
            _pages.Add(page);

            int start = pageIndex * safeItemsPerPage;
            int end = Mathf.Min(start + safeItemsPerPage, _items.Count);

            for (int itemIndex = start; itemIndex < end; itemIndex++)
            {
                AE_ProjectCardUI card = Instantiate(cardPrefab, page);
                card.name = "Item_" + (itemIndex + 1);
                card.Bind(_items[itemIndex], HandleItemClicked);
            }

            if (shouldShowDots)
            {
                PaginationDotUI dot = Instantiate(dotPrefab, dotRoot);
                dot.name = "Dot_" + (pageIndex + 1);
                dot.Bind(pageIndex, SetPage);
                _dots.Add(dot);
            }
        }

        SetPage(0);
    }

    private void HandleItemClicked(ExhibitionProjectData data)
    {
        ItemSelected?.Invoke(data);

        if (data != null)
            Debug.Log("[AE_ProjectSelectionUI] Clicked project: " + data.Title);
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

    private void SetMinistryLogo(string url)
    {
        ResetImage(ministryLogoImage, defaultMinistryLogo);
        StopRoutine(ref _ministryLogoLoadRoutine);

        if (ministryLogoImage == null || string.IsNullOrWhiteSpace(url))
            return;

        if (TryApplyCachedSprite(url, ministryLogoImage))
            return;

        _ministryLogoLoadRoutine = StartCoroutine(
            LoadSpriteIntoImage(url, ministryLogoImage, HandleMinistryLogoLoadFinished)
        );
    }

    private void SetAgencyLogo(string url)
    {
        ResetImage(agencyLogoImage, defaultAgencyLogo);
        StopRoutine(ref _agencyLogoLoadRoutine);

        if (agencyLogoImage == null || string.IsNullOrWhiteSpace(url))
            return;

        if (TryApplyCachedSprite(url, agencyLogoImage))
            return;

        _agencyLogoLoadRoutine = StartCoroutine(
            LoadSpriteIntoImage(url, agencyLogoImage, HandleAgencyLogoLoadFinished)
        );
    }

    private void HandleMinistryLogoLoadFinished()
    {
        _ministryLogoLoadRoutine = null;
    }

    private void HandleAgencyLogoLoadFinished()
    {
        _agencyLogoLoadRoutine = null;
    }

    private static void ResetImage(Image image, Sprite fallback)
    {
        if (image == null)
            return;

        image.sprite = fallback;
        image.preserveAspect = true;
    }

    private static bool TryApplyCachedSprite(string url, Image target)
    {
        if (target == null || string.IsNullOrWhiteSpace(url))
            return false;

        Sprite cached;
        if (!SpriteCache.TryGetValue(url, out cached) || cached == null)
            return false;

        target.sprite = cached;
        target.preserveAspect = true;
        return true;
    }

    private IEnumerator LoadSpriteIntoImage(string url, Image targetImage, Action onFinished)
    {
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Accept", "image/png,image/jpeg,image/*,*/*");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning("[AE_ProjectSelectionUI] Failed to load image: " + url + "\n" + request.error);
                if (onFinished != null)
                    onFinished();

                yield break;
            }

            byte[] bytes = request.downloadHandler.data;

            if (bytes == null || bytes.Length == 0)
            {
                Debug.LogWarning("[AE_ProjectSelectionUI] Empty image data: " + url);
                if (onFinished != null)
                    onFinished();

                yield break;
            }

            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            bool loaded = texture.LoadImage(bytes, false);

            if (!loaded)
            {
                Destroy(texture);
                Debug.LogWarning("[AE_ProjectSelectionUI] Texture load failed: " + url);

                if (onFinished != null)
                    onFinished();

                yield break;
            }

            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f)
            );

            SpriteCache[url] = sprite;

            if (targetImage != null)
            {
                targetImage.sprite = sprite;
                targetImage.preserveAspect = true;
            }

            if (onFinished != null)
                onFinished();
        }
    }

    private void StopRoutine(ref Coroutine routine)
    {
        if (routine == null)
            return;

        StopCoroutine(routine);
        routine = null;
    }
}