using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.Video;

public class AE_ProjectDetailUI : MonoBehaviour
{
    private enum NarratorState
    {
        Idle,
        Downloading,
        Playing,
        Failed,
        Completed,
        Stopped
    }

    [Header("Config")]
    [SerializeField] private ApiConfig apiConfig;
    [SerializeField] private bool useEnglishContent;
    [SerializeField] private bool useEnglishNarrator;
    [SerializeField] private int shortDescriptionCharacterLimit = 220;

    [Header("Linked Panels")]
    [SerializeField] private AE_AdditionalProjectDetailUI additionalProjectDetailUI;
    [SerializeField] private AE_ProjectVideoUI projectVideoUI;

    [Header("Root")]
    [SerializeField] private GameObject root;

    [Header("Text")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text agencyText;
    [SerializeField] private TMP_Text descriptionText;

    [Header("Main Visual")]
    [SerializeField] private Image mainImage;
    [SerializeField] private Button mainImageButton;
    [SerializeField] private Sprite defaultMainImage;

    [Header("Gallery")]
    [SerializeField] private List<Image> galleryImages = new List<Image>();
    [SerializeField] private Sprite defaultGalleryImage;

    [Header("Buttons")]
    [SerializeField] private Button moreInfoButton;
    [SerializeField] private Button narratorButton;

    [Header("Narrator Feedback")]
    [SerializeField] private TMP_Text narratorStatusText;
    [SerializeField] private GameObject narratorLoadingObject;
    [SerializeField] private GameObject narratorPlayingObject;
    [SerializeField] private GameObject narratorFailedObject;
    [SerializeField] private GameObject narratorCompletedObject;

    [Header("Narrator Messages")]
    [SerializeField] private string narratorIdleMessage = "";
    [SerializeField] private string narratorLoadingMessage = "Loading narrator audio...";
    [SerializeField] private string narratorPlayingMessage = "Narrator is playing.";
    [SerializeField] private string narratorFailedMessage = "Failed to load narrator audio.";
    [SerializeField] private string narratorCompletedMessage = "Narrator playback finished.";
    [SerializeField] private string narratorStoppedMessage = "Narrator playback stopped.";

    [Header("Audio")]
    [SerializeField] private ExhibitionAudioSource narratorAudioSource;

    [Header("Video Thumbnail")]
    [SerializeField] private int videoThumbnailWidth = 1280;
    [SerializeField] private int videoThumbnailHeight = 720;
    [SerializeField] private float videoPrepareTimeout = 8f;
    [SerializeField] private float videoFrameTimeout = 3f;

    private GovernmentProjectDto _currentProject;
    private string _currentAgencyName;

    private Coroutine _mainVisualRoutine;
    private Coroutine _narrationDownloadRoutine;
    private Coroutine _narrationMonitorRoutine;

    private readonly List<Coroutine> _galleryLoadRoutines = new List<Coroutine>();
    private readonly List<Object> _runtimeAssets = new List<Object>();

    private VideoPlayer _thumbnailVideoPlayer;
    private RenderTexture _thumbnailRenderTexture;

    private NarratorState _narratorState = NarratorState.Idle;

    public bool HasProject => _currentProject != null;

    private void Awake()
    {
        EnsureThumbnailPlayer();

        if (moreInfoButton != null)
            moreInfoButton.onClick.AddListener(HandleMoreInfoClicked);

        if (narratorButton != null)
            narratorButton.onClick.AddListener(HandleNarratorClicked);

        if (mainImageButton != null)
            mainImageButton.onClick.AddListener(HandleMainImageClicked);
    }

    private void OnDestroy()
    {
        if (moreInfoButton != null)
            moreInfoButton.onClick.RemoveListener(HandleMoreInfoClicked);

        if (narratorButton != null)
            narratorButton.onClick.RemoveListener(HandleNarratorClicked);

        if (mainImageButton != null)
            mainImageButton.onClick.RemoveListener(HandleMainImageClicked);

        StopAllRunningCoroutines();
        StopNarrationInternal(false);
        ClearRuntimeAssets();
        ReleaseThumbnailPlayer();
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
        BindGalleryImages();
        LoadMainVisual();
        SetNarratorState(NarratorState.Idle);
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
        SetNarratorState(NarratorState.Idle);

        if (root != null)
            root.SetActive(false);
    }

    public void StopNarration()
    {
        StopNarrationInternal(true);
        SetNarratorState(NarratorState.Stopped);
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
            descriptionText.text = TruncateWithEllipsis(
                GetProjectDescription(_currentProject),
                shortDescriptionCharacterLimit
            );
    }

    private void ApplyEmptyState()
    {
        if (titleText != null)
            titleText.text = string.Empty;

        if (agencyText != null)
            agencyText.text = string.Empty;

        if (descriptionText != null)
            descriptionText.text = string.Empty;

        if (mainImage != null)
        {
            mainImage.sprite = defaultMainImage;
            mainImage.preserveAspect = true;
        }

        ClearGallerySlots();
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

    private void HandleMainImageClicked()
    {
        if (_currentProject == null)
            return;

        if (projectVideoUI == null)
        {
            Debug.LogWarning("[AE_ProjectDetailUI] ProjectVideoUI is not assigned.");
            return;
        }

        if (string.IsNullOrWhiteSpace(_currentProject.videoUrl))
        {
            Debug.LogWarning("[AE_ProjectDetailUI] Current project has no video url.");
            return;
        }

        projectVideoUI.Show(_currentProject, _currentAgencyName);
    }

    private void HandleNarratorClicked()
    {
        if (_currentProject == null)
            return;

        if (_narratorState == NarratorState.Downloading)
            return;

        if (_narratorState == NarratorState.Playing)
        {
            StopNarration();
            return;
        }

        string url = BuildProjectTtsUrl(_currentProject);

        if (string.IsNullOrWhiteSpace(url))
        {
            SetNarratorState(NarratorState.Failed, "Narrator URL is empty.");
            return;
        }

        if (_narrationDownloadRoutine != null)
        {
            StopCoroutine(_narrationDownloadRoutine);
            _narrationDownloadRoutine = null;
        }

        _narrationDownloadRoutine = StartCoroutine(DownloadAndPlayNarration(url));
    }

    private void LoadMainVisual()
    {
        if (mainImage != null)
        {
            mainImage.sprite = defaultMainImage;
            mainImage.preserveAspect = true;
        }

        if (_mainVisualRoutine != null)
        {
            StopCoroutine(_mainVisualRoutine);
            _mainVisualRoutine = null;
        }

        if (_currentProject == null)
            return;

        _mainVisualRoutine = StartCoroutine(LoadMainVisualRoutine());
    }

    private IEnumerator LoadMainVisualRoutine()
    {
        bool loadedFromVideo = false;

        if (!string.IsNullOrWhiteSpace(_currentProject.videoUrl))
        {
            yield return StartCoroutine(TryLoadVideoFirstFrame(
                _currentProject.videoUrl,
                success => loadedFromVideo = success
            ));
        }

        if (!loadedFromVideo)
        {
            string fallbackImageUrl = GetPrimaryGalleryImageUrl(_currentProject);

            if (!string.IsNullOrWhiteSpace(fallbackImageUrl))
                yield return StartCoroutine(LoadImageIntoImage(fallbackImageUrl, mainImage, -1));
        }

        _mainVisualRoutine = null;
    }

    private void BindGalleryImages()
    {
        ClearGallerySlots();

        if (_currentProject == null || _currentProject.imageUrls == null || _currentProject.imageUrls.Count == 0)
        {
            Debug.Log("[AE_ProjectDetailUI] No gallery images in current project.");
            return;
        }

        int count = Mathf.Min(galleryImages.Count, _currentProject.imageUrls.Count);

        for (int i = 0; i < count; i++)
        {
            Image slot = galleryImages[i];
            if (slot == null)
            {
                Debug.LogWarning($"[AE_ProjectDetailUI] Gallery slot {i} is null.");
                continue;
            }

            string imageUrl = _currentProject.imageUrls[i];

            slot.gameObject.SetActive(true);
            slot.sprite = defaultGalleryImage;
            slot.preserveAspect = true;

            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                Debug.LogWarning($"[AE_ProjectDetailUI] Gallery slot {i} has empty image url.");
                continue;
            }

            Coroutine routine = StartCoroutine(LoadImageIntoImage(imageUrl, slot, i));
            _galleryLoadRoutines.Add(routine);
        }
    }

    private void ClearGallerySlots()
    {
        for (int i = 0; i < galleryImages.Count; i++)
        {
            if (galleryImages[i] == null)
                continue;

            galleryImages[i].sprite = defaultGalleryImage;
            galleryImages[i].preserveAspect = true;
            galleryImages[i].gameObject.SetActive(false);
        }
    }

    private IEnumerator DownloadAndPlayNarration(string url)
    {
        SetNarratorState(NarratorState.Downloading);

        using UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.UNKNOWN);

        ApplyAuthorizationHeader(request);

        yield return request.SendWebRequest();

        _narrationDownloadRoutine = null;

        if (request.result != UnityWebRequest.Result.Success)
        {
            SetNarratorState(NarratorState.Failed, $"{narratorFailedMessage} ({request.error})");
            yield break;
        }

        AudioClip clip = DownloadHandlerAudioClip.GetContent(request);

        if (clip == null)
        {
            SetNarratorState(NarratorState.Failed, narratorFailedMessage);
            yield break;
        }

        if (narratorAudioSource == null || narratorAudioSource.AudioSource == null)
        {
            SetNarratorState(NarratorState.Failed, "Narrator AudioSource is missing.");
            yield break;
        }

        ApplyNarrationBgmMute(true);
        SetNarratorState(NarratorState.Playing);

        narratorAudioSource.AudioSource.Stop();
        narratorAudioSource.AudioSource.clip = clip;
        narratorAudioSource.AudioSource.Play();

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
            SetNarratorState(NarratorState.Failed, "Narrator AudioSource is missing.");
            _narrationMonitorRoutine = null;
            yield break;
        }

        AudioSource source = narratorAudioSource.AudioSource;

        while (source != null && source.isPlaying)
            yield return null;

        ApplyNarrationBgmMute(false);
        SetNarratorState(NarratorState.Completed);
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

    private void SetNarratorState(NarratorState state, string overrideMessage = null)
    {
        _narratorState = state;

        if (narratorLoadingObject != null)
            narratorLoadingObject.SetActive(state == NarratorState.Downloading);

        if (narratorPlayingObject != null)
            narratorPlayingObject.SetActive(state == NarratorState.Playing);

        if (narratorFailedObject != null)
            narratorFailedObject.SetActive(state == NarratorState.Failed);

        if (narratorCompletedObject != null)
            narratorCompletedObject.SetActive(state == NarratorState.Completed || state == NarratorState.Stopped);

        if (narratorStatusText != null)
            narratorStatusText.text = string.IsNullOrWhiteSpace(overrideMessage)
                ? GetNarratorStateMessage(state)
                : overrideMessage;

        if (narratorButton != null)
            narratorButton.interactable = _currentProject != null && state != NarratorState.Downloading;
    }

    private string GetNarratorStateMessage(NarratorState state)
    {
        switch (state)
        {
            case NarratorState.Downloading:
                return narratorLoadingMessage;
            case NarratorState.Playing:
                return narratorPlayingMessage;
            case NarratorState.Failed:
                return narratorFailedMessage;
            case NarratorState.Completed:
                return narratorCompletedMessage;
            case NarratorState.Stopped:
                return narratorStoppedMessage;
            default:
                return narratorIdleMessage;
        }
    }

    private string BuildProjectTtsUrl(GovernmentProjectDto project)
    {
        if (project == null || apiConfig == null)
            return string.Empty;

        string projectId = FirstNotEmpty(project.id, project.runtimeId);
        if (string.IsNullOrWhiteSpace(projectId))
            return string.Empty;

        string template = "ok";
        // Fix this later on
        
        if (string.IsNullOrWhiteSpace(template))
            return string.Empty;

        return template.Replace(":id", projectId);
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

    private string GetPrimaryGalleryImageUrl(GovernmentProjectDto project)
    {
        if (project == null || project.imageUrls == null || project.imageUrls.Count == 0)
            return string.Empty;

        return project.imageUrls[0];
    }

    private IEnumerator TryLoadVideoFirstFrame(string videoUrl, System.Action<bool> onFinished)
    {
        EnsureThumbnailPlayer();

        if (_thumbnailVideoPlayer == null || _thumbnailRenderTexture == null)
        {
            onFinished?.Invoke(false);
            yield break;
        }

        _thumbnailVideoPlayer.Stop();
        _thumbnailVideoPlayer.source = VideoSource.Url;
        _thumbnailVideoPlayer.url = videoUrl;
        _thumbnailVideoPlayer.audioOutputMode = VideoAudioOutputMode.None;
        _thumbnailVideoPlayer.renderMode = VideoRenderMode.RenderTexture;
        _thumbnailVideoPlayer.targetTexture = _thumbnailRenderTexture;

        _thumbnailVideoPlayer.Prepare();

        float prepareTimer = videoPrepareTimeout;

        while (!_thumbnailVideoPlayer.isPrepared && prepareTimer > 0f)
        {
            prepareTimer -= Time.unscaledDeltaTime;
            yield return null;
        }

        if (!_thumbnailVideoPlayer.isPrepared)
        {
            onFinished?.Invoke(false);
            yield break;
        }

        _thumbnailVideoPlayer.Play();

        float frameTimer = videoFrameTimeout;

        while (frameTimer > 0f)
        {
            bool hasFrame = _thumbnailVideoPlayer.texture != null &&
                            (_thumbnailVideoPlayer.frame > 0 || _thumbnailVideoPlayer.time > 0.01d);

            if (hasFrame)
                break;

            frameTimer -= Time.unscaledDeltaTime;
            yield return null;
        }

        yield return new WaitForEndOfFrame();

        if (_thumbnailRenderTexture == null)
        {
            _thumbnailVideoPlayer.Stop();
            onFinished?.Invoke(false);
            yield break;
        }

        Texture2D texture = new Texture2D(
            _thumbnailRenderTexture.width,
            _thumbnailRenderTexture.height,
            TextureFormat.RGBA32,
            false
        );

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = _thumbnailRenderTexture;
        texture.ReadPixels(
            new Rect(0f, 0f, _thumbnailRenderTexture.width, _thumbnailRenderTexture.height),
            0,
            0
        );
        texture.Apply();
        RenderTexture.active = previous;

        _thumbnailVideoPlayer.Stop();

        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f)
        );

        RegisterRuntimeAsset(texture);
        RegisterRuntimeAsset(sprite);

        if (mainImage != null)
        {
            mainImage.sprite = sprite;
            mainImage.preserveAspect = true;
        }

        onFinished?.Invoke(true);
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
        targetImage.enabled = true;

        Color color = targetImage.color;
        color.a = 1f;
        targetImage.color = color;

        if (slotIndex >= 0)
            targetImage.gameObject.SetActive(true);
    }

    private void StopAllRunningCoroutines()
    {
        if (_mainVisualRoutine != null)
        {
            StopCoroutine(_mainVisualRoutine);
            _mainVisualRoutine = null;
        }

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

        for (int i = 0; i < _galleryLoadRoutines.Count; i++)
        {
            if (_galleryLoadRoutines[i] != null)
                StopCoroutine(_galleryLoadRoutines[i]);
        }

        _galleryLoadRoutines.Clear();
    }

    private void EnsureThumbnailPlayer()
    {
        if (_thumbnailVideoPlayer != null && _thumbnailRenderTexture != null)
            return;

        GameObject playerObject = new GameObject("AE_ProjectDetail_ThumbnailPlayer");
        playerObject.transform.SetParent(transform, false);
        playerObject.hideFlags = HideFlags.HideInHierarchy;

        _thumbnailVideoPlayer = playerObject.AddComponent<VideoPlayer>();
        _thumbnailVideoPlayer.playOnAwake = false;
        _thumbnailVideoPlayer.waitForFirstFrame = true;
        _thumbnailVideoPlayer.isLooping = false;
        _thumbnailVideoPlayer.skipOnDrop = false;
        _thumbnailVideoPlayer.audioOutputMode = VideoAudioOutputMode.None;
        _thumbnailVideoPlayer.renderMode = VideoRenderMode.RenderTexture;

        _thumbnailRenderTexture = new RenderTexture(
            videoThumbnailWidth,
            videoThumbnailHeight,
            0,
            RenderTextureFormat.ARGB32
        );
        _thumbnailRenderTexture.Create();

        _thumbnailVideoPlayer.targetTexture = _thumbnailRenderTexture;
    }

    private void ReleaseThumbnailPlayer()
    {
        if (_thumbnailVideoPlayer != null)
            Destroy(_thumbnailVideoPlayer.gameObject);

        _thumbnailVideoPlayer = null;

        if (_thumbnailRenderTexture != null)
        {
            _thumbnailRenderTexture.Release();
            Destroy(_thumbnailRenderTexture);
        }

        _thumbnailRenderTexture = null;
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

        string accessToken = GetAccessToken();

        if (!string.IsNullOrWhiteSpace(accessToken))
            request.SetRequestHeader("Authorization", $"Bearer {accessToken}");
    }

    private string GetAccessToken()
    {
        return PlayerPrefs.GetString("access_token", string.Empty);
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