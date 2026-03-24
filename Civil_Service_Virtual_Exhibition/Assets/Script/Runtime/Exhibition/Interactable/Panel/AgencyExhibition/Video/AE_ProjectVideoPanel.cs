using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AE_ProjectVideoPanel : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button closeButton;

    [Header("Overlay")]
    [SerializeField] private StatusOverlay overlay;

    [Header("Header")]
    [SerializeField] private TMP_Text projectNameText;
    [SerializeField] private TMP_Text agencyNameText;

    [Header("References")]
    [SerializeField] private AE_ProjectVideoPlayerController playerController;
    [SerializeField] private AE_ProjectVideoControlsView controlsView;

    [Header("Overlay Messages")]
    [SerializeField] private string overlayLoadingTitle = "กำลังเตรียมวิดีโอ";
    [SerializeField] private string overlayLoadingSubtitle = "กรุณารอสักครู่";
    [SerializeField] private string overlayFailedTitle = "โหลดวิดีโอไม่สำเร็จ";
    [SerializeField] private string overlayFailedSubtitle = "กรุณาลองใหม่อีกครั้ง";

    private GovernmentProjectDto _currentProject;
    private string _currentAgencyName;

    private void Awake()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(Hide);

        if (playerController != null)
        {
            playerController.Prepared += HandlePrepared;
            playerController.Failed += HandleFailed;
        }

        if (controlsView != null && playerController != null)
            controlsView.Bind(playerController);

        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    private void OnDestroy()
    {
        if (closeButton != null)
            closeButton.onClick.RemoveListener(Hide);

        if (playerController != null)
        {
            playerController.Prepared -= HandlePrepared;
            playerController.Failed -= HandleFailed;
        }

        if (controlsView != null)
            controlsView.Unbind();
    }

    public void Show(GovernmentProjectDto project, string agencyName)
    {
        _currentProject = project;
        _currentAgencyName = agencyName;

        if (panelRoot != null)
            panelRoot.SetActive(true);

        BindHeader();

        string url = _currentProject != null ? _currentProject.videoUrl : string.Empty;
        if (string.IsNullOrWhiteSpace(url))
        {
            overlay?.Hide();
            playerController?.StopPlayback();
            return;
        }

        overlay?.ShowLoading(overlayLoadingTitle, overlayLoadingSubtitle);
        playerController?.Prepare(url);
    }

    public void Hide()
    {
        overlay?.Hide();
        playerController?.StopPlayback();

        _currentProject = null;
        _currentAgencyName = string.Empty;

        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    private void BindHeader()
    {
        if (projectNameText != null)
            projectNameText.text = FirstNotEmpty(_currentProject?.name, _currentProject?.nameEn);

        if (agencyNameText != null)
            agencyNameText.text = _currentAgencyName ?? string.Empty;
    }

    private void HandlePrepared()
    {
        overlay?.Hide();
        playerController?.Play();
    }

    private void HandleFailed(string _)
    {
        overlay?.ShowFailed(overlayFailedTitle, overlayFailedSubtitle);
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