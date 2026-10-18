#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Localization;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.SceneManagement;

/// <summary>Imports the CSV master into Unity tables and binds authored text without changing gameplay IDs.</summary>
public static class CompleteLocalizationBuilder
{
    const string Folder = "Assets/Localization";
    const string RuntimePath = "Assets/00_Project/03_scriptableObjects/Resources/RuntimeLocalizationData.asset";
    const string FontFolder = "Assets/00_Project/04_ِArt/UI/Fonts/";

    [MenuItem("Tools/Localization/Build Complete Localization")]
    public static void Build()
    {
        BuildTables();
        WireAuthoredText(LocalizationCsvTool.LoadOrCreateDatabase());
    }

    public static void BuildTables()
    {
        LocalizationCsvTool.Import(File.ReadAllText("Localization.csv"));
    }

    public static void SynchronizeTables()
    {
        var db = LocalizationCsvTool.LoadOrCreateDatabase();
        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets", "Localization");
        var settings = LocalizationEditorSettings.ActiveLocalizationSettings;
        if (settings == null)
        {
            settings = ScriptableObject.CreateInstance<LocalizationSettings>();
            AssetDatabase.CreateAsset(settings, Folder + "/LocalizationSettings.asset");
            LocalizationEditorSettings.ActiveLocalizationSettings = settings;
        }
        settings.GetStartupLocaleSelectors().Clear();
        settings.GetStartupLocaleSelectors().Add(new SavedGameLocaleSelector());
        var locales = new List<Locale>();
        foreach (var language in db.Languages)
        {
            var locale = LocalizationEditorSettings.GetLocales().FirstOrDefault(l => l.Identifier.Code == language.Code);
            if (locale == null)
            {
                locale = Locale.CreateLocale(language.Code);
                AssetDatabase.CreateAsset(locale, Folder + "/" + language.Code + ".asset");
                LocalizationEditorSettings.AddLocale(locale);
            }
            locale.LocaleName = language.NativeName;
            locales.Add(locale);
            EditorUtility.SetDirty(locale);
        }
        var collection = LocalizationEditorSettings.GetStringTableCollection("GameText")
            ?? LocalizationEditorSettings.CreateStringTableCollection("GameText", Folder, locales);
        foreach (var column in db.Columns)
        {
            var table = (StringTable)collection.GetTable(column.Code);
            if (table == null) table = (StringTable)collection.AddNewTable(column.Code);
            for (int i = 0; i < db.Keys.Length; i++)
                table.AddEntry(db.Keys[i], column.Values[i]);
            EditorUtility.SetDirty(table);
        }
        // Direct resource references have already loaded these tables before the first scene.
        collection.SetPreloadTableFlag(false);
        var data = AssetDatabase.LoadAssetAtPath<RuntimeLocalizationData>(RuntimePath);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<RuntimeLocalizationData>();
            AssetDatabase.CreateAsset(data, RuntimePath);
        }
        data.locales = locales.ToArray();
        data.tables = db.Languages.Select(l => (StringTable)collection.GetTable(l.Code)).ToArray();
        settings.SetAvailableLocales(new GameLocalesProvider(locales));
        settings.GetStringDatabase().TableProvider = new GameStringTableProvider(data.tables);
        data.latinFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontFolder + "Fredoka-SemiBold SDF.asset");
        data.arabicFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontFolder + "BalooBhaijaan2-SemiBold SDF.asset");
        data.latinSource = AssetDatabase.LoadAssetAtPath<Font>(FontFolder + "Baloo2-SemiBold.ttf");
        data.arabicSource = AssetDatabase.LoadAssetAtPath<Font>(FontFolder + "BalooBhaijaan2-SemiBold.ttf");
        EditorUtility.SetDirty(data);
        EditorUtility.SetDirty(settings);
        EditorUtility.SetDirty(collection);
        EditorUtility.SetDirty(collection.SharedData);
        LocalizationEditorSettings.EditorEvents.RaiseCollectionModified(collection, collection);
        AssetDatabase.SaveAssets();
        Localizer.RefreshDatabase();
        FallbackFontBuilder.Build();
    }

    static string Normalize(string text) => text.Replace("\\n", "\n").Replace("\r", "").Trim();

    static void WireAuthoredText(LocalizationDatabase_SO db)
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save open scene changes before localization wiring.");
        var english = db.BuildTable("en");
        var reverse = new Dictionary<string, string>();
        foreach (var pair in english) reverse.TryAdd(Normalize(pair.Value), pair.Key);
        var setup = EditorSceneManager.GetSceneManagerSetup();
        var audit = new List<string>();
        int wired = 0;
        var builds = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        try
        {
            var prefabs = AssetDatabase.GetDependencies(builds, true).Where(p => p.EndsWith(".prefab") && p.StartsWith("Assets/"));
            foreach (var path in prefabs)
            {
                if (!File.ReadAllText(path).Contains("m_text:")) continue;
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    int count = Wire(root, path, reverse, english, audit);
                    if (count > 0) PrefabUtility.SaveAsPrefabAsset(root, path);
                    wired += count;
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            foreach (var path in builds)
            {
                var scene = EditorSceneManager.OpenScene(path);
                int count = 0;
                foreach (var root in scene.GetRootGameObjects()) count += Wire(root, path, reverse, english, audit);
                if (count > 0) EditorSceneManager.SaveScene(scene);
                wired += count;
            }
        }
        finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
        Directory.CreateDirectory("IntegrationEvidence/Localization");
        File.WriteAllLines("IntegrationEvidence/Localization/authored-text-audit.txt", audit.Distinct());
        Debug.Log($"Localization: wired {wired} labels. See IntegrationEvidence/Localization/authored-text-audit.txt for coverage.");
    }

    static int Wire(GameObject root, string source, Dictionary<string, string> reverse, Dictionary<string, string> english, List<string> audit)
    {
        int count = 0;
        foreach (var label in root.GetComponentsInChildren<TMP_Text>(true))
        {
            var localized = label.GetComponent<LocalizedText>();
            string text = Normalize(label.text ?? "");
            string path = source + " :: " + AnimationUtility.CalculateTransformPath(label.transform, null);
            if (localized != null && !string.IsNullOrEmpty(localized.Key))
            {
                audit.Add((english.ContainsKey(localized.Key) ? "BOUND " : "MISSING KEY ") + path + " :: " + localized.Key);
                continue;
            }
            // These components own dynamic content; binding their preview strings would overwrite live state.
            bool dynamic = label.GetComponent<InputIconText>() != null ||
                label.GetComponentInParent<ActionPromptController>() != null ||
                label.GetComponentInParent<HeldItemNameUI>() != null ||
                label.GetComponentInParent<EquipmentItemNameUI>() != null ||
                label.GetComponentInParent<UpgradeCardUI>() != null ||
                label.GetComponentInParent<LanguageButtonUI>() != null ||
                label.GetComponentInParent<MainMenu>() != null && (label.name.Contains("Detail") || label.name.Contains("Theme"));
            if (dynamic || !text.Any(char.IsLetter)) { audit.Add("DYNAMIC/NUMERIC " + path); continue; }
            if (text.StartsWith("Details: ") || text.StartsWith("Theme: "))
            { audit.Add("DYNAMIC " + path); continue; }
            string key;
            if (text.StartsWith("COLLECTOR DIRECTORY")) key = "sign.directory";
            else if (text.StartsWith("GOLDEN CAR")) key = "sign.golden_car";
            else if (!reverse.TryGetValue(text, out key))
            { audit.Add("UNMATCHED " + path + " :: " + text.Replace("\n", "\\n")); continue; }
            if (localized == null) localized = label.gameObject.AddComponent<LocalizedText>();
            var serialized = new SerializedObject(localized);
            serialized.FindProperty("key").stringValue = key;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            // Keep English authored previews; OnEnable supplies the player's locale at runtime.
            label.text = english[key];
            label.enableAutoSizing = true;
            label.fontSizeMax = label.fontSize;
            label.fontSizeMin = label.fontSize * .6f;
            EditorUtility.SetDirty(label);
            EditorUtility.SetDirty(localized);
            audit.Add("WIRED " + path + " :: " + key);
            count++;
        }
        return count;
    }
}
#endif
