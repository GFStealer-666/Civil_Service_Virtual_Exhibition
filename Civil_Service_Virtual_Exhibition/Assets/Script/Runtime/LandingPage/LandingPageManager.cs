using UnityEngine;

public class LandingPageManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject starterPanel;
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private GameObject registerPanel;
    [SerializeField] private GameObject forgotPasswordPanel;
    [SerializeField] private GameObject termOfServicePanel;
    private GameObject[] _allPanels;

    private void Awake()
    {
        _allPanels = new[] { starterPanel, loginPanel, registerPanel, forgotPasswordPanel };
    }

    private void Start()
    {
        ShowStarter();
    }

    public void ShowStarter()        => SwitchTo(starterPanel);
    public void ShowLogin()          => SwitchTo(loginPanel);
    public void ShowRegister()       => SwitchTo(registerPanel);
    public void ShowForgotPassword() => SwitchTo(forgotPasswordPanel);
    public void ShowTermOfService() => SwitchTo(termOfServicePanel);
    private void SwitchTo(GameObject target)
    {
        foreach (var p in _allPanels)
            p.SetActive(p == target);
    }
}