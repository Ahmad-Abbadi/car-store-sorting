using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class ItemDetailQualityTests
{
    private const string Key = "Item_Detail";
    private bool hadPreference;
    private int savedPreference;
    private int savedQuality;

    [SetUp]
    public void SaveSettings()
    {
        hadPreference = PlayerPrefs.HasKey(Key);
        savedPreference = PlayerPrefs.GetInt(Key);
        savedQuality = QualitySettings.GetQualityLevel();
    }

    [TearDown]
    public void RestoreSettings()
    {
        if (hadPreference) PlayerPrefs.SetInt(Key, savedPreference);
        else PlayerPrefs.DeleteKey(Key);
        PlayerPrefs.Save();
        QualitySettings.SetQualityLevel(savedQuality, true);
    }

    [TestCase(ItemDetailLevel.Low, 0.8f, 1, true, true, 2048, 40f)]
    [TestCase(ItemDetailLevel.Medium, 0.9f, 2, true, true, 2048, 40f)]
    [TestCase(ItemDetailLevel.High, 1f, 4, true, true, 2048, 40f)]
    public void SelectionAppliesQualityBeforeNotification(ItemDetailLevel level, float scale,
        int samples, bool shadows, bool softShadows, int shadowResolution, float shadowDistance)
    {
        int notifications = 0;
        Action changed = () =>
        {
            notifications++;
            Assert.AreEqual(level.ToString(), QualitySettings.names[QualitySettings.GetQualityLevel()]);
        };
        ItemDetail.OnChanged += changed;
        try { ItemDetail.Set(level); }
        finally { ItemDetail.OnChanged -= changed; }

        Assert.AreEqual(1, notifications);
        Assert.AreEqual((int)level, PlayerPrefs.GetInt(Key));
        var asset = QualitySettings.renderPipeline as UniversalRenderPipelineAsset;
        Assert.IsNotNull(asset);
        Assert.AreEqual(level + "_URP", asset.name);
        Assert.AreEqual(scale, asset.renderScale, 0.001f);
        Assert.AreEqual(samples, asset.msaaSampleCount);
        Assert.AreEqual(shadows, asset.supportsMainLightShadows);
        Assert.AreEqual(softShadows, asset.supportsSoftShadows);
        Assert.AreEqual(shadowResolution, asset.mainLightShadowmapResolution);
        Assert.AreEqual(shadowDistance, asset.shadowDistance);
        Assert.AreEqual(UnityEngine.Rendering.Universal.LightRenderingMode.PerPixel, asset.additionalLightsRenderingMode);
        Assert.AreEqual(4, asset.maxAdditionalLightsCount);
        Assert.AreEqual(0, QualitySettings.globalTextureMipmapLimit);
        Assert.AreEqual((int)level, QualitySettings.GetTextureMipmapLimitSettings(ItemDetailBuilder.TextureGroup).limitBias);
    }

    [TestCase(-1)]
    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(99)]
    public void StartupAppliesSavedQualityWithoutRewritingPreference(int value)
    {
        if (value == -1) PlayerPrefs.DeleteKey(Key);
        else PlayerPrefs.SetInt(Key, value);
        var expected = value >= 0 && value <= 2 ? (ItemDetailLevel)value : ItemDetailLevel.Medium;
        typeof(ItemDetail).GetMethod("ApplySavedQuality", BindingFlags.Static | BindingFlags.NonPublic)
            .Invoke(null, null);
        Assert.AreEqual(expected, ItemDetail.Current);
        Assert.AreEqual(expected.ToString(), QualitySettings.names[QualitySettings.GetQualityLevel()]);
        Assert.AreEqual(value != -1, PlayerPrefs.HasKey(Key));
    }

    [Test]
    public void GraphicsQualityChangesWithoutBakedLods()
    {
        var flags = BindingFlags.Static | BindingFlags.NonPublic;
        var setField = typeof(ItemDetail).GetField("_lodSet", flags);
        var loadedField = typeof(ItemDetail).GetField("_lodSetLoaded", flags);
        object oldSet = setField.GetValue(null);
        object oldLoaded = loadedField.GetValue(null);
        try
        {
            setField.SetValue(null, null);
            loadedField.SetValue(null, true);
            foreach (ItemDetailLevel level in new[] { ItemDetailLevel.High, ItemDetailLevel.Low, ItemDetailLevel.Medium })
            {
                ItemDetail.Set(level);
                Assert.AreEqual(0, ItemDetail.Lod);
                Assert.AreEqual(level.ToString(), QualitySettings.names[QualitySettings.GetQualityLevel()]);
            }
        }
        finally
        {
            setField.SetValue(null, oldSet);
            loadedField.SetValue(null, oldLoaded);
        }
    }
}
