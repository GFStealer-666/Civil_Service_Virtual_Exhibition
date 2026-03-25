using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoginHandler : BaseHandler
{
    [Header("Fields")]
    [SerializeField] private TMP_InputField emailInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private Button submitBtn;
    [SerializeField] private Button guestBtn;
    [SerializeField] private TMProLinkButton toRegisterBtn;
    [SerializeField] private TMProLinkButton forgotPasswordBtn;

    [Header("Navigation")]
    [SerializeField] private LandingPageManager pageManager;

    private void Start()
    {
        if (submitBtn != null)
            submitBtn.onClick.AddListener(OnLoginClicked);

        if (guestBtn != null)
            guestBtn.onClick.AddListener(OnGuestClicked);

        if (toRegisterBtn != null && pageManager != null)
            toRegisterBtn.onLinkClicked.AddListener(pageManager.ShowRegister);

        if (forgotPasswordBtn != null && pageManager != null)
            forgotPasswordBtn.onLinkClicked.AddListener(pageManager.ShowForgotPassword);
    }

    private void OnDestroy()
    {
        if (submitBtn != null)
            submitBtn.onClick.RemoveListener(OnLoginClicked);

        if (guestBtn != null)
            guestBtn.onClick.RemoveListener(OnGuestClicked);

        if (toRegisterBtn != null && pageManager != null)
            toRegisterBtn.onLinkClicked.RemoveListener(pageManager.ShowRegister);

        if (forgotPasswordBtn != null && pageManager != null)
            forgotPasswordBtn.onLinkClicked.RemoveListener(pageManager.ShowForgotPassword);
    }

    private void OnLoginClicked()
    {
        string email = emailInput != null ? emailInput.text.Trim() : string.Empty;
        string password = passwordInput != null ? passwordInput.text : string.Empty;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
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
                Debug.Log($"[LoginHandler] Login response: {json}");

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
                        string.IsNullOrWhiteSpace(res.message)
                            ? "อีเมลหรือรหัสผ่านไม่ถูกต้อง"
                            : res.message
                    );
                    SetButtons(true);
                    return;
                }

                if (res.data == null || res.data.player == null)
                {
                    ShowFailedOverlay("ไม่พบข้อมูลผู้ใช้งาน");
                    SetButtons(true);
                    return;
                }

                EnterMainScene(res.data);
            },
            onError: _ =>
            {
                SetButtons(true);
            });
    }

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
                Debug.Log($"[LoginHandler] Guest response: {json}");

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
                        string.IsNullOrWhiteSpace(res.message)
                            ? "ไม่สามารถเข้าใช้งานแบบ Guest ได้"
                            : res.message
                    );
                    SetButtons(true);
                    return;
                }

                if (res.data == null)
                {
                    ShowFailedOverlay("ไม่พบข้อมูลการเข้าสู่ระบบ");
                    SetButtons(true);
                    return;
                }

                if (res.data.player == null)
                    res.data.player = new PlayerData();

                PlayerData player = res.data.player;

                if (string.IsNullOrWhiteSpace(player.characterName))
                    player.characterName = $"Guest_{UnityEngine.Random.Range(1000, 9999)}";

                if (string.IsNullOrWhiteSpace(player.gender))
                    player.gender = PlayerGender.Male.ToString();

                if (string.IsNullOrWhiteSpace(player.department))
                    player.department = "ไม่มีข้อมูลเนื่องจากไม่ได้ล็อกอิน";

                if (string.IsNullOrWhiteSpace(player.email))
                    player.email = "ไม่มีข้อมูลเนื่องจากไม่ได้ล็อกอิน";

                if (string.IsNullOrWhiteSpace(player.phone))
                    player.phone = "ไม่มีข้อมูลเนื่องจากไม่ได้ล็อกอิน";

                player.isAnonymous = true;

                EnterMainScene(res.data);
            },
            onError: _ =>
            {
                SetButtons(true);
            });
    }

    private void SetButtons(bool interactable)
    {
        if (submitBtn != null)
            submitBtn.interactable = interactable;

        if (guestBtn != null)
            guestBtn.interactable = interactable;
    }
}