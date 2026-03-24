using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class AE_ProjectDetailUI : MonoBehaviour
{
    private enum DownloadState
    {
        Idle,
        Downloading,
        Playing,
        Failed,
        Completed,
        Stopped
    }

    private const int MaxImageSlots = 4;

    [Header("Config")]
    [SerializeField] private ApiConfig apiConfig;
    [SerializeField] private bool useEnglishContent;
    [SerializeField] private bool useEnglishNarrator;
    [SerializeField] private int shortDescriptionCharacterLimit = 220;

    [Header("Linked Panels")]
    [SerializeField] private AE_AdditionalProjectDetailUI additionalProjectDetailUI;

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

    private GovernmentProjectDto _currentProject;
    private string _currentAgencyName;

    private Coroutine _narrationDownloadRoutine;
    private Coroutine _narrationMonitorRoutine;

    private readonly List<Coroutine> _imageLoadRoutines = new();
    private readonly List<Object> _runtimeAssets = new();

    private DownloadState _narratorState = DownloadState.Idle;

    public bool HasProject => _currentProject != null;

    private void Awake()
    {
        if (moreInfoButton != null)
            moreInfoButton.onClick.AddListener(HandleMoreInfoClicked);

        if (narratorButton != null)
            narratorButton.onClick.AddListener(HandleNarratorClicked);
    }

    private void OnDestroy()
    {
        if (moreInfoButton != null)
            moreInfoButton.onClick.RemoveListener(HandleMoreInfoClicked);

        if (narratorButton != null)
            narratorButton.onClick.RemoveListener(HandleNarratorClicked);

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
        SetNarratorState(DownloadState.Idle);
    }

    public void Hide()
    {
        StopAllRunningCoroutines();
        StopNarrationInternal(true);
        ClearRuntimeAssets();

        if (additionalProjectDetailUI != null)
            additionalProjectDetailUI.Hide();

        _currentProject = null;
        _currentAgencyName = string.Empty;

        ApplyEmptyState();
        SetNarratorState(DownloadState.Idle);

        if (root != null)
            root.SetActive(false);
    }

    public void StopNarration()
    {
        StopNarrationInternal(true);
        SetNarratorState(DownloadState.Stopped);
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

        if (_narratorState == DownloadState.Downloading)
            return;

        if (_narratorState == DownloadState.Playing)
        {
            StopNarration();
            return;
        }

        string url = BuildProjectTtsUrl(_currentProject);
        Debug.Log($"[AE_ProjectDetailUI] Narrator URL = {url}");

        if (string.IsNullOrWhiteSpace(url))
        {
            SetNarratorState(DownloadState.Failed, "Narrator URL is empty.");
            return;
        }

        if (_narrationDownloadRoutine != null)
        {
            StopCoroutine(_narrationDownloadRoutine);
            _narrationDownloadRoutine = null;
        }

        _narrationDownloadRoutine = StartCoroutine(DownloadAndPlayNarration(url));
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
        SetNarratorState(DownloadState.Downloading);

        using UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.UNKNOWN);
        ApplyAuthorizationHeader(request);

        yield return request.SendWebRequest();

        _narrationDownloadRoutine = null;

        if (request.result != UnityWebRequest.Result.Success)
        {
            SetNarratorState(DownloadState.Failed, $"{narratorFailedMessage} ({request.error})");
            yield break;
        }

        AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
        if (clip == null)
        {
            SetNarratorState(DownloadState.Failed, narratorFailedMessage);
            yield break;
        }

        if (narratorAudioSource == null || narratorAudioSource.AudioSource == null)
        {
            SetNarratorState(DownloadState.Failed, "Narrator AudioSource is missing.");
            yield break;
        }

        ApplyNarrationBgmMute(true);
        SetNarratorState(DownloadState.Playing);

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
            SetNarratorState(DownloadState.Failed, "Narrator AudioSource is missing.");
            _narrationMonitorRoutine = null;
            yield break;
        }

        AudioSource source = narratorAudioSource.AudioSource;

        while (source != null && source.isPlaying)
            yield return null;

        ApplyNarrationBgmMute(false);
        SetNarratorState(DownloadState.Completed);
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

    private void SetNarratorState(DownloadState state, string overrideMessage = null)
    {
        _narratorState = state;

        if (narratorLoadingObject != null)
            narratorLoadingObject.SetActive(state == DownloadState.Downloading);

        if (narratorPlayingObject != null)
            narratorPlayingObject.SetActive(state == DownloadState.Playing);

        if (narratorFailedObject != null)
            narratorFailedObject.SetActive(state == DownloadState.Failed);

        if (narratorCompletedObject != null)
            narratorCompletedObject.SetActive(
                state == DownloadState.Completed || state == DownloadState.Stopped
            );

        if (narratorStatusText != null)
        {
            narratorStatusText.text = string.IsNullOrWhiteSpace(overrideMessage)
                ? GetNarratorStateMessage(state)
                : overrideMessage;
        }

        if (narratorButton != null)
            narratorButton.interactable = _currentProject != null && state != DownloadState.Downloading;
    }

    private string GetNarratorStateMessage(DownloadState state)
    {
        switch (state)
        {
            case DownloadState.Downloading:
                return narratorLoadingMessage;
            case DownloadState.Playing:
                return narratorPlayingMessage;
            case DownloadState.Failed:
                return narratorFailedMessage;
            case DownloadState.Completed:
                return narratorCompletedMessage;
            case DownloadState.Stopped:
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