using System;
using NUnit.Framework;
using UnityEngine;

public class LightingThemeTests
{
    private const string Key = "Lighting_Theme";
    private bool hadPreference;
    private int originalValue;

    [SetUp]
    public void SavePreference()
    {
        hadPreference = PlayerPrefs.HasKey(Key);
        originalValue = PlayerPrefs.GetInt(Key);
    }

    [TearDown]
    public void RestorePreference()
    {
        if (hadPreference) PlayerPrefs.SetInt(Key, originalValue);
        else PlayerPrefs.DeleteKey(Key);
        PlayerPrefs.Save();
    }

    [Test]
    public void NewPlayerDefaultsToBrightWithoutWritingPreference()
    {
        PlayerPrefs.DeleteKey(Key);
        Assert.AreEqual(LightingTheme.Bright, LightingThemeSettings.Current);
        Assert.IsFalse(PlayerPrefs.HasKey(Key));
    }

    [TestCase(-1)]
    [TestCase(2)]
    [TestCase(99)]
    public void InvalidSavedValueDefaultsToBright(int value)
    {
        PlayerPrefs.SetInt(Key, value);
        Assert.AreEqual(LightingTheme.Bright, LightingThemeSettings.Current);
    }

    [TestCase(LightingTheme.Bright)]
    [TestCase(LightingTheme.Dark)]
    public void SelectionIsSavedBeforeNotificationAndDoesNotChangeQuality(LightingTheme theme)
    {
        int quality = QualitySettings.GetQualityLevel();
        int notifications = 0;
        Action<LightingTheme> changed = value =>
        {
            notifications++;
            Assert.AreEqual(theme, value);
            Assert.AreEqual(theme, LightingThemeSettings.Current);
            Assert.AreEqual((int)theme, PlayerPrefs.GetInt(Key));
        };
        LightingThemeSettings.Changed += changed;
        try { LightingThemeSettings.Set(theme); }
        finally { LightingThemeSettings.Changed -= changed; }
        Assert.AreEqual(1, notifications);
        Assert.AreEqual(quality, QualitySettings.GetQualityLevel());
    }
}
