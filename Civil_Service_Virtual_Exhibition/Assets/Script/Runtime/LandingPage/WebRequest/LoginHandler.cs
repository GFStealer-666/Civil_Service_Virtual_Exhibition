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
                EnterMainScene(p);
            },
            onError: _ => SetButtons(true));
    }

    // ── Guest ───────────────────────────────────────────────────

    public void OnGuestClicked() => StartCoroutine(GuestLogin());

    private IEnumerator GuestLogin()
    {
        SetButtons(false);

        yield return PostRequest(api.AnonymousUrl, "{}",
            onSuccess: json =>
            {
                var res = JsonUtility.FromJson<LoginResponse>(json);
                Debug.Log($"[Login Handler] {json}");
                if (!res.success)
                {
                    overlay.ShowError(
                        string.IsNullOrEmpty(res.message)
                            ? "ไม่สามารถเข้าใช้งานแบบ Guest ได้"
                            : res.message);
                    SetButtons(true);
                    return;
                }
                // create dto 
                var player = res.data != null ? res.data.player : null;
                if (player == null)
                    player = new PlayerData();

                if (string.IsNullOrWhiteSpace(player.characterName))
                    player.characterName = "Guest_" + Random.Range(1000, 9999);

                if (string.IsNullOrWhiteSpace(player.gender))
                    player.gender = PlayerGender.Male.ToString();

                player.id ??= res.data.player.id;
                player.token ??= res.data.token;
                player.isAnonymous = true;
                player.department ??= "ไม่มีข้อมูลเนื่องจากไม่ได้ล็อคอิน";
                player.email ??= "ไม่มีข้อมูลเนื่องจากไม่ได้ล็อคอิน";
                player.phone ??= "ไม่มีข้อมูลเนื่องจากไม่ได้ล็อคอิน";

                EnterMainScene(player);
            },
            onError: _ => SetButtons(true));
    }

    private void SetButtons(bool interactable)
    {
        submitBtn.interactable = interactable;
        guestBtn .interactable = interactable;
    }
}