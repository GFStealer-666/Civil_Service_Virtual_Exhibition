#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

public static class UI_HOH
{
    private const string CollectionName = "UI_HOH";
    private const string RootFolder = "Assets/Localization";
    private const string LocalesFolder = RootFolder + "/Locales";
    private const string TablesFolder = RootFolder + "/StringTables";

    private readonly struct EntryData
    {
        public readonly string Key;
        public readonly string Thai;
        public readonly string English;

        public EntryData(string key, string thai, string english)
        {
            Key = key;
            Thai = thai;
            English = english;
        }
    }

    private static readonly EntryData[] Entries =
    {
        new EntryData("HOH_Ministry_Title", "ข้าราชการในสังกัดกระทรวง", "Civil Servants by Ministry"),
        new EntryData("HOH_Ministry_Subtitle", "เลือกกระทรวงที่ต้องการเยี่ยมชม", "Select a ministry to visit"),

        new EntryData("HOH_Province_Title", "ข้าราชการในสังกัดจังหวัด", "Civil Servants by Province"),
        new EntryData("HOH_Province_Subtitle", "เลือกจังหวัดที่ต้องการเยี่ยมชม", "Select a province to visit"),

        new EntryData("HOH_University_Title", "ข้าราชการในสังกัดมหาวิทยาลัย", "Civil Servants by University"),
        new EntryData("HOH_University_Subtitle", "เลือกมหาวิทยาลัยที่ต้องการเยี่ยมชม", "Select a university to visit"),

        new EntryData("HOH_Filter_All", "ทั้งหมด", "All"),
        new EntryData("HOH_Filter_Economy", "เศรษฐกิจ", "Economy"),
        new EntryData("HOH_Filter_Social", "สังคม", "Social"),
        new EntryData("HOH_Filter_Security", "ความมั่นคง", "Security"),
        new EntryData("HOH_Filter_Infrastructure", "โครงสร้างพื้นฐาน", "Infrastructure"),
        new EntryData("HOH_Filter_Service", "บริการ", "Service"),

        new EntryData("HOH_Filter_North", "เหนือ", "North"),
        new EntryData("HOH_Filter_Central", "กลาง", "Central"),
        new EntryData("HOH_Filter_South", "ใต้", "South"),
        new EntryData("HOH_Filter_East", "ตะวันออก", "East"),
        new EntryData("HOH_Filter_Northeast", "ตะวันออกเฉียงเหนือ", "Northeast"),
        new EntryData("HOH_Filter_West", "ตะวันตก", "West"),

        new EntryData("HOH_Search_Quick", "ค้นหาด่วน", "Quick Search"),

        new EntryData("HOH_Empty_LoadFailed", "โหลดข้อมูลไม่สำเร็จ", "Failed to load data"),
        new EntryData("HOH_Empty_NoData", "ยังไม่มีข้อมูล", "No data yet"),
        new EntryData("HOH_Empty_NoResults", "ไม่พบข้อมูล", "No results found"),

        new EntryData("HOH_OfficerSelection_Subtitle", "ข้าราชการดีเด่นของหน่วยงาน", "Outstanding officers in this unit"),
        new EntryData("HOH_OfficerSelection_Empty", "ไม่พบข้อมูลบุคลากร", "No officer data found")
    };

    [MenuItem("Tools/Localization/Generate UI_HOH")]
    public static void Generate()
    {
        EnsureFolder(RootFolder);
        EnsureFolder(LocalesFolder);
        EnsureFolder(TablesFolder);

        Locale thaiLocale = EnsureLocale("th", "Thai");
        Locale englishLocale = EnsureLocale("en", "English");

        if (thaiLocale == null || englishLocale == null)
        {
            Debug.LogError("[UI_HOH] Could not create or load required locales.");
            return;
        }

        StringTableCollection collection = LocalizationEditorSettings.GetStringTableCollection(CollectionName);

        if (collection == null)
        {
            collection = LocalizationEditorSettings.CreateStringTableCollection(
                CollectionName,
                TablesFolder,
                new List<Locale> { thaiLocale, englishLocale }
            );
        }

        if (collection == null)
        {
            Debug.LogError("[UI_HOH] Failed to create or load the UI_HOH string table collection.");
            return;
        }

        StringTable thaiTable = EnsureStringTable(collection, thaiLocale);
        StringTable englishTable = EnsureStringTable(collection, englishLocale);

        if (thaiTable == null || englishTable == null)
        {
            Debug.LogError("[UI_HOH] Failed to create or load one or more locale tables.");
            return;
        }

        for (int i = 0; i < Entries.Length; i++)
        {
            EntryData entry = Entries[i];
            thaiTable.AddEntry(entry.Key, entry.Thai);
            englishTable.AddEntry(entry.Key, entry.English);
        }

        EditorUtility.SetDirty(thaiTable);
        EditorUtility.SetDirty(englishTable);
        EditorUtility.SetDirty(thaiTable.SharedData);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorGUIUtility.PingObject(thaiTable.SharedData);
        Debug.Log($"[UI_HOH] Generated/updated '{CollectionName}' with {Entries.Length} keys.");
    }

    private static Locale EnsureLocale(string code, string assetName)
    {
        LocaleIdentifier identifier = new LocaleIdentifier(code);

        Locale locale = LocalizationEditorSettings.GetLocale(identifier);
        if (locale != null)
            return locale;

        locale = FindLocaleAsset(identifier);
        if (locale == null)
        {
            locale = Locale.CreateLocale(code);
            string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{LocalesFolder}/{assetName}.asset");
            AssetDatabase.CreateAsset(locale, assetPath);
        }

        if (LocalizationEditorSettings.GetLocale(locale.Identifier) == null)
            LocalizationEditorSettings.AddLocale(locale);

        EditorUtility.SetDirty(locale);
        return locale;
    }

    private static Locale FindLocaleAsset(LocaleIdentifier identifier)
    {
        string[] guids = AssetDatabase.FindAssets("t:Locale", new[] { "Assets" });

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            Locale locale = AssetDatabase.LoadAssetAtPath<Locale>(path);

            if (locale == null)
                continue;

            if (locale.Identifier.Code == identifier.Code)
                return locale;
        }

        return null;
    }

    private static StringTable EnsureStringTable(StringTableCollection collection, Locale locale)
    {
        if (collection == null || locale == null)
            return null;

        StringTable table = collection.GetTable(locale.Identifier) as StringTable;
        if (table != null)
            return table;

        table = collection.AddNewTable(locale.Identifier) as StringTable;

        if (table != null)
            EditorUtility.SetDirty(table);

        return table;
    }

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
            return;

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
}
#endif