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
        string email = emailInput.text.Trim();
        string password = passwordInput.text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            ShowFailedOverlay("กรุณากรอกอีเมลและรหัสผ่าน");
            return;
        }

        StartCoroutine(DoLogin(email, password));
    }

    private IEnumerator DoLogin(string email, string password)
    {
        SetButtons(false);

        string body = JsonUtility.ToJson(new LoginRequestBody
        {
            email = email,
            password = password
        });

        yield return PostRequest(
            Api.LoginUrl,
            body,
            onSuccess: json =>
            {
                LoginResponse res = JsonUtility.FromJson<LoginResponse>(json);

                if (res == null)
                {
                    ShowFailedOverlay("รูปแบบข้อมูลตอบกลับไม่ถูกต้อง");
                    SetButtons(true);
                    return;
                }

                if (!res.success)
                {
                    ShowFailedOverlay(
                        string.IsNullOrEmpty(res.message)
                            ? "อีเมลหรือรหัสผ่านไม่ถูกต้อง"
                            : res.message
                    );
                    SetButtons(true);
                    return;
                }

                PlayerData player = res.data != null ? res.data.player : null;
                if (player == null)
                {
                    ShowFailedOverlay("ไม่พบข้อมูลผู้ใช้งาน");
                    SetButtons(true);
                    return;
                }

                EnterMainScene(player);
            },
            onError: _ =>
            {
                SetButtons(true);
            });
    }


    // ── Guest ───────────────────────────────────────────────────

    public void OnGuestClicked()
    {
        StartCoroutine(GuestLogin());
    }

    private IEnumerator GuestLogin()
    {
        SetButtons(false);

        yield return PostRequest(
            Api.AnonymousUrl,
            "{}",
            onSuccess: json =>
            {
                LoginResponse res = JsonUtility.FromJson<LoginResponse>(json);
                Debug.Log($"[LoginHandler] {json}");

                if (res == null)
                {
                    ShowFailedOverlay("รูปแบบข้อมูลตอบกลับไม่ถูกต้อง");
                    SetButtons(true);
                    return;
                }

                if (!res.success)
                {
                    ShowFailedOverlay(
                        string.IsNullOrEmpty(res.message)
                            ? "ไม่สามารถเข้าใช้งานแบบ Guest ได้"
                            : res.message
                    );
                    SetButtons(true);
                    return;
                }

                PlayerData sourcePlayer = res.data != null ? res.data.player : null;
                string token = res.data != null ? res.data.token : null;

                PlayerData player = sourcePlayer ?? new PlayerData();

                if (string.IsNullOrWhiteSpace(player.characterName))
                    player.characterName = "Guest_" + UnityEngine.Random.Range(1000, 9999);

                if (string.IsNullOrWhiteSpace(player.gender))
                    player.gender = PlayerGender.Male.ToString();

                if (string.IsNullOrWhiteSpace(player.id) && sourcePlayer != null)
                    player.id = sourcePlayer.id;

                if (string.IsNullOrWhiteSpace(player.token))
                    player.token = token;

                player.isAnonymous = true;

                if (string.IsNullOrWhiteSpace(player.department))
                    player.department = "ไม่มีข้อมูลเนื่องจากไม่ได้ล็อกอิน";

                if (string.IsNullOrWhiteSpace(player.email))
                    player.email = "ไม่มีข้อมูลเนื่องจากไม่ได้ล็อกอิน";

                if (string.IsNullOrWhiteSpace(player.phone))
                    player.phone = "ไม่มีข้อมูลเนื่องจากไม่ได้ล็อกอิน";

                EnterMainScene(player);
            },
            onError: _ =>
            {
                SetButtons(true);
            });
    }

    private void SetButtons(bool interactable)
    {
        submitBtn.interactable = interactable;
        guestBtn .interactable = interactable;
    }
}