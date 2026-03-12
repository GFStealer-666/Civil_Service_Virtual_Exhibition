// LandingPageManager.cs — navigation only
using UnityEngine;

public class LandingPageManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject starterPanel;
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private GameObject registerPanel;
    [SerializeField] private GameObject forgotPasswordPanel;

    [Header("Handlers")]
    [SerializeField] private LoginHandler          loginHandler;
    [SerializeField] private RegisterHandler       registerHandler;
    [SerializeField] private ForgotPasswordHandler forgotHandler;

    [Header("Starter Links")]
    [SerializeField] private TMProLinkButton starterRegisterBtn;
    [SerializeField] private TMProLinkButton starterGuestBtn;

    private void Start()
    {
        starterRegisterBtn.onLinkClicked.AddListener(ShowRegister);
        starterGuestBtn   .onLinkClicked.AddListener(() => loginHandler.OnGuestClicked());

        ShowStarter();
    }

    public void ShowStarter()        => SwitchPanel(starterPanel);
    public void ShowLogin()          => SwitchPanel(loginPanel);
    public void ShowRegister()       => SwitchPanel(registerPanel);
    public void ShowForgotPassword() => SwitchPanel(forgotPasswordPanel);

    private void SwitchPanel(GameObject target)
    {
        starterPanel       .SetActive(starterPanel        == target);
        loginPanel         .SetActive(loginPanel          == target);
        registerPanel      .SetActive(registerPanel       == target);
        forgotPasswordPanel.SetActive(forgotPasswordPanel == target);
    }
}