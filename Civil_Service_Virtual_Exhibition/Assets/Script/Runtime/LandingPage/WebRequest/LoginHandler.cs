// LoginHandler.cs
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class LoginHandler : MonoBehaviour
{
    [Header("Fields")]
    [SerializeField] private TMP_InputField  emailInput;
    [SerializeField] private TMP_InputField  passwordInput;
    [SerializeField] private Button          submitBtn;
    [SerializeField] private Button          guestBtn;
    [SerializeField] private TMProLinkButton toRegisterBtn;
    [SerializeField] private TMProLinkButton forgotPasswordBtn;
    [SerializeField] private TMP_Text        errorText;
    [SerializeField] private GameObject      loadingOverlay;

    [Header("Navigation")]
    [SerializeField] private LandingPageManager pageManager;

    [Header("API")]
    [SerializeField] private string apiBaseUrl      = "https://your-api.example.com";
    [SerializeField] private string loginEndpoint   = "/api/auth/login";
    [SerializeField] private string anonymousEndpoint = "/anonymous";
    [SerializeField] private string mainSceneName   = "MainScene";

    private void Start()
    {
        submitBtn        .onClick.AddListener(OnLoginClicked);
        guestBtn         .onClick.AddListener(OnGuestClicked);
        toRegisterBtn    .onLinkClicked.AddListener(pageManager.ShowRegister);
        forgotPasswordBtn.onLinkClicked.AddListener(pageManager.ShowForgotPassword);

        ClearError();
    }

    private void OnLoginClicked()
    {
        string email    = emailInput.text.Trim();
        string password = passwordInput.text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            ShowError("กรุณากรอกอีเมลและรหัสผ่าน");
            return;
        }

        StartCoroutine(LoginRequest(email, password));
    }

    private IEnumerator LoginRequest(string email, string password)
    {
        SetLoading(true);

        var body = JsonUtility.ToJson(new LoginRequestBody { email = email, password = password });
        using var req = APIHelper.PostJson(apiBaseUrl + loginEndpoint, body);
        Debug.Log(apiBaseUrl + loginEndpoint);
        yield return req.SendWebRequest();
        Debug.Log($"Code={req.responseCode} Error={req.error} Body={req.downloadHandler.text}");
        SetLoading(false);

        if (req.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
        {
            ShowError("เชื่อมต่อเซิร์ฟเวอร์ไม่ได้ กรุณาลองใหม่");
            yield break;
        }

        var response = JsonUtility.FromJson<LoginResponse>(req.downloadHandler.text);
        if (!response.success)
        {
            Debug.Log($"[LoginHandler] Login failed {response.message} ");
            ShowError(string.IsNullOrEmpty(response.message) ? "อีเมลหรือรหัสผ่านไม่ถูกต้อง" : response.message);
            yield break;
        }

        EnterScene(response.data.player.characterName, response.data.player.gender, response.data.player.department, isGuest: response.data.player.isAnonymous);

    }

    public void OnGuestClicked()
    {
        StartCoroutine(GuestRequest());
    }

    private IEnumerator GuestRequest()
    {
        SetLoading(true);

        using var req = APIHelper.PostJson(apiBaseUrl + anonymousEndpoint, "{}");
        yield return req.SendWebRequest();

        SetLoading(false);

        string guestName = "Guest_" + Random.Range(1000, 9999); // fallback

        if (req.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
        {
            var response = JsonUtility.FromJson<LoginResponse>(req.downloadHandler.text);

            // Only use API name if it's actually not empty
            if (response.success && !string.IsNullOrEmpty(response.data?.player.characterName))
                guestName = response.data.player.characterName;
        }

        Debug.Log($"[EnterScene] Resolved guest name: {guestName}");
        EnterScene(guestName, "male", "", isGuest: true);
    }

    private void EnterScene(string playerName, string gender, string department, bool isGuest)
    {
        var data = LocalPlayerData.Instance;

        if (data == null)
        {
            var go = new GameObject("LocalPlayerData");
            data = go.AddComponent<LocalPlayerData>();
        }

        data.PlayerName   = playerName;
        data.Organization = department;
        data.IsGuest      = isGuest;
        data.Gender       = gender == "female" ? PlayerGender.Female : PlayerGender.Male;

        Debug.Log($"[EnterScene] Name={data.PlayerName} Guest={data.IsGuest}");
        SceneManager.LoadScene(mainSceneName);
    }

    private void SetLoading(bool active)
    {
        if (loadingOverlay != null) loadingOverlay.SetActive(active);
        submitBtn.interactable = !active;
        guestBtn .interactable = !active;
    }

    private void ShowError(string msg)
    {
        if (errorText == null) return;
        errorText.text = msg;
        errorText.gameObject.SetActive(true);
    }

    public void ClearError()
    {
        if (errorText == null) return;
        errorText.text = "";
        errorText.gameObject.SetActive(false);
    }
}