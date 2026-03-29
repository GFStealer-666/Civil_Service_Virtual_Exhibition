using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PSC_OrganizationSelectionPanelController : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private GameObject previousPageRoot;

    [Header("Data")]
    [SerializeField] private PSC_Repository repository;
    [SerializeField] private PSC_ServiceDetailPanelController serviceDetailPanel;

    [Header("Header")]
    [SerializeField] private Image ministryLogoImage;
    [SerializeField] private UniversalImageLoader ministryLogoLoader;
    [SerializeField] private Sprite fallbackMinistryLogo;

    [SerializeField] private TMP_Text panelTitleText;
    [SerializeField] private string panelTitleTh;
    [SerializeField] private string panelTitleEn;

    [SerializeField] private TMP_Text welcomeTitleText;
    [SerializeField] private string welcomeTextThai = "ยินดีต้อนรับสู่";
    [SerializeField] private string welcomeTextEnglish = "Welcome to";

    [SerializeField] private TMP_Text ministryNameText;
    [SerializeField] private TMP_Text organizationCountText;
    [SerializeField] private string organizationCountFormatTh = "จำนวน {0} หน่วยงาน";
    [SerializeField] private string organizationCountFormatEnSingular = "{0} organization";
    [SerializeField] private string organizationCountFormatEnPlural = "{0} organizations";

    [Header("List")]
    [SerializeField] private Transform contentRoot;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private PSC_OrganizationItemView itemPrefab;

    [Header("Empty State")]
    [SerializeField] private GameObject emptyStateRoot;
    [SerializeField] private TMP_Text emptyStateText;
    [SerializeField] private string emptyMessageTh = "ไม่พบข้อมูล";
    [SerializeField] private string emptyMessageEn = "No data found";

    [Header("Buttons")]
    [SerializeField] private Button closeButton;
    [SerializeField] private Button backButton;

    [Header("Display")]
    [SerializeField] private bool pushUiBlockOnOpen = true;

    public PSC_ServiceMinistryDto SelectedMinistry { get; private set; }
    public PSC_ServiceOrganizationDto SelectedOrganization { get; private set; }
    public bool IsOpen => panelRoot != null ? panelRoot.activeInHierarchy : gameObject.activeInHierarchy;

    public event Action<PSC_ServiceOrganizationDto> OrganizationSelected;
    public event Action Closed;

    private readonly List<PSC_OrganizationItemView> _spawnedItems = new();
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

    public void Open(PSC_ServiceMinistryDto ministry)
    {
        if (ministry == null)
        {
            Debug.LogWarning("[PSC_OrganizationSelectionPanelController] Ministry is null.");
            return;
        }

        SelectedMinistry = ministry;
        SelectedOrganization = null;

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
        if (SelectedMinistry == null)
        {
            SetEmptyState(true, GetEmptyMessage());
            return;
        }

        ApplyStaticLocalization();
        RefreshHeader();
        RefreshList();
    }

    private void HandleItemClicked(PSC_ServiceOrganizationDto organization)
    {
        if (organization == null)
            return;

        SelectedOrganization = organization;

        if (serviceDetailPanel != null)
        {
            serviceDetailPanel.Open(organization);
            return;
        }

        OrganizationSelected?.Invoke(organization);
    }

    private void RefreshHeader()
    {
        if (SelectedMinistry == null)
            return;

        if (welcomeTitleText != null)
            welcomeTitleText.text = Pick(welcomeTextThai, welcomeTextEnglish, string.Empty);

        if (ministryNameText != null)
            ministryNameText.text = GetMinistryDisplayName(SelectedMinistry);

        if (organizationCountText != null)
            organizationCountText.text = BuildOrganizationCountText(SelectedMinistry.runtimeOrganizationCount);

        BindMinistryLogo(SelectedMinistry);
    }

    private void ApplyStaticLocalization()
    {
        if (panelTitleText != null)
            panelTitleText.text = Pick(panelTitleTh, panelTitleEn, string.Empty);
    }

    private void BindMinistryLogo(PSC_ServiceMinistryDto ministry)
    {
        string logoUrl = ministry != null ? ministry.ministryLogo : string.Empty;

        if (ministryLogoImage != null)
        {
            ministryLogoImage.sprite = fallbackMinistryLogo;
            ministryLogoImage.enabled = true;
        }

        if (ministryLogoLoader == null)
            return;

        if (string.IsNullOrWhiteSpace(logoUrl))
            return;

        ministryLogoLoader.Load(logoUrl);
    }

    private void RefreshList()
    {
        ClearItems();

        if (contentRoot == null || itemPrefab == null)
        {
            Debug.LogWarning("[PSC_OrganizationSelectionPanelController] ContentRoot or ItemPrefab is missing.");
            SetEmptyState(true, GetEmptyMessage());
            return;
        }

        if (repository == null)
        {
            SetEmptyState(true, IsEnglish() ? "Repository not found" : "ไม่พบ Repository");
            return;
        }

        if (SelectedMinistry == null)
        {
            SetEmptyState(true, GetEmptyMessage());
            return;
        }

        List<PSC_ServiceOrganizationDto> organizations =
            repository.GetOrganizationsByMinistry(SelectedMinistry.runtimeId);

        if (organizations == null || organizations.Count == 0)
        {
            SetEmptyState(true, GetEmptyMessage());
            return;
        }

        for (int i = 0; i < organizations.Count; i++)
        {
            PSC_ServiceOrganizationDto organization = organizations[i];
            if (organization == null)
                continue;

            PSC_OrganizationItemView view = Instantiate(itemPrefab, contentRoot);
            view.gameObject.SetActive(true);
            view.Bind(organization, HandleItemClicked);

            _spawnedItems.Add(view);
        }

        SetEmptyState(_spawnedItems.Count == 0, GetEmptyMessage());
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

        if (welcomeTitleText != null)
            welcomeTitleText.text = string.Empty;

        if (ministryNameText != null)
            ministryNameText.text = string.Empty;

        if (organizationCountText != null)
            organizationCountText.text = string.Empty;

        if (ministryLogoImage != null)
        {
            ministryLogoImage.sprite = fallbackMinistryLogo;
            ministryLogoImage.enabled = ministryLogoImage.sprite != null;
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

    private string GetMinistryDisplayName(PSC_ServiceMinistryDto ministry)
    {
        if (ministry == null)
            return string.Empty;

        return Pick(ministry.ministry, ministry.ministryEn, string.Empty);
    }

    private string BuildOrganizationCountText(int count)
    {
        if (!IsEnglish())
            return string.Format(Clean(organizationCountFormatTh, "จำนวน {0} หน่วยงาน"), count);

        string format = count == 1
            ? Clean(organizationCountFormatEnSingular, "{0} organization")
            : Clean(organizationCountFormatEnPlural, "{0} organizations");

        return string.Format(format, count);
    }

    private string GetEmptyMessage()
    {
        return Pick(emptyMessageTh, emptyMessageEn, "No data found");
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