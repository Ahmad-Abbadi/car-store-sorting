using System;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

/// <summary>Preserves the game's original language preference without marking first launch as a player choice.</summary>
[Serializable]
public sealed class SavedGameLocaleSelector : IStartupLocaleSelector
{
    public Locale GetStartupLocale(ILocalesProvider availableLocales)
    {
        string code = PlayerPrefs.GetString("Language", "");
        if (string.IsNullOrEmpty(code))
        {
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
        }
        return availableLocales.GetLocale(code) ?? availableLocales.GetLocale("en");
    }
}
