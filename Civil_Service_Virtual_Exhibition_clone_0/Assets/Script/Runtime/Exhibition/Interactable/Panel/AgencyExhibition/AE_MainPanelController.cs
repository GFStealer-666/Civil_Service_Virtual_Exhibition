using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class AE_MainPanelController : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button closeButton;

    [Header("Header")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text subtitleText;
    [SerializeField] private Image ministryLogoImage;
    [SerializeField] private Sprite defaultMinistryLogo;

    [Header("Agency List")]
    [SerializeField] private AE_MinistrySelectionUI ministryPanel;

    [Header("Fallback Card Art")]
    [SerializeField] private Sprite defaultAgencyBackground;
    [SerializeField] private Sprite defaultAgencyLogo;
    [SerializeField] private Sprite defaultProjectBackground;

    [Header("Pages")]
    [SerializeField] private GameObject mainPageRoot;
    [SerializeField] private GameObject projectSelectionPageRoot;
    [SerializeField] private GameObject projectDetailPageRoot;

    [Header("Project Selection")]
    [SerializeField] private TMP_Text projectSelectionAgencyTitleText;
    [SerializeField] private TMP_Text projectSelectionSubtitleText;
    [SerializeField] private Button projectSelectionBackButton;
    [SerializeField] private AE_ProjectSelectionUI projectSelectionPanel;

    [Header("Project Detail")]
    [SerializeField] private Button projectDetailBackButton;
    [SerializeField] private AE_ProjectDetailUI projectDetailPanel;

    private Coroutine _ministryLogoLoadRoutine;
    private string _currentMinistryKey;
    private ExhibitionAgencyData _selectedAgency;

    public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

    private void Awake()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        if (ministryPanel != null)
            ministryPanel.ItemSelected += HandleAgencySelected;

        if (projectSelectionBackButton != null)
            projectSelectionBackButton.onClick.AddListener(ShowMainPage);

        if (projectSelectionPanel != null)
            projectSelectionPanel.ItemSelected += HandleProjectSelected;

        if (projectDetailBackButton != null)
            projectDetailBackButton.onClick.AddListener(ShowProjectSelectionPage);

        if (panelRoot != null)
            panelRoot.SetActive(false);

        ShowMainPage();
    }

    private void OnDestroy()
    {
        if (closeButton != null)
            closeButton.onClick.RemoveListener(Close);

        if (ministryPanel != null)
            ministryPanel.ItemSelected -= HandleAgencySelected;

        if (projectSelectionBackButton != null)
            projectSelectionBackButton.onClick.RemoveListener(ShowMainPage);

        if (projectSelectionPanel != null)
            projectSelectionPanel.ItemSelected -= HandleProjectSelected;

        if (projectDetailBackButton != null)
            projectDetailBackButton.onClick.RemoveListener(ShowProjectSelectionPage);
    }

    public void Open(string ministryKey)
    {
        _currentMinistryKey = ministryKey;

        if (panelRoot != null)
            panelRoot.SetActive(true);

        PlayerInput.GameplayInputBlocked = true;

        ShowMainPage();
        BuildPanel();
    }

    public void Close()
    {
        if (projectDetailPanel != null)
            projectDetailPanel.Hide();

        if (panelRoot != null)
            panelRoot.SetActive(false);

        PlayerInput.GameplayInputBlocked = false;
    }

    private void BuildPanel()
    {
        AE_CatalogRepository store = AE_CatalogRepository.Instance;

        if (store == null || !store.HasData)
        {
            Debug.LogWarning("[MinistryExhibitionPanelController] GovernmentCatalogStore has no data yet.");
            ApplyHeaderFallback(_currentMinistryKey);
            SetAgencyItems(new List<ExhibitionAgencyData>());
            return;
        }

        GovernmentMinistryDto ministry = store.FindMinistry(_currentMinistryKey);

        if (ministry == null)
        {
            Debug.LogWarning($"[MinistryExhibitionPanelController] Ministry not found: {_currentMinistryKey}");
            ApplyHeaderFallback(_currentMinistryKey);
            SetAgencyItems(new List<ExhibitionAgencyData>());
            return;
        }

        if (titleText != null)
            titleText.text = FirstNotEmpty(ministry.ministry, ministry.ministryEn);

        if (subtitleText != null)
            subtitleText.text = "เลือกหน่วยงานที่ต้องการเยี่ยมชม";

        SetMinistryLogo(ministry.ministryLogo);

        List<GovernmentAgencyDto> agencies = store.GetAgenciesByMinistry(ministry.runtimeId);
        List<ExhibitionAgencyData> items = new List<ExhibitionAgencyData>(agencies.Count);

        for (int i = 0; i < agencies.Count; i++)
        {
            GovernmentAgencyDto agency = agencies[i];
            if (agency == null)
                continue;

            ExhibitionAgencyData item = new ExhibitionAgencyData
            {
                AgencyRuntimeId = agency.runtimeId,
                Title = FirstNotEmpty(agency.organizationName, agency.organizationNameEn),
                BackgroundUrl = agency.coverUrl,
                LogoUrl = agency.logoUrl,
                FallbackBackgroundSprite = defaultAgencyBackground,
                FallbackLogoSprite = defaultAgencyLogo
            };

            items.Add(item);
        }

        SetAgencyItems(items);
    }

    private void SetAgencyItems(List<ExhibitionAgencyData> items)
    {
        if (ministryPanel == null)
        {
            Debug.LogError("[MinistryExhibitionPanelController] Paginated panel is missing.");
            return;
        }

        ministryPanel.SetItems(items);
    }

    private void ApplyHeaderFallback(string ministryKey)
    {
        if (titleText != null)
            titleText.text = ministryKey;

        if (subtitleText != null)
            subtitleText.text = "เลือกหน่วยงานที่ต้องการเยี่ยมชม";

        SetMinistryLogo(null);
    }

    private void HandleAgencySelected(ExhibitionAgencyData data)
    {
        if (data == null)
            return;

        Debug.Log($"[MinistryExhibitionPanelController] Agency selected: {data.AgencyRuntimeId}");
        BuildProjectSelection(data);
    }

    private void BuildProjectSelection(ExhibitionAgencyData agencyData)
    {
        _selectedAgency = agencyData;

        if (_selectedAgency == null)
        {
            Debug.LogWarning("[MinistryExhibitionPanelController] Selected agency is null.");
            return;
        }

        AE_CatalogRepository store = AE_CatalogRepository.Instance;
        if (store == null || !store.HasData)
        {
            Debug.LogWarning("[MinistryExhibitionPanelController] Store has no data.");
            return;
        }

        if (projectSelectionAgencyTitleText != null)
            projectSelectionAgencyTitleText.text = _selectedAgency.Title;

        if (projectSelectionSubtitleText != null)
            projectSelectionSubtitleText.text = "โครงการภายใต้การดูแลของหน่วยงาน";

        List<GovernmentProjectDto> projects = store.GetProjectsByAgency(_selectedAgency.AgencyRuntimeId);
        List<ExhibitionProjectData> projectItems = new List<ExhibitionProjectData>(projects.Count);

        for (int i = 0; i < projects.Count; i++)
        {
            GovernmentProjectDto project = projects[i];
            if (project == null)
                continue;

            string backgroundUrl = string.Empty;

            if (project.imageUrls != null && project.imageUrls.Count > 0)
                backgroundUrl = project.imageUrls[0];

            ExhibitionProjectData item = new ExhibitionProjectData
            {
                ProjectRuntimeId = project.runtimeId,
                AgencyRuntimeId = _selectedAgency.AgencyRuntimeId,
                Title = FirstNotEmpty(project.name, project.nameEn),
                BackgroundUrl = backgroundUrl,
                FallbackBackgroundSprite = defaultProjectBackground
            };

            projectItems.Add(item);
        }

        if (projectSelectionPanel != null)
            projectSelectionPanel.SetItems(projectItems);

        ShowProjectSelectionPage();
    }

    private void HandleProjectSelected(ExhibitionProjectData data)
    {
        if (data == null)
            return;

        Debug.Log($"[MinistryExhibitionPanelController] Project selected: {data.ProjectRuntimeId}");

        GovernmentProjectDto selectedProject = FindProjectDto(data);
        if (selectedProject == null)
        {
            Debug.LogWarning($"[MinistryExhibitionPanelController] Project DTO not found: {data.ProjectRuntimeId}");
            return;
        }

        if (projectDetailPanel != null)
            projectDetailPanel.Show(selectedProject, _selectedAgency != null ? _selectedAgency.Title : string.Empty);

        ShowProjectDetailPage();
    }

    private GovernmentProjectDto FindProjectDto(ExhibitionProjectData data)
    {
        if (data == null)
            return null;

        AE_CatalogRepository store = AE_CatalogRepository.Instance;
        if (store == null || !store.HasData)
            return null;

        List<GovernmentProjectDto> projects = store.GetProjectsByAgency(data.AgencyRuntimeId);

        for (int i = 0; i < projects.Count; i++)
        {
            GovernmentProjectDto project = projects[i];
            if (project == null)
                continue;

            if (string.Equals(project.runtimeId, data.ProjectRuntimeId, System.StringComparison.OrdinalIgnoreCase))
                return project;
        }

        return null;
    }

    private string FirstNotEmpty(params string[] values)
    {
        for (int i = 0; i < values.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(values[i]))
                return values[i];
        }

        return string.Empty;
    }

    private void SetMinistryLogo(string logoUrl)
    {
        if (ministryLogoImage == null)
            return;

        ministryLogoImage.sprite = defaultMinistryLogo;
        ministryLogoImage.preserveAspect = true;

        if (_ministryLogoLoadRoutine != null)
        {
            StopCoroutine(_ministryLogoLoadRoutine);
            _ministryLogoLoadRoutine = null;
        }

        if (string.IsNullOrWhiteSpace(logoUrl))
        {
            Debug.Log("[MinistryExhibitionPanelController] Ministry logo url is empty. Using default sprite.");
            return;
        }

        _ministryLogoLoadRoutine = StartCoroutine(LoadSpriteIntoImage(logoUrl, ministryLogoImage));
    }

    private IEnumerator LoadSpriteIntoImage(string url, Image targetImage)
    {
        using UnityWebRequest request = UnityWebRequest.Get(url);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Accept", "image/png,image/jpeg,image/*,*/*");

        yield return request.SendWebRequest();

        Debug.Log(
            $"[MinistryExhibitionPanelController] Image response => " +
            $"result={request.result} | " +
            $"responseCode={request.responseCode} | " +
            $"content-type={request.GetResponseHeader("Content-Type")} | " +
            $"content-length={request.GetResponseHeader("Content-Length")} | " +
            $"url={url}"
        );

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning(
                $"[MinistryExhibitionPanelController] Failed to download image bytes from url: {url}\n" +
                $"Error: {request.error}"
            );
            _ministryLogoLoadRoutine = null;
            yield break;
        }

        byte[] bytes = request.downloadHandler.data;

        if (bytes == null || bytes.Length == 0)
        {
            Debug.LogWarning($"[MinistryExhibitionPanelController] Downloaded image bytes are empty. Url: {url}");
            _ministryLogoLoadRoutine = null;
            yield break;
        }

        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        bool loaded = texture.LoadImage(bytes, false);

        if (!loaded)
        {
            string contentType = request.GetResponseHeader("Content-Type");
            Debug.LogWarning(
                $"[MinistryExhibitionPanelController] Texture2D.LoadImage failed. " +
                $"content-type={contentType} | bytes={bytes.Length} | url={url}"
            );

            Destroy(texture);
            _ministryLogoLoadRoutine = null;
            yield break;
        }

        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f)
        );

        if (targetImage != null)
        {
            targetImage.sprite = sprite;
            targetImage.preserveAspect = true;
        }

        _ministryLogoLoadRoutine = null;
    }

    private void ShowMainPage()
    {
        if (mainPageRoot != null)
            mainPageRoot.SetActive(true);

        if (projectSelectionPageRoot != null)
            projectSelectionPageRoot.SetActive(false);

        if (projectDetailPageRoot != null)
            projectDetailPageRoot.SetActive(false);

        if (projectDetailPanel != null)
            projectDetailPanel.Hide();
    }

    private void ShowProjectSelectionPage()
    {
        if (mainPageRoot != null)
            mainPageRoot.SetActive(false);

        if (projectSelectionPageRoot != null)
            projectSelectionPageRoot.SetActive(true);

        if (projectDetailPageRoot != null)
            projectDetailPageRoot.SetActive(false);

        if (projectDetailPanel != null)
            projectDetailPanel.StopNarration();
    }

    private void ShowProjectDetailPage()
    {
        if (mainPageRoot != null)
            mainPageRoot.SetActive(false);

        if (projectSelectionPageRoot != null)
            projectSelectionPageRoot.SetActive(false);

        if (projectDetailPageRoot != null)
            projectDetailPageRoot.SetActive(true);
    }
}