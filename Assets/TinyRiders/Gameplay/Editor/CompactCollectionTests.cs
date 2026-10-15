using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TinyRiders;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.TestTools;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class CompactCollectionTests
{
    const string Key = "TinyRiders.Compact.QA";
    static ThemedCollectionSession Session => ThemedCollectionSession.Instance;
    static void SetKey(UnityEngine.Object value, string field, string key)
    { if (value != null) value.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(value, key); }
    internal static void Isolate(Scene scene, LoadSceneMode mode)
    {
        SetKey(UnityEngine.Object.FindFirstObjectByType<LevelSaveManager>(), "saveKey", Key);
        SetKey(UnityEngine.Object.FindFirstObjectByType<PlayerSavePosition>(), "saveKey", Key + ".position");
        var coins = UnityEngine.Object.FindFirstObjectByType<CoinsManager>();
        SetKey(coins, "saveKey", Key + ".coins");
        // Scene callbacks run after Awake, so also load the isolated wallet balance.
        if (coins != null) typeof(CoinsManager).GetProperty(nameof(CoinsManager.Coins))
            .SetValue(coins, Mathf.Max(0, PlayerPrefs.GetInt(Key + ".coins", 0)));
        // Automated bulk placement is not a representative performance sample.
        // Keep the optional metrics writer from contending with external report readers on Windows.
        foreach (var probe in UnityEngine.Object.FindObjectsByType<ThemedPerformanceProbe>(FindObjectsSortMode.None))
            probe.enabled = false;
    }
    static IEnumerator Tick(float seconds)
    { float until = Time.realtimeSinceStartup + seconds; while (Time.realtimeSinceStartup < until) yield return null; }
    static IEnumerator ReloadWorld(int expectedWorldCount)
    {
        var previous = Session;
        yield return SceneManager.LoadSceneAsync("Level");
        Application.runInBackground = true;
        // This is an EditMode test entering Play mode; explicit frame yields work in both runners.
        while(Session==null||ReferenceEquals(Session,previous)||Session.LivePileCount!=expectedWorldCount) yield return null;
        yield return Tick(.3f);
    }
    static SortingShelf Shelf(string theme, int unit = 0) => UnityEngine.Object.FindObjectsByType<SortingShelf>(FindObjectsSortMode.None)
        .Where(s => s.GetComponent<ThemeShelfBinding>().themeId == theme).OrderBy(s => s.SaveId, StringComparer.Ordinal).ElementAt(unit);
    static WorldItemBase World(ThemedCarCatalog.Entry entry)
    {
        var record = Session.State.instances.First(r => r.collectibleId == entry.collectibleId && (r.state == "world" || r.state == "pending"));
        var world = WorldItemBase.All.FirstOrDefault(w => ThemedWorldInstance.Read(w) == record.id);
        if (world != null) return world;
        world = UnityEngine.Object.Instantiate(entry.sortingItem.WorldItem, Session.pileOrigin.position + Vector3.up, Quaternion.identity);
        Session.Dropped(world, record.id); return world;
    }
    static string Pick(ThemedCarCatalog.Entry entry)
    {
        var world = World(entry); string id = ThemedWorldInstance.Read(world);
        Assert.True(Player.Instance.ItemCollector.TryCollectItem(world));
        Assert.AreEqual(id, ThemedWorldInstance.Read(Player.Instance.ItemHandler.CurrentEquipped));
        Assert.AreEqual(entry.collectibleId, Player.Instance.ItemHandler.CurrentEquipped.GetComponentInChildren<ThemedCarIdentity>().collectibleId);
        return id;
    }
    static void Place(SortingShelf shelf, int slot)
    {
        Assert.True(Player.Instance.SortingBehaviour.TryGetHeldSortable(out var item));
        Assert.True(shelf.CanAccept(item, slot, out var reason), reason);
        Assert.True(Player.Instance.SortingBehaviour.TryGetPlacedItemFromHand(out var placed));
        shelf.AddItem(placed, slot);
    }
    static void Note(string text) { File.AppendAllText("IntegrationEvidence/compact_playmode_passed.txt", text + "\n"); }

    [UnityTest, Timeout(600000)]
    public IEnumerator CompactRoom_CollectionSortingSaveCompletionAndReplay()
    {
        SessionState.SetBool("TinyRiders.Compact.QA.Active", true);
        PlayerPrefs.DeleteKey(Key); PlayerPrefs.DeleteKey(Key + ".position"); PlayerPrefs.DeleteKey(Key + ".coins");
        File.WriteAllText("IntegrationEvidence/compact_playmode_passed.txt", "");
        EditorSceneManager.OpenScene(CompactCollectionBuilder.ScenePath); Isolate(default, LoadSceneMode.Single);
        yield return new EnterPlayMode();
        Application.runInBackground = true;
        while(Time.frameCount<=5||Session==null||Player.Instance==null||Session.LivePileCount!=1200) yield return null;
        yield return Exercise();
        yield return new ExitPlayMode();
    }

    static IEnumerator Exercise()
    {
        Assert.AreEqual(600, Session.ActiveDefinitions.Count()); Assert.AreEqual(1200, Session.Total); Assert.AreEqual(1200, Session.Capacity);
        Assert.AreEqual(1200, Session.LivePileCount);
        var initial = WorldItemBase.All.Where(w => w.gameObject.activeInHierarchy).ToArray();
        Assert.AreEqual(600, initial.Select(w => w.Data.SaveId).Distinct().Count());
        Assert.True(initial.GroupBy(w=>w.Data.SaveId).All(g=>g.Count()==2));
        Assert.AreEqual(0,Session.State.instances.Count(r=>r.state=="pending"));
        Assert.True(initial.SelectMany(w=>w.GetComponentsInChildren<Renderer>()).All(r=>r.enabled&&!r.forceRenderingOff));
        var view=Camera.main;Assert.AreEqual(10000,view.farClipPlane);Assert.False(view.useOcclusionCulling);
        Assert.True(view.layerCullDistances.All(d=>d==0));
        Assert.AreEqual(40, initial.Select(w => w.GetComponentInChildren<ThemedCarIdentity>().themeId).Distinct().Count());
        Assert.AreEqual(15, initial.Select(w => w.GetComponentInChildren<ThemedCarIdentity>().baseCarId).Distinct().Count());
        Assert.True(initial.All(w => Session.roomLayout != null ? Session.roomLayout.Contains(w.transform.position,.5f) : Mathf.Abs(w.transform.position.x - 500) < 17 && Mathf.Abs(w.transform.position.z) < 15));
        Assert.IsEmpty(UnityEngine.Object.FindObjectsByType<CarVisualCulling>(FindObjectsSortMode.None));
        Assert.IsEmpty(UnityEngine.Object.FindObjectsByType<ThemedVisibility>(FindObjectsSortMode.None));
        Note("600 designs, 1200 records, 1200 initial cars / 600 varieties, all 40 themes and 15 shapes, bounded staging");
        foreach(var corner in new[]{Session.roomLayout.origin+new Vector3(-10.7f,.05f,-11.2f),Session.roomLayout.origin+new Vector3(10.7f,.05f,11.2f)})
        {
            Player.Instance.Motor.SetPosition(corner);Player.Instance.Look.SetLook(corner.x<500?45:225,0);
            yield return Tick(.4f);
            var distant=initial.Where(w=>Vector3.Distance(w.transform.position,Camera.main.transform.position)>20).ToArray();
            Assert.Greater(distant.Length,100);
            Assert.True(distant.SelectMany(w=>w.GetComponentsInChildren<Renderer>()).All(r=>r.enabled&&!r.forceRenderingOff));
        }
        Player.Instance.Motor.SetPosition(Session.roomLayout.spawn);Player.Instance.Look.SetLook(0,0);
        Note("Opposite room viewpoints keep full car renderers enabled beyond 20 metres, with no distance hiding");

        var egypt = Session.ActiveDefinitions.Where(e => e.themeId == "TH_ANCIENT_EGYPT").OrderBy(e => e.baseCarId).ToArray();
        string firstId = Pick(egypt[0]);
        yield return Tick(.6f);
        Assert.AreEqual(1199,Session.LivePileCount,"Pickup must not spawn a replacement");
        Assert.AreEqual(0,Session.State.instances.Count(r=>r.state=="pending"));
        int count = Player.Instance.StashHandler.Inventory.Count;
        var wrongShelf = Shelf("TH_ALIEN");
        int coinsBeforeWrong = CoinsManager.Instance.Coins;
        wrongShelf.OnInteractSecondary(null);
        Assert.AreEqual(count - 1, Player.Instance.StashHandler.Inventory.Count); Assert.AreEqual(0, Session.Sorted);
        Assert.AreEqual(0, Session.UniqueDiscovered); Assert.AreEqual(coinsBeforeWrong, CoinsManager.Instance.Coins);
        Assert.True(wrongShelf.GetOccupant(0).IsWrong);
        Assert.AreEqual("misplaced", Session.State.instances.Single(r => r.id == firstId).state);
        yield return Tick(.6f);
        Assert.True(wrongShelf.TryTakeBack(wrongShelf.GetOccupant(0)));
        Assert.AreEqual(count, Player.Instance.StashHandler.Inventory.Count);
        Assert.AreEqual(firstId, ThemedWorldInstance.Read(Player.Instance.ItemHandler.CurrentEquipped));
        typeof(ItemsHandler).GetMethod("ExecuteThrow", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(Player.Instance.ItemHandler, new object[] { 0f });
        yield return Tick(.5f);
        var dropped = WorldItemBase.All.Single(w => ThemedWorldInstance.Read(w) == firstId);
        Assert.True(Player.Instance.ItemCollector.TryCollectItem(dropped));
        Place(Shelf(egypt[0].themeId), 0); yield return Tick(.3f);
        Assert.AreEqual(1, Session.Sorted); Assert.AreEqual(1, Session.UniqueDiscovered);
        Assert.AreEqual(firstId, ThemedWorldInstance.Read(Shelf(egypt[0].themeId).GetOccupant(0)));
        Assert.NotNull(Shelf(egypt[0].themeId).GetOccupant(0).GetComponent<Collider>());
        Assert.True(Shelf(egypt[0].themeId).TryTakeBack(Shelf(egypt[0].themeId).GetOccupant(0)));
        Assert.AreEqual(firstId, ThemedWorldInstance.Read(Player.Instance.ItemHandler.CurrentEquipped));
        Assert.AreEqual(0, Session.Sorted);
        Place(Shelf(egypt[0].themeId),0);yield return Tick(.3f);
        Assert.False(Shelf(egypt[0].themeId).CanAccept(egypt[1].sortingItem, 0, out _));
        Pick(egypt[0]); Place(Shelf(egypt[0].themeId), 1); yield return Tick(.3f);
        Assert.AreEqual(2, Session.Sorted); Assert.AreEqual(1, Session.UniqueDiscovered);
        for (int i = 1; i < 14; i++) { Pick(egypt[i]); Place(Shelf(egypt[i].themeId), i + 1); yield return null; }
        yield return Tick(.4f);
        Assert.True(Shelf(egypt[0].themeId).IsCompleted);
        Assert.False(Shelf(egypt[0].themeId).HasFreeSpot);
        Assert.False(Shelf(egypt[0].themeId).CanAccept(egypt[14].sortingItem, 15, out _));
        Pick(egypt[14]); Place(Shelf(egypt[0].themeId, 1), 0); yield return Tick(.3f);
        Pick(egypt[1]); Place(Shelf(egypt[0].themeId, 1), 1);
        Pick(egypt[2]); Place(Shelf(egypt[0].themeId, 1), 2);
        Pick(egypt[3]); Place(Shelf(egypt[0].themeId, 1), 3); yield return Tick(.3f);
        Note("Pickup/drop/repick, identity retention, wrong-theme acceptance without credit, takeback, occupied-slot rejection, 15 designs, duplicate discovery, 15-slot full shelf and second-shelf placement");

        foreach (var theme in Session.ActiveDefinitions.GroupBy(e => e.themeId).Where(g => g.Key != egypt[0].themeId))
        { var entry = theme.First(); Pick(entry); Place(Shelf(entry.themeId), 0); yield return null; }
        yield return Tick(.4f);
        int sorted = Session.Sorted; Assert.AreEqual(58, sorted); Assert.AreEqual(54, Session.UniqueDiscovered);
        UnityEngine.Object.FindFirstObjectByType<LevelSaveManager>().Save();
        Assert.True(PlayerPrefs.GetString(Key).Contains(firstId));
        yield return ReloadWorld(1200-sorted);
        Assert.AreEqual(sorted, Session.Sorted); Assert.AreEqual(firstId, ThemedWorldInstance.Read(Shelf(egypt[0].themeId).GetOccupant(0)));
        Assert.False(WorldItemBase.All.Any(w => Session.State.instances.Any(r => r.id == ThemedWorldInstance.Read(w) && r.state == "sorted")));
        Assert.AreEqual(1200 - sorted, Session.LivePileCount);
        Assert.True(WorldItemBase.All.Where(w => Session.State.instances.Any(r => r.id == ThemedWorldInstance.Read(w) && r.state == "world"))
            .All(w => w.transform.position.x > 480 && w.transform.position.x < 520 && w.transform.position.z > -25 && w.transform.position.z < 25));
        Note("Real sorting across all 40 themes; scene reload restores every remaining car without pending batches or duplicate sorted cars");

        if (Session.roomLayout != null)
        {
            var heldEntry = Session.ActiveDefinitions.First(e => Session.State.instances.Any(r => r.collectibleId == e.collectibleId && r.state == "world"));
            string heldId = Pick(heldEntry); yield return Tick(.4f);
            var manager = UnityEngine.Object.FindFirstObjectByType<LevelSaveManager>(); manager.Save();
            var old = JsonUtility.FromJson<LevelSaveData>(PlayerPrefs.GetString(Key));
            old.themed.pileLayoutRevision = Session.roomLayout.revision - 1;
            foreach (var r in old.themed.instances.Where(r => r.state == "world")) r.position = new Vector3(900,0,900);
            PlayerPrefs.SetString(Key,JsonUtility.ToJson(old));
            PlayerPrefs.SetString(Key+".position","{\"scene\":\"Level\",\"position\":{\"x\":900,\"y\":0,\"z\":900},\"yaw\":90,\"pitch\":0}");
            yield return ReloadWorld(1199-sorted);
            Assert.True(Session.LayoutMigrated); Assert.AreEqual(Session.roomLayout.revision,Session.State.pileLayoutRevision);
            Assert.AreEqual(sorted,Session.Sorted); Assert.AreEqual(54,Session.UniqueDiscovered);
            Assert.AreEqual(heldId,ThemedWorldInstance.Read(Player.Instance.ItemHandler.CurrentEquipped));
            Assert.Less(Vector3.Distance(Player.Instance.Motor.TransientPosition,Session.roomLayout.spawn),.4f);
            Assert.True(Session.State.instances.Where(r=>r.state=="world").All(r=>Session.roomLayout.Contains(r.position,.5f)));
            var moved=WorldItemBase.All.First(w=>Session.State.instances.Any(r=>r.id==ThemedWorldInstance.Read(w)&&r.state=="world"));
            string movedId=ThemedWorldInstance.Read(moved); Vector3 movedPose=Session.roomLayout.origin+new Vector3(0,.1f,-10.1f);
            moved.transform.position=movedPose;
            if(moved.Body!=null) { moved.Body.isKinematic=true; moved.Body.position=movedPose; }
            typeof(WorldItemBase).GetProperty("RestPosition",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(moved,movedPose);
            Player.Instance.Motor.SetPosition(Session.roomLayout.origin+new Vector3(0,.05f,-10.9f));
            yield return Tick(1f); // Allow the existing physics culler to settle the moved car onto the floor.
            Assert.Less(Vector2.Distance(new Vector2(moved.transform.position.x,moved.transform.position.z),new Vector2(movedPose.x,movedPose.z)),.2f);
            movedPose=moved.transform.position;
            UnityEngine.Object.FindFirstObjectByType<LevelSaveManager>().Save();
            UnityEngine.Object.FindFirstObjectByType<PlayerSavePosition>().Save();
            var savedPlayer=Player.Instance.Motor.TransientPosition;
            yield return ReloadWorld(1199-sorted);
            Assert.False(Session.LayoutMigrated);Assert.AreEqual(sorted,Session.Sorted);
            Assert.AreEqual(heldId,ThemedWorldInstance.Read(Player.Instance.ItemHandler.CurrentEquipped));
            Assert.Less(Vector3.Distance(Session.State.instances.Single(r=>r.id==movedId).position,movedPose),.1f);
            Assert.Less(Vector3.Distance(Player.Instance.Motor.TransientPosition,savedPlayer),.4f);
            // Return the held copy to the world before the existing completion test drains the collection.
            typeof(ItemsHandler).GetMethod("ExecuteThrow",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(Player.Instance.ItemHandler,new object[]{0f});
            yield return Tick(.4f);
            Assert.AreEqual(1200-sorted,Session.LivePileCount);
            Note("Room revision migration retains sorted/discovered/held IDs, relocates old world cars and player once, and preserves moved cars/player on second reload");
        }

        // Physically sort every remaining instance into an actual slot; no injected completion states.
        Session.suspendPersistenceForTests = true;
        var allShelves = UnityEngine.Object.FindObjectsByType<SortingShelf>(FindObjectsSortMode.None).OrderBy(s => s.SaveId).ToArray();
        ExactShelfCapacityTests.CaptureShelves("before_completion", Shelf(egypt[0].themeId));
        int placedCount = 0;
        foreach (var record in Session.State.instances.Where(r => r.state == "world" || r.state == "pending").ToArray())
        {
            var entry = Session.ActiveDefinitions.Single(e => e.collectibleId == record.collectibleId);
            var shelf = allShelves.First(s => s.GetComponent<ThemeShelfBinding>().themeId == entry.themeId && s.HasFreeSpot);
            int slot = Enumerable.Range(0, shelf.SpotCount).First(shelf.IsSpotFree);
            Pick(entry); Place(shelf, slot);
            if (++placedCount % 20 == 0) yield return Tick(.25f);
        }
        yield return Tick(.9f); // The completion screen waits 0.6 seconds for the final jump.
        Assert.AreEqual(1200, Session.Sorted); Assert.AreEqual(600, Session.UniqueDiscovered);
        Assert.AreEqual(0, Session.LivePileCount);
        Assert.True(allShelves.All(s => !s.HasFreeSpot && s.IsCompleted));
        var occupants = allShelves.SelectMany(s => Enumerable.Range(0,s.SpotCount).Select(s.GetOccupant)).ToArray();
        Assert.AreEqual(1200, occupants.Length);
        Assert.AreEqual(1200, occupants.Select(ThemedWorldInstance.Read).Distinct().Count());
        Assert.True(occupants.All(o => o != null && !o.IsWrong));
        ExactShelfCapacityTests.CaptureShelves("filled", Shelf(egypt[0].themeId));
        ThemedShelfArtTests.CaptureCheckpoints("filled");
        Session.suspendPersistenceForTests = false;
        var save = UnityEngine.Object.FindFirstObjectByType<LevelSaveManager>(); save.Save();
        Assert.True(save.StoreCompleted);
        Assert.AreEqual(GameFlowManager.GameState.LevelFinished, GameFlowManager.Instance.currentState);
        Assert.True(JsonUtility.FromJson<LevelSaveData>(PlayerPrefs.GetString(Key)).completed);
        yield return ReloadWorld(0);
        Assert.AreEqual(1200,Session.Sorted); Assert.AreEqual(600,Session.UniqueDiscovered);
        Assert.True(UnityEngine.Object.FindObjectsByType<SortingShelf>(FindObjectsSortMode.None).All(s=>s.IsCompleted&&!s.HasFreeSpot));
        Note("All 1200 cars physically placed into 1200 slots, all 80 shelves full, 40 themes complete; completion UI/save and fully occupied reload verified");

        // A heavy scene can consume the real-time settle interval in one frame.
        // Replay is available to players only after the completion UI initializes its save reference.
        var replayUI = UnityEngine.Object.FindFirstObjectByType<CarStoreUI>();
        var completionReady = typeof(CarStoreUI).GetField("shownComplete", BindingFlags.Instance | BindingFlags.NonPublic);
        float uiDeadline = Time.realtimeSinceStartup + 15f;
        while (!(bool)completionReady.GetValue(replayUI) && Time.realtimeSinceStartup < uiDeadline) yield return null;
        Assert.True((bool)completionReady.GetValue(replayUI), "Completion UI must be ready before replay.");

        // Replay resets upgrades by design; preserve the user's real upgrade preferences around that test.
        var upgrades = UpgradeManager.Instance.Upgrades;
        foreach (var upgrade in upgrades)
        {
            string key = "Upgrade_" + upgrade.SaveId;
            SessionState.SetInt("CompactQA.backup." + key, PlayerPrefs.HasKey(key) ? PlayerPrefs.GetInt(key) : -1);
        }
        SessionState.SetBool("CompactQA.restoreUpgrades", true);
        replayUI.Replay();
        while(Session==null||Session.Sorted!=0||Session.LivePileCount!=1200) yield return null;
        yield return Tick(.3f);
        Assert.AreEqual(0, Session.Sorted); Assert.AreEqual(1200, Session.Total); Assert.AreEqual(1200, Session.LivePileCount);
        Note("Replay returns to Level with a fresh 1200-copy collection and a complete 1200-car pile");
        yield return SceneManager.LoadSceneAsync("MainMenu Scene"); yield return null;
        UnityEngine.Object.FindFirstObjectByType<MainMenu>().Play();
        while(SceneManager.GetActiveScene().name!="Level"||Session==null||Session.LivePileCount!=1200) yield return null;
        yield return Tick(.3f);
        Assert.AreEqual(CompactCollectionBuilder.CatalogPath, AssetDatabase.GetAssetPath(Session.catalog));
        Assert.True(UnityEngine.Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsSortMode.None).Any(t=>t.text.Contains("0 / 1200")&&t.text.Contains("0 / 600")));
        Note("Main-menu Play follows the Loading scene into the compact Level and shows 0/1200 cars and 0/600 designs");
        Session.suspendPersistenceForTests = true;
    }

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        if (EditorApplication.isPlaying) { if (Session != null) Session.suspendPersistenceForTests = true; yield return new ExitPlayMode(); }
        SessionState.SetBool("TinyRiders.Compact.QA.Active", false);
        if (SessionState.GetBool("CompactQA.restoreUpgrades", false))
        foreach (string guid in AssetDatabase.FindAssets("t:UpgradeSO"))
        {
            var upgrade = AssetDatabase.LoadAssetAtPath<UpgradeSO>(AssetDatabase.GUIDToAssetPath(guid)); string key = "Upgrade_" + upgrade.SaveId;
            int level = SessionState.GetInt("CompactQA.backup." + key, -2);
            if (level == -1) PlayerPrefs.DeleteKey(key); else if (level >= 0) PlayerPrefs.SetInt(key, level);
            upgrade.Reload();
        }
        SessionState.SetBool("CompactQA.restoreUpgrades", false);
        PlayerPrefs.DeleteKey(Key); PlayerPrefs.DeleteKey(Key + ".position"); PlayerPrefs.DeleteKey(Key + ".coins"); PlayerPrefs.Save();
        EditorSceneManager.OpenScene(CompactCollectionBuilder.ScenePath);
    }
}

[InitializeOnLoad]
internal static class CompactCollectionTestIsolation
{
    static CompactCollectionTestIsolation()
    {
        SceneManager.sceneLoaded += (scene, mode) =>
        { if (SessionState.GetBool("TinyRiders.Compact.QA.Active", false) && EditorApplication.isPlaying) CompactCollectionTests.Isolate(scene, mode); };
    }
}

