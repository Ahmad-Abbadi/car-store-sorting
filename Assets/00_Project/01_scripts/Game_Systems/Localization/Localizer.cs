using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary> Looks up translated strings for the chosen language, falling back to the first one. </summary>
public static class Localizer
{
    private const string PrefKey = "Language";

    private static LocalizationDatabase_SO _db;
    private static LanguageInfo _current;
    private static Dictionary<string, string> _table = new Dictionary<string, string>();
    private static Dictionary<string, string> _fallbackTable = new Dictionary<string, string>();
    private static bool _initialized;

    /// <summary> Fired after the language changes. </summary>
    public static event Action LanguageChanged;

    /// <summary> True once the player has picked a language. </summary>
    public static bool HasChosenLanguage => PlayerPrefs.HasKey(PrefKey);

    /// <summary> All selectable languages in picker order. </summary>
    public static IReadOnlyList<LanguageInfo> Languages
    {
        get
        {
            Init();
            return _db != null ? _db.Languages : Array.Empty<LanguageInfo>();
        }
    }

    /// <summary> Active language; null when no database exists. </summary>
    public static LanguageInfo Current
    {
        get
        {
            Init();
            return _current;
        }
    }

    /// <summary> Code of the active language. </summary>
    public static string CurrentCode => Current != null ? Current.Code : "en";

    /// <summary> True when the active language reads right to left. </summary>
    public static bool IsRtl => Current != null && Current.IsRtl;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _db = null;
        _current = null;
        _initialized = false;
        _table = new Dictionary<string, string>();
        _fallbackTable = new Dictionary<string, string>();
        LanguageChanged = null;
    }

    /// <summary> Loads the database and the saved language once. </summary>
    private static void Init()
    {
        if (_initialized && (_db != null || !Application.isEditor)) return; // editor retries until the asset exists
        _initialized = true;

        _db = Resources.Load<LocalizationDatabase_SO>(LocalizationDatabase_SO.ResourcePath);
        if (_db == null || _db.Languages.Count == 0)
        {
            _db = null;
            return;
        }

        _fallbackTable = _db.BuildTable(_db.Languages[0].Code);
        string saved = PlayerPrefs.GetString(PrefKey, string.Empty);
        if (saved.Length == 0) saved = MatchDeviceCode(); // first launch previews the device language
        Apply(_db.FindLanguage(saved) ?? _db.Languages[0]);
    }

    private static void Apply(LanguageInfo info)
    {
        _current = info;
        _table = _db.BuildTable(info.Code);
    }

    /// <summary> Switches language, saves it and notifies listeners. </summary>
    public static void SetLanguage(string code)
    {
        Init();
        if (_db == null) return;

        LanguageInfo info = _db.FindLanguage(code) ?? _db.Languages[0];
        Apply(info);
        PlayerPrefs.SetString(PrefKey, info.Code);
        PlayerPrefs.Save();
        LanguageChanged?.Invoke();
    }

#if UNITY_EDITOR
    /// <summary>Rebuilds cached tables after an editor tool changes the localization asset.</summary>
    public static void RefreshDatabase()
    {
        _initialized = false;
        Init();
        LanguageChanged?.Invoke();
    }
#endif

    /// <summary> Picker language matching the device language; first language when unsupported. </summary>
    public static string DetectDeviceCode()
    {
        Init();
        return _db != null ? MatchDeviceCode() : "en";
    }

    private static string MatchDeviceCode()
    {
        string code;
        switch (Application.systemLanguage)
        {
            case SystemLanguage.Spanish: code = "es"; break;
            case SystemLanguage.Portuguese: code = "pt-BR"; break;
            case SystemLanguage.Indonesian: code = "id"; break;
            case SystemLanguage.Turkish: code = "tr"; break;
            case SystemLanguage.Polish: code = "pl"; break;
            case SystemLanguage.Vietnamese: code = "vi"; break;
            case SystemLanguage.Arabic: code = "ar"; break;
            default: code = "en"; break;
        }
        return _db.FindLanguage(code) != null ? code : _db.Languages[0].Code;
    }

    /// <summary> Text for a key; the key itself (marked in the editor) when missing. </summary>
    public static string Get(string key)
    {
        if (TryGet(key, out string text)) return text;
#if UNITY_EDITOR
        return "⟦" + key + "⟧";
#else
        return key;
#endif
    }

    /// <summary> Text for a key, or the given fallback when the key is empty or missing. </summary>
    public static string GetOr(string key, string fallback) =>
        TryGet(key, out string text) ? text : fallback;

    /// <summary> Localized text of an English Inspector string, keyed group.slug (e.g. prompt.take_back). </summary>
    public static string FromEnglish(string group, string english)
    {
        if (string.IsNullOrEmpty(english)) return english;
        return GetOr(KeyFromEnglish(group, english), english);
    }

    /// <summary> Key for an English string: group plus lowercase letters and digits joined by underscores. </summary>
    public static string KeyFromEnglish(string group, string english)
    {
        var sb = new System.Text.StringBuilder(group.Length + 1 + english.Length);
        sb.Append(group).Append('.');
        bool gap = false;
        foreach (char ch in english)
        {
            if (char.IsLetterOrDigit(ch))
            {
                if (gap && sb[sb.Length - 1] != '.') sb.Append('_');
                sb.Append(char.ToLowerInvariant(ch));
                gap = false;
            }
            else gap = true;
        }
        return sb.ToString();
    }

    /// <summary> Text for a key with {0}, {1}... filled by args. </summary>
    public static string Format(string key, params object[] args) => FormatText(Get(key), args);

    /// <summary> Fills {0}, {1}... in text; returns it unchanged on a bad template. </summary>
    public static string FormatText(string text, params object[] args)
    {
        try { return string.Format(text, args); }
        catch (FormatException) { return text; }
    }

    /// <summary> Finds a key in the active language, then the fallback language. </summary>
    public static bool TryGet(string key, out string text)
    {
        text = null;
        if (string.IsNullOrEmpty(key)) return false;
        Init();
        if (!_table.TryGetValue(key, out text) && !_fallbackTable.TryGetValue(key, out text)) return false;
        text = ResolveHints(text);
        return true;
    }

    /// <summary> Replaces {key_interact}, {key_jump} and {key_skip} with key names, or touch icons on touch. </summary>
    public static string ResolveHints(string text)
    {
        if (text == null || text.IndexOf("{key_", StringComparison.Ordinal) < 0) return text;

        bool touch = InputIcons.IsTouch;
        return text
            .Replace("{key_interact}", touch ? InputIcons.TouchTag("Interact") : "E")
            .Replace("{key_jump}", touch ? InputIcons.TouchTag("Jump") : "[Space]")
            .Replace("{key_skip}", "[P]");
    }
}
