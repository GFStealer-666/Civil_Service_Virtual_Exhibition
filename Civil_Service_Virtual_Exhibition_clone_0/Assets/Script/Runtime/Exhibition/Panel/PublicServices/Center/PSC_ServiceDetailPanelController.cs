using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class PSC_ServiceDetailPanelController : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private GameObject previousPageRoot;

    [Header("Data")]
    [SerializeField] private PSC_Repository repository;

    [Header("Header")]
    [SerializeField] private Image ministryImage;
    [SerializeField] private UniversalImageLoader ministryImageLoader;
    [SerializeField] private Sprite fallbackMinistrySprite;

    [SerializeField] private Image organizationImage;
    [SerializeField] private UniversalImageLoader organizationImageLoader;
    [SerializeField] private Sprite fallbackOrganizationSprite;

    [SerializeField] private TMP_Text organizationNameText;

    [Header("List")]
    [SerializeField] private Transform contentRoot;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private PSC_ServiceDetailItemView itemPrefab;

    [Header("State")]
    [SerializeField] private GameObject emptyStateRoot;
    [SerializeField] private TMP_Text emptyStateText;
    [SerializeField] private string emptyMessage = "ไม่พบข้อมูลงานบริการ";

    [Header("Buttons")]
    [SerializeField] private Button closeButton;
    [SerializeField] private Button backButton;

    [Header("Display")]
    [SerializeField] private bool pushUiBlockOnOpen = true;

    public PSC_ServiceOrganizationDto SelectedOrganization { get; private set; }
    public PSC_ServiceMinistryDto SelectedMinistry { get; private set; }

    public bool IsOpen => panelRoot != null ? panelRoot.activeInHierarchy : gameObject.activeInHierarchy;

    public event Action Closed;

    private readonly List<PSC_ServiceDetailItemView> _spawnedItems = new();

    private Coroutine _ministryImageRoutine;
    private Coroutine _organizationImageRoutine;
    private bool _uiBound;

    private static readonly Dictionary<string, Sprite> SpriteCache = new Dictionary<string, Sprite>();

    private void Awake()
    {
        ResolveReferences();
        BindUi();
    }

    public void Open(PSC_ServiceOrganizationDto organization)
    {
        if (organization == null)
        {
            Debug.LogWarning("[PSC_ServiceDetailPanelController] Organization is null.");
            return;
        }

        SelectedOrganization = organization;
        SelectedMinistry = ResolveParentMinistry(organization);

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
        StopImageLoading();

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
        if (SelectedOrganization == null)
        {
            SetEmptyState(true, emptyMessage);
            return;
        }

        SelectedMinistry = ResolveParentMinistry(SelectedOrganization);

        RefreshHeader();
        RefreshList();
    }

    private void OnDestroy()
    {
        StopImageLoading();
    }

    private void RefreshHeader()
    {
        if (organizationNameText != null)
            organizationNameText.text = GetOrganizationDisplayName(SelectedOrganization);

        string ministryImageUrl = SelectedMinistry != null ? SelectedMinistry.ministryImage : string.Empty;
        string organizationImageUrl = SelectedOrganization != null ? SelectedOrganization.organizationImage : string.Empty;

        LoadImage(
            ministryImage,
            ministryImageLoader,
            ministryImageUrl,
            fallbackMinistrySprite,
            ref _ministryImageRoutine
        );

        LoadImage(
            organizationImage,
            organizationImageLoader,
            organizationImageUrl,
            fallbackOrganizationSprite,
            ref _organizationImageRoutine
        );
    }

    private void RefreshList()
    {
        ClearItems();

        if (repository == null)
        {
            SetEmptyState(true, "Repository not found");
            return;
        }

        if (contentRoot == null || itemPrefab == null)
        {
            SetEmptyState(true, emptyMessage);
            return;
        }

        if (SelectedOrganization == null)
        {
            SetEmptyState(true, emptyMessage);
            return;
        }

        List<PSC_ServiceItemDto> services = repository.GetServicesByOrganization(SelectedOrganization.runtimeId);

        if (services == null || services.Count == 0)
        {
            SetEmptyState(true, emptyMessage);
            return;
        }

        for (int i = 0; i < services.Count; i++)
        {
            PSC_ServiceItemDto service = services[i];
            if (service == null)
                continue;

            PSC_ServiceDetailItemView view = Instantiate(itemPrefab, contentRoot);
            view.gameObject.SetActive(true);
            view.Bind(service);

            _spawnedItems.Add(view);
        }

        SetEmptyState(_spawnedItems.Count == 0, emptyMessage);
    }

    private PSC_ServiceMinistryDto ResolveParentMinistry(PSC_ServiceOrganizationDto organization)
    {
        if (organization == null || repository == null)
            return null;

        if (string.IsNullOrWhiteSpace(organization.parentMinistryId))
            return null;

        return repository.GetMinistryById(organization.parentMinistryId);
    }

    private string GetOrganizationDisplayName(PSC_ServiceOrganizationDto organization)
    {
        if (organization == null)
            return string.Empty;

        if (!string.IsNullOrWhiteSpace(organization.name))
            return organization.name;

        return organization.nameEn;
    }

    private void LoadImage(
        Image targetImage,
        UniversalImageLoader loader,
        string url,
        Sprite fallback,
        ref Coroutine routine)
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        if (loader != null)
        {
            if (targetImage != null)
            {
                targetImage.sprite = fallback;
                targetImage.enabled = targetImage.sprite != null;
            }

            loader.Load(string.IsNullOrWhiteSpace(url) ? string.Empty : url);
            return;
        }

        if (targetImage == null)
            return;

        if (string.IsNullOrWhiteSpace(url))
        {
            targetImage.sprite = fallback;
            targetImage.enabled = targetImage.sprite != null;
            return;
        }

        if (SpriteCache.TryGetValue(url, out Sprite cached))
        {
            targetImage.sprite = cached != null ? cached : fallback;
            targetImage.enabled = targetImage.sprite != null;
            return;
        }

        targetImage.sprite = fallback;
        targetImage.enabled = targetImage.sprite != null;
        routine = StartCoroutine(DownloadSpriteRoutine(url, targetImage, fallback));
    }

    private IEnumerator DownloadSpriteRoutine(string url, Image targetImage, Sprite fallback)
    {
        using UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
        yield return request.SendWebRequest();

        if (targetImage == null)
            yield break;

        if (request.result != UnityWebRequest.Result.Success)
        {
            targetImage.sprite = fallback;
            targetImage.enabled = targetImage.sprite != null;
            yield break;
        }

        Texture2D texture = DownloadHandlerTexture.GetContent(request);
        if (texture == null)
        {
            targetImage.sprite = fallback;
            targetImage.enabled = targetImage.sprite != null;
            yield break;
        }

        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f)
        );

        if (!SpriteCache.ContainsKey(url))
            SpriteCache.Add(url, sprite);

        targetImage.sprite = sprite;
        targetImage.enabled = true;
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

    private void StopImageLoading()
    {
        if (_ministryImageRoutine != null)
        {
            StopCoroutine(_ministryImageRoutine);
            _ministryImageRoutine = null;
        }

        if (_organizationImageRoutine != null)
        {
            StopCoroutine(_organizationImageRoutine);
            _organizationImageRoutine = null;
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