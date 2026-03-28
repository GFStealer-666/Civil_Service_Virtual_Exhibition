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
        public const string CentralHub = "UI_CentralHub";
        public const string Quiz = "UI_Quiz";
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
    public static class CentralHub
    {
        public const string TabLessons = "centralhub.tab.lessons";
        public const string TabSettings = "centralhub.tab.settings";

        public const string ChatTitle = "centralhub.chat.title";
        public const string ChatInputPlaceholder = "centralhub.chat.input_placeholder";

        public const string SettingsTitle = "centralhub.settings.title";
        public const string UserInfoTitle = "centralhub.settings.user_info_title";
        public const string BackgroundVolume = "centralhub.settings.background_volume";
        public const string EffectVolume = "centralhub.settings.effect_volume";

        public const string AcceptTermsButton = "centralhub.settings.accept_terms_button";
        public const string LogoutButton = "centralhub.settings.logout_button";
        public const string CancelMembershipButton = "centralhub.settings.cancel_membership_button";
        public const string EmailPrefix = "centralhub.settings.email_prefix";
        public const string PhonePrefix = "centralhub.settings.phone_prefix";
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
    public static class Quiz
    {
        public const string IntroTitle = "quiz.intro.title";
        public const string IntroSubtitle = "quiz.intro.subtitle";
        public const string IntroStartButton = "quiz.intro.start_button";
        public const string IntroDescription = "quiz.intro.description";
        public const string IntroRoundLabelFormat = "quiz.intro.round_label_format";
        public const string IntroLeaderboardButton = "quiz.intro.leaderboard_button";
        public const string IntroRewardsButton = "quiz.intro.rewards_button";
        public const string IntroHowToPlayButton = "quiz.intro.how_to_play_button";

        public const string PlayTimeRemaining = "quiz.play.time_remaining";
        public const string PlaySeconds = "quiz.play.seconds";
        public const string PlayTotalQuestionsFormat = "quiz.play.total_questions_format";
        public const string PlayConfirmAnswer = "quiz.play.confirm_answer";

        public const string CompleteTitle = "quiz.complete.title";
        public const string CompleteSubtitle = "quiz.complete.subtitle";
        public const string CompleteThankYou = "quiz.complete.thank_you";
        public const string CompleteBackToHome = "quiz.complete.back_to_home";

        public const string LeaderboardTitle = "quiz.leaderboard.title";
        public const string LeaderboardSubtitle = "quiz.leaderboard.subtitle";
        public const string LeaderboardRankHeader = "quiz.leaderboard.rank_header";
        public const string LeaderboardNameHeader = "quiz.leaderboard.name_header";
        public const string LeaderboardScoreHeader = "quiz.leaderboard.score_header";
        public const string LeaderboardMyScoreFormat = "quiz.leaderboard.my_score_format";

        public const string RankCardRankFormat = "quiz.rank_card.rank_format";
        public const string RankCardScoreFormat = "quiz.rank_card.score_format";

        public const string LeaderboardLoading = "quiz.leaderboard.loading";
        public const string LeaderboardRepositoryMissing = "quiz.leaderboard.repository_missing";
        public const string LeaderboardLoadFailed = "quiz.leaderboard.load_failed";

        public const string QuitConfirmTitle = "quiz.quit_confirm.title";
        public const string QuitConfirmMessage = "quiz.quit_confirm.message";

        public const string CooldownBlockedTitle = "quiz.cooldown.blocked_title";
        public const string CooldownBlockedReasonFormat = "quiz.cooldown.blocked_reason_format";
        public const string CooldownTimeFormatHoursMinutesSeconds = "quiz.cooldown.time_format_hms";
        public const string CooldownTimeFormatMinutesSeconds = "quiz.cooldown.time_format_ms";
        public const string CooldownTimeFormatSeconds = "quiz.cooldown.time_format_s";

        public const string StartFailedTitle = "quiz.start_failed.title";
        public const string StartFailedLoadQuiz = "quiz.start_failed.load_quiz";
        public const string StartFailedNoQuestions = "quiz.start_failed.no_questions";

        public const string QuitConfirmCancelButton = "quiz.quit_confirm.cancel_button";
        public const string QuitConfirmConfirmButton = "quiz.quit_confirm.confirm_button";

    }
}