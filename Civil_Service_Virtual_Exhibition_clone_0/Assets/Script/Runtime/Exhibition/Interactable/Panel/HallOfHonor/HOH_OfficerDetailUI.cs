using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class HOH_OfficerDetailUI : MonoBehaviour
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

    [Header("Linked Panels")]
    [SerializeField] private HOH_OfficerAdditionalDetailUI additionalDetailUI;

    [Header("Root")]
    [SerializeField] private GameObject root;
    [SerializeField] private Button closeButton;

    [Header("Text")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text roleText;
    [SerializeField] private TMP_Text organizationText;
    [SerializeField] private TMP_Text shortDescriptionText;

    [Header("Buttons")]
    [SerializeField] private Button moreInfoButton;
    [SerializeField] private Button narratorButton;
    [SerializeField] private GameObject narratorButtonRoot;
    [SerializeField] private bool narratorAvailable = true;

    [Header("Config")]
    
    [SerializeField] private bool useEnglishNarrator = false;
    [SerializeField] private int shortDescriptionCharacterLimit = 150;
    private ApiService Api => ApiService.Instance;
    [Header("Loader")]
    [SerializeField] private UniversalImageLoader photoLoader;

    [Header("Overlay")]
    [SerializeField] private StatusOverlay overlay;

    [Header("Audio")]
    [SerializeField] private ExhibitionAudioSource narratorAudioSource;

    [Header("Overlay Messages")]
    [SerializeField] private string overlayLoadingTitle = "กำลังดาวน์โหลดเสียงบรรยาย";
    [SerializeField] private string overlayLoadingSubtitle = "กรุณารอสักครู่";
    [SerializeField] private string overlaySuccessTitle = "พร้อมใช้งานเสียงบรรยาย";
    [SerializeField] private string overlaySuccessSubtitle = "กำลังเริ่มเล่น";
    [SerializeField] private string overlayFailedTitle = "โหลดเสียงบรรยายไม่สำเร็จ";
    [SerializeField] private string overlayFailedSubtitle = "กรุณาลองใหม่อีกครั้ง";

    private HOH_PersonDto _currentPerson;
    private HOH_UnitDto _currentUnit;
    private HOH_OfficerSelectionPanelController _previousSelection;

    private Coroutine _narrationDownloadRoutine;
    private Coroutine _narrationMonitorRoutine;

    private NarratorState _narratorState = NarratorState.Idle;

    public bool HasPerson => _currentPerson != null;

    private void Awake()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(Hide);

        if (moreInfoButton != null)
            moreInfoButton.onClick.AddListener(HandleMoreInfoClicked);

        if (narratorButton != null)
            narratorButton.onClick.AddListener(HandleNarratorClicked);
    }

    private void OnDestroy()
    {
        if (closeButton != null)
            closeButton.onClick.RemoveListener(Hide);

        if (moreInfoButton != null)
            moreInfoButton.onClick.RemoveListener(HandleMoreInfoClicked);

        if (narratorButton != null)
            narratorButton.onClick.RemoveListener(HandleNarratorClicked);

        StopNarrationInternal(false);
        overlay?.Hide();
    }

    public void Show(HOH_PersonDto person, HOH_UnitDto unit, HOH_OfficerSelectionPanelController previousSelection)
    {
        _currentPerson = person;
        _currentUnit = unit;
        _previousSelection = previousSelection;

        StopNarrationInternal(false);
        overlay?.Hide();

        if (root != null)
            root.SetActive(true);

        BindText();
        BindPhoto();
        SetNarratorState(NarratorState.Idle);
    }

    public void Hide()
    {
        if (additionalDetailUI != null)
            additionalDetailUI.HideSilently();

        StopNarrationInternal(true);
        overlay?.Hide();

        _currentPerson = null;
        _currentUnit = null;

        ApplyEmptyState();
        SetNarratorState(NarratorState.Idle);

        if (root != null)
            root.SetActive(false);

        if (_previousSelection != null)
            _previousSelection.ReopenFromChild();
    }

    public void HideSilently()
    {
        StopNarrationInternal(true);
        overlay?.Hide();

        _currentPerson = null;
        _currentUnit = null;

        ApplyEmptyState();
        SetNarratorState(NarratorState.Idle);

        if (root != null)
            root.SetActive(false);
    }

    public void ReopenFromChild()
    {
        if (root != null)
            root.SetActive(true);
    }

    public void StopNarration()
    {
        StopNarrationInternal(true);
        SetNarratorState(NarratorState.Stopped);
    }

    private void BindText()
    {
        if (_currentPerson == null)
        {
            ApplyEmptyState();
            return;
        }

        if (titleText != null)
            titleText.text = BuildFullName(_currentPerson);

        if (roleText != null)
            roleText.text = BuildRoleText(_currentPerson);

        if (organizationText != null)
            organizationText.text = BuildOrganizationLine(_currentPerson, _currentUnit);

        if (shortDescriptionText != null)
        {
            shortDescriptionText.text = TruncateWithEllipsis(
                BuildShortDescription(_currentPerson),
                shortDescriptionCharacterLimit
            );
        }
    }

    private void BindPhoto()
    {
        if (photoLoader == null)
            return;

        if (_currentPerson == null)
        {
            photoLoader.Load(string.Empty);
            return;
        }

        photoLoader.Load(_currentPerson.photoUrl);
    }

    private void ApplyEmptyState()
    {
        if (titleText != null)
            titleText.text = string.Empty;

        if (roleText != null)
            roleText.text = string.Empty;

        if (organizationText != null)
            organizationText.text = string.Empty;

        if (shortDescriptionText != null)
            shortDescriptionText.text = string.Empty;

        if (photoLoader != null)
            photoLoader.Load(string.Empty);
    }

    private void HandleMoreInfoClicked()
    {
        if (_currentPerson == null)
            return;

        if (additionalDetailUI == null)
        {
            Debug.LogWarning("[HOH_OfficerDetailUI] AdditionalDetailUI is not assigned.");
            return;
        }

        StopNarration();

        if (root != null)
            root.SetActive(false);

        additionalDetailUI.Show(_currentPerson, _currentUnit, this);
    }

    private void HandleNarratorClicked()
    {
        if (!narratorAvailable || _currentPerson == null)
            return;

        if (_narratorState == NarratorState.Downloading)
            return;

        if (_narratorState == NarratorState.Playing)
        {
            StopNarration();
            return;
        }

        string url = BuildOfficerTtsUrl(_currentPerson);
        Debug.Log($"[HOH_OfficerDetailUI] Narrator URL = {url}");

        if (string.IsNullOrWhiteSpace(url))
        {
            SetNarratorState(NarratorState.Failed, "Narrator URL is empty.");
            overlay?.ShowFailed(overlayFailedTitle, "ไม่พบลิงก์เสียงบรรยาย");
            return;
        }

        if (_narrationDownloadRoutine != null)
        {
            StopCoroutine(_narrationDownloadRoutine);
            _narrationDownloadRoutine = null;
        }

        _narrationDownloadRoutine = StartCoroutine(DownloadAndPlayNarration(url));
    }

    private IEnumerator DownloadAndPlayNarration(string url)
    {
        SetNarratorState(NarratorState.Downloading);
        overlay?.ShowLoading(overlayLoadingTitle, overlayLoadingSubtitle);

        using UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.UNKNOWN);
        ApplyAuthorizationHeader(request);

        yield return request.SendWebRequest();

        _narrationDownloadRoutine = null;

        if (request.result != UnityWebRequest.Result.Success)
        {
            overlay?.ShowFailed(overlayFailedTitle, overlayFailedSubtitle);
            yield break;
        }

        AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
        if (clip == null)
        {
            overlay?.ShowFailed(overlayFailedTitle, "ไม่พบข้อมูลเสียงบรรยาย");
            yield break;
        }

        if (narratorAudioSource == null || narratorAudioSource.AudioSource == null)
        {
            SetNarratorState(NarratorState.Failed, "Narrator AudioSource is missing.");
            overlay?.ShowFailed("ไม่สามารถเล่นเสียงบรรยาย", "ยังไม่ได้ตั้งค่า Audio Source");
            yield break;
        }

        ApplyNarrationBgmMute(true);

        narratorAudioSource.AudioSource.Stop();
        narratorAudioSource.AudioSource.clip = clip;
        narratorAudioSource.AudioSource.Play();

        SetNarratorState(NarratorState.Playing);
        overlay?.ShowSuccess(overlaySuccessTitle, overlaySuccessSubtitle, true);

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

        if (narratorButtonRoot != null)
            narratorButtonRoot.SetActive(narratorAvailable);

        RefreshButtons();
    }

    private void RefreshButtons()
    {
        if (moreInfoButton != null)
            moreInfoButton.interactable = _currentPerson != null;

        if (narratorButton != null)
        {
            narratorButton.interactable =
                narratorAvailable &&
                _currentPerson != null &&
                _narratorState != NarratorState.Downloading;
        }
    }
    private string BuildOfficerTtsUrl(HOH_PersonDto person)
    {
        if (person == null)
            return string.Empty;

        string officerId = FirstNotEmpty(person.id, person.runtimeId);
        if (string.IsNullOrWhiteSpace(officerId))
            return string.Empty;

        if (Api == null)
            return string.Empty;

        return useEnglishNarrator
            ? Api.GetHallOfHonorTtsEngUrl(officerId)
            : Api.GetHallOfHonorTtsThUrl(officerId);
    }

    private void ApplyAuthorizationHeader(UnityWebRequest request)
    {
        if (request == null)
            return;

        string accessToken = PlayerPrefs.GetString("access_token", string.Empty);
        if (!string.IsNullOrWhiteSpace(accessToken))
            request.SetRequestHeader("Authorization", $"Bearer {accessToken}");
    }

    private string BuildFullName(HOH_PersonDto person)
    {
        if (person == null)
            return string.Empty;

        string prefix = !string.IsNullOrWhiteSpace(person.prefixOther) ? person.prefixOther : person.prefix;
        return $"{prefix} {person.firstName} {person.lastName}".Trim();
    }

    private string BuildOrganizationLine(HOH_PersonDto person, HOH_UnitDto unit)
    {
        string ministry = FirstNotEmpty(person?.ministry, unit?.ministry);
        string department = FirstNotEmpty(person?.department, unit?.unit);
        string division = person?.division;

        return JoinNonEmpty(
            "\n",
            JoinNonEmpty(" , ", ministry, department),
            division
        );
    }

    private string BuildRoleText(HOH_PersonDto person)
    {
        if (person == null)
            return string.Empty;

        return JoinNonEmpty(" ", person.position, person.positionLevel);
    }

    private string BuildShortDescription(HOH_PersonDto person)
    {
        if (person == null)
            return string.Empty;

        return FirstNotEmpty(
            person.outstandingWork,
            JoinNonEmpty(" ", person.education, person.institution),
            "ไม่พบข้อมูลรายละเอียด"
        );
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

    private string JoinNonEmpty(string separator, params string[] values)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();

        if (values == null)
            return string.Empty;

        for (int i = 0; i < values.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(values[i]))
                continue;

            if (builder.Length > 0)
                builder.Append(separator);

            builder.Append(values[i].Trim());
        }

        return builder.ToString();
    }
}