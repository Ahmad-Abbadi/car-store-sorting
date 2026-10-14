#if UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;

/// <summary> Menu tool that gathers every English source string in the project into the database. </summary>
public static class LocalizationKeyCollector
{
    private const string SourceCode = "en";

    private static readonly string[] PromptFields = { "actionPrompt", "secondaryActionPrompt", "hoverPrompt" };

    // Prompts used as script defaults, which no scene needs to contain.
    private static readonly string[] DefaultPrompts = { "Pick up", "Take back", "Press", "Upgrades" };

    [MenuItem("Tools/Localization/Collect English Keys (project + open scene)")]
    private static void CollectMenu()
    {
        var found = new Dictionary<string, string>();
        CollectAssets(found);
        CollectScene(found);

        LocalizationDatabase_SO db = LocalizationCsvTool.LoadOrCreateDatabase();
        int added = Merge(db, found);
        AssetDatabase.SaveAssets();
        Debug.Log($"Localization collect: {found.Count} source strings found, {added} new keys added to '{SourceCode}'.");
    }

    private static void CollectAssets(Dictionary<string, string> found)
    {
        foreach (string guid in AssetDatabase.FindAssets("t:ItemDataSO"))
        {
            var item = AssetDatabase.LoadAssetAtPath<ItemDataSO>(AssetDatabase.GUIDToAssetPath(guid));
            if (item != null) found[item.NameKey] = item.SourceName;
        }

        foreach (string guid in AssetDatabase.FindAssets("t:Catagory_SO"))
        {
            var category = AssetDatabase.LoadAssetAtPath<Catagory_SO>(AssetDatabase.GUIDToAssetPath(guid));
            if (category != null) found[category.NameKey] = string.IsNullOrWhiteSpace(category.CatagoryName) ? category.name : category.CatagoryName;
        }

        foreach (string guid in AssetDatabase.FindAssets("t:UpgradeSO"))
        {
            var upgrade = AssetDatabase.LoadAssetAtPath<UpgradeSO>(AssetDatabase.GUIDToAssetPath(guid));
            if (upgrade == null) continue;
            found[upgrade.NameKey] = upgrade.SourceName;
            found[upgrade.DescriptionKey] = upgrade.SourceDescription;
        }

        foreach (string prompt in DefaultPrompts) found[Localizer.KeyFromEnglish("prompt", prompt)] = prompt;
        foreach (var pair in LocalizationUiWiring.AllStrings()) found[pair.Key] = pair.Value;
    }

    private static void CollectScene(Dictionary<string, string> found)
    {
        foreach (var label in Object.FindObjectsByType<LocalizedText>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var so = new SerializedObject(label);
            string key = so.FindProperty("key").stringValue;
            var text = label.GetComponent<TMP_Text>();
            if (!string.IsNullOrEmpty(key) && text != null && !found.ContainsKey(key)) found[key] = text.text;
        }

        foreach (var behaviour in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var so = new SerializedObject(behaviour);
            foreach (string field in PromptFields)
            {
                SerializedProperty prompt = so.FindProperty(field);
                if (prompt != null && prompt.propertyType == SerializedPropertyType.String && !string.IsNullOrEmpty(prompt.stringValue))
                    found[Localizer.KeyFromEnglish("prompt", prompt.stringValue)] = prompt.stringValue;
            }
        }
    }

    /// <summary> Replaces typed key names with {key_*} placeholders. </summary>
    public static string Tokenize(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        text = text.Replace("[Space]", "{key_jump}").Replace("[P]", "{key_skip}");
        return System.Text.RegularExpressions.Regex.Replace(text, @"\b([Pp]ress) E\b", "$1 {key_interact}");
    }

    /// <summary> Adds new keys to the source column and pads every other column; returns new key count. </summary>
    private static int Merge(LocalizationDatabase_SO db, Dictionary<string, string> found)
    {
        var keys = new List<string>(db.Keys);
        var index = new Dictionary<string, int>();
        for (int i = 0; i < keys.Count; i++) index[keys[i]] = i;

        var columns = new List<LocalizationDatabase_SO.Column>(db.Columns);
        LocalizationDatabase_SO.Column source = columns.Find(c => c.Code == SourceCode);
        if (source == null)
        {
            source = new LocalizationDatabase_SO.Column { Code = SourceCode, Values = new string[keys.Count] };
            columns.Insert(0, source);
        }

        var added = new List<string>();
        foreach (var pair in found)
        {
            if (index.ContainsKey(pair.Key)) continue;
            index[pair.Key] = keys.Count;
            keys.Add(pair.Key);
            added.Add(pair.Key);
        }

        foreach (var column in columns)
        {
            var values = new string[keys.Count];
            System.Array.Copy(column.Values, values, Mathf.Min(column.Values.Length, keys.Count));
            column.Values = values;
        }

        foreach (string key in added) source.Values[index[key]] = Tokenize(found[key]);

        // Fill English for empty keys; swap raw key names for placeholders.
        foreach (var pair in found)
        {
            int i = index[pair.Key];
            if (string.IsNullOrEmpty(source.Values[i]) || source.Values[i] == pair.Value) source.Values[i] = Tokenize(pair.Value);
        }

        db.SetTable(keys.ToArray(), columns);
        return added.Count;
    }
}
#endif
