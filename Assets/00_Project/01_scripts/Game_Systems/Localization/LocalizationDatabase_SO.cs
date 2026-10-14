using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary> One selectable language and how the picker shows it. </summary>
[Serializable]
public class LanguageInfo
{
    [Tooltip("Code used in the CSV header and PlayerPrefs, e.g. en, pt-BR.")]
    public string Code;

    [Tooltip("Name in its own language, shown on the picker button.")]
    public string NativeName;

    public Sprite Flag;

    [Tooltip("Right-to-left script such as Arabic.")]
    public bool IsRtl;
}

/// <summary> Every language and string, filled from CSV by the importer. </summary>
[CreateAssetMenu(fileName = "LocalizationDatabase", menuName = "ScriptableObjects/Localization Database")]
public class LocalizationDatabase_SO : ScriptableObject
{
    /// <summary> Resources path the Localizer loads this asset from. </summary>
    public const string ResourcePath = "LocalizationDatabase";

    /// <summary> Strings of one language, aligned with the shared key list. </summary>
    [Serializable]
    public class Column
    {
        public string Code;
        public string[] Values = Array.Empty<string>();
    }

    [Tooltip("Picker order; the first entry is the fallback language.")]
    [SerializeField] private List<LanguageInfo> languages = new List<LanguageInfo>();

    [Tooltip("Shared key list. Set by the CSV importer, don't edit by hand.")]
    [SerializeField] private string[] keys = Array.Empty<string>();

    [SerializeField] private List<Column> columns = new List<Column>();

    /// <summary> Languages in picker order. </summary>
    public IReadOnlyList<LanguageInfo> Languages => languages;

    /// <summary> Number of keys. </summary>
    public int KeyCount => keys.Length;

    /// <summary> Language info for a code, or null. </summary>
    public LanguageInfo FindLanguage(string code)
    {
        foreach (LanguageInfo info in languages)
            if (string.Equals(info.Code, code, StringComparison.OrdinalIgnoreCase)) return info;
        return null;
    }

    /// <summary> Key to text map for a language; empty cells are left out. </summary>
    public Dictionary<string, string> BuildTable(string code)
    {
        var table = new Dictionary<string, string>(keys.Length);
        foreach (Column column in columns)
        {
            if (!string.Equals(column.Code, code, StringComparison.OrdinalIgnoreCase)) continue;
            int count = Mathf.Min(keys.Length, column.Values.Length);
            for (int i = 0; i < count; i++)
                if (!string.IsNullOrEmpty(column.Values[i])) table[keys[i]] = column.Values[i];
            break;
        }
        return table;
    }

#if UNITY_EDITOR
    /// <summary> Replaces all strings; adds a language entry for any new column code. </summary>
    public void SetTable(string[] newKeys, List<Column> newColumns)
    {
        keys = newKeys;
        columns = newColumns;
        foreach (Column column in newColumns)
            if (FindLanguage(column.Code) == null)
                languages.Add(new LanguageInfo { Code = column.Code, NativeName = DefaultNativeName(column.Code), IsRtl = column.Code.StartsWith("ar") });
        UnityEditor.EditorUtility.SetDirty(this);
    }

    private static string DefaultNativeName(string code)
    {
        switch (code)
        {
            case "en": return "English";
            case "es": return "Español";
            case "pt-BR": return "Português";
            case "id": return "Bahasa Indonesia";
            case "tr": return "Türkçe";
            case "pl": return "Polski";
            case "vi": return "Tiếng Việt";
            case "ar": return "العربية";
            default: return code;
        }
    }

    /// <summary> Current key list, for editor tools. </summary>
    public string[] Keys => keys;

    /// <summary> Current columns, for editor tools. </summary>
    public List<Column> Columns => columns;
#endif
}
