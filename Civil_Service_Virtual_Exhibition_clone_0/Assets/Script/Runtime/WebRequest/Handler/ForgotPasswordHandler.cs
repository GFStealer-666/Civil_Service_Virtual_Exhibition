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
        if (string.IsNullOrEmpty(email)) { ShowFailedOverlay("กรุณากรอกอีเมล"); return; }
        StartCoroutine(DoReset(email));
    }

    private IEnumerator DoReset(string email)
    {
        submitBtn.interactable = false;

        string body = JsonUtility.ToJson(new ForgotPasswordRequestBody
        {
            email = email
        });

        yield return PostRequest(
            Api.ResetPasswordUrl,
            body,
            onSuccess: json =>
            {
                BaseResponse res = JsonUtility.FromJson<BaseResponse>(json);

                if (res == null)
                {
                    ShowFailedOverlay(
                        "รูปแบบข้อมูลตอบกลับไม่ถูกต้อง",
                        onDismissed: () => submitBtn.interactable = true
                    );
                    return;
                }

                if (!res.success)
                {
                    ShowFailedOverlay(
                        string.IsNullOrEmpty(res.message)
                            ? "ไม่พบอีเมลนี้ในระบบ"
                            : res.message,
                        onDismissed: () => submitBtn.interactable = true
                    );
                    return;
                }

                ShowSuccessOverlay(
                    "ส่งลิงก์ยืนยันไปทางอีเมล",
                    "กรุณาเข้าสู่ระบบ",
                    true,
                    () =>
                    {
                        submitBtn.interactable = true;
                        pageManager.ShowLogin();
                    }
                );
            },
            onError: _ =>
            {
                submitBtn.interactable = true;
            }
        );
    }
}