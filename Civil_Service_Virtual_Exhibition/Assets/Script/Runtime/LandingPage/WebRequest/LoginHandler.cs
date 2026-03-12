// LoginHandler.cs
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LoginHandler : BaseHandler
{
    [Header("Fields")]
    [SerializeField] private TMP_InputField  emailInput;
    [SerializeField] private TMP_InputField  passwordInput;
    [SerializeField] private Button          submitBtn;
    [SerializeField] private Button          guestBtn;
    [SerializeField] private TMProLinkButton toRegisterBtn;
    [SerializeField] private TMProLinkButton forgotPasswordBtn;

    [Header("Navigation")]
    [SerializeField] private LandingPageManager pageManager;

    private void Start()
    {
        submitBtn        .onClick.AddListener(OnLoginClicked);
        guestBtn         .onClick.AddListener(OnGuestClicked);
        toRegisterBtn    .onLinkClicked.AddListener(pageManager.ShowRegister);
        forgotPasswordBtn.onLinkClicked.AddListener(pageManager.ShowForgotPassword);
    }

    // ── Login ───────────────────────────────────────────────────

    private void OnLoginClicked()
    {
        string email    = emailInput.text.Trim();
        string password = passwordInput.text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            overlay.ShowError("กรุณากรอกอีเมลและรหัสผ่าน");
            return;
        }

        StartCoroutine(DoLogin(email, password));
    }

    private IEnumerator DoLogin(string email, string password)
    {
        SetButtons(false);
        string body = JsonUtility.ToJson(new LoginRequestBody { email = email, password = password });

        yield return PostRequest(api.LoginUrl, body,
            onSuccess: json =>
            {
                var res = JsonUtility.FromJson<LoginResponse>(json);
                if (!res.success)
                {
                    overlay.ShowError(
                        string.IsNullOrEmpty(res.message)
                            ? "อีเมลหรือรหัสผ่านไม่ถูกต้อง"
                            : res.message);
                    SetButtons(true);
                    return;
                }
                var p = res.data.player;
                EnterMainScene(p.characterName, p.gender, p.department, p.isAnonymous);
            },
            onError: _ => SetButtons(true));
    }

    // ── Guest ───────────────────────────────────────────────────

    public void OnGuestClicked() => StartCoroutine(DoGuest());

    private IEnumerator DoGuest()
    {
        SetButtons(false);

        yield return PostRequest(api.AnonymousUrl, "{}",
            onSuccess: json =>
            {
                string guestName = "Guest_" + Random.Range(1000, 9999);
                var res = JsonUtility.FromJson<LoginResponse>(json);
                if (res.success && !string.IsNullOrEmpty(res.data?.player.characterName))
                    guestName = res.data.player.characterName;

                EnterMainScene(guestName, "male", "", isGuest: true);
            },
            onError: _ => SetButtons(true));
    }

    private void SetButtons(bool interactable)
    {
        submitBtn.interactable = interactable;
        guestBtn .interactable = interactable;
    }
}