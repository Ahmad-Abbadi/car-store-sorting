using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TMPro;
using TinyRiders;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public sealed class CompleteLocalizationTests
{
    const string CatalogPath = "Assets/TinyRiders/ThemedCollection/Data/Themes/ThemedCarCatalog.asset";
    const string Evidence = "IntegrationEvidence/Localization";
    static string[] Tokens(string value) => Regex.Matches(value, @"\{[^}]+\}").Cast<Match>().Select(m => m.Value).OrderBy(s => s).ToArray();

    [Test]
    public void Tables_CoverEveryLanguageAndStableCatalogId_WithMatchingPlaceholders()
    {
        var db = Resources.Load<LocalizationDatabase_SO>(LocalizationDatabase_SO.ResourcePath);
        var data = Resources.Load<RuntimeLocalizationData>("RuntimeLocalizationData");
        var catalog = AssetDatabase.LoadAssetAtPath<ThemedCarCatalog>(CatalogPath);
        Assert.AreEqual(8, data.locales.Length);
        CollectionAssert.AreEqual(db.Languages.Select(l => l.Code), data.locales.Select(l => l.Identifier.Code));
        foreach (var language in db.Languages)
        {
            foreach (string key in db.Keys)
            {
                Assert.IsTrue(data.TryGet(language.Code, key, out string value), language.Code + ": " + key);
                Assert.IsTrue(data.TryGet("en", key, out string english));
                CollectionAssert.AreEqual(Tokens(english), Tokens(value), language.Code + ": " + key);
            }
            foreach (var theme in catalog.themes) Assert.IsTrue(data.TryGet(language.Code, theme.NameKey, out _), theme.NameKey);
            foreach (var entry in catalog.entries)
            {
                Assert.IsTrue(data.TryGet(language.Code, entry.NameKey, out _), entry.NameKey);
                Assert.AreEqual(entry.NameKey, entry.sortingItem.NameKey);
            }
        }
        Assert.IsFalse(File.ReadAllLines(Evidence + "/authored-text-audit.txt").Any(l => l.StartsWith("UNMATCHED ") || l.StartsWith("MISSING KEY ")));
    }

    [Test]
    public void Fonts_ContainAllTranslatedGlyphs_IncludingShapedArabic()
    {
        var data = Resources.Load<RuntimeLocalizationData>("RuntimeLocalizationData");
        foreach (var table in data.tables)
        {
            bool arabic = table.LocaleIdentifier.Code == "ar";
            var font = arabic ? data.arabicFont : data.latinFont;
            foreach (var entry in table.Values)
            {
                string text = Regex.Replace(entry.LocalizedValue, "<[^>]+>|\\{[^}]+\\}", "");
                if (arabic) text = ArabicText.Shape(text);
                foreach (char ch in text.Where(ch => !char.IsWhiteSpace(ch)).Distinct())
                    Assert.IsTrue(font.HasCharacter(ch, true, true), table.LocaleIdentifier.Code + " missing " + ch + " in " + entry.Key);
            }
        }
    }

    [UnityTest, Timeout(180000)]
    public IEnumerator Switching_RefreshesMessagesNamesActiveAndReopenedLabels_WithoutChangingSession()
    {
        var setup = EditorSceneManager.GetSceneManagerSetup();
        bool hadLanguage = PlayerPrefs.HasKey("Language");
        string savedLanguage = PlayerPrefs.GetString("Language", "en");
        SessionState.SetString("CompleteLocalization.SceneSetup", JsonUtility.ToJson(new SavedSceneSetup { paths = setup.Where(s => s.isLoaded).Select(s => s.path).ToArray(), activePath = setup.FirstOrDefault(s => s.isActive)?.path }));
        SessionState.SetBool("CompleteLocalization.HadLanguage", hadLanguage);
        SessionState.SetString("CompleteLocalization.Language", savedLanguage);
        SessionState.SetBool("CompleteLocalization.Active", true);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        yield return new EnterPlayMode();
        yield return LocalizationSettings.InitializationOperation;
        Directory.CreateDirectory(Evidence);
        var catalog = AssetDatabase.LoadAssetAtPath<ThemedCarCatalog>(CatalogPath);
        var go = new GameObject("Localization test session");
        go.SetActive(false);
        var session = go.AddComponent<ThemedCollectionSession>();
        session.catalog = catalog;
        session.suspendPersistenceForTests = true;
        // Exercise real session initialization without spawning the physical collection or touching persistence.
        go.SetActive(true);
        session.enabled = false;
        string stateBefore = JsonUtility.ToJson(session.State);
        var root = new GameObject("Localization test label", typeof(RectTransform), typeof(TextMeshProUGUI));
        var label = root.GetComponent<TMP_Text>();
        var binding = root.AddComponent<LocalizedText>();
        binding.SetKey("collection.carrying", new Localizer.Value(catalog.themes[0].NameKey));
        session.ReportKey("collection.discovered", new Localizer.Value(catalog.entries[0].NameKey));
        foreach (var language in Localizer.Languages)
        {
            Localizer.SetLanguage(language.Code);
            Assert.AreEqual(language.Code, LocalizationSettings.SelectedLocale.Identifier.Code);
            Assert.AreEqual(language.Code, PlayerPrefs.GetString("Language"));
            var officialString = LocalizationSettings.StringDatabase.GetLocalizedStringAsync("GameText", "collection.title", Localizer.Data.FindLocale(language.Code));
            yield return officialString;
            Assert.AreEqual(Localizer.Get("collection.title"), officialString.Result);
            Assert.AreEqual(Localizer.Format("collection.discovered", catalog.entries[0].DisplayName), session.Message);
            string expected = Localizer.Format("collection.carrying", catalog.themes[0].DisplayName);
            Assert.AreEqual(Rendered(expected), label.text);
            root.SetActive(false);
            yield return LocalizationSettings.InitializationOperation;
            Localizer.SetLanguage("en");
            yield return LocalizationSettings.InitializationOperation;
            Localizer.SetLanguage(language.Code);
            yield return LocalizationSettings.InitializationOperation;
            root.SetActive(true);
            Assert.AreEqual(Rendered(expected), label.text);
            Assert.AreEqual(stateBefore, JsonUtility.ToJson(session.State));
            Assert.AreSame(language.Code == "ar" ? Localizer.Data.arabicFont : Localizer.Data.latinFont, label.font);
            yield return null;
        }
        // Switching through the package directly must also propagate through the compatibility facade.
        LocalizationSettings.SelectedLocale = Localizer.Data.FindLocale("es");
        for (int i = 0; i < 120 && label.text != Rendered(Localizer.Format("collection.carrying", catalog.themes[0].DisplayName)); i++) yield return null;
        Assert.AreEqual("es", Localizer.CurrentCode);
        Assert.AreEqual(Rendered(Localizer.Format("collection.carrying", catalog.themes[0].DisplayName)), label.text);
        foreach (var language in Localizer.Languages) Localizer.SetLanguage(language.Code);
        yield return LocalizationSettings.InitializationOperation;
        Assert.AreEqual("ar", Localizer.CurrentCode, "Rapid changes retain the last selection.");
        Object.Destroy(root); Object.Destroy(go);
        File.WriteAllText(Evidence + "/runtime-switching.txt", "PASS: all eight languages; Unity locale source; immediate label and message refresh; names; inactive panel reopen; preference persistence; unchanged session state.\n");
        yield return new ExitPlayMode();
        RestoreUserState();
    }

    static string Rendered(string value) => !Localizer.IsRtl || !ArabicText.NeedsShaping(value) ? value
        : value.Length <= ArabicText.MaxVisualLength && !value.Contains("\n") ? ArabicText.ToVisual(value) : ArabicText.ToLayout(value);

    [UnityTest, Timeout(180000)]
    public IEnumerator VisibleScreens_AllLanguagesFitMenuAndLoading()
    {
        SessionState.SetBool("CompleteLocalization.CameraSave", EditorPrefs.GetBool("SaveDuringPlay_Enabled", false));
        SessionState.SetBool("CompleteLocalization.CameraSaveChanged", true);
        EditorPrefs.SetBool("SaveDuringPlay_Enabled", false);
        var setup = EditorSceneManager.GetSceneManagerSetup();
        SessionState.SetString("CompleteLocalization.SceneSetup", JsonUtility.ToJson(new SavedSceneSetup { paths = setup.Where(s => s.isLoaded).Select(s => s.path).ToArray(), activePath = setup.FirstOrDefault(s => s.isActive)?.path }));
        SessionState.SetBool("CompleteLocalization.HadLanguage", PlayerPrefs.HasKey("Language"));
        SessionState.SetString("CompleteLocalization.Language", PlayerPrefs.GetString("Language", "en"));
        SessionState.SetBool("CompleteLocalization.Active", true);
        PlayerPrefs.SetString("Language", "en");
        EditorSceneManager.OpenScene("Assets/00_Project/00_Scenes/MainMenu Scene.unity");
        CinematicMenuTests.SetGameSize(1280, 800);
        yield return new EnterPlayMode();
        yield return LocalizationSettings.InitializationOperation;
        Directory.CreateDirectory(Evidence);
        for (int i = 0; i < 10; i++) yield return null;
        foreach (var language in Localizer.Languages)
        {
            Localizer.SetLanguage(language.Code);
            yield return LocalizationSettings.InitializationOperation;
            for (int i = 0; i < 3; i++) yield return null;
            Canvas.ForceUpdateCanvases();
            foreach (var binding in Object.FindObjectsByType<LocalizedText>(FindObjectsSortMode.None))
            {
                var label = binding.GetComponent<TextMeshProUGUI>();
                if (label == null || label.canvas == null || label.canvas.renderMode == RenderMode.WorldSpace || string.IsNullOrEmpty(binding.Key)) continue;
                Assert.AreEqual(Rendered(Localizer.Get(binding.Key)), label.text, language.Code + " " + binding.Key);
                label.ForceMeshUpdate();
                Assert.IsFalse(label.isTextOverflowing, language.Code + " overflowing " + binding.Key);
            }
            ScreenCapture.CaptureScreenshot(Path.GetFullPath(Evidence + "/menu-" + language.Code + ".png"));
            for (int i = 0; i < 3; i++) yield return null;
        }
        var loading = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/00_Project/02_prefabs/UI/StoreLoadingPresentation.prefab"));
        foreach (var language in Localizer.Languages)
        {
            Localizer.SetLanguage(language.Code);
            yield return LocalizationSettings.InitializationOperation;
            Canvas.ForceUpdateCanvases();
            foreach (var label in loading.GetComponentsInChildren<TMP_Text>())
            {
                label.ForceMeshUpdate();
                Assert.IsFalse(label.isTextOverflowing, language.Code + " loading text overflow");
                Assert.IsNotEmpty(label.text);
            }
            if (language.Code == "ar" || language.Code == "pl")
            {
                ScreenCapture.CaptureScreenshot(Path.GetFullPath(Evidence + "/loading-" + language.Code + ".png"));
                for (int i = 0; i < 3; i++) yield return null;
            }
        }
        Object.Destroy(loading);
        File.WriteAllText(Evidence + "/visual-layout.txt", "PASS: menu and loading labels in all eight languages at 1280x800; no overflow; localized menu screenshots and Arabic/Polish loading screenshots.\n");
        yield return new ExitPlayMode();
        RestoreUserState();
    }

    [Serializable] class SavedSceneSetup { public string[] paths; public string activePath; }
    [UnityTearDown] public IEnumerator Cleanup()
    {
        if (Application.isPlaying) yield return new ExitPlayMode();
        RestoreUserState();
    }
    public void RestoreUserState()
    {
        if (SessionState.GetBool("CompleteLocalization.CameraSaveChanged", false))
        {
            EditorPrefs.SetBool("SaveDuringPlay_Enabled", SessionState.GetBool("CompleteLocalization.CameraSave", false));
            SessionState.SetBool("CompleteLocalization.CameraSaveChanged", false);
        }
        if (!SessionState.GetBool("CompleteLocalization.Active", false)) return;
        if (Application.isPlaying) return;
        string code = SessionState.GetString("CompleteLocalization.Language", "en");
        if (SessionState.GetBool("CompleteLocalization.HadLanguage", false)) PlayerPrefs.SetString("Language", code);
        else PlayerPrefs.DeleteKey("Language");
        PlayerPrefs.Save(); Localizer.RefreshDatabase();
        string json = SessionState.GetString("CompleteLocalization.SceneSetup", "");
        SessionState.EraseString("CompleteLocalization.Language");
        SessionState.SetBool("CompleteLocalization.Active", false);
        if (!string.IsNullOrEmpty(json))
        {
            var saved = JsonUtility.FromJson<SavedSceneSetup>(json);
            if (saved.paths != null && saved.paths.Any(p => !string.IsNullOrEmpty(p)))
                EditorSceneManager.RestoreSceneManagerSetup(saved.paths.Where(p => !string.IsNullOrEmpty(p)).Select(p => new SceneSetup { path = p, isLoaded = true, isActive = p == saved.activePath }).ToArray());
        }
    }
}
