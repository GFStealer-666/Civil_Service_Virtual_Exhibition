using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AE_ProjectVideoPanel : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button closeButton;

    [Header("Header")]
    [SerializeField] private TMP_Text projectNameText;
    [SerializeField] private TMP_Text agencyNameText;

    [Header("References")]
    [SerializeField] private AE_ProjectVideoSessionController videoSessionController;
    [SerializeField] private AE_ProjectVideoControlsView controlsView;

    private GovernmentProjectDto _currentProject;
    private string _currentAgencyNameTh;
    private string _currentAgencyNameEn;

    private void Awake()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(HandleCloseClicked);

        if (controlsView != null && videoSessionController != null)
            controlsView.Bind(videoSessionController);

        if (videoSessionController != null)
        {
            videoSessionController.LoadingCanceled += HandleLoadingCanceled;
            videoSessionController.FailureAcknowledged += HandleFailureAcknowledged;
        }

        if (panelRoot != null)
            panelRoot.SetActive(false);

        controlsView?.ResetView();
    }

    private void OnDestroy()
    {
        if (closeButton != null)
            closeButton.onClick.RemoveListener(HandleCloseClicked);

        if (videoSessionController != null)
        {
            videoSessionController.LoadingCanceled -= HandleLoadingCanceled;
            videoSessionController.FailureAcknowledged -= HandleFailureAcknowledged;
        }

        controlsView?.Unbind();
    }

    private void OnEnable()
    {
        RefreshLocalization();
    }

    public void Show(GovernmentProjectDto project, string agencyNameTh, string agencyNameEn)
    {
        _currentProject = project;
        _currentAgencyNameTh = agencyNameTh;
        _currentAgencyNameEn = agencyNameEn;

        if (panelRoot != null)
            panelRoot.SetActive(true);

        BindHeader();
        controlsView?.ResetView();

        if (videoSessionController == null)
        {
            Debug.LogWarning("[AE_ProjectVideoPanel] VideoSessionController is not assigned.");
            return;
        }

        videoSessionController.PrepareAndPlay(
            _currentProject != null ? _currentProject.videoUrl : string.Empty
        );
    }

    public void Hide()
    {
        ReturnToProjectDetail(true);
    }

    public void RefreshLocalization()
    {
        BindHeader();
    }

    private void HandleCloseClicked()
    {
        if (videoSessionController != null &&
            (videoSessionController.State == MediaPlaybackState.Loading ||
             videoSessionController.IsPreparing))
        {
            videoSessionController.CancelMediaLoading();
            return;
        }

        ReturnToProjectDetail(true);
    }

    private void HandleLoadingCanceled()
    {
        ReturnToProjectDetail(false);
    }

    private void HandleFailureAcknowledged()
    {
        ReturnToProjectDetail(true);
    }

    private void ReturnToProjectDetail(bool resetSession)
    {
        if (resetSession)
            videoSessionController?.ResetSession();

        controlsView?.ResetView();
        ClearVideoPanelData();

        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    private void ClearVideoPanelData()
    {
        _currentProject = null;
        _currentAgencyNameTh = string.Empty;
        _currentAgencyNameEn = string.Empty;

        if (projectNameText != null)
            projectNameText.text = string.Empty;

        if (agencyNameText != null)
            agencyNameText.text = string.Empty;
    }

    private void BindHeader()
    {
        bool useEnglish = IsEnglish();

        if (projectNameText != null)
        {
            projectNameText.text = useEnglish
                ? FirstNotEmpty(_currentProject?.nameEn, _currentProject?.name)
                : FirstNotEmpty(_currentProject?.name, _currentProject?.nameEn);
        }

        if (agencyNameText != null)
        {
            agencyNameText.text = useEnglish
                ? FirstNotEmpty(_currentAgencyNameEn, _currentAgencyNameTh)
                : FirstNotEmpty(_currentAgencyNameTh, _currentAgencyNameEn);
        }
    }

    private bool IsEnglish()
    {
        string code = LocalizationService.CurrentLocaleCode;
        return !string.IsNullOrWhiteSpace(code) &&
               code.StartsWith("en", System.StringComparison.OrdinalIgnoreCase);
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