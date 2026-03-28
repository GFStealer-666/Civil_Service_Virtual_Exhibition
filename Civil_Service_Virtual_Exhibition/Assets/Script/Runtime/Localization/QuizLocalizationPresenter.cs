using System;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class QuizLocalizationPresenter : MonoBehaviour
{
    [Header("Intro Panel")]
    [SerializeField] private TMP_Text introTitleText;
    [SerializeField] private TMP_Text introSubtitleText;
    [SerializeField] private TMP_Text introStartButtonText;
    [SerializeField] private TMP_Text introDescriptionText;
    [SerializeField] private TMP_Text[] roundLabelTexts;
    [SerializeField] private TMP_Text leaderboardButtonText;
    [SerializeField] private TMP_Text rewardsButtonText;
    [SerializeField] private TMP_Text howToPlayButtonText;

    [Header("Play Panel")]
    [SerializeField] private TMP_Text timerTitleText;
    [SerializeField] private TMP_Text timerUnitText;
    [SerializeField] private TMP_Text playTitleText;
    [SerializeField] private TMP_Text playSubtitleText;
    [SerializeField] private TMP_Text totalQuestionsText;
    [SerializeField] private TMP_Text confirmAnswerButtonText;
    [SerializeField] private TMP_Text confirmToLeaveText;
    [SerializeField] private TMP_Text confirmToStayText;
    [Header("Complete Panel")]
    [SerializeField] private TMP_Text completeTitleText;
    [SerializeField] private TMP_Text completeSubtitleText;
    [SerializeField] private TMP_Text completeThankYouText;
    [SerializeField] private TMP_Text backToHomeButtonText;

    [Header("Leaderboard Panel")]
    [SerializeField] private TMP_Text leaderboardTitleText;
    [SerializeField] private TMP_Text leaderboardSubtitleText;
    [SerializeField] private TMP_Text rankHeaderText;
    [SerializeField] private TMP_Text nameHeaderText;
    [SerializeField] private TMP_Text scoreHeaderText;
    [SerializeField] private TMP_Text myScoreSummaryText;

    [Header("World Rank Card")]
    [SerializeField] private TMP_Text worldRankText;
    [SerializeField] private TMP_Text worldScoreText;

    private int _currentQuestion;
    private int _totalQuestions;
    private bool _hasQuestionProgress;

    private int _myScore;
    private int _myRank;
    private bool _hasMyScoreSummary;

    private int _worldRank;
    private int _worldScore;
    private bool _hasWorldRankCard;

    private void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
        RefreshTexts();
    }

    private void Start()
    {
        RefreshTexts();
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }

    private void OnLocaleChanged(Locale _)
    {
        RefreshTexts();
    }

    [ContextMenu("Refresh Localization")]
    public void RefreshTexts()
    {
        string quizTitle = T(
            LocalizationKeys.Quiz.IntroTitle,
            "CIVIL SERVICE\nKNOWLEDGE CHALLENGE"
        );

        string quizSubtitle = T(
            LocalizationKeys.Quiz.IntroSubtitle,
            "เรียนรู้บทบาทข้าราชการผ่านประสบการณ์และสถานการณ์จริง"
        );

        SetText(introTitleText, quizTitle);
        SetText(playTitleText, quizTitle);

        SetText(introSubtitleText, quizSubtitle);
        SetText(playSubtitleText, quizSubtitle);

        SetText(
            introStartButtonText,
            T(LocalizationKeys.Quiz.IntroStartButton, "เข้าเริ่มกิจกรรม")
        );

        SetText(
            introDescriptionText,
            T(
                LocalizationKeys.Quiz.IntroDescription,
                "เกมตอบคำถามเชิงสถานการณ์ที่ออกแบบเพื่อให้ผู้เข้าร่วมได้เรียนรู้บทบาท หน้าที่ และคุณค่าของการเป็นข้าราชการผ่านประสบการณ์จำลองจากเหตุการณ์ที่อาจพบได้จริงในการทำงานภาครัฐ ผู้เล่นจะได้ฝึกทักษะในการใช้วิจารณญาณ การสื่อสาร การตัดสินใจเชิงจริยธรรม และการปฏิบัติหน้าที่ของข้าราชการ โดยต้องเลือกคำตอบที่เหมาะสมที่สุดในแต่ละสถานการณ์"
            )
        );

        if (roundLabelTexts != null)
        {
            for (int i = 0; i < roundLabelTexts.Length; i++)
            {
                SetText(
                    roundLabelTexts[i],
                    F(LocalizationKeys.Quiz.IntroRoundLabelFormat, "รอบที่ {0}", i + 1)
                );
            }
        }

        SetText(
            leaderboardButtonText,
            T(LocalizationKeys.Quiz.IntroLeaderboardButton, "ตารางคะแนน")
        );

        SetText(
            rewardsButtonText,
            T(LocalizationKeys.Quiz.IntroRewardsButton, "ของรางวัล")
        );

        SetText(
            howToPlayButtonText,
            T(LocalizationKeys.Quiz.IntroHowToPlayButton, "วิธีการเล่น")
        );

        SetText(
            timerTitleText,
            T(LocalizationKeys.Quiz.PlayTimeRemaining, "เวลาที่เหลือ")
        );

        SetText(
            timerUnitText,
            T(LocalizationKeys.Quiz.PlaySeconds, "วินาที")
        );

        SetText(
            confirmAnswerButtonText,
            T(LocalizationKeys.Quiz.PlayConfirmAnswer, "ยืนยันคำตอบ")
        );

        SetText(
            completeTitleText,
            T(LocalizationKeys.Quiz.CompleteTitle, "ยินดีด้วย")
        );

        SetText(
            completeSubtitleText,
            T(LocalizationKeys.Quiz.CompleteSubtitle, "ท่านได้ทำแบบทดสอบเสร็จสมบูรณ์")
        );

        SetText(
            completeThankYouText,
            T(
                LocalizationKeys.Quiz.CompleteThankYou,
                "ขอขอบคุณที่ร่วมกิจกรรม และหวังว่าท่านจะได้รับความรู้และความเข้าใจเกี่ยวกับบทบาทและความสำคัญของข้าราชการพลเรือนมากยิ่งขึ้น"
            )
        );

        SetText(
            backToHomeButtonText,
            T(LocalizationKeys.Quiz.CompleteBackToHome, "กลับหน้าแรก")
        );

        SetText(
            leaderboardTitleText,
            T(LocalizationKeys.Quiz.LeaderboardTitle, "อันดับผู้ทำคะแนนสูงสุด")
        );

        SetText(
            leaderboardSubtitleText,
            T(LocalizationKeys.Quiz.LeaderboardSubtitle, "Leaderboard - กิจกรรมตอบคำถาม")
        );

        SetText(
            rankHeaderText,
            T(LocalizationKeys.Quiz.LeaderboardRankHeader, "ลำดับ")
        );

        SetText(
            nameHeaderText,
            T(LocalizationKeys.Quiz.LeaderboardNameHeader, "ชื่อ")
        );

        SetText(
            scoreHeaderText,
            T(LocalizationKeys.Quiz.LeaderboardScoreHeader, "คะแนน")
        );
        ApplyQuestionProgress();
        ApplyMyScoreSummary();
    }



    private void ApplyQuestionProgress()
    {
        if (!_hasQuestionProgress || totalQuestionsText == null)
            return;

        SetText(
            totalQuestionsText,
            F(
                LocalizationKeys.Quiz.PlayTotalQuestionsFormat,
                "จำนวนข้อทั้งหมด {0}/{1} ข้อ",
                _currentQuestion,
                _totalQuestions
            )
        );
    }

    private void ApplyMyScoreSummary()
    {
        if (!_hasMyScoreSummary || myScoreSummaryText == null)
            return;

        SetText(
            myScoreSummaryText,
            F(
                LocalizationKeys.Quiz.LeaderboardMyScoreFormat,
                "คะแนนของคุณ: {0} คะแนน อันดับของคุณ: อันดับที่ {1}",
                _myScore,
                _myRank
            )
        );
    }

    private string T(string key, string fallback)
    {
        string value = LocalizationSettings.StringDatabase.GetLocalizedString(
            LocalizationKeys.Tables.Quiz,
            key
        );

        return string.IsNullOrEmpty(value) ? fallback : value;
    }

    private string F(string key, string fallback, params object[] args)
    {
        string format = T(key, fallback);

        try
        {
            return string.Format(format, args);
        }
        catch (FormatException)
        {
            return fallback;
        }
    }

    private static void SetText(TMP_Text target, string value)
    {
        if (target != null)
            target.text = value;
    }
}