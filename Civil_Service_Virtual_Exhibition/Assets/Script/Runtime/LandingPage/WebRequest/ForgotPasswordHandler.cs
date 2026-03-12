// ForgotPasswordHandler.cs
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ForgotPasswordHandler : MonoBehaviour
{
    [Header("Fields")]
    [SerializeField] private TMP_InputField emailInput;
    [SerializeField] private Button         submitBtn;
    [SerializeField] private Button         backBtn;
    [SerializeField] private TMP_Text       statusText;
    [SerializeField] private GameObject     loadingOverlay;

    [Header("Navigation")]
    [SerializeField] private LandingPageManager pageManager;

    [Header("API")]
    [SerializeField] private string apiBaseUrl            = "https://your-api.example.com";
    [SerializeField] private string resetPasswordEndpoint = "/api/auth/reset-password";

    private void Start()
    {
        submitBtn.onClick.AddListener(OnSubmitClicked);
        backBtn  .onClick.AddListener(pageManager.ShowLogin);
        ClearStatus();
    }

    private void OnSubmitClicked()
    {
        string email = emailInput.text.Trim();
        if (string.IsNullOrEmpty(email)) { ShowStatus("กรุณากรอกอีเมล", isError: true); return; }
        StartCoroutine(ResetRequest(email));
    }

    private IEnumerator ResetRequest(string email)
    {
        SetLoading(true);

        var body = JsonUtility.ToJson(new ForgotPasswordRequestBody { email = email });
        using var req = APIHelper.PostJson(apiBaseUrl + resetPasswordEndpoint, body);
        yield return req.SendWebRequest();

        SetLoading(false);

        if (req.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
        {
            ShowStatus("เชื่อมต่อเซิร์ฟเวอร์ไม่ได้ กรุณาลองใหม่", isError: true);
            yield break;
        }

        var response = JsonUtility.FromJson<BaseResponse>(req.downloadHandler.text);

        ShowStatus(
            response.success
                ? "ส่งลิงก์รีเซ็ตรหัสผ่านไปที่อีเมลของท่านแล้ว"
                : (string.IsNullOrEmpty(response.message) ? "ไม่พบอีเมลนี้ในระบบ" : response.message),
            isError: !response.success
        );
    }

    private void SetLoading(bool active)
    {
        if (loadingOverlay != null) loadingOverlay.SetActive(active);
        submitBtn.interactable = !active;
    }

    private void ShowStatus(string msg, bool isError)
    {
        if (statusText == null) return;
        statusText.text  = msg;
        statusText.color = isError ? Color.red : Color.green;
        statusText.gameObject.SetActive(true);
    }

    public void ClearStatus()
    {
        if (statusText == null) return;
        statusText.text = "";
        statusText.gameObject.SetActive(false);
    }
}