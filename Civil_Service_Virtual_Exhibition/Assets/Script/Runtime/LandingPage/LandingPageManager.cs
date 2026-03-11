using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controls the LandingPage scene UI flow:
///   Login  →  set LocalPlayerData  →  LoadScene("MainScene")
///   Register → (web request) → auto-login
///   Guest  →  set guest LocalPlayerData  →  LoadScene("MainScene")
/// </summary>
public class LandingPageManager : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string mainSceneName = "MainScene";

    [Header("Panels")]
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private GameObject registerPanel;
    [SerializeField] private GameObject loadingOverlay;   // spinner shown during requests

    [Header("Login Fields")]
    [SerializeField] private TMP_InputField loginEmailInput;
    [SerializeField] private TMP_InputField loginPasswordInput;
    [SerializeField] private Button         loginSubmitBtn;
    [SerializeField] private Button         guestBtn;
    [SerializeField] private Button         toRegisterBtn;
    [SerializeField] private TMP_Text       loginErrorText;

    [Header("Register Fields")]
    [SerializeField] private TMP_InputField regEmailInput;
    [SerializeField] private TMP_InputField regCharacterNameInput;
    [SerializeField] private TMP_InputField regPasswordInput;
    [SerializeField] private TMP_InputField regConfirmPasswordInput;
    [SerializeField] private TMP_InputField regFirstNameInput;
    [SerializeField] private TMP_InputField regLastNameInput;
    [SerializeField] private TMP_InputField regPhoneInput;

    // Dropdowns — use TMP_Dropdown (or your custom DropdownController)
    [SerializeField] private TMP_Dropdown   regOrganizationDropdown;
    [SerializeField] private TMP_Dropdown   regGenderDropdown;

    [SerializeField] private Toggle         regTermsToggle;
    [SerializeField] private Button         regSubmitBtn;
    [SerializeField] private Button         toLoginBtn;
    [SerializeField] private TMP_Text       registerErrorText;

    // ─── API endpoints (set from inspector or override per environment) ──────
    [Header("API")]
    [SerializeField] private string apiBaseUrl   = "https://your-api.example.com";
    [SerializeField] private string loginEndpoint    = "/api/auth/login";
    [SerializeField] private string registerEndpoint = "/api/auth/register";


    private void Start()
    {
        // Default state
        ShowLogin();

        loginSubmitBtn .onClick.AddListener(OnLoginClicked);
        guestBtn       .onClick.AddListener(OnGuestClicked);
        toRegisterBtn  .onClick.AddListener(ShowRegister);
        regSubmitBtn   .onClick.AddListener(OnRegisterClicked);
        toLoginBtn     .onClick.AddListener(ShowLogin);

        ClearErrors();
        SetLoading(false);
    }

    public void ShowLogin()
    {
        loginPanel   .SetActive(true);
        registerPanel.SetActive(false);
        ClearErrors();
    }

    public void ShowRegister()
    {
        loginPanel   .SetActive(false);
        registerPanel.SetActive(true);
        ClearErrors();
    }

    private void OnLoginClicked()
    {
        string email    = loginEmailInput.text.Trim();
        string password = loginPasswordInput.text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            ShowLoginError("กรุณากรอกอีเมลและรหัสผ่าน");
            return;
        }

        StartCoroutine(LoginRequest(email, password));
    }

    private IEnumerator LoginRequest(string email, string password)
    {
        SetLoading(true);
        ClearErrors();

        var body = JsonUtility.ToJson(new LoginRequestBody { email = email, password = password });

        using var req = APIHelper.PostJson(apiBaseUrl + loginEndpoint, body);
        yield return req.SendWebRequest();

        SetLoading(false);

        if (req.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
        {
            ShowLoginError("เชื่อมต่อเซิร์ฟเวอร์ไม่ได้ กรุณาลองใหม่");
            Debug.LogError($"[Login] {req.error}");
            yield break;
        }

        var response = JsonUtility.FromJson<LoginResponse>(req.downloadHandler.text);

        if (!response.success)
        {
            ShowLoginError(string.IsNullOrEmpty(response.message) ? "อีเมลหรือรหัสผ่านไม่ถูกต้อง" : response.message);
            yield break;
        }

        ApplyPlayerDataAndEnter(
            playerName    : response.data.characterName,
            genderString  : response.data.gender,
            organization  : response.data.organization,
            isGuest       : false
        );
    }

    private void OnRegisterClicked()
    {
        // ── Validation ──
        string email       = regEmailInput.text.Trim();
        string charName    = regCharacterNameInput.text.Trim();
        string password    = regPasswordInput.text;
        string confirm     = regConfirmPasswordInput.text;
        string firstName   = regFirstNameInput.text.Trim();
        string lastName    = regLastNameInput.text.Trim();
        string phone       = regPhoneInput.text.Trim();
        string org         = regOrganizationDropdown.options[regOrganizationDropdown.value].text;
        string gender      = regGenderDropdown.options[regGenderDropdown.value].text;

        if (string.IsNullOrEmpty(email))    { ShowRegisterError("กรุณากรอกอีเมล"); return; }
        if (string.IsNullOrEmpty(charName)) { ShowRegisterError("กรุณากรอกชื่อตัวละคร"); return; }
        if (string.IsNullOrEmpty(password)) { ShowRegisterError("กรุณากรอกรหัสผ่าน"); return; }
        if (password != confirm)            { ShowRegisterError("รหัสผ่านไม่ตรงกัน"); return; }
        if (!regTermsToggle.isOn)           { ShowRegisterError("กรุณายอมรับเงื่อนไขการใช้งาน"); return; }

        StartCoroutine(RegisterRequest(email, charName, password, firstName, lastName, phone, org, gender));
    }

    private IEnumerator RegisterRequest(
        string email, string charName, string password,
        string firstName, string lastName, string phone,
        string org, string gender)
    {
        SetLoading(true);
        ClearErrors();

        var body = JsonUtility.ToJson(new RegisterRequestBody
        {
            email         = email,
            characterName = charName,
            password      = password,
            firstName     = firstName,
            lastName      = lastName,
            phone         = phone,
            organization  = org,
            gender        = gender
        });

        using var req = APIHelper.PostJson(apiBaseUrl + registerEndpoint, body);
        yield return req.SendWebRequest();

        SetLoading(false);

        if (req.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
        {
            ShowRegisterError("เชื่อมต่อเซิร์ฟเวอร์ไม่ได้ กรุณาลองใหม่");
            Debug.LogError($"[Register] {req.error}");
            yield break;
        }

        var response = JsonUtility.FromJson<RegisterResponse>(req.downloadHandler.text);

        if (!response.success)
        {
            ShowRegisterError(string.IsNullOrEmpty(response.message) ? "สมัครสมาชิกไม่สำเร็จ" : response.message);
            yield break;
        }

        // Auto-enter after successful registration
        ApplyPlayerDataAndEnter(
            playerName   : charName,
            genderString : gender,
            organization : org,
            isGuest      : false
        );
    }



    private void OnGuestClicked()
    {
        // Generate a temporary guest name
        string guestName = "Guest_" + Random.Range(1000, 9999);
        ApplyPlayerDataAndEnter(playerName: guestName, genderString: "ชาย", organization: "", isGuest: true);
    }


    private void ApplyPlayerDataAndEnter(string playerName, string genderString, string organization, bool isGuest)
    {
        var data = LocalPlayerData.Instance;

        data.PlayerName   = playerName;
        data.Organization = organization;
        data.IsGuest      = isGuest;
        data.Gender       = genderString switch
        {
            "หญิง"   => PlayerGender.Female,
            "Female" => PlayerGender.Female,
            _        => PlayerGender.Male
        };

        Debug.Log($"[LandingPage] Entering as '{playerName}' | Gender: {data.Gender} | Guest: {isGuest}");

        SceneManager.LoadScene(mainSceneName);
    }


    private void SetLoading(bool active)
    {
        if (loadingOverlay != null)
            loadingOverlay.SetActive(active);

        loginSubmitBtn.interactable  = !active;
        guestBtn.interactable        = !active;
        regSubmitBtn.interactable    = !active;
    }

    private void ShowLoginError(string msg)
    {
        if (loginErrorText != null) { loginErrorText.text = msg; loginErrorText.gameObject.SetActive(true); }
    }

    private void ShowRegisterError(string msg)
    {
        if (registerErrorText != null) { registerErrorText.text = msg; registerErrorText.gameObject.SetActive(true); }
    }

    private void ClearErrors()
    {
        if (loginErrorText    != null) { loginErrorText.text    = ""; loginErrorText.gameObject.SetActive(false); }
        if (registerErrorText != null) { registerErrorText.text = ""; registerErrorText.gameObject.SetActive(false); }
    }
}