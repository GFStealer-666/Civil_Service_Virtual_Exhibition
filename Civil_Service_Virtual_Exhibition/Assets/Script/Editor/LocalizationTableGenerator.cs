using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

public static class LocalizationTableGenerator
{
    private const string OutputFolder = "Assets/Localization/Generated";

    private class EntryRow
    {
        public string Key;
        public string Thai;
        public string English;

        public EntryRow(string key, string thai, string english)
        {
            Key = key;
            Thai = thai;
            English = english;
        }
    }

    [MenuItem("Tools/Localization/Generate/UI_CentralHub")]
    public static void GenerateCentralHub()
    {
        CreateOrUpdateStringTableCollection(
            "UI_CentralHub",
            BuildCentralHubEntries()
        );
    }

    [MenuItem("Tools/Localization/Generate/UI_Quiz")]
    public static void GenerateQuiz()
    {
        CreateOrUpdateStringTableCollection(
            "UI_Quiz",
            BuildQuizEntries()
        );
    }

    [MenuItem("Tools/Localization/Generate/All UI Tables")]
    public static void GenerateAll()
    {
        CreateOrUpdateStringTableCollection("UI_CentralHub", BuildCentralHubEntries());
        CreateOrUpdateStringTableCollection("UI_Quiz", BuildQuizEntries());

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[LocalizationTableGenerator] All UI tables generated.");
    }

    private static void CreateOrUpdateStringTableCollection(
        string tableName,
        List<EntryRow> rows)
    {
        EnsureFolderExists(OutputFolder);

        Locale thaiLocale = LocalizationEditorSettings.GetLocale("th");
        Locale englishLocale = LocalizationEditorSettings.GetLocale("en");

        if (thaiLocale == null || englishLocale == null)
        {
            Debug.LogError(
                "[LocalizationTableGenerator] Missing locale. Make sure both 'th' and 'en' exist in Localization Settings."
            );
            return;
        }

        StringTableCollection collection = LocalizationEditorSettings
            .GetStringTableCollections()
            .FirstOrDefault(c => c.TableCollectionName == tableName);

        if (collection == null)
        {
            collection = LocalizationEditorSettings.CreateStringTableCollection(
                tableName,
                OutputFolder,
                new List<Locale> { englishLocale, thaiLocale }
            );

            if (collection == null)
            {
                Debug.LogError($"[LocalizationTableGenerator] Failed to create collection: {tableName}");
                return;
            }
        }
        else
        {
            EnsureLocaleTableExists(collection, englishLocale);
            EnsureLocaleTableExists(collection, thaiLocale);
        }

        collection.Group = "UI";

        StringTable thaiTable = collection.GetTable(thaiLocale.Identifier) as StringTable;
        StringTable englishTable = collection.GetTable(englishLocale.Identifier) as StringTable;

        if (thaiTable == null || englishTable == null)
        {
            Debug.LogError($"[LocalizationTableGenerator] Could not get locale tables for: {tableName}");
            return;
        }

        foreach (EntryRow row in rows)
        {
            englishTable.AddEntry(row.Key, row.English);
            thaiTable.AddEntry(row.Key, row.Thai);
        }

        EditorUtility.SetDirty(collection);
        EditorUtility.SetDirty(collection.SharedData);
        EditorUtility.SetDirty(englishTable);
        EditorUtility.SetDirty(thaiTable);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[LocalizationTableGenerator] Generated table: {tableName}");
    }

    private static void EnsureLocaleTableExists(StringTableCollection collection, Locale locale)
    {
        if (collection.GetTable(locale.Identifier) == null)
            collection.AddNewTable(locale.Identifier);
    }

    private static void EnsureFolderExists(string folderPath)
    {
        string[] parts = folderPath.Split('/');
        string current = parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);

            current = next;
        }
    }

    private static List<EntryRow> BuildCentralHubEntries()
    {
        return new List<EntryRow>
        {
            new EntryRow("centralhub.tab.lessons", "บทเรียน", "Lessons"),
            new EntryRow("centralhub.tab.settings", "ตั้งค่า", "Settings"),
            new EntryRow("centralhub.chat.title", "กล่องข้อความ", "Chat Box"),
            new EntryRow("centralhub.chat.input_placeholder", "พิมพ์ข้อความของคุณ....", "Type your message...."),
            new EntryRow("centralhub.settings.title", "ตั้งค่า", "Settings"),
            new EntryRow("centralhub.settings.user_info_title", "ข้อมูลผู้ใช้งาน", "User Information"),
            new EntryRow("centralhub.settings.background_volume", "ระดับเสียงพื้นหลัง", "Background Volume"),
            new EntryRow("centralhub.settings.effect_volume", "ระดับเสียงเอฟเฟ็กต์", "Effect Volume"),
            new EntryRow("centralhub.settings.accept_terms_button", "ยอมรับเงื่อนไขการใช้งาน", "Accept Terms of Use"),
            new EntryRow("centralhub.settings.logout_button", "ออกจากระบบ", "Log Out"),
            new EntryRow("centralhub.settings.cancel_membership_button", "ยกเลิกสมาชิก", "Cancel Membership"),
            new EntryRow("centralhub.settings.email_prefix", "อีเมล :", "Email:"),
            new EntryRow("centralhub.settings.phone_prefix", "เบอร์ติดต่อ :", "Phone:")
        };
    }

    private static List<EntryRow> BuildQuizEntries()
    {
        return new List<EntryRow>
        {
            new EntryRow("quiz.intro.title", "CIVIL SERVICE\nKNOWLEDGE CHALLENGE", "CIVIL SERVICE\nKNOWLEDGE CHALLENGE"),
            new EntryRow("quiz.intro.subtitle", "เรียนรู้บทบาทข้าราชการผ่านประสบการณ์และสถานการณ์จริง", "Learn the role of civil servants through real experiences and situations"),
            new EntryRow("quiz.intro.start_button", "เข้าเริ่มกิจกรรม", "Start Activity"),
            new EntryRow(
                "quiz.intro.description",
                "เกมตอบคำถามเชิงสถานการณ์ที่ออกแบบเพื่อให้ผู้เข้าร่วมได้เรียนรู้บทบาท หน้าที่ และคุณค่าของการเป็นข้าราชการผ่านประสบการณ์จำลองจากเหตุการณ์ที่อาจพบได้จริงในการทำงานภาครัฐ ผู้เล่นจะได้ฝึกทักษะในการใช้วิจารณญาณ การสื่อสาร การตัดสินใจเชิงจริยธรรม และการปฏิบัติหน้าที่ของข้าราชการ โดยต้องเลือกคำตอบที่เหมาะสมที่สุดในแต่ละสถานการณ์",
                "A scenario-based quiz game designed to help participants learn the roles, duties, and values of being a civil servant through simulated situations that may occur in real public service work. Players practice judgment, communication, ethical decision-making, and proper civil service conduct by choosing the most appropriate answer in each situation."
            ),
            new EntryRow("quiz.intro.round_label_format", "รอบที่ {0}", "Round {0}"),
            new EntryRow("quiz.intro.leaderboard_button", "ตารางคะแนน", "Leaderboard"),
            new EntryRow("quiz.intro.rewards_button", "ของรางวัล", "Rewards"),
            new EntryRow("quiz.intro.how_to_play_button", "วิธีการเล่น", "How to Play"),

            new EntryRow("quiz.play.time_remaining", "เวลาที่เหลือ", "Time Left"),
            new EntryRow("quiz.play.seconds", "วินาที", "Seconds"),
            new EntryRow("quiz.play.total_questions_format", "จำนวนข้อทั้งหมด {0}/{1} ข้อ", "Total Questions {0}/{1}"),
            new EntryRow("quiz.play.confirm_answer", "ยืนยันคำตอบ", "Confirm Answer"),

            new EntryRow("quiz.complete.title", "ยินดีด้วย", "Congratulations"),
            new EntryRow("quiz.complete.subtitle", "ท่านได้ทำแบบทดสอบเสร็จสมบูรณ์", "You have completed the quiz"),
            new EntryRow(
                "quiz.complete.thank_you",
                "ขอขอบคุณที่ร่วมกิจกรรม และหวังว่าท่านจะได้รับความรู้และความเข้าใจเกี่ยวกับบทบาทและความสำคัญของข้าราชการพลเรือนมากยิ่งขึ้น",
                "Thank you for joining the activity. We hope you gained more knowledge and understanding about the role and importance of civil servants."
            ),
            new EntryRow("quiz.complete.back_to_home", "กลับหน้าแรก", "Back to Home"),

            new EntryRow("quiz.leaderboard.title", "อันดับผู้ทำคะแนนสูงสุด", "Top Scorers"),
            new EntryRow("quiz.leaderboard.subtitle", "Leaderboard - กิจกรรมตอบคำถาม", "Leaderboard - Quiz Activity"),
            new EntryRow("quiz.leaderboard.rank_header", "ลำดับ", "Rank"),
            new EntryRow("quiz.leaderboard.name_header", "ชื่อ", "Name"),
            new EntryRow("quiz.leaderboard.score_header", "คะแนน", "Score"),
            new EntryRow("quiz.leaderboard.my_score_format", "คะแนนของคุณ: {0} คะแนน อันดับของคุณ: อันดับที่ {1}", "Your score: {0} points  Your rank: #{1}"),

            new EntryRow("quiz.rank_card.rank_format", "อันดับ {0}", "Rank {0}"),
            new EntryRow("quiz.rank_card.score_format", "{0} คะแนน", "{0} points")
        };
    }
}