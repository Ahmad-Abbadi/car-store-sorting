using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ByteBrewSDK;
using NUnit.Framework;
using TinyRiders.Quests;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class ByteBrewIntegrationTests
{
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    private readonly List<string> events = new();
    private GameObject root;
    private GameAnalytics analytics;

    [SetUp]
    public void SetUp()
    {
        root = new GameObject("Analytics test");
        root.SetActive(false);
        analytics = root.AddComponent<GameAnalytics>();
        analytics.logEventsInEditor = true;
        Application.logMessageReceived += Capture;
        Call(analytics, "OnEnable");
    }

    [TearDown]
    public void TearDown()
    {
        Call(analytics, "OnDisable");
        Application.logMessageReceived -= Capture;
        Object.DestroyImmediate(root);
        events.Clear();
    }

    private void Capture(string text, string stack, LogType type)
    { if (text.StartsWith("[GameAnalytics] ")) events.Add(text); }
    private static void Call(object target, string method, params object[] args) =>
        target.GetType().GetMethod(method, PrivateInstance).Invoke(target, args);
    private static void Raise(Type type, string name, object target, params object[] args) =>
        ((Delegate)type.GetField(name, BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance).GetValue(target))?.DynamicInvoke(args);

    [Test]
    public void SettingsMenuResolvesTheExistingAssetBeforeDelayedInitialization()
    {
        var cache = typeof(ByteBrewSettingsHandler).GetField("_settingsInstance", BindingFlags.Static | BindingFlags.NonPublic);
        var previous = cache.GetValue(null);
        try
        {
            cache.SetValue(null, null);
            Assert.AreSame(AssetDatabase.LoadAssetAtPath<ByteBrewSettings>("Assets/ByteBrewSDK/Resources/ByteBrewSettings.asset"),
                ByteBrewSettingsHandler.SettingsInstance);
            Assert.IsNotNull(ByteBrewSettingsHandler.SettingsInstance);
        }
        finally { cache.SetValue(null, previous); }
    }

    [Test]
    public void CredentialsRequireEnabledWebAndNonWhitespaceKeys()
    {
        var settings = ScriptableObject.CreateInstance<ByteBrewSettings>();
        try
        {
            Assert.IsFalse(ByteBrewInitializer.HasWebCredentials(null));
            settings.webEnabled = true;
            Assert.IsFalse(ByteBrewInitializer.HasWebCredentials(settings));
            settings.webGameID = "game"; settings.webSDKKey = "  ";
            Assert.IsFalse(ByteBrewInitializer.HasWebCredentials(settings));
            settings.webSDKKey = "key";
            Assert.IsTrue(ByteBrewInitializer.HasWebCredentials(settings));
            settings.webEnabled = false;
            Assert.IsFalse(ByteBrewInitializer.HasWebCredentials(settings));
        }
        finally { Object.DestroyImmediate(settings); }
    }

    [Test]
    public void LevelEventsUseDestinationSceneAndUnsubscribeWithoutDuplicates()
    {
        Call(analytics, "OnEnable");
        LevelsManager.RaiseLevelEntered(2, "ThemedSorting");
        Raise(typeof(LevelsManager), "OnLevelCompleted", null, 2);
        Assert.AreEqual(2, events.Count);
        StringAssert.Contains("level_started scene=ThemedSorting;level=2;", events[0]);
        StringAssert.Contains("level_completed", events[1]);
        Call(analytics, "OnDisable");
        LevelsManager.RaiseLevelEntered(3, "Level");
        Assert.AreEqual(2, events.Count);
        Call(analytics, "OnEnable");
        LevelsManager.RaiseLevelEntered(4, "Level");
        Assert.AreEqual(3, events.Count);
        Assert.IsFalse(ByteBrewInitializer.ReportingEnabled);
        Assert.IsFalse(ByteBrew.IsInitilized, "Editor logging must never initialize the SDK.");
    }

    [Test]
    public void StoreCompletionOnlyReportsNewProgressAndNeverWritesAnUnreadySave()
    {
        var save = root.AddComponent<LevelSaveManager>();
        var instanceField = typeof(LevelSaveManager).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);
        var previous = instanceField.GetValue(null);
        var completedField = typeof(LevelSaveManager).GetField("_storeCompleted", PrivateInstance);
        try
        {
            Call(save, "Awake");
            completedField.SetValue(save, true); // State restored from an already completed save.
            LevelSaveManager.MarkStoreCompleted();
            Assert.IsEmpty(events);
            completedField.SetValue(save, false);
            LevelSaveManager.MarkStoreCompleted();
            LevelSaveManager.MarkStoreCompleted();
            Assert.AreEqual(1, events.Count);
            StringAssert.Contains("level_completed", events[0]);
        }
        finally { instanceField.SetValue(null, previous); }
    }

    [Test]
    public void DirectStoreEntryAndCompletionAreTrackedWithoutDoubleCountingManagerEvents()
    {
        var scene = EditorSceneManager.NewPreviewScene();
        scene.name = "Level";
        try
        {
            Call(analytics, "SceneLoaded", scene, UnityEngine.SceneManagement.LoadSceneMode.Single);
            Raise(typeof(LevelSaveManager), "OnStoreCompleted", null, "Level");
            Raise(typeof(LevelsManager), "OnLevelCompleted", null, 1);
            Assert.AreEqual(2, events.Count);
            StringAssert.Contains("level_started scene=Level;level=1;", events[0]);
            StringAssert.Contains("level_completed scene=Level;level=1;", events[1]);
            LevelsManager.RaiseLevelEntered(1, "Level");
            Call(analytics, "SceneLoaded", scene, UnityEngine.SceneManagement.LoadSceneMode.Single);
            Assert.AreEqual(3, events.Count, "Managed entry must not also report a direct entry.");
            Raise(typeof(LevelSaveManager), "OnStoreCompleted", null, "Level");
            Assert.AreEqual(4, events.Count, "A replay can complete again.");
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }

    [Test]
    public void TutorialStepsAreUniquePerAttemptAndExitEventsAreDistinct()
    {
        var tutorial = root.AddComponent<TutorialController>();
        Raise(typeof(TutorialController), "Started", null, tutorial);
        Call(tutorial, "PublishStep");
        Call(tutorial, "SetStep", TutorialController.Lesson.Pickup);
        Call(tutorial, "SetStep", TutorialController.Lesson.Match);
        Call(tutorial, "SetStep", TutorialController.Lesson.Pickup);
        Call(tutorial, "SetStep", TutorialController.Lesson.Practice);
        Call(tutorial, "SetStep", TutorialController.Lesson.Complete);
        Raise(typeof(TutorialController), "Finished", null, tutorial, false);
        Assert.AreEqual(5, events.Count(e => e.Contains("tutorial_step ")));
        Assert.AreEqual(1, events.Count(e => e.Contains("tutorial_completed ")));
        var replay = root.AddComponent<TutorialController>();
        Raise(typeof(TutorialController), "Started", null, replay);
        Call(replay, "PublishStep");
        Raise(typeof(TutorialController), "Finished", null, replay, true);
        Assert.AreEqual(6, events.Count(e => e.Contains("tutorial_step ")));
        Assert.AreEqual(1, events.Count(e => e.Contains("tutorial_skipped ")));
    }

    [Test]
    public void QuestBindingDoesNotReplaySavedProgressAndRebindsToReplacement()
    {
        var property = typeof(CollectorQuestManager).GetProperty("Instance");
        var previous = property.GetValue(null);
        var first = root.AddComponent<CollectorQuestManager>();
        var nextRoot = new GameObject("Replacement quest manager");
        nextRoot.SetActive(false);
        var next = nextRoot.AddComponent<CollectorQuestManager>();
        try
        {
            property.SetValue(null, first);
            Call(analytics, "BindQuests");
            Call(analytics, "BindQuests");
            Assert.IsEmpty(events, "Binding must not inspect and replay saved achievements.");
            var quest = new CollectorQuestView(new CollectorQuestRecord { id = "sorting_10", complete = true });
            Raise(typeof(CollectorQuestManager), "Completed", first, quest);
            Raise(typeof(CollectorQuestManager), "RankedUp", first, 2);
            Assert.AreEqual(2, events.Count);
            StringAssert.Contains("quest_id=sorting_10;", events[0]);
            property.SetValue(null, next);
            Call(analytics, "LateUpdate");
            Raise(typeof(CollectorQuestManager), "Completed", first, quest);
            Assert.AreEqual(2, events.Count, "Old manager must be unsubscribed.");
            Raise(typeof(CollectorQuestManager), "Completed", next, quest);
            Assert.AreEqual(3, events.Count);
        }
        finally
        {
            Call(analytics, "OnDisable");
            property.SetValue(null, previous);
            Object.DestroyImmediate(nextRoot);
        }
    }

    [TestCase("MainMenu Scene", "MainMenu_Scene")]
    [TestCase("quest:1=test;value.x", "quest_1_test_value_x")]
    public void ParameterValuesCannotBreakByteBrewEncoding(string value, string expected) =>
        Assert.AreEqual(expected, GameAnalytics.ParameterToken(value));

    [Test]
    public void StartupSceneHasOneSdkAndInitializerRegisteredAfterCrazyGames()
    {
        var scene = EditorSceneManager.OpenScene(ByteBrewIntegrationSetup.StartupScene, OpenSceneMode.Additive);
        try
        {
            var roots = scene.GetRootGameObjects();
            Assert.AreEqual(1, roots.SelectMany(go => go.GetComponentsInChildren<ByteBrew>(true)).Count());
            var initializer = roots.SelectMany(go => go.GetComponentsInChildren<ByteBrewInitializer>(true)).Single();
            Assert.IsNotNull(initializer.GetComponent<GameAnalytics>());
            var startup = roots.SelectMany(go => go.GetComponentsInChildren<GameInitializer>(true)).Single();
            var systems = new SerializedObject(startup).FindProperty("systems");
            var entries = Enumerable.Range(0, systems.arraySize).Select(i => systems.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
            Assert.AreEqual(1, entries.Count(x => x == initializer));
            Assert.IsTrue(entries.Any(x => x is CrazyGamesInitializer));
            Assert.Greater(Array.IndexOf(entries, initializer), Array.FindIndex(entries, x => x is CrazyGamesInitializer));
        }
        finally { EditorSceneManager.CloseScene(scene, true); }
    }
}
