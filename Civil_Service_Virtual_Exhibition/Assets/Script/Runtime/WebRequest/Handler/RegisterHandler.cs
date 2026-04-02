// RegisterHandler.cs
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text.RegularExpressions;
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

    private void OnRegisterClicked()
    {
        string email = emailInput.text.Trim();
        string charName = characterNameInput.text.Trim();
        string password = passwordInput.text;
        string confirm = confirmPasswordInput.text;
        string firstName = firstNameInput.text.Trim();
        string lastName = lastNameInput.text.Trim();
        string phone = phoneInput.text.Trim();

        var organizationSetup = departmentDropdown != null
            ? departmentDropdown.GetComponent<OrganizationDropdownSetup>()
            : null;

        string department = organizationSetup != null
            ? organizationSetup.GetSelectedOrganization()
            : departmentDropdown.options[departmentDropdown.value].text;

        var genderSetup = genderDropdown.GetComponent<GenderDropdownSetup>();
        string gender = genderSetup != null ? genderSetup.GetSelectedGender() : string.Empty;

        if (!TryValidateRegisterForm(
                email,
                charName,
                password,
                confirm,
                firstName,
                lastName,
                phone,
                department,
                gender,
                out string validationMessage))
        {
            ShowFailedOverlay(validationMessage);
            return;
        }

        StartCoroutine(DoRegister(
            email,
            charName,
            password,
            firstName,
            lastName,
            phone,
            department,
            gender
        ));
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
                Debug.Log($"[Register] raw json = {json}");

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
                        GetRegisterFailureMessage(res.DisplayMessage),
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

    private string GetRegisterFailureMessage(string apiMessage)
    {
        switch (apiMessage)
        {
            case "กรุณากรอกอีเมล":
                return L("กรุณากรอกอีเมล", "Please enter your email.");

            case "รูปแบบอีเมลไม่ถูกต้อง":
                return L("รูปแบบอีเมลไม่ถูกต้อง", "Invalid email format.");

            case "กรุณากรอกชื่อตัวละคร":
                return L("กรุณากรอกชื่อตัวละคร", "Please enter your character name.");

            case "กรุณากรอกรหัสผ่าน":
                return L("กรุณากรอกรหัสผ่าน", "Please enter your password.");

            case "รหัสผ่านต้องมีความยาวอย่างน้อย 8 ตัวอักษร":
                return L("รหัสผ่านต้องมีความยาวอย่างน้อย 8 ตัวอักษร", "Password must be at least 8 characters.");

            case "กรุณากรอกชื่อ":
                return L("กรุณากรอกชื่อ", "Please enter your first name.");

            case "กรุณากรอกนามสกุล":
                return L("กรุณากรอกนามสกุล", "Please enter your last name.");

            case "กรุณากรอกหน่วยงาน":
                return L("กรุณากรอกหน่วยงาน", "Please select your department.");

            case "กรุณากรอกเบอร์โทรศัพท์":
                return L("กรุณากรอกเบอร์โทรศัพท์", "Please enter your phone number.");

            case "Expected required property":
                return L("กรุณาเลือกเพศ", "Please select your gender.");

            case "ค่าเพศไม่ถูกต้อง":
                return L("ค่าเพศไม่ถูกต้อง", "Invalid gender value.");

            case "อีเมลนี้ถูกใช้งานแล้ว":
                return L("อีเมลนี้ถูกใช้งานแล้ว", "This email is already in use.");

            default:
                return string.IsNullOrWhiteSpace(apiMessage)
                    ? L("สมัครสมาชิกไม่สำเร็จ", "Registration failed.")
                    : apiMessage;
        }
    }
    private bool TryValidateRegisterForm(
    string email,
    string charName,
    string password,
    string confirm,
    string firstName,
    string lastName,
    string phone,
    string department,
    string gender,
    out string errorMessage)
    {
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(email))
        {
            errorMessage = L("กรุณากรอกอีเมล", "Please enter your email.");
            return false;
        }

        if (!IsValidEmail(email))
        {
            errorMessage = L("รูปแบบอีเมลไม่ถูกต้อง", "Invalid email format.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(charName))
        {
            errorMessage = L("กรุณากรอกชื่อตัวละคร", "Please enter your username.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            errorMessage = L("กรุณากรอกรหัสผ่าน", "Please enter your password.");
            return false;
        }

        if (password.Length < 8)
        {
            errorMessage = L("รหัสผ่านต้องมีความยาวอย่างน้อย 8 ตัวอักษร", "Password must be at least 8 characters.");
            return false;
        }

        if (password != confirm)
        {
            errorMessage = L("รหัสผ่านไม่ตรงกัน", "Passwords do not match.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(firstName))
        {
            errorMessage = L("กรุณากรอกชื่อ", "Please enter your first name.");
            return false;
        }

        if (!ContainsOnlyLettersAndSpaces(firstName))
        {
            errorMessage = L("ชื่อต้องไม่มีตัวเลข", "First name must not contain numbers.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            errorMessage = L("กรุณากรอกนามสกุล", "Please enter your last name.");
            return false;
        }

        if (!ContainsOnlyLettersAndSpaces(lastName))
        {
            errorMessage = L("นามสกุลต้องไม่มีตัวเลข", "Last name must not contain numbers.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(department) || departmentDropdown.value == PlaceholderIndex)
        {
            errorMessage = L("กรุณากรอกหน่วยงาน", "Please select your department.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(phone))
        {
            errorMessage = L("กรุณากรอกเบอร์โทรศัพท์", "Please enter your phone number.");
            return false;
        }

        if (!Regex.IsMatch(phone, @"^[0-9]+$"))
        {
            errorMessage = L("เบอร์โทรศัพท์ต้องเป็นตัวเลขเท่านั้น", "Phone number must contain digits only.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(gender) || genderDropdown.value == PlaceholderIndex)
        {
            errorMessage = L("กรุณาเลือกเพศ", "Please select your gender.");
            return false;
        }

        if (gender != "male" && gender != "female")
        {
            errorMessage = L("ค่าเพศไม่ถูกต้อง", "Invalid gender value.");
            return false;
        }

        if (!termsToggle.isOn)
        {
            errorMessage = L("กรุณายอมรับเงื่อนไขการใช้งาน", "Please accept the terms of service.");
            return false;
        }

        return true;
    }

    private bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        return Regex.IsMatch(
            email.Trim(),
            @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
            RegexOptions.IgnoreCase
        );
    }

    private bool ContainsOnlyLettersAndSpaces(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        return Regex.IsMatch(value.Trim(), @"^[\p{L}\s]+$");
    }
}