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

    [SerializeField] private TMP_Text panelTitleText;
    [SerializeField] private string panelTitleTh;
    [SerializeField] private string panelTitleEn;

    [SerializeField] private TMP_Text organizationNameText;

    [Header("List")]
    [SerializeField] private Transform contentRoot;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private PSC_ServiceDetailItemView itemPrefab;

    [Header("State")]
    [SerializeField] private GameObject emptyStateRoot;
    [SerializeField] private TMP_Text emptyStateText;
    [SerializeField] private string emptyMessageTh = "ไม่พบข้อมูลงานบริการ";
    [SerializeField] private string emptyMessageEn = "No service information found";

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
    private string _lastLocaleCode = string.Empty;

    private void Awake()
    {
        ResolveReferences();
        BindUi();
    }

    private void OnEnable()
    {
        _lastLocaleCode = GetLocaleCode();
        ApplyStaticLocalization();
    }

    private void Update()
    {
        string localeCode = GetLocaleCode();
        if (string.Equals(_lastLocaleCode, localeCode, StringComparison.OrdinalIgnoreCase))
            return;

        _lastLocaleCode = localeCode;

        ApplyStaticLocalization();

        if (IsOpen)
            RefreshCurrent();
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

        ApplyStaticLocalization();
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
            ApplyStaticLocalization();
            SetEmptyState(true, GetEmptyMessage());
            return;
        }

        SelectedMinistry = ResolveParentMinistry(SelectedOrganization);

        ApplyStaticLocalization();
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

    private void ApplyStaticLocalization()
    {
        if (panelTitleText != null)
            panelTitleText.text = GetPanelTitle();
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
            SetEmptyState(true, IsEnglish() ? "Repository not found" : "ไม่พบ Repository");
            return;
        }

        if (contentRoot == null || itemPrefab == null)
        {
            SetEmptyState(true, GetEmptyMessage());
            return;
        }

        if (SelectedOrganization == null)
        {
            SetEmptyState(true, GetEmptyMessage());
            return;
        }

        List<PSC_ServiceItemDto> services = repository.GetServicesByOrganization(SelectedOrganization.runtimeId);

        if (services == null || services.Count == 0)
        {
            SetEmptyState(true, GetEmptyMessage());
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

        SetEmptyState(_spawnedItems.Count == 0, GetEmptyMessage());
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

        return Pick(organization.name, organization.nameEn, string.Empty);
    }

    private string GetPanelTitle()
    {
        string localizedManualTitle = Pick(panelTitleTh, panelTitleEn, string.Empty);
        if (!string.IsNullOrWhiteSpace(localizedManualTitle))
            return localizedManualTitle;

        return GetOrganizationDisplayName(SelectedOrganization);
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
        if (panelTitleText != null)
            panelTitleText.text = string.Empty;

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

    private string GetEmptyMessage()
    {
        return IsEnglish()
            ? Clean(emptyMessageEn, "No service information found")
            : Clean(emptyMessageTh, "ไม่พบข้อมูลงานบริการ");
    }

    private static string Pick(string thai, string english, string fallback)
    {
        bool useEnglish = IsEnglish();

        string primary = useEnglish ? english : thai;
        if (!string.IsNullOrWhiteSpace(primary))
            return primary.Trim();

        string secondary = useEnglish ? thai : english;
        if (!string.IsNullOrWhiteSpace(secondary))
            return secondary.Trim();

        return fallback;
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