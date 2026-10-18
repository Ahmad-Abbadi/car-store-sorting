using System;
using NUnit.Framework;
using UnityEngine;
using System.Collections;
using System.IO;
using System.Linq;
using TinyRiders;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

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

public class CompactLightingTests
{
    const string Backup = "CompactLighting.QA";
    const string Evidence = MutedShopSurfaceBuilder.Evidence;

    [UnityTest, Timeout(180000)]
    public IEnumerator OriginalThemes_SwitchPersistReloadAndCapture()
    {
        SessionState.SetBool(Backup + ".hadTheme", PlayerPrefs.HasKey("Lighting_Theme"));
        SessionState.SetInt(Backup + ".theme", PlayerPrefs.GetInt("Lighting_Theme"));
        SessionState.SetBool(Backup + ".active", true);
        SessionState.SetBool("TinyRiders.Compact.QA.Active", true);
        foreach (var suffix in new[] { "", ".position", ".coins", ".CollectorQuests" }) PlayerPrefs.DeleteKey("TinyRiders.Compact.QA" + suffix);
        EditorSceneManager.OpenScene(CompactCollectionBuilder.ScenePath);
        CompactCollectionTests.Isolate(default, LoadSceneMode.Single);
        yield return new EnterPlayMode();
        yield return Exercise();
        yield return new ExitPlayMode();
    }

    static IEnumerator Exercise()
    {
        Application.runInBackground = true;
        while (SceneManager.GetActiveScene().name != "Level" || ThemedCollectionSession.Instance == null || !ThemedCollectionSession.Instance.IsReady) yield return null;
        ThemedCollectionSession.Instance.suspendPersistenceForTests = true;
        Directory.CreateDirectory(Evidence);
        foreach (var theme in new[] { LightingTheme.Bright, LightingTheme.Dark })
        {
            LightingThemeSettings.Set(theme);
            Assert.AreEqual((int)theme, PlayerPrefs.GetInt("Lighting_Theme"));
            AssertRig(theme);
            yield return null;
            Capture(Evidence + "/entrance-" + theme.ToString().ToLowerInvariant() + ".png");
            CaptureView(Evidence + "/ceiling-" + theme.ToString().ToLowerInvariant() + ".png", true);
            yield return SceneManager.LoadSceneAsync("MainMenu Scene");
            while (SceneManager.GetActiveScene().name != "MainMenu Scene" || Object.FindFirstObjectByType<MainMenu>() == null) yield return null;
            yield return null;
            Assert.AreEqual(theme, LightingThemeSettings.Current);
            var menu = Object.FindFirstObjectByType<MainMenu>();
            var button = (UnityEngine.UI.Button)new SerializedObject(menu).FindProperty("themeButton").objectReferenceValue;
            Assert.NotNull(button);
            button.onClick.Invoke();
            Assert.AreNotEqual(theme, LightingThemeSettings.Current, "Existing menu selector must toggle the theme.");
            button.onClick.Invoke();
            Assert.AreEqual(theme, LightingThemeSettings.Current);
            yield return SceneManager.LoadSceneAsync("Level");
            while (SceneManager.GetActiveScene().name != "Level" || ThemedCollectionSession.Instance == null || !ThemedCollectionSession.Instance.IsReady) yield return null;
            ThemedCollectionSession.Instance.suspendPersistenceForTests = true;
            AssertRig(theme);
        }
        File.WriteAllText(Evidence + "/validation.txt", "PASS: original lighting presets and diffuser materials; all 8 lights and 11 ceiling/diffuser surfaces switch; muted ceiling materials and wall textures; both reflection maps; Bright and Dark saved through menu and Level reload; existing menu toggle; matching entrance and ceiling captures; original floor and removed category signs retained.\n");
    }

    static void AssertRig(LightingTheme theme)
    {
        var rig = Object.FindFirstObjectByType<ShowroomLightingController>();
        var preset = theme == LightingTheme.Bright ? rig.brightPreset : rig.darkPreset;
        Assert.AreEqual(8, rig.ceilingLights.Length);
        Assert.AreEqual(11, rig.surfaces.Length);
        Assert.AreEqual(preset.volumeProfile, rig.volume.sharedProfile);
        Assert.AreEqual(preset.keyIntensity, rig.keyLight.intensity);
        Assert.AreEqual(preset.ambientSky, RenderSettings.ambientSkyColor);
        Assert.AreSame(theme == LightingTheme.Bright ? rig.brightReflection : rig.darkReflection, rig.reflectionProbe.customBakedTexture);
        Assert.NotNull(rig.reflectionProbe.customBakedTexture);
        Assert.IsInstanceOf<Cubemap>(rig.brightReflection);
        Assert.IsInstanceOf<Cubemap>(rig.darkReflection);
        Assert.AreEqual(rig.reflectionProbe.resolution, rig.reflectionProbe.customBakedTexture.width);
        foreach (var light in rig.ceilingLights)
        {
            Assert.AreEqual(preset.ceilingLightIntensity, light.intensity);
            Assert.AreEqual(preset.ceilingLightColor, light.color);
            Assert.AreEqual(preset.ceilingLightRange, light.range);
        }
        foreach (var surface in rig.surfaces)
        {
            Assert.AreSame(theme == LightingTheme.Bright ? surface.bright : surface.dark, surface.renderer.sharedMaterial);
            bool ceiling = surface.renderer.name == "Store Ceiling";
            Assert.AreEqual(ceiling ? MutedShopSurfaceBuilder.Root + "/Materials/Slate Ceiling Bright.mat"
                : ShopInteriorPolishBuilder.ArtRoot + "/Materials/Even Opal Diffusers.mat", AssetDatabase.GetAssetPath(surface.bright));
            Assert.AreEqual(ceiling ? MutedShopSurfaceBuilder.Root + "/Materials/Slate Ceiling Dark.mat"
                : ShowroomLightingBuilder.Root + "/Showroom Even Opal Diffusers.mat", AssetDatabase.GetAssetPath(surface.dark));
        }
        var room = GameObject.Find("Collector Promenade");
        foreach (var r in room.GetComponentsInChildren<MeshRenderer>().Where(r => r.name == "Gallery wall" || r.name == "Teal dado" || r.name == "Store Ceiling"))
        {
            Assert.AreEqual(2048, r.sharedMaterial.mainTexture.width); Assert.AreEqual(2048, r.sharedMaterial.mainTexture.height);
            Assert.True(AssetDatabase.GetAssetPath(r.GetComponent<MeshFilter>().sharedMesh).StartsWith(MutedShopSurfaceBuilder.Root + "/Meshes/"));
        }
        Assert.AreEqual(5, room.transform.Find(RacingFloorBuilder.DecorationName).childCount);
        Assert.False(room.GetComponentsInChildren<Transform>().Any(t => System.Text.RegularExpressions.Regex.IsMatch(t.name, @"^\d\d  .* sign$")));
    }

    static void Capture(string path)
    { CaptureView(path, false); }

    static void CaptureView(string path, bool ceiling)
    {
        var go = new GameObject("Lighting verification camera");
        var camera = go.AddComponent<Camera>(); camera.CopyFrom(Camera.main); camera.enabled = false;
        camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
        camera.transform.SetPositionAndRotation(ceiling ? new Vector3(489.2f, 1.8f, -11.5f) : new Vector3(500, 1.8f, -15.8f),
            ceiling ? Quaternion.Euler(-28, -35, 0) : Quaternion.Euler(6, 0, 0));
        camera.fieldOfView = 75;
        var rt = new RenderTexture(1600, 1000, 24); var old = RenderTexture.active;
        var image = new Texture2D(1600, 1000, TextureFormat.RGB24, false);
        try
        {
            camera.targetTexture = rt; camera.Render(); RenderTexture.active = rt;
            image.ReadPixels(new Rect(0, 0, 1600, 1000), 0, 0); image.Apply(); File.WriteAllBytes(path, image.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = null; RenderTexture.active = old;
            Object.DestroyImmediate(image); Object.DestroyImmediate(rt); Object.DestroyImmediate(go);
        }
    }

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        var cleanup = new CompactCollectionTests().Cleanup();
        while (cleanup.MoveNext()) yield return cleanup.Current;
        PlayerPrefs.DeleteKey("TinyRiders.Compact.QA.CollectorQuests");
        if (SessionState.GetBool(Backup + ".active", false))
        {
            if (SessionState.GetBool(Backup + ".hadTheme", false)) PlayerPrefs.SetInt("Lighting_Theme", SessionState.GetInt(Backup + ".theme", 0));
            else PlayerPrefs.DeleteKey("Lighting_Theme");
            PlayerPrefs.Save(); SessionState.SetBool(Backup + ".active", false);
            Object.FindFirstObjectByType<ShowroomLightingController>().ApplyTheme(LightingThemeSettings.Current);
        }
    }
}
