// ForgotPasswordHandler.cs
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ForgotPasswordHandler : BaseHandler
{
    [Header("Fields")]
    [SerializeField] private TMP_InputField emailInput;
    [SerializeField] private Button         submitBtn;
    [SerializeField] private Button         backBtn;

    [Header("Navigation")]
    [SerializeField] private LandingPageManager pageManager;

    private void Start()
    {
        submitBtn.onClick.AddListener(OnSubmitClicked);
        backBtn  .onClick.AddListener(pageManager.ShowLogin);
    }

    private void OnSubmitClicked()
    {
        string email = emailInput.text.Trim();
        if (string.IsNullOrEmpty(email)) { overlay.ShowError("กรุณากรอกอีเมล"); return; }
        StartCoroutine(DoReset(email));
    }

    private IEnumerator DoReset(string email)
    {
        submitBtn.interactable = false;
        string body = JsonUtility.ToJson(new ForgotPasswordRequestBody { email = email });

        yield return PostRequest(api.ResetPasswordUrl, body,
            onSuccess: json =>
            {
                var res = JsonUtility.FromJson<BaseResponse>(json);
                if (!res.success)
                {
                    overlay.ShowError(
                        string.IsNullOrEmpty(res.message)
                            ? "ไม่พบอีเมลนี้ในระบบ"
                            : res.message,
                        onDismissed: () => submitBtn.interactable = true);
                    return;
                }
                // Show success, then go back to login after dismiss
                overlay.ShowSuccessDismiss(
                   "ส่งลิ้งยืนยันไปทางอีเมล",
                    "กรุณาเข้าสู่ระบบ",     
                () =>
                {
                    submitBtn.interactable = true;
                    pageManager.ShowLogin();
                });
            },
            onError: _ => submitBtn.interactable = true);
    }
}