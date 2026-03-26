using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class AE_ProjectDetailUI : MonoBehaviour
{
    private const int MaxImageSlots = 4;

    [Header("Config")]
    [SerializeField] private ApiConfig apiConfig;
    [SerializeField] private bool useEnglishContent;
    [SerializeField] private bool useEnglishNarrator;
    [SerializeField] private int shortDescriptionCharacterLimit = 220;

    [Header("Linked Panels")]
    [SerializeField] private AE_AdditionalProjectDetailUI additionalProjectDetailUI;
    [SerializeField] private AE_ProjectVideoPanel projectVideoUI;

    [Header("Media")]
    [SerializeField] private AE_ProjectNarrationController narrationController;
    [SerializeField] private MediaSessionCoordinator mediaCoordinator;

    [Header("Root")]
    [SerializeField] private GameObject root;

    [Header("Text")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text agencyText;
    [SerializeField] private TMP_Text descriptionText;

    [Header("Images")]
    [SerializeField] private List<Image> imageSlots = new();
    [SerializeField] private Sprite defaultImage;

    [Header("Buttons")]
    [SerializeField] private Button moreInfoButton;
    [SerializeField] private Button narratorButton;
    [SerializeField] private TMP_Text narratorButtonLabel;
    [SerializeField] private Button videoButton;

    [Header("Narrator Button Labels")]
    [SerializeField] private string narratorPlayLabel = "Play";
    [SerializeField] private string narratorCancelLabel = "Cancel";
    [SerializeField] private string narratorStopLabel = "Stop";

    private GovernmentProjectDto _currentProject;
    private string _currentAgencyName;

    private readonly List<Coroutine> _imageLoadRoutines = new();
    private readonly List<Object> _runtimeAssets = new();

    public bool HasProject => _currentProject != null;

    private void Awake()
    {
        if (moreInfoButton != null)
            moreInfoButton.onClick.AddListener(HandleMoreInfoClicked);

        if (narratorButton != null)
            narratorButton.onClick.AddListener(HandleNarratorClicked);

        if (videoButton != null)
            videoButton.onClick.AddListener(HandleVideoClicked);

        if (narrationController != null)
            narrationController.StateChanged += HandleNarrationStateChanged;
    }

    private void OnDestroy()
    {
        if (moreInfoButton != null)
            moreInfoButton.onClick.RemoveListener(HandleMoreInfoClicked);

        if (narratorButton != null)
            narratorButton.onClick.RemoveListener(HandleNarratorClicked);

        if (videoButton != null)
            videoButton.onClick.RemoveListener(HandleVideoClicked);

        if (narrationController != null)
            narrationController.StateChanged -= HandleNarrationStateChanged;

        StopAllRunningCoroutines();
        ClearRuntimeAssets();
    }

    public void Show(GovernmentProjectDto project, string agencyName)
    {
        _currentProject = project;
        _currentAgencyName = agencyName;

        StopAllRunningCoroutines();
        narrationController?.ResetSession(false);
        ClearRuntimeAssets();

        if (root != null)
            root.SetActive(true);

        BindText();
        BindImageSlots();
        RefreshButtons();
    }

    public void Hide()
    {
        StopAllRunningCoroutines();
        narrationController?.ResetSession(true);
        ClearRuntimeAssets();

        if (additionalProjectDetailUI != null)
            additionalProjectDetailUI.Hide();

        if (projectVideoUI != null)
            projectVideoUI.Hide();

        _currentProject = null;
        _currentAgencyName = string.Empty;

        ApplyEmptyState();

        if (root != null)
            root.SetActive(false);

        RefreshButtons();
    }

    public void StopNarration()
    {
        narrationController?.StopMedia();
        RefreshButtons();
    }

    private void BindText()
    {
        if (_currentProject == null)
        {
            ApplyEmptyState();
            return;
        }

        if (titleText != null)
            titleText.text = GetProjectTitle(_currentProject);

        if (agencyText != null)
            agencyText.text = _currentAgencyName ?? string.Empty;

        if (descriptionText != null)
        {
            descriptionText.text = TruncateWithEllipsis(
                GetProjectDescription(_currentProject),
                shortDescriptionCharacterLimit
            );
        }
    }

    private void ApplyEmptyState()
    {
        if (titleText != null)
            titleText.text = string.Empty;

        if (agencyText != null)
            agencyText.text = string.Empty;

        if (descriptionText != null)
            descriptionText.text = string.Empty;

        ClearImageSlots();
    }

    private void HandleMoreInfoClicked()
    {
        if (_currentProject == null)
            return;

        if (additionalProjectDetailUI == null)
        {
            Debug.LogWarning("[AE_ProjectDetailUI] AdditionalProjectDetailUI is not assigned.");
            return;
        }

        additionalProjectDetailUI.Show(_currentProject, _currentAgencyName);
    }

    private void HandleNarratorClicked()
    {
        if (_currentProject == null || narrationController == null)
            return;

        string url = BuildProjectTtsUrl(_currentProject);
        Debug.Log($"[AE_ProjectDetailUI] Narrator URL = {url}");

        narrationController.ToggleFromUrl(url);
        RefreshButtons();
    }

    private void HandleVideoClicked()
    {
        if (_currentProject == null)
            return;

        if (projectVideoUI == null)
        {
            Debug.LogWarning("[AE_ProjectDetailUI] ProjectVideoUI is not assigned.");
            return;
        }

        IMediaControllable videoMedia = projectVideoUI as IMediaControllable;

        if (videoMedia != null && mediaCoordinator != null)
            mediaCoordinator.TakeFocus(videoMedia);
        else
            narrationController?.StopMedia();

        projectVideoUI.Show(_currentProject, _currentAgencyName);
    }

    private void HandleNarrationStateChanged(MediaPlaybackState state)
    {
        RefreshButtons();
    }

    private void BindImageSlots()
    {
        ClearImageSlots();

        if (_currentProject == null || _currentProject.imageUrls == null || _currentProject.imageUrls.Count == 0)
        {
            Debug.Log("[AE_ProjectDetailUI] No images in current project.");
            return;
        }

        int count = Mathf.Min(
            MaxImageSlots,
            Mathf.Min(imageSlots.Count, _currentProject.imageUrls.Count)
        );

        for (int i = 0; i < count; i++)
        {
            Image slot = imageSlots[i];
            if (slot == null)
            {
                Debug.LogWarning($"[AE_ProjectDetailUI] Image slot {i} is null.");
                continue;
            }

            string imageUrl = _currentProject.imageUrls[i];

            slot.gameObject.SetActive(true);
            slot.sprite = defaultImage;
            slot.preserveAspect = true;

            Color color = slot.color;
            color.a = 1f;
            slot.color = color;

            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                Debug.LogWarning($"[AE_ProjectDetailUI] Image slot {i} has empty image url.");
                continue;
            }

            Coroutine routine = StartCoroutine(LoadImageIntoImage(imageUrl, slot, i));
            _imageLoadRoutines.Add(routine);
        }
    }

    private void RefreshButtons()
    {
        bool hasProject = _currentProject != null;

        if (moreInfoButton != null)
            moreInfoButton.interactable = hasProject;

        if (narratorButton != null)
            narratorButton.interactable = hasProject && narrationController != null;

        if (videoButton != null)
        {
            bool hasVideo = hasProject && !string.IsNullOrWhiteSpace(_currentProject.videoUrl);
            videoButton.interactable = hasVideo;
        }

        RefreshNarratorButtonLabel();
    }

    private void RefreshNarratorButtonLabel()
    {
        if (narratorButtonLabel == null)
            return;

        if (_currentProject == null || narrationController == null)
        {
            narratorButtonLabel.text = narratorPlayLabel;
            return;
        }

        switch (narrationController.State)
        {
            case MediaPlaybackState.Loading:
                narratorButtonLabel.text = narratorCancelLabel;
                break;

            case MediaPlaybackState.Playing:
                narratorButtonLabel.text = narratorStopLabel;
                break;

            default:
                narratorButtonLabel.text = narratorPlayLabel;
                break;
        }
    }

    private void ClearImageSlots()
    {
        for (int i = 0; i < imageSlots.Count; i++)
        {
            if (imageSlots[i] == null)
                continue;

            imageSlots[i].sprite = defaultImage;
            imageSlots[i].preserveAspect = true;
            imageSlots[i].gameObject.SetActive(false);

            Color color = imageSlots[i].color;
            color.a = 1f;
            imageSlots[i].color = color;
        }
    }

    private IEnumerator LoadImageIntoImage(string url, Image targetImage, int slotIndex)
    {
        if (targetImage == null)
        {
            Debug.LogWarning($"[AE_ProjectDetailUI] Target image is null. slot={slotIndex}");
            yield break;
        }

        using UnityWebRequest request = UnityWebRequest.Get(url);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Accept", "image/png,image/jpeg,image/*,*/*");
        ApplyAuthorizationHeader(request);

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning(
                $"[AE_ProjectDetailUI] Image download failed. slot={slotIndex} | " +
                $"responseCode={request.responseCode} | error={request.error} | url={url}"
            );
            yield break;
        }

        byte[] bytes = request.downloadHandler.data;
        if (bytes == null || bytes.Length == 0)
        {
            Debug.LogWarning($"[AE_ProjectDetailUI] Image bytes empty. slot={slotIndex} | url={url}");
            yield break;
        }

        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        bool loaded = texture.LoadImage(bytes, false);

        if (!loaded)
        {
            Debug.LogWarning(
                $"[AE_ProjectDetailUI] Texture decode failed. slot={slotIndex} | " +
                $"contentType={request.GetResponseHeader("Content-Type")} | url={url}"
            );

            Destroy(texture);
            yield break;
        }

        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f)
        );

        RegisterRuntimeAsset(texture);
        RegisterRuntimeAsset(sprite);

        targetImage.sprite = sprite;
        targetImage.preserveAspect = true;
        targetImage.gameObject.SetActive(true);
    }

    private void StopAllRunningCoroutines()
    {
        for (int i = 0; i < _imageLoadRoutines.Count; i++)
        {
            if (_imageLoadRoutines[i] != null)
                StopCoroutine(_imageLoadRoutines[i]);
        }

        _imageLoadRoutines.Clear();
    }

    private void RegisterRuntimeAsset(Object asset)
    {
        if (asset == null)
            return;

        _runtimeAssets.Add(asset);
    }

    private void ClearRuntimeAssets()
    {
        for (int i = 0; i < _runtimeAssets.Count; i++)
        {
            if (_runtimeAssets[i] != null)
                Destroy(_runtimeAssets[i]);
        }

        _runtimeAssets.Clear();
    }

    private void ApplyAuthorizationHeader(UnityWebRequest request)
    {
        if (request == null)
            return;

        string accessToken = PlayerPrefs.GetString("access_token", string.Empty);
        if (!string.IsNullOrWhiteSpace(accessToken))
            request.SetRequestHeader("Authorization", $"Bearer {accessToken}");
    }

    private string BuildProjectTtsUrl(GovernmentProjectDto project)
    {
        if (project == null)
            return string.Empty;

        string projectId = FirstNotEmpty(project.id, project.runtimeId);
        if (string.IsNullOrWhiteSpace(projectId))
            return string.Empty;

        if (ApiService.Instance != null)
        {
            return useEnglishNarrator
                ? ApiService.Instance.GetAgencyExhibitionTtsEngUrl(projectId)
                : ApiService.Instance.GetAgencyExhibitionTtsThUrl(projectId);
        }

        if (apiConfig != null)
        {
            return useEnglishNarrator
                ? apiConfig.GetAgencyExhibitionTtsEngUrl(projectId)
                : apiConfig.GetAgencyExhibitionTtsThUrl(projectId);
        }

        return string.Empty;
    }

    private string GetProjectTitle(GovernmentProjectDto project)
    {
        if (project == null)
            return string.Empty;

        return useEnglishContent
            ? FirstNotEmpty(project.nameEn, project.name)
            : FirstNotEmpty(project.name, project.nameEn);
    }

    private string GetProjectDescription(GovernmentProjectDto project)
    {
        if (project == null)
            return string.Empty;

        return useEnglishContent
            ? FirstNotEmpty(project.descriptionEn, project.description)
            : FirstNotEmpty(project.description, project.descriptionEn);
    }

    private string TruncateWithEllipsis(string value, int maxCharacters)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        if (maxCharacters <= 0 || value.Length <= maxCharacters)
            return value;

        return value.Substring(0, maxCharacters).TrimEnd() + "...";
    }

    private string FirstNotEmpty(params string[] values)
    {
        if (values == null)
            return string.Empty;

        for (int i = 0; i < values.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(values[i]))
                return values[i];
        }

        return string.Empty;
    }
}