public static class LocalizationKeys
{
    public static class Tables
    {
        public const string Boot = "UI_Boot";
        public const string Landing = "UI_Landing";
        public const string Common = "UI_Common";
        public const string Login = "UI_Login";
        public const string Register = "UI_Register";
        public const string ForgotPassword = "UI_ForgotPassword";
    }

    public static class Boot
    {
        public const string TitleNative = "boot.language.title.native";
        public const string TitleEnglish = "boot.language.title.english";
        public const string ButtonThai = "boot.language.button.th";
        public const string ButtonEnglish = "boot.language.button.en";
    }

    public static class Landing
    {
        public const string Title = "landing.title";
        public const string TapToStart = "landing.tap_to_start";
    }

    public static class Common
    {
        public const string EmailLabel = "common.email_label";
        public const string EmailPlaceholder = "common.email_placeholder";
        public const string PasswordLabel = "common.password_label";
        public const string PasswordPlaceholder = "common.password_placeholder";
        public const string ConfirmPasswordLabel = "common.confirm_password_label";
        public const string ConfirmPasswordPlaceholder = "common.confirm_password_placeholder";
        public const string FirstNameLabel = "common.first_name_label";
        public const string FirstNamePlaceholder = "common.first_name_placeholder";
        public const string LastNameLabel = "common.last_name_label";
        public const string LastNamePlaceholder = "common.last_name_placeholder";
        public const string PhoneLabel = "common.phone_label";
        public const string PhonePlaceholder = "common.phone_placeholder";
        public const string GenderLabel = "common.gender_label";
        public const string GenderPlaceholder = "common.gender_placeholder";
        public const string DepartmentLabel = "common.department_label";
        public const string DepartmentPlaceholder = "common.department_placeholder";
        public const string AcceptTerms = "common.accept_terms";
        public const string Or = "common.or";
    }

    public static class Login
    {
        public const string Title = "auth.login.title";
        public const string ForgotPassword = "auth.login.forgot_password";
        public const string Submit = "auth.login.submit";
        public const string GuestLogin = "auth.login.guest_login";
        public const string NoAccount = "auth.login.no_account";
        public const string RegisterLink = "auth.login.register_link";
    }

    public static class Register
    {
        public const string Title = "auth.register.title";
        public const string DisplayNameLabel = "auth.register.display_name_label";
        public const string DisplayNamePlaceholder = "auth.register.display_name_placeholder";
        public const string PhoneHelper = "auth.register.phone_helper";
        public const string Submit = "auth.register.submit";
    }

    public static class ForgotPassword
    {
        public const string Title = "auth.forgot_password.title";
        public const string Description = "auth.forgot_password.description";
        public const string Submit = "auth.forgot_password.submit";
    }

    public static class Error
    {
        public const string ServerUnreachable = "error.server_unreachable";
        public const string NetworkNotReady = "error.network_not_ready";
        public const string ApiConfigMissing = "error.api_config_missing";
        public const string InitialRoomMissing = "error.initial_room_missing";
        public const string JoinFailed = "error.join_failed";
        public const string RoomUnavailable = "error.room_unavailable";
    }
}