using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

public static class QuizTableAppendMissingEntries
{
    private const string TableName = "UI_Quiz";

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

    [MenuItem("Tools/Localization/Quiz/Add Missing Quiz Entries")]
    public static void AddMissingQuizEntries()
    {
        Locale thaiLocale = LocalizationEditorSettings.GetLocale("th");
        Locale englishLocale = LocalizationEditorSettings.GetLocale("en");

        if (thaiLocale == null || englishLocale == null)
        {
            Debug.LogError("[QuizTableAppendMissingEntries] Missing locale. Please create both 'th' and 'en' first.");
            return;
        }

        StringTableCollection collection = LocalizationEditorSettings.GetStringTableCollection(TableName);
        if (collection == null)
        {
            Debug.LogError($"[QuizTableAppendMissingEntries] Table collection '{TableName}' was not found.");
            return;
        }

        EnsureLocaleTableExists(collection, thaiLocale);
        EnsureLocaleTableExists(collection, englishLocale);

        StringTable thaiTable = collection.GetTable(thaiLocale.Identifier) as StringTable;
        StringTable englishTable = collection.GetTable(englishLocale.Identifier) as StringTable;

        if (thaiTable == null || englishTable == null)
        {
            Debug.LogError("[QuizTableAppendMissingEntries] Could not load locale tables.");
            return;
        }

        List<EntryRow> rows = BuildMissingQuizEntries();

        int addedCount = 0;
        int skippedCount = 0;

        for (int i = 0; i < rows.Count; i++)
        {
            EntryRow row = rows[i];

            bool thaiExists = thaiTable.GetEntry(row.Key) != null;
            bool englishExists = englishTable.GetEntry(row.Key) != null;

            if (!englishExists)
            {
                englishTable.AddEntry(row.Key, row.English);
                addedCount++;
            }
            else
            {
                skippedCount++;
            }

            if (!thaiExists)
            {
                thaiTable.AddEntry(row.Key, row.Thai);
                addedCount++;
            }
            else
            {
                skippedCount++;
            }
        }

        EditorUtility.SetDirty(collection);
        EditorUtility.SetDirty(collection.SharedData);
        EditorUtility.SetDirty(thaiTable);
        EditorUtility.SetDirty(englishTable);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            $"[QuizTableAppendMissingEntries] Done. Added {addedCount} missing entries, skipped {skippedCount} existing entries in '{TableName}'."
        );
    }

    private static void EnsureLocaleTableExists(StringTableCollection collection, Locale locale)
    {
        if (collection.GetTable(locale.Identifier) == null)
            collection.AddNewTable(locale.Identifier);
    }

    private static List<EntryRow> BuildMissingQuizEntries()
    {
        return new List<EntryRow>
        {
            new EntryRow(
                "quiz.leaderboard.loading",
                "กำลังโหลด...",
                "Loading..."
            ),
            new EntryRow(
                "quiz.leaderboard.repository_missing",
                "ไม่พบ Quiz Repository",
                "Quiz repository is missing."
            ),
            new EntryRow(
                "quiz.leaderboard.load_failed",
                "โหลดตารางคะแนนไม่สำเร็จ",
                "Failed to load leaderboard."
            ),
            new EntryRow(
                "quiz.leaderboard.my_summary_format",
                "ชื่อ: {0} | คะแนนของคุณ: {1} คะแนน | อันดับของคุณ: {2}",
                "Name: {0} | Your score: {1} points | Your rank: {2}"
            ),
            new EntryRow(
                "quiz.quit_confirm.title",
                "ออกจากควิซ?",
                "Exit quiz?"
            ),
            new EntryRow(
                "quiz.quit_confirm.message",
                "หากออกจากควิซตอนนี้ คุณจะไม่สามารถเล่นได้อีกเป็นเวลา 24 ชั่วโมง",
                "If you leave the quiz now, you will not be able to play again for 24 hours."
            ),
            new EntryRow(
                "quiz.cooldown.blocked_title",
                "ไม่สามารถเริ่มควิซได้",
                "Unable to start quiz"
            ),
            new EntryRow(
                "quiz.cooldown.blocked_reason_format",
                "คุณเล่นควิซนี้ไปแล้ว กรุณารออีก {0} ก่อนที่จะเล่นได้",
                "You already played this quiz. Please wait {0} before playing again."
            ),
            new EntryRow(
                "quiz.cooldown.time_format_hms",
                "{0} ชั่วโมง {1} นาที {2} วินาที",
                "{0} hours {1} minutes {2} seconds"
            ),
            new EntryRow(
                "quiz.cooldown.time_format_ms",
                "{0} นาที {1} วินาที",
                "{0} minutes {1} seconds"
            ),
            new EntryRow(
                "quiz.cooldown.time_format_s",
                "{0} วินาที",
                "{0} seconds"
            ),
            new EntryRow(
                "quiz.start_failed.title",
                "เริ่มควิซไม่สำเร็จ",
                "Failed to start quiz"
            ),
            new EntryRow(
                "quiz.start_failed.load_quiz",
                "ไม่สามารถโหลดควิซได้",
                "Unable to load quiz."
            ),
            new EntryRow(
                "quiz.start_failed.no_questions",
                "ไม่พบคำถามควิซ",
                "No quiz questions found."
            )
        };
    }
}