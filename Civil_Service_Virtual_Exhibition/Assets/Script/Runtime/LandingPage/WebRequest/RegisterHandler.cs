// RegisterHandler.cs
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class RegisterHandler : MonoBehaviour
{
    [Header("Fields")]
    [SerializeField] private TMP_InputField emailInput;
    [SerializeField] private TMP_InputField characterNameInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private TMP_InputField confirmPasswordInput;
    [SerializeField] private TMP_InputField firstNameInput;
    [SerializeField] private TMP_InputField lastNameInput;
    [SerializeField] private TMP_InputField phoneInput;
    [SerializeField] private TMP_Dropdown   departmentDropdown;
    [SerializeField] private TMP_Dropdown   genderDropdown;
    [SerializeField] private Toggle         termsToggle;
    [SerializeField] private Button         submitBtn;
    [SerializeField] private Button         backBtn;
    [SerializeField] private TMP_Text       errorText;
    [SerializeField] private GameObject     loadingOverlay;

    [Header("Navigation")]
    [SerializeField] private LandingPageManager pageManager;

    [Header("API")]
    [SerializeField] private string apiBaseUrl         = "https://thaicivil.mxrth.co/api/game";
    [SerializeField] private string registerEndpoint   = "/register";
    [SerializeField] private string mainSceneName      = "MainScene";

    private void Start()
    {
        submitBtn .onClick.AddListener(OnRegisterClicked);
        backBtn.onClick.AddListener(pageManager.ShowLogin);
        ClearError();
    }

    private void OnRegisterClicked()
    {
        string email    = emailInput.text.Trim();
        string charName = characterNameInput.text.Trim();
        string password = passwordInput.text;
        string confirm  = confirmPasswordInput.text;
        string firstName = firstNameInput.text.Trim();
        string lastName  = lastNameInput.text.Trim();
        string phone     = phoneInput.text.Trim();
        string department = departmentDropdown.options[departmentDropdown.value].text;

        // Convert dropdown to API value
        int genderIndex = genderDropdown.value;
        string gender = genderIndex == 1 ? "female" : "male";  // 0=placeholder/male, 1=ชาย, 2=หญิง — adjust to your order

        if (string.IsNullOrEmpty(email))    { ShowError("กรุณากรอกอีเมล"); return; }
        if (string.IsNullOrEmpty(charName)) { ShowError("กรุณากรอกชื่อตัวละคร"); return; }
        if (string.IsNullOrEmpty(password)) { ShowError("กรุณากรอกรหัสผ่าน"); return; }
        if (password != confirm)            { ShowError("รหัสผ่านไม่ตรงกัน"); return; }
        if (!termsToggle.isOn)              { ShowError("กรุณายอมรับเงื่อนไขการใช้งาน"); return; }

        StartCoroutine(RegisterRequest(email, charName, password, firstName, lastName, phone, department, gender));
    }

    private IEnumerator RegisterRequest(
        string email, string charName, string password,
        string firstName, string lastName, string phone,
        string department, string gender)
    {
        SetLoading(true);

        var body = JsonUtility.ToJson(new RegisterRequestBody
        {
            email         = email,
            characterName = charName,
            password      = password,
            firstName     = firstName,
            lastName      = lastName,
            department    = department,
            phone         = phone,
            gender        = gender        // "male" or "female"
        });

        using var req = APIHelper.PostJson(apiBaseUrl + registerEndpoint, body);
        Debug.Log($"[RegisterHandler] + {req}");
        yield return req.SendWebRequest();

        SetLoading(false);

        if (req.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
        {
            Debug.Log("[Register Handler] : Unable to connect to server");
            ShowError("เชื่อมต่อเซิร์ฟเวอร์ไม่ได้ กรุณาลองใหม่");
            yield break;
        }

        var response = JsonUtility.FromJson<BaseResponse>(req.downloadHandler.text);
        if (!response.success)
        {
            Debug.Log($"[Register Handler] Failed to register : {response.message}");
            ShowError(string.IsNullOrEmpty(response.message) ? "สมัครสมาชิกไม่สำเร็จ" : response.message);
            yield break;
        }

        var data          = LocalPlayerData.Instance;
        data.PlayerName   = charName;
        data.Organization = department;
        data.IsGuest      = false;
        data.Gender       = gender == "female" ? PlayerGender.Female : PlayerGender.Male;

        SceneManager.LoadScene(mainSceneName);
    }

    private void SetLoading(bool active)
    {
        if (loadingOverlay != null) loadingOverlay.SetActive(active);
        submitBtn.interactable = !active;
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