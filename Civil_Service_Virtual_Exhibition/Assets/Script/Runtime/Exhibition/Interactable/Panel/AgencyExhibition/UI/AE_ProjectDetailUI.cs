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
    [SerializeField] private Button videoButton;



    [Header("Narrator Messages")]
    [SerializeField] private string narratorIdleMessage = "";
    [SerializeField] private string narratorLoadingMessage = "Loading narrator audio...";
    [SerializeField] private string narratorPlayingMessage = "Narrator is playing.";
    [SerializeField] private string narratorFailedMessage = "Failed to load narrator audio.";
    [SerializeField] private string narratorCompletedMessage = "Narrator playback finished.";
    [SerializeField] private string narratorStoppedMessage = "Narrator playback stopped.";

    [Header("Audio")]
    [SerializeField] private ExhibitionAudioSource narratorAudioSource;
    [Header("Overlay")]
    [SerializeField] private StatusOverlay overlay;
    private GovernmentProjectDto _currentProject;
    private string _currentAgencyName;

    private Coroutine _narrationDownloadRoutine;
    private Coroutine _narrationMonitorRoutine;

    private readonly List<Coroutine> _imageLoadRoutines = new();
    private readonly List<Object> _runtimeAssets = new();

    private MediaState _narratorState = MediaState.Idle;

    public bool HasProject => _currentProject != null;

    private void Awake()
    {
        if (moreInfoButton != null)
            moreInfoButton.onClick.AddListener(HandleMoreInfoClicked);

        if (narratorButton != null)
            narratorButton.onClick.AddListener(HandleNarratorClicked);

        if (videoButton != null)
            videoButton.onClick.AddListener(HandleVideoClicked);
    }

    private void OnDestroy()
    {
        if (moreInfoButton != null)
            moreInfoButton.onClick.RemoveListener(HandleMoreInfoClicked);

        if (narratorButton != null)
            narratorButton.onClick.RemoveListener(HandleNarratorClicked);

        if (videoButton != null)
            videoButton.onClick.RemoveListener(HandleVideoClicked);

        StopAllRunningCoroutines();
        StopNarrationInternal(false);
        ClearRuntimeAssets();
    }

    public void Show(GovernmentProjectDto project, string agencyName)
    {
        _currentProject = project;
        _currentAgencyName = agencyName;

        StopAllRunningCoroutines();
        StopNarrationInternal(false);
        ClearRuntimeAssets();

        if (root != null)
            root.SetActive(true);

        BindText();
        BindImageSlots();
        SetNarratorState(MediaState.Idle);
        RefreshButtons();
    }

    public void Hide()
    {
        StopAllRunningCoroutines();
        StopNarrationInternal(true);
        ClearRuntimeAssets();

        if (additionalProjectDetailUI != null)
            additionalProjectDetailUI.Hide();

        if (projectVideoUI != null)
            projectVideoUI.Hide();

        _currentProject = null;
        _currentAgencyName = string.Empty;

        ApplyEmptyState();
        SetNarratorState(MediaState.Idle);

        if (root != null)
            root.SetActive(false);
    }

    public void StopNarration()
    {
        StopNarrationInternal(true);
        SetNarratorState(MediaState.Stopped);
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
        if (_currentProject == null)
            return;

        if (_narratorState == MediaState.Downloading)
            return;

        if (_narratorState == MediaState.Playing)
        {
            StopNarration();
            return;
        }

        string url = BuildProjectTtsUrl(_currentProject);
        Debug.Log($"[AE_ProjectDetailUI] Narrator URL = {url}");

        if (string.IsNullOrWhiteSpace(url))
        {
            SetNarratorState(MediaState.Failed, "Narrator URL is empty.");
            overlay?.ShowFailed("ไม่สามารถโหลดเสียงบรรยาย", "ไม่พบลิงก์เสียงบรรยาย");
            return;
        }

        if (_narrationDownloadRoutine != null)
        {
            StopCoroutine(_narrationDownloadRoutine);
            _narrationDownloadRoutine = null;
        }

        _narrationDownloadRoutine = StartCoroutine(DownloadAndPlayNarration(url));
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

        StopNarration();

        projectVideoUI.Show(_currentProject, _currentAgencyName);
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
        if (moreInfoButton != null)
            moreInfoButton.interactable = _currentProject != null;

        if (narratorButton != null)
            narratorButton.interactable = _currentProject != null && _narratorState != MediaState.Downloading;

        if (videoButton != null)
        {
            bool hasVideo = _currentProject != null && !string.IsNullOrWhiteSpace(_currentProject.videoUrl);
            // videoButton.interactable = hasVideo;
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

    private IEnumerator DownloadAndPlayNarration(string url)
    {
        SetNarratorState(MediaState.Downloading);
        overlay?.ShowLoading("กำลังดาวน์โหลดเสียงบรรยาย", "กรุณารอสักครู่");

        using UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.UNKNOWN);
        ApplyAuthorizationHeader(request);

        yield return request.SendWebRequest();

        _narrationDownloadRoutine = null;

        if (request.result != UnityWebRequest.Result.Success)
        {
            string message = $"{narratorFailedMessage} ({request.error})";
            SetNarratorState(MediaState.Failed, message);

            overlay?.ShowFailed(
                "โหลดเสียงบรรยายไม่สำเร็จ",
                "กรุณาลองใหม่อีกครั้ง"
            );
            yield break;
        }

        AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
        if (clip == null)
        {
            SetNarratorState(MediaState.Failed, narratorFailedMessage);

            overlay?.ShowFailed(
                "โหลดเสียงบรรยายไม่สำเร็จ",
                "ไม่พบข้อมูลเสียงบรรยาย"
            );
            yield break;
        }

        if (narratorAudioSource == null || narratorAudioSource.AudioSource == null)
        {
            SetNarratorState(MediaState.Failed, "Narrator AudioSource is missing.");

            overlay?.ShowFailed(
                "ไม่สามารถเล่นเสียงบรรยาย",
                "ไม่พบ Audio Source"
            );
            yield break;
        }

        ApplyNarrationBgmMute(true);

        narratorAudioSource.AudioSource.Stop();
        narratorAudioSource.AudioSource.clip = clip;
        narratorAudioSource.AudioSource.Play();

        SetNarratorState(MediaState.Playing);

        overlay?.ShowSuccess(
            "พร้อมใช้งานเสียงบรรยาย",
            "กำลังเริ่มเล่น",
            true
        );

        if (_narrationMonitorRoutine != null)
        {
            StopCoroutine(_narrationMonitorRoutine);
            _narrationMonitorRoutine = null;
        }

        _narrationMonitorRoutine = StartCoroutine(MonitorNarrationPlayback());
    }

    private IEnumerator MonitorNarrationPlayback()
    {
        if (narratorAudioSource == null || narratorAudioSource.AudioSource == null)
        {
            ApplyNarrationBgmMute(false);
            SetNarratorState(MediaState.Failed, "Narrator AudioSource is missing.");
            _narrationMonitorRoutine = null;
            yield break;
        }

        AudioSource source = narratorAudioSource.AudioSource;

        while (source != null && source.isPlaying)
            yield return null;

        ApplyNarrationBgmMute(false);
        SetNarratorState(MediaState.Completed);
        _narrationMonitorRoutine = null;
    }

    private void StopNarrationInternal(bool restoreBgm)
    {
        if (_narrationDownloadRoutine != null)
        {
            StopCoroutine(_narrationDownloadRoutine);
            _narrationDownloadRoutine = null;
        }

        if (_narrationMonitorRoutine != null)
        {
            StopCoroutine(_narrationMonitorRoutine);
            _narrationMonitorRoutine = null;
        }

        if (narratorAudioSource != null && narratorAudioSource.AudioSource != null)
        {
            narratorAudioSource.AudioSource.Stop();
            narratorAudioSource.AudioSource.clip = null;
        }

        if (restoreBgm)
            ApplyNarrationBgmMute(false);
    }

    private void ApplyNarrationBgmMute(bool mute)
    {
        if (ExhibitionAudioManager.Instance == null)
            return;

        if (mute)
            ExhibitionAudioManager.Instance.SetTemporaryBgmVolume(0f);
        else
            ExhibitionAudioManager.Instance.ClearTemporaryBgmVolume();
    }

    private void SetNarratorState(MediaState state, string overrideMessage = null)
    {
        _narratorState = state;

        if (narratorButton != null)
            narratorButton.interactable = _currentProject != null && state != MediaState.Downloading;

        RefreshButtons();
    }

    private string GetNarratorStateMessage(MediaState state)
    {
        switch (state)
        {
            case MediaState.Downloading:
                return narratorLoadingMessage;
            case MediaState.Playing:
                return narratorPlayingMessage;
            case MediaState.Failed:
                return narratorFailedMessage;
            case MediaState.Completed:
                return narratorCompletedMessage;
            case MediaState.Stopped:
                return narratorStoppedMessage;
            default:
                return narratorIdleMessage;
        }
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
        if (_narrationDownloadRoutine != null)
        {
            StopCoroutine(_narrationDownloadRoutine);
            _narrationDownloadRoutine = null;
        }

        if (_narrationMonitorRoutine != null)
        {
            StopCoroutine(_narrationMonitorRoutine);
            _narrationMonitorRoutine = null;
        }

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

public enum MediaState
{
    Idle,
    Downloading,
    Playing,
    Failed,
    Completed,
    Stopped
}