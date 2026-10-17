using System;
using UnityEngine;

public enum LightingTheme { Bright = 0, Dark = 1 }

/// <summary>Saved lighting choice, independent of graphics quality.</summary>
public static class LightingThemeSettings
{
    private const string PrefKey = "Lighting_Theme";
    public static event Action<LightingTheme> Changed;
    public static LightingTheme Current => PlayerPrefs.GetInt(PrefKey, 0) == (int)LightingTheme.Dark
        ? LightingTheme.Dark : LightingTheme.Bright;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => Changed = null;

    public static void Set(LightingTheme theme)
    {
        if (theme != LightingTheme.Dark) theme = LightingTheme.Bright;
        PlayerPrefs.SetInt(PrefKey, (int)theme);
        PlayerPrefs.Save();
        Changed?.Invoke(theme);
    }
}
