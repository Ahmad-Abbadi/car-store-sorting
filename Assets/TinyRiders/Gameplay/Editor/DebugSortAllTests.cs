using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TinyRiders;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public class DebugSortAllTests
{
    internal const string Key = "TinyRiders.DebugSortAll.QA";
    internal const string Active = Key + ".Active";
    const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    static ThemedCollectionSession Session => ThemedCollectionSession.Instance;
    static LevelSaveManager Save => Object.FindFirstObjectByType<LevelSaveManager>();
    static SortingShelf[] Shelves => Object.FindObjectsByType<SortingShelf>(FindObjectsSortMode.None);

    static void Prepare(bool boundedPile)
    {
        // Enter/ExitPlayMode preserves the current editor scene, including unsaved edits.
        // Only switch scenes if another clean scene was open when the tests were invoked.
        if (SceneManager.GetActiveScene().path != CompactCollectionBuilder.ScenePath)
        {
            Assert.False(Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty),
                "Open Level before running these tests when another scene has unsaved edits.");
            EditorSceneManager.OpenScene(CompactCollectionBuilder.ScenePath);
        }
        PlayerPrefs.DeleteKey(Key);
        PlayerPrefs.DeleteKey(Key + ".coins");
        PlayerPrefs.DeleteKey(Key + ".position");
        SessionState.SetBool(Active, true);
        SessionState.SetBool(Key + ".Bounded", boundedPile);
    }

    static IEnumerator WaitUntilReady()
    {
        Application.runInBackground = true;
        while (Session == null || Player.Instance == null || Time.frameCount < 10) yield return null;
        Session.suspendPersistenceForTests = true;
        Time.timeScale = 1;
        GameFlowManager.Instance?.SetState(GameFlowManager.GameState.Playing);
    }

    static void AssertSorted()
    {
        Assert.AreEqual(Session.Total, Session.Sorted);
        Assert.AreEqual(Session.ActiveDefinitions.Count(), Session.UniqueDiscovered);
        Assert.AreEqual(Session.ActiveDefinitions.Select(e => e.themeId).Distinct().Count(), Session.State.completedThemes.Count);
        Assert.AreEqual(0, Session.LivePileCount);
        Assert.AreEqual(0, Player.Instance.StashHandler.Inventory.Count);
        Assert.IsNull(Player.Instance.ItemHandler.CurrentEquipped);
        var occupants = Shelves.SelectMany(s => Enumerable.Range(0, s.SpotCount).Select(s.GetOccupant)).Where(i => i != null).ToArray();
        Assert.AreEqual(Session.Total, occupants.Length);
        Assert.AreEqual(Session.Total, occupants.Select(ThemedWorldInstance.Read).Distinct().Count());
        foreach (var item in occupants)
        {
            Assert.True(item.Shelf.IsCorrectItem(item.Data));
            Assert.True(item.IsSettled);
            Assert.AreEqual(Vector3.zero, item.transform.localPosition);
            Assert.Less(Quaternion.Angle(Quaternion.identity, item.transform.localRotation), .001f);
            var record = Session.State.instances.Single(r => r.id == ThemedWorldInstance.Read(item));
            Assert.AreEqual("sorted", record.state);
            Assert.AreEqual(record.shelfId, item.Shelf.SaveId);
            Assert.AreEqual(record.slotId, item.transform.parent.name);
        }
        Assert.False(WorldItemBase.All.Any(w => w.gameObject.activeInHierarchy && Session.Definition(w.Data) != null));
        Assert.AreEqual(Session.Total, SortingProgressTracker.Instance.Collected);
    }

    [UnityTest, Timeout(240000)]
    public IEnumerator FreshCollection_F8SortsCompletesSavesAndReloads()
    {
        Prepare(false);
        yield return new EnterPlayMode();
        yield return ExerciseFresh();
    }

    static IEnumerator ExerciseFresh()
    {
        yield return WaitUntilReady();
        var reward = Object.FindFirstObjectByType<GoldenCarReveal>();
        Assert.NotNull(reward);
        Assert.False(reward.IsRevealed);
        Assert.True(reward.cover.gameObject.activeSelf);
        Assert.False(reward.trophy.activeSelf);
        var originalCamera = Camera.main;
        Vector3 cameraPosition = originalCamera.transform.localPosition;
        Quaternion cameraRotation = originalCamera.transform.localRotation;
        Assert.AreEqual(1200, Session.Total);
        Assert.AreEqual(Session.Total, Session.LivePileCount);
        Session.suspendPersistenceForTests = false;
        var testSettings = InputSystem.settings;
        var originalEditorBehavior = testSettings.editorInputBehaviorInPlayMode;
        var originalBackgroundBehavior = testSettings.backgroundBehavior;
        testSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        testSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        var keyboard = InputSystem.AddDevice<Keyboard>();
        try
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(UnityEngine.InputSystem.Key.F8));
            InputSystem.Update();
            Assert.True(keyboard.f8Key.wasPressedThisFrame, "Synthetic F8 must reach the game's Input System");
            Session.SendMessage("Update");
            int nextFrame = Time.frameCount + 3;
            while (Time.frameCount < nextFrame) yield return null;
        }
        finally
        {
            InputSystem.RemoveDevice(keyboard);
            testSettings.editorInputBehaviorInPlayMode = originalEditorBehavior;
            testSettings.backgroundBehavior = originalBackgroundBehavior;
        }
        AssertSorted();
        Assert.AreEqual("Debug: All cars sorted", Session.Message);
        Assert.True(Save.StoreCompleted);
        Assert.True(JsonUtility.FromJson<LevelSaveData>(PlayerPrefs.GetString(Key)).completed);
        float until = Time.realtimeSinceStartup + .8f;
        while (Time.realtimeSinceStartup < until) yield return null;
        Assert.AreEqual(GameFlowManager.GameState.Freeze, GameFlowManager.Instance.currentState);
        Assert.True(reward.IsRevealing);
        yield return new WaitForSecondsRealtime(reward.duration + .5f);
        Assert.True(reward.IsRevealed);
        Assert.False(reward.IsRevealing);
        Assert.True(reward.trophy.activeSelf);
        Assert.False(reward.cover.gameObject.activeSelf);
        Assert.AreEqual(GameFlowManager.GameState.Playing, GameFlowManager.Instance.currentState);
        Assert.True(originalCamera.enabled);
        Assert.Less(Vector3.Distance(cameraPosition, originalCamera.transform.localPosition), .02f);
        Assert.Less(Quaternion.Angle(cameraRotation, originalCamera.transform.localRotation), 1f);
        Assert.IsEmpty(reward.trophy.GetComponentsInChildren<WorldItemBase>(true));
        Assert.IsEmpty(reward.trophy.GetComponentsInChildren<Collider>(true));
        var ui = Object.FindFirstObjectByType<CarStoreUI>();
        Assert.False(((GameObject)typeof(CarStoreUI).GetField("finish", Private).GetValue(ui)).activeSelf);
        GameFlowManager.Instance.PauseGame();
        Assert.True(GameFlowManager.Instance.IsPaused);
        GameFlowManager.Instance.ResumeGame();

        var ids = Shelves.SelectMany(s => Enumerable.Range(0, s.SpotCount).Select(s.GetOccupant)).Select(i => i.GetInstanceID()).OrderBy(i => i).ToArray();
        string saved = PlayerPrefs.GetString(Key);
        int coins = CoinsManager.Instance.Coins;
        Assert.True(Session.DebugSortAll());
        Assert.AreEqual(saved, PlayerPrefs.GetString(Key));
        Assert.AreEqual(coins, CoinsManager.Instance.Coins);
        CollectionAssert.AreEqual(ids, Shelves.SelectMany(s => Enumerable.Range(0, s.SpotCount).Select(s.GetOccupant)).Select(i => i.GetInstanceID()).OrderBy(i => i).ToArray());

        Session.suspendPersistenceForTests = true;
        var previous = Session;
        yield return SceneManager.LoadSceneAsync(CompactCollectionBuilder.ScenePath);
        while (Session == null || ReferenceEquals(previous, Session) || Time.frameCount < 3) yield return null;
        yield return null;
        yield return null;
        AssertSorted();
        Assert.True(Save.StoreCompleted);
        reward = Object.FindFirstObjectByType<GoldenCarReveal>();
        Assert.True(reward.IsRevealed);
        Assert.False(reward.IsRevealing, "Completed saves must not replay the cinematic");
        Assert.AreEqual(GameFlowManager.GameState.Playing, GameFlowManager.Instance.currentState);
    }

    static WorldItemBase Pick()
    {
        var world = WorldItemBase.All.First(w => w.gameObject.activeInHierarchy && Session.Definition(w.Data) != null);
        Assert.True(Player.Instance.ItemCollector.TryCollectItem(world));
        return world;
    }

    static PlacedOnShelfItemBase Place(bool correct)
    {
        Assert.True(Player.Instance.SortingBehaviour.TryGetPlacedItemFromHand(out var placed));
        var shelf = Shelves.First(s => s.IsCorrectItem(placed.Data) == correct && s.CanAccept(placed.Data, 0, out _));
        shelf.AddItem(placed, 0);
        return placed;
    }

    [UnityTest, Timeout(240000)]
    public IEnumerator MixedCollection_PreservesSortedAndConsumesHeldDroppedMisplacedAndPending()
    {
        Prepare(true);
        yield return new EnterPlayMode();
        yield return ExerciseMixed();
    }

    static IEnumerator ExerciseMixed()
    {
        yield return WaitUntilReady();
        Assert.Greater(Session.State.instances.Count(r => r.state == "pending"), 0);
        Pick();
        yield return new WaitForSecondsRealtime(.6f);
        var correct = Place(true);
        Pick();
        yield return new WaitForSecondsRealtime(.6f);
        var wrong = Place(false);
        Pick();
        yield return new WaitForSecondsRealtime(.6f);
        typeof(ItemsHandler).GetMethod("ExecuteThrow", Private).Invoke(Player.Instance.ItemHandler, new object[] { 0f });
        Pick(); // F8 while the pickup flight and previous throw are still running.
        Assert.Greater(Player.Instance.StashHandler.Inventory.Count, 0);
        Assert.True(Session.State.instances.Any(r => r.state == "held"));
        Assert.True(Session.State.instances.Any(r => r.state == "misplaced"));
        Assert.True(Session.State.instances.Any(r => r.state == "world"));
        var correctId = ThemedWorldInstance.Read(correct);
        var oldTransform = correct.transform.parent;
        int notifications = 0;
        Action changed = () => notifications++;
        Session.Changed += changed;
        try { Assert.True(Session.DebugSortAll()); }
        finally { Session.Changed -= changed; }
        Assert.AreEqual(1, notifications, "Publish progress once for the batch");
        yield return null;
        yield return null;
        AssertSorted();
        Assert.True(correct != null);
        Assert.AreEqual(correctId, ThemedWorldInstance.Read(correct));
        Assert.AreEqual(oldTransform, correct.transform.parent);
        Assert.True(wrong == null);
        yield return new WaitForSecondsRealtime(.8f);
    }

    [UnityTest, Timeout(240000)]
    public IEnumerator MissingCompatibleCapacity_DoesNotChangeCarsInventoryProgressOrSave()
    {
        Prepare(true);
        yield return new EnterPlayMode();
        yield return ExerciseMissingCapacity();
    }

    static IEnumerator ExerciseMissingCapacity()
    {
        yield return WaitUntilReady();
        Pick();
        yield return new WaitForSecondsRealtime(.6f);
        var wrong = Place(false);
        Pick();
        // Remove one compatible slot while preserving total slot count and scene objects.
        var shelf = Shelves.First(s => s.GetOccupant(0) == null);
        var binding = shelf.GetComponent<ThemeShelfBinding>();
        var clearance = binding.clearance;
        binding.clearance = Vector3.zero;
        var state = JsonUtility.ToJson(Session.Capture());
        var saved = PlayerPrefs.GetString(Key);
        var worlds = WorldItemBase.All.Where(w => w.gameObject.activeInHierarchy).Select(w => w.GetInstanceID()).OrderBy(i => i).ToArray();
        var hand = Player.Instance.ItemHandler.CurrentEquipped;
        int count = Player.Instance.StashHandler.Inventory.Count;
        try { Assert.False(Session.DebugSortAll()); }
        finally { binding.clearance = clearance; }
        StringAssert.Contains("insufficient compatible shelf space", Session.Message);
        Assert.AreEqual(state, JsonUtility.ToJson(Session.Capture()));
        Assert.AreEqual(saved, PlayerPrefs.GetString(Key));
        Assert.AreSame(hand, Player.Instance.ItemHandler.CurrentEquipped);
        Assert.AreEqual(count, Player.Instance.StashHandler.Inventory.Count);
        Assert.True(wrong != null);
        CollectionAssert.AreEqual(worlds, WorldItemBase.All.Where(w => w.gameObject.activeInHierarchy).Select(w => w.GetInstanceID()).OrderBy(i => i).ToArray());
        Assert.True(Session.DebugSortAll(), "A failed preflight must release the reentrancy guard");
        yield return null;
        AssertSorted();
    }

    static void AssertReset()
    {
        var reward = Object.FindFirstObjectByType<GoldenCarReveal>();
        Assert.False(reward.IsRevealed);
        Assert.False(reward.IsRevealing);
        Assert.True(reward.cover.gameObject.activeSelf);
        Assert.False(reward.trophy.activeSelf);
        Assert.True(Camera.main.enabled);
        Assert.AreEqual(0, Session.Sorted);
        Assert.AreEqual(0, Session.UniqueDiscovered);
        Assert.IsEmpty(Session.State.completedThemes);
        Assert.IsEmpty(Session.State.discovered);
        Assert.IsEmpty(Session.State.heldOrder);
        Assert.AreEqual(0, Player.Instance.StashHandler.Inventory.Count);
        Assert.IsNull(Player.Instance.ItemHandler.CurrentEquipped);
        Assert.False(Save.StoreCompleted);
        Assert.AreEqual(GameFlowManager.GameState.Playing, GameFlowManager.Instance.currentState);
        Assert.AreEqual(1f, Time.timeScale);
        Assert.AreEqual(Session.Total, Session.LivePileCount);
        var worlds = WorldItemBase.All.Where(w => w.gameObject.activeInHierarchy && Session.Definition(w.Data) != null).ToArray();
        Assert.AreEqual(Session.Total, worlds.Length);
        CollectionAssert.AreEquivalent(Session.State.instances.Select(r => r.id), worlds.Select(ThemedWorldInstance.Read));
        Assert.True(Session.State.instances.All(r => r.state == "world" && r.shelfId == null && r.slotId == null));
        Assert.True(Shelves.All(s => Enumerable.Range(0, s.SpotCount).All(i => s.GetOccupant(i) == null)));
        Assert.AreEqual(0, SortingProgressTracker.Instance.Collected);
    }

    [UnityTest, Timeout(240000)]
    public IEnumerator ResetCollection_F9ResumesCompletedCollectionAndPersistsPile()
    {
        Prepare(false);
        yield return new EnterPlayMode();
        yield return ExerciseResetCompleted();
    }

    static IEnumerator ExerciseResetCompleted()
    {
        yield return WaitUntilReady();
        Session.suspendPersistenceForTests = false;
        int coins = CoinsManager.Instance.Coins;
        var ids = Session.State.instances.Select(r => r.id).ToArray();
        Assert.True(Session.DebugSortAll());
        yield return new WaitForSecondsRealtime(.8f);
        Assert.AreEqual(GameFlowManager.GameState.Freeze, GameFlowManager.Instance.currentState);
        Assert.True(Object.FindFirstObjectByType<GoldenCarReveal>().IsRevealing);
        var settings = InputSystem.settings;
        var editorBehavior = settings.editorInputBehaviorInPlayMode;
        var backgroundBehavior = settings.backgroundBehavior;
        settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        var keyboard = InputSystem.AddDevice<Keyboard>();
        try
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(UnityEngine.InputSystem.Key.F9));
            InputSystem.Update();
            Assert.True(keyboard.f9Key.wasPressedThisFrame);
            Session.SendMessage("Update");
        }
        finally
        {
            InputSystem.RemoveDevice(keyboard);
            settings.editorInputBehaviorInPlayMode = editorBehavior;
            settings.backgroundBehavior = backgroundBehavior;
        }
        yield return null;
        AssertReset();
        Assert.AreEqual(coins, CoinsManager.Instance.Coins);
        CollectionAssert.AreEquivalent(ids, Session.State.instances.Select(r => r.id));
        var saved = JsonUtility.FromJson<LevelSaveData>(PlayerPrefs.GetString(Key));
        Assert.False(saved.completed);
        Assert.True(saved.themed.instances.All(r => r.state == "world"));
        Assert.IsEmpty(saved.themed.discovered);
        var ui = Object.FindFirstObjectByType<CarStoreUI>();
        Assert.False(((GameObject)typeof(CarStoreUI).GetField("finish", Private).GetValue(ui)).activeSelf);

        Session.suspendPersistenceForTests = true;
        var previous = Session;
        yield return SceneManager.LoadSceneAsync(CompactCollectionBuilder.ScenePath);
        while (Session == null || ReferenceEquals(previous, Session) || Session.LivePileCount != Session.Total) yield return null;
        yield return null;
        AssertReset();
        Assert.True(Session.DebugSortAll());
        Assert.True(Session.DebugResetCollection()); // Also reset during the pending completion delay.
        yield return new WaitForSecondsRealtime(.8f);
        AssertReset();
    }

    [UnityTest, Timeout(240000)]
    public IEnumerator ResetCollection_MixedStatesAndRepeatedResetHaveOnePileCopyPerCar()
    {
        Prepare(true);
        yield return new EnterPlayMode();
        yield return ExerciseResetMixed();
    }

    static IEnumerator ExerciseResetMixed()
    {
        yield return WaitUntilReady();
        Pick();
        yield return new WaitForSecondsRealtime(.6f);
        Place(true);
        Pick();
        yield return new WaitForSecondsRealtime(.6f);
        Place(false);
        Pick();
        yield return new WaitForSecondsRealtime(.6f);
        typeof(ItemsHandler).GetMethod("ExecuteThrow", Private).Invoke(Player.Instance.ItemHandler, new object[] { 0f });
        Pick(); // Reset while holding a car, with another car still in its throw animation.
        Assert.True(Session.State.instances.Any(r => r.state == "held"));
        Assert.True(Session.State.instances.Any(r => r.state == "misplaced"));
        Assert.True(Session.State.instances.Any(r => r.state == "pending"));
        int coins = CoinsManager.Instance.Coins;
        int notifications = 0;
        Action changed = () => notifications++;
        Session.Changed += changed;
        try { Assert.True(Session.DebugResetCollection()); }
        finally { Session.Changed -= changed; }
        Assert.AreEqual(1, notifications);
        yield return new WaitForSecondsRealtime(.8f);
        AssertReset();
        Assert.AreEqual(coins, CoinsManager.Instance.Coins);
        Assert.True(Session.DebugResetCollection());
        yield return null;
        AssertReset();
        Assert.True(Session.DebugSortAll());
        yield return null;
        AssertSorted();
    }

    [UnityTest, Timeout(240000)]
    public IEnumerator GoldenCar_LastNormalPlacementRevealsAndDisableRestoresControls()
    {
        Prepare(false);
        yield return new EnterPlayMode();
        yield return WaitUntilReady();
        // Fill the shop without completing it, then take back one real car.
        Session.completeStoreWhenSorted = false;
        Assert.True(Session.DebugSortAll());
        var shelf = Shelves.First(s => s.GetOccupant(0) != null);
        Assert.True(shelf.TryTakeBack(shelf.GetOccupant(0)));
        yield return new WaitForSecondsRealtime(.8f);
        Session.completeStoreWhenSorted = true;
        var reward = Object.FindFirstObjectByType<GoldenCarReveal>();
        Assert.AreEqual(Session.Total - 1, Session.Sorted);
        Assert.False(Save.StoreCompleted);
        Assert.False(reward.IsRevealed);
        Assert.False(reward.IsRevealing);
        Assert.True(reward.cover.gameObject.activeSelf);
        Assert.True(Player.Instance.SortingBehaviour.TryGetPlacedItemFromHand(out var placed));
        shelf.AddItem(placed, 0);
        float timeout = Time.realtimeSinceStartup + 5f;
        while (!reward.IsRevealing && Time.realtimeSinceStartup < timeout) yield return null;
        Assert.True(Save.StoreCompleted);
        Assert.True(reward.IsRevealing);
        // Let the temporary camera take ownership before interrupting the sequence.
        yield return new WaitForSecondsRealtime(.2f);
        reward.enabled = false;
        yield return null;
        Assert.True(reward.IsRevealed);
        Assert.False(reward.IsRevealing);
        Assert.True(Camera.main.enabled);
        Assert.AreEqual(GameFlowManager.GameState.Playing, GameFlowManager.Instance.currentState);
        Assert.IsTrue(Player.Instance.Motor.enabled);
        Assert.IsEmpty(Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Where(c => c.name == "Golden Car Reveal Camera"));
    }

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        if (EditorApplication.isPlaying)
        {
            if (Session != null) Session.suspendPersistenceForTests = true;
            yield return new ExitPlayMode();
        }
        SessionState.SetBool(Active, false);
        PlayerPrefs.DeleteKey(Key);
        PlayerPrefs.DeleteKey(Key + ".position");
        PlayerPrefs.DeleteKey(Key + ".coins");
        PlayerPrefs.Save();
    }
}

[InitializeOnLoad]
internal static class DebugSortAllTestIsolation
{
    static DebugSortAllTestIsolation()
    {
        SceneManager.sceneLoaded += (scene, mode) =>
        {
            if (!EditorApplication.isPlaying || !SessionState.GetBool(DebugSortAllTests.Active, false)) return;
            void Set(Object target, string field, object value)
            {
                if (target != null) target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
            }
            Set(Object.FindFirstObjectByType<LevelSaveManager>(), "saveKey", DebugSortAllTests.Key);
            Set(Object.FindFirstObjectByType<LevelSaveManager>(), "autosaveInterval", 0f);
            Set(Object.FindFirstObjectByType<PlayerSavePosition>(), "saveKey", DebugSortAllTests.Key + ".position");
            var coins = Object.FindFirstObjectByType<CoinsManager>();
            Set(coins, "saveKey", DebugSortAllTests.Key + ".coins");
            if (coins != null) typeof(CoinsManager).GetProperty(nameof(CoinsManager.Coins)).SetValue(coins, 0);
            var session = ThemedCollectionSession.Instance;
            if (session != null && SessionState.GetBool(DebugSortAllTests.Key + ".Bounded", false))
            { session.spawnEntireCollectionAtStart = false; session.visiblePileLimit = 8; }
            foreach (var probe in Object.FindObjectsByType<ThemedPerformanceProbe>(FindObjectsSortMode.None)) probe.enabled = false;
        };
    }
}
