using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SessionSignOut : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string loginSceneName = "LandingPage";

    [Header("Optional")]
    [SerializeField] private GameObject signingOutUI;

    private bool _isSigningOut;

    public void SignOut()
    {
        if (_isSigningOut)
            return;

        _ = SignOutAsync();
    }

    private async Task SignOutAsync()
    {
        _isSigningOut = true;

        try
        {
            if (signingOutUI != null)
                signingOutUI.SetActive(true);

            PlayerInput.GameplayInputBlocked = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (NetworkLauncher.Instance != null && NetworkLauncher.Instance.IsRunning)
                await NetworkLauncher.Instance.ShutdownCurrentRoom();

            LocalPlayerData.Instance?.ClearForSignOut();

            SceneManager.LoadScene(loginSceneName);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
        finally
        {
            PlayerInput.GameplayInputBlocked = false;

            if (signingOutUI != null)
                signingOutUI.SetActive(false);

            _isSigningOut = false;
        }
    }
}