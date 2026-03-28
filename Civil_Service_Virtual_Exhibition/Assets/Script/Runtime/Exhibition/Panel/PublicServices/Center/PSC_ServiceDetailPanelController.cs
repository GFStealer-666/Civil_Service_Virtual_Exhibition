using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
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
    private bool _uiBound;

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
        if (SelectedOrganization == null)
        {
            SetEmptyState(true, emptyMessage);
            return;
        }

        SelectedMinistry = ResolveParentMinistry(SelectedOrganization);

        RefreshHeader();
        RefreshList();
    }

    private void RefreshHeader()
    {
        if (organizationNameText != null)
            organizationNameText.text = GetOrganizationDisplayName(SelectedOrganization);

        string ministryLogoUrl = SelectedMinistry != null ? SelectedMinistry.ministryLogo : string.Empty;
        string organizationLogoUrl = SelectedOrganization != null ? SelectedOrganization.logoUrl : string.Empty;

        BindHeaderImage(
            ministryImage,
            ministryImageLoader,
            ministryLogoUrl,
            fallbackMinistrySprite
        );

        BindHeaderImage(
            organizationImage,
            organizationImageLoader,
            organizationLogoUrl,
            fallbackOrganizationSprite
        );
    }

    private void BindHeaderImage(
        Image targetImage,
        UniversalImageLoader loader,
        string imageUrl,
        Sprite fallbackSprite)
    {
        if (targetImage != null)
        {
            targetImage.sprite = fallbackSprite;
            targetImage.enabled = true;
        }

        if (loader == null)
            return;

        if (string.IsNullOrWhiteSpace(imageUrl))
            return;

        loader.Load(imageUrl);
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
        if (organizationNameText != null)
            organizationNameText.text = string.Empty;

        if (ministryImage != null)
        {
            ministryImage.sprite = fallbackMinistrySprite;
            ministryImage.enabled = ministryImage.sprite != null;
        }

        if (organizationImage != null)
        {
            organizationImage.sprite = fallbackOrganizationSprite;
            organizationImage.enabled = organizationImage.sprite != null;
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