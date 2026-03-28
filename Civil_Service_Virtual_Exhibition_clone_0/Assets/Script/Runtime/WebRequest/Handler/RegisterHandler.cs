// RegisterHandler.cs
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RegisterHandler : BaseHandler
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

    [Header("Navigation")]
    [SerializeField] private LandingPageManager pageManager;

    private const int PlaceholderIndex = 0;

    private void Start()
    {
        submitBtn.onClick.AddListener(OnRegisterClicked);
        backBtn  .onClick.AddListener(pageManager.ShowLogin);

        emailInput          .onValueChanged.AddListener(_ => RefreshSubmitButton());
        characterNameInput  .onValueChanged.AddListener(_ => RefreshSubmitButton());
        passwordInput       .onValueChanged.AddListener(_ => RefreshSubmitButton());
        confirmPasswordInput.onValueChanged.AddListener(_ => RefreshSubmitButton());
        firstNameInput      .onValueChanged.AddListener(_ => RefreshSubmitButton());
        lastNameInput       .onValueChanged.AddListener(_ => RefreshSubmitButton());
        phoneInput          .onValueChanged.AddListener(_ => RefreshSubmitButton());
        departmentDropdown  .onValueChanged.AddListener(_ => RefreshSubmitButton());
        genderDropdown      .onValueChanged.AddListener(_ => RefreshSubmitButton());
        termsToggle         .onValueChanged.AddListener(_ => RefreshSubmitButton());

        RefreshSubmitButton();
    }

    private bool AllFieldsFilled()
    {
        return !string.IsNullOrEmpty(emailInput.text.Trim())
            && !string.IsNullOrEmpty(characterNameInput.text.Trim())
            && !string.IsNullOrEmpty(passwordInput.text)
            && !string.IsNullOrEmpty(confirmPasswordInput.text)
            && !string.IsNullOrEmpty(firstNameInput.text.Trim())
            && !string.IsNullOrEmpty(lastNameInput.text.Trim())
            && !string.IsNullOrEmpty(phoneInput.text.Trim())
            && departmentDropdown.value != PlaceholderIndex
            && genderDropdown.value     != PlaceholderIndex
            && termsToggle.isOn;
    }

    private void RefreshSubmitButton()
    {
        submitBtn.interactable = AllFieldsFilled();
    }
    private static void InjectPlaceholder(TMP_Dropdown dropdown, string label)
    {
        // Remove any existing placeholder we may have added before (idempotent)
        if (dropdown.options.Count > 0 && dropdown.options[0].text == label)
            return;

        dropdown.options.Insert(0, new TMP_Dropdown.OptionData(label));
        dropdown.value         = 0;
        dropdown.captionText.text = label;

        // Grey out the placeholder visually via a custom ItemTemplate trick —
        // the simplest Unity-friendly way is tinting the caption when index == 0
        dropdown.onValueChanged.AddListener(i =>
        {
            if (dropdown.captionText == null) return;
            dropdown.captionText.color = i == 0
                ? new Color(0.6f, 0.6f, 0.6f)   // grey placeholder
                : Color.black;                    // normal selected colour
        });

        // Apply grey immediately on start
        if (dropdown.captionText != null)
            dropdown.captionText.color = new Color(0.6f, 0.6f, 0.6f);
    }

    private void OnRegisterClicked()
    {
        string email      = emailInput.text.Trim();
        string charName   = characterNameInput.text.Trim();
        string password   = passwordInput.text;
        string confirm    = confirmPasswordInput.text;
        string firstName  = firstNameInput.text.Trim();
        string lastName   = lastNameInput.text.Trim();
        string phone      = phoneInput.text.Trim();
        var organizationSetup = departmentDropdown != null
            ? departmentDropdown.GetComponent<OrganizationDropdownSetup>()
            : null;

        string department = organizationSetup != null
            ? organizationSetup.GetSelectedOrganization()
            : departmentDropdown.options[departmentDropdown.value].text;

        // Delegate to the component that owns gender logic
        var genderSetup = genderDropdown.GetComponent<GenderDropdownSetup>();
        string gender   = genderSetup != null ? genderSetup.GetSelectedGender() : "male";

        if (password != confirm)
        {
            ShowFailedOverlay(L("รหัสผ่านไม่ตรงกัน", "Passwords do not match."));
            return;
        }
        

        StartCoroutine(DoRegister(email, charName, password, firstName, lastName, phone, department, gender));
    }

    private IEnumerator DoRegister(
        string email,
        string charName,
        string password,
        string firstName,
        string lastName,
        string phone,
        string department,
        string gender)
    {
        submitBtn.interactable = false;

        string body = JsonUtility.ToJson(new RegisterRequestBody
        {
            email = email,
            characterName = charName,
            password = password,
            firstName = firstName,
            lastName = lastName,
            department = department,
            phone = phone,
            gender = gender
        });

        yield return PostRequest(
            Api.RegisterUrl,
            body,
            onSuccess: json =>
            {
                BaseResponse res = JsonUtility.FromJson<BaseResponse>(json);

                if (res == null)
                {
                    ShowFailedOverlay(
                        L("รูปแบบข้อมูลตอบกลับไม่ถูกต้อง", "Invalid response format."),
                        onDismissed: () => submitBtn.interactable = AllFieldsFilled()
                    );
                    return;
                }

                if (!res.success)
                {
                    ShowFailedOverlay(
                        string.IsNullOrEmpty(res.message)
                            ? L("สมัครสมาชิกไม่สำเร็จ", "Registration failed.")
                            : res.message,
                        onDismissed: () => submitBtn.interactable = AllFieldsFilled()
                    );
                    return;
                }

                ShowSuccessOverlay(
                L("สมัครสมาชิกสำเร็จ", "Registration successful"),
                L("กรุณาเข้าสู่ระบบ", "Please log in"),
                true,
                () =>
                {
                    submitBtn.interactable = true;
                    ClearRegisterForm();
                    pageManager.ShowLogin();
                }
            );
            },
            onError: _ =>
            {
                submitBtn.interactable = AllFieldsFilled();
            }
        );
    }
    private void ClearRegisterForm()
    {
        emailInput.text = "";
        characterNameInput.text = "";
        passwordInput.text = "";
        confirmPasswordInput.text = "";
        firstNameInput.text = "";
        lastNameInput.text = "";
        phoneInput.text = "";

        departmentDropdown.value = PlaceholderIndex;
        genderDropdown.value = PlaceholderIndex;
        termsToggle.isOn = false;

        RefreshSubmitButton();
    }
}