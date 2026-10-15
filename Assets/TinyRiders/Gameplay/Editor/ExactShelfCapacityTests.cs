using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TinyRiders;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public class ExactShelfCapacityTests
{
    const string Key = "TinyRiders.Compact.QA";
    static ThemedCollectionSession Session => ThemedCollectionSession.Instance;
    static LevelSaveData Capture(LevelSaveManager manager) => (LevelSaveData)typeof(LevelSaveManager)
        .GetMethod("Capture", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(manager, null);
    static SortingShelf[] Shelves => Object.FindObjectsByType<SortingShelf>(FindObjectsSortMode.None).OrderBy(s => s.SaveId, StringComparer.Ordinal).ToArray();

    [Test]
    public void AuthoredRoom_ExactlyMatchesEveryThemeAndVisibleCompartment()
    {
        EditorSceneManager.OpenScene(CompactCollectionBuilder.ScenePath);
        ExactThemeShelfBuilder.ValidateScene();
        Assert.AreEqual(80, Shelves.Length);
        Assert.AreEqual(1200, Shelves.Sum(s => s.SpotCount));
        foreach (var shelf in Shelves)
        {
            Assert.AreEqual(15, shelf.SpotCount);
            var art = shelf.transform.Find("TR_Shelf_Base_01").gameObject;
            Assert.AreEqual(15, ExactThemeShelfBuilder.Markers(art).Length);
            Assert.AreEqual(12, art.GetComponentsInChildren<Transform>().Count(t => t.name.StartsWith("Divider_")));
            Assert.AreEqual(15, Enumerable.Range(0, shelf.SpotCount).Select(i => shelf.GetSpot(i).position).Distinct().Count());
        }
        var session = Object.FindFirstObjectByType<ThemedCollectionSession>();
        foreach (var entry in ThemedShelfCapacity.EligibleDefinitions(session.catalog))
        {
            var clearance = Shelves.First(s => s.GetComponent<ThemeShelfBinding>().themeId == entry.themeId).GetComponent<ThemeShelfBinding>().clearance;
            Assert.LessOrEqual(entry.displaySize.x, clearance.x, entry.collectibleId);
            Assert.LessOrEqual(entry.displaySize.y, clearance.y, entry.collectibleId);
            Assert.LessOrEqual(entry.displaySize.z, clearance.z, entry.collectibleId);
        }
        CaptureShelves("empty", Shelves.First(s => s.GetComponent<ThemeShelfBinding>().themeId == "TH_ANCIENT_EGYPT"));
    }

    [Test]
    public void CapacityAudit_RejectsPerThemeMismatchEvenWhenTotalMatches()
    {
        EditorSceneManager.OpenScene(CompactCollectionBuilder.ScenePath);
        var shelves = Shelves; var session = Object.FindFirstObjectByType<ThemedCollectionSession>();
        var binding = shelves[0].GetComponent<ThemeShelfBinding>(); string old = binding.themeId; var category = shelves[0].Catagory;
        var other = shelves.First(s => s.GetComponent<ThemeShelfBinding>().themeId != old);
        try
        {
            binding.themeId = other.GetComponent<ThemeShelfBinding>().themeId; shelves[0].SetCatagory(other.Catagory);
            Assert.AreEqual(1200, shelves.Sum(s => s.SpotCount));
            var error = Assert.Throws<InvalidOperationException>(() => ThemedShelfCapacity.Validate(session.catalog, session.copiesPerDesign, shelves));
            StringAssert.Contains(old, error.Message); StringAssert.Contains(binding.themeId, error.Message);
        }
        finally { binding.themeId = old; shelves[0].SetCatagory(category); EditorSceneManager.OpenScene(CompactCollectionBuilder.ScenePath); }
    }

    [Test]
    public void CapacityAudit_UsesSessionEligibilityAndCopyCount()
    {
        EditorSceneManager.OpenScene(CompactCollectionBuilder.ScenePath);
        var session = Object.FindFirstObjectByType<ThemedCollectionSession>();
        var catalog = Object.Instantiate(session.catalog);
        // Clone nested data so toggles never touch the asset's entries.
        JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(session.catalog), catalog);
        try
        {
            Assert.AreEqual(600, ThemedShelfCapacity.EligibleDefinitions(catalog).Count());
            catalog.entries[0].enabled = false; catalog.entries[1].prefab = null; catalog.entries[2].sortingItem = null;
            Assert.AreEqual(597, ThemedShelfCapacity.EligibleDefinitions(catalog).Count());
            Assert.Throws<InvalidOperationException>(() => ThemedShelfCapacity.Validate(catalog, 2, Shelves));
            catalog.themes.First(t => t.id == catalog.entries[3].themeId).enabled = false;
            Assert.False(ThemedShelfCapacity.EligibleDefinitions(catalog).Any(e => e.themeId == catalog.entries[3].themeId));
            Assert.Throws<InvalidOperationException>(() => ThemedShelfCapacity.Validate(session.catalog, 3, Shelves));
        }
        finally { Object.DestroyImmediate(catalog); }
    }

    static IEnumerator Reload(int expectedSorted, int expectedWorld)
    {
        var previous = Session;
        yield return SceneManager.LoadSceneAsync("Level");
        while (Session == null || ReferenceEquals(previous, Session) || Session.LivePileCount != expectedWorld) yield return null;
        Assert.AreEqual(expectedSorted, Session.Sorted);
        yield return null;
    }

    [UnityTest, Timeout(300000)]
    public IEnumerator OldSixColumnSaves_PreserveSortedHeldMisplacedAndCompletedProgress()
    {
        SessionState.SetBool("TinyRiders.Compact.QA.Active", true);
        PlayerPrefs.DeleteKey(Key); PlayerPrefs.DeleteKey(Key + ".position"); PlayerPrefs.DeleteKey(Key + ".coins");
        EditorSceneManager.OpenScene(CompactCollectionBuilder.ScenePath); CompactCollectionTests.Isolate(default, LoadSceneMode.Single);
        yield return new EnterPlayMode();
        Application.runInBackground = true;
        while (Session == null || Player.Instance == null || Session.LivePileCount != 1200) yield return null;
        yield return ExerciseOldSaves();
        yield return new ExitPlayMode();
    }

    static IEnumerator ExerciseOldSaves()
    {
        Assert.Greater(Time.frameCount, 0);
        Session.suspendPersistenceForTests = true;
        var manager = Object.FindFirstObjectByType<LevelSaveManager>();
        Assert.NotNull(manager);
        var data = Capture(manager);
        Assert.NotNull(data); Assert.NotNull(data.themed); Assert.NotNull(data.themed.instances);
        string theme = "TH_ANCIENT_EGYPT";
        var shelfPair = Shelves.Where(s => s.GetComponent<ThemeShelfBinding>().themeId == theme).ToArray();
        var records = data.themed.instances.Where(r => Session.ActiveDefinitions.Single(e => e.collectibleId == r.collectibleId).themeId == theme).OrderBy(r => r.id).ToArray();
        for (int i = 0; i < 23; i++)
        {
            int oldSlot = i % 18;
            records[i].state = "sorted"; records[i].shelfId = shelfPair[i / 18].SaveId;
            records[i].slotId = $"Slot_R{oldSlot / 6 + 1:00}_C{oldSlot % 6 + 1:00}";
        }
        records[23].state = "held"; data.themed.heldOrder = new System.Collections.Generic.List<string> { records[23].id };
        var wrong = data.themed.instances.First(r => !records.Contains(r));
        wrong.state = "misplaced"; wrong.shelfId = shelfPair[1].SaveId; wrong.slotId = "Slot_R01_C06";
        data.themed.discovered = records.Take(23).Select(r => r.collectibleId).Distinct().ToList();
        string held = records[23].id, misplaced = wrong.id, first = records[0].id;
        int discovered = data.themed.discovered.Count, coins = CoinsManager.Instance.Coins;
        PlayerPrefs.SetString(Key, JsonUtility.ToJson(data));
        yield return Reload(23, 1175);
        Assert.AreEqual(discovered, Session.UniqueDiscovered); Assert.AreEqual(coins, CoinsManager.Instance.Coins);
        Assert.AreEqual(held, ThemedWorldInstance.Read(Player.Instance.ItemHandler.CurrentEquipped));
        Assert.AreEqual("misplaced", Session.State.instances.Single(r => r.id == misplaced).state);
        Assert.AreEqual("Slot_R01_C01", Session.State.instances.Single(r => r.id == first).slotId);
        Assert.False(Session.State.instances.Where(r => r.state == "sorted" || r.state == "misplaced").Any(r => r.slotId.EndsWith("C06")));
        var locations = Session.State.instances.Where(r => r.state == "sorted" || r.state == "misplaced").ToDictionary(r => r.id, r => r.shelfId + "/" + r.slotId);
        Assert.AreEqual(locations.Count, locations.Values.Distinct().Count());
        Object.FindFirstObjectByType<LevelSaveManager>().Save();
        yield return Reload(23, 1175);
        foreach (var r in Session.State.instances.Where(r => locations.ContainsKey(r.id))) Assert.AreEqual(locations[r.id], r.shelfId + "/" + r.slotId);
        Assert.AreEqual(coins, CoinsManager.Instance.Coins);

        Session.suspendPersistenceForTests = true;
        data = Capture(Object.FindFirstObjectByType<LevelSaveManager>());
        data.themed.heldOrder.Clear(); data.themed.discovered = Session.ActiveDefinitions.Select(e => e.collectibleId).ToList();
        data.themed.completedThemes = Session.ActiveDefinitions.Select(e => e.themeId).Distinct().ToList(); data.completed = true;
        foreach (var group in data.themed.instances.GroupBy(r => Session.ActiveDefinitions.Single(e => e.collectibleId == r.collectibleId).themeId))
        {
            var pair = Shelves.Where(s => s.GetComponent<ThemeShelfBinding>().themeId == group.Key).ToArray();
            int i = 0;
            foreach (var record in group.OrderBy(r => r.id))
            {
                int oldSlot = i % 18; record.state = "sorted"; record.shelfId = pair[i / 18].SaveId;
                record.slotId = $"Slot_R{oldSlot / 6 + 1:00}_C{oldSlot % 6 + 1:00}"; i++;
            }
        }
        PlayerPrefs.SetString(Key, JsonUtility.ToJson(data));
        yield return Reload(1200, 0);
        Assert.AreEqual(600, Session.UniqueDiscovered); Assert.AreEqual(40, Session.State.completedThemes.Count);
        Assert.AreEqual(coins, CoinsManager.Instance.Coins);
        Assert.True(Shelves.All(s => !s.HasFreeSpot && s.IsCompleted));
        Assert.AreEqual(1200, Shelves.SelectMany(s => Enumerable.Range(0, s.SpotCount).Select(i => ThemedWorldInstance.Read(s.GetOccupant(i)))).Distinct().Count());
        Object.FindFirstObjectByType<LevelSaveManager>().Save();
        yield return Reload(1200, 0);
        Assert.True(Shelves.All(s => !s.HasFreeSpot && s.IsCompleted));
        Assert.AreEqual(coins, CoinsManager.Instance.Coins);
        File.WriteAllText(ExactThemeShelfBuilder.Evidence + "/save_migration_passed.txt", "Partial and completed 18-slot saves migrated; sorted, misplaced, held, discovery, coins and stable second reload verified.\n");
        Session.suspendPersistenceForTests = true;
    }

    public static void CaptureShelves(string name, SortingShelf shelf)
    {
        Directory.CreateDirectory(ExactThemeShelfBuilder.Evidence);
        var go = new GameObject("Shelf QA Camera"); var camera = go.AddComponent<Camera>();
        if (Camera.main != null) camera.CopyFrom(Camera.main);
        camera.enabled = false; camera.fieldOfView = 45; camera.nearClipPlane = .02f; camera.farClipPlane = 40;
        var target = shelf.transform.position + Vector3.up * .87f;
        camera.transform.position = target + shelf.transform.forward * 2.7f;
        camera.transform.LookAt(target);
        var rt = new RenderTexture(1280, 960, 24); var previous = RenderTexture.active;
        Texture2D texture = null;
        try
        {
            camera.targetTexture = rt; camera.Render(); RenderTexture.active = rt;
            texture = new Texture2D(1280, 960, TextureFormat.RGB24, false); texture.ReadPixels(new Rect(0, 0, 1280, 960), 0, 0); texture.Apply();
            File.WriteAllBytes(ExactThemeShelfBuilder.Evidence + "/" + name + ".png", texture.EncodeToPNG());
        }
        finally { camera.targetTexture = null; RenderTexture.active = previous; Object.DestroyImmediate(go); Object.DestroyImmediate(rt); if (texture != null) Object.DestroyImmediate(texture); }
    }

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        if (EditorApplication.isPlaying) { if (Session != null) Session.suspendPersistenceForTests = true; yield return new ExitPlayMode(); }
        var cleanup = new CompactCollectionTests().Cleanup();
        while (cleanup.MoveNext()) yield return cleanup.Current;
    }
}
