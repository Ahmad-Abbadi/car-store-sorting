using System;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

/// <summary>Direct references keep synchronous legacy text lookups safe on WebGL.</summary>
public sealed class RuntimeLocalizationData : ScriptableObject
{
    public Locale[] locales = Array.Empty<Locale>();
    public StringTable[] tables = Array.Empty<StringTable>();
    public TMP_FontAsset latinFont, arabicFont;
    public Font latinSource, arabicSource;

    public Locale FindLocale(string code) => Array.Find(locales, l => l.Identifier.Code == code);
    public bool TryGet(string code, string key, out string value)
    {
        value = null;
        var table = Array.Find(tables, t => t.LocaleIdentifier.Code == code);
        var entry = table != null ? table.GetEntry(key) : null;
        if (entry == null || string.IsNullOrEmpty(entry.LocalizedValue)) return false;
        value = entry.LocalizedValue;
        return true;
    }
}
