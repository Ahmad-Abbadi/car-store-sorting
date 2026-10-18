using NUnit.Framework;
using TinyRiders;
using UnityEngine;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using KinematicCharacterController.Examples;
using TinyRiders.Quests;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public class CollectorPromenadeTests
{
    const string Key = "TinyRiders.Compact.QA";
    static ThemedCollectionSession Session => ThemedCollectionSession.Instance;
    static IEnumerator Tick(float seconds)
    { float end = Time.realtimeSinceStartup + seconds; while (Time.realtimeSinceStartup < end) yield return null; }

    [UnityTest, Timeout(240000)]
    public IEnumerator CompactScatter_InteractionAndRevisionFiveMigration()
    {
        SessionState.SetBool("TinyRiders.Compact.QA.Active", true);
        foreach (var suffix in new[] { "", ".position", ".coins", ".CollectorQuests" }) PlayerPrefs.DeleteKey(Key + suffix);
        EditorSceneManager.OpenScene(CompactCollectionBuilder.ScenePath);
        CompactCollectionTests.Isolate(default, LoadSceneMode.Single);
        yield return new EnterPlayMode();
        yield return Exercise();
        yield return new ExitPlayMode();
    }

    static IEnumerator Exercise()
    {
        Application.runInBackground = true;
        while (Session == null || Session.LivePileCount != 1200 || Player.Instance == null || !CollectorQuestManager.Instance.Ready) yield return null;
        yield return Tick(.5f);
        Session.suspendPersistenceForTests = true;
        Assert.AreEqual(6, Session.roomLayout.revision);
        Assert.AreEqual(600, Session.ActiveDefinitions.Count());
        Assert.AreEqual(1200, Session.Capacity);
        Assert.AreEqual(1200, Session.State.instances.Select(r => r.id).Distinct().Count());
        var upgradeLevels = UpgradeManager.Instance.Upgrades.ToDictionary(u => u.SaveId, u => u.Level);
        var first = WorldItemBase.All.First(w => w.gameObject.activeInHierarchy && !string.IsNullOrEmpty(ThemedWorldInstance.Read(w)));
        string sortedId = ThemedWorldInstance.Read(first);
        string collectible = Session.State.instances.Single(r => r.id == sortedId).collectibleId;
        string theme = Session.ActiveDefinitions.Single(e => e.collectibleId == collectible).themeId;
        Assert.True(Player.Instance.ItemCollector.TryCollectItem(first));
        var shelf = Object.FindObjectsByType<SortingShelf>(FindObjectsSortMode.None).First(s => s.GetComponent<ThemeShelfBinding>().themeId == theme);
        Assert.True(Player.Instance.SortingBehaviour.TryGetHeldSortable(out var item));
        Assert.True(shelf.CanAccept(item, 0, out var reason), reason);
        Assert.True(Player.Instance.SortingBehaviour.TryGetPlacedItemFromHand(out var placed));
        shelf.AddItem(placed, 0);
        yield return Tick(.3f);
        var second = WorldItemBase.All.First(w => w.gameObject.activeInHierarchy && !string.IsNullOrEmpty(ThemedWorldInstance.Read(w)));
        string heldId = ThemedWorldInstance.Read(second);
        Assert.True(Player.Instance.ItemCollector.TryCollectItem(second));
        yield return Tick(.3f);
        Assert.AreEqual(1, Session.Sorted);
        var manager = Object.FindFirstObjectByType<LevelSaveManager>();
        var captured = (LevelSaveData)typeof(LevelSaveManager).GetMethod("Capture", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(manager, null);
        var data = JsonUtility.FromJson<LevelSaveData>(JsonUtility.ToJson(captured));
        data.themed.pileLayoutRevision = 5;
        foreach (var record in data.themed.instances.Where(r => r.state == "world")) record.position = Session.roomLayout.origin + new Vector3(8, .2f, 14);
        PlayerPrefs.SetString(Key, JsonUtility.ToJson(data));
        PlayerPrefs.SetString(Key + ".position", "{\"scene\":\"Level\",\"position\":{\"x\":508,\"y\":0.05,\"z\":14},\"yaw\":90,\"pitch\":0,\"roomLayoutRevision\":5}");
        var career = JsonUtility.FromJson<CollectorCareer>(PlayerPrefs.GetString(Key + ".CollectorQuests"));
        Assert.NotNull(career);
        career.xp = 125; var claimed = career.quests.First(); claimed.complete = claimed.claimed = true; claimed.progress = claimed.target;
        career.pinnedId = career.quests.Skip(1).First().id; career.pinChosen = true;
        string claimId = claimed.id, pinId = career.pinnedId;
        PlayerPrefs.SetString(Key + ".CollectorQuests", JsonUtility.ToJson(career));
        var previous = Session;
        yield return SceneManager.LoadSceneAsync("Level");
        while (Session == null || ReferenceEquals(previous, Session) || Session.LivePileCount != 1198 || !CollectorQuestManager.Instance.Ready) yield return null;
        Session.suspendPersistenceForTests = true;
        yield return Tick(.4f);
        Assert.True(Session.LayoutMigrated);
        Assert.AreEqual(6, Session.State.pileLayoutRevision);
        Assert.AreEqual(1, Session.Sorted);
        Assert.AreEqual(1, Session.UniqueDiscovered);
        Assert.AreEqual(heldId, ThemedWorldInstance.Read(Player.Instance.ItemHandler.CurrentEquipped));
        Assert.AreEqual("sorted", Session.State.instances.Single(r => r.id == sortedId).state);
        Assert.True(Session.State.instances.Where(r => r.state == "world").All(r => Session.roomLayout.Contains(r.position)));
        Assert.Less(Vector3.Distance(Player.Instance.Motor.TransientPosition, Session.roomLayout.spawn), .2f);
        Assert.AreEqual(125, CollectorQuestManager.Instance.XP);
        Assert.True(CollectorQuestManager.Instance.Quests.Single(q => q.Id == claimId).Claimed);
        Assert.AreEqual(pinId, CollectorQuestManager.Instance.PinnedId);
        foreach (var u in UpgradeManager.Instance.Upgrades) Assert.AreEqual(upgradeLevels[u.SaveId], u.Level);
        var board = Object.FindFirstObjectByType<CollectorQuestBoard>();
        board.OnInteract(null); Assert.True(board.interfaceUI.IsOpen); board.interfaceUI.Close();
        Object.FindFirstObjectByType<UpgradeBox>().OnInteract(null);
        var panel = Object.FindFirstObjectByType<UpgradePanelUI>(); Assert.True(panel.IsOpen); panel.Close();
        var player = Player.Instance;
        player.StateMachine.enabled = false; player.Look.enabled = false;
        player.Motor.SetPositionAndRotation(Session.roomLayout.origin + new Vector3(0, .05f, -10), Quaternion.identity);
        float startZ = player.Motor.TransientPosition.z;
        float until = Time.realtimeSinceStartup + 3;
        while (Time.realtimeSinceStartup < until)
        {
            var input = new PlayerCharacterInputs { MoveAxisForward = 1, CameraRotation = Quaternion.identity };
            player.Character.SetInputs(ref input); yield return null;
        }
        var idle = new PlayerCharacterInputs { CameraRotation = Quaternion.identity }; player.Character.SetInputs(ref idle);
        Assert.Greater(player.Motor.TransientPosition.z - startZ, 2, "Player must navigate the central loose-car scatter.");
        Assert.Greater(player.Motor.TransientPosition.y, -.1f);
        File.WriteAllText(CollectorPromenadeBuilder.Evidence + "/playmode-validation.txt", "PASS: 1200 unique cars, 600 designs, matching shelf capacity; collection and sorting; revision 5 migration of loose cars and safe player spawn; held/sorted/discovery/quest/upgrade preservation; quest and upgrade interfaces; movement through crowded central floor.\n");
    }

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        var cleanup = new CompactCollectionTests().Cleanup();
        while (cleanup.MoveNext()) yield return cleanup.Current;
        PlayerPrefs.DeleteKey(Key + ".CollectorQuests");
    }

    [Test]
    public void NonRectangularRoomRejectsEmptySpaceBesideItsWings()
    {
        var layout = UnityEditor.AssetDatabase.LoadAssetAtPath<CollectorRoomLayout>(CollectorPromenadeBuilder.Root + "/PromenadeLayout.asset");
        Assert.NotNull(layout);
        Assert.True(layout.Contains(layout.spawn));
        Assert.True(layout.Contains(layout.origin + new Vector3(0, 0, 16)));
        Assert.True(layout.Contains(layout.origin + new Vector3(11, 0, 10)));
        Assert.False(layout.Contains(layout.origin + new Vector3(9, 0, -15)), "An old saved player pose cannot restore outside the narrower lobby.");
        Assert.False(layout.Contains(layout.origin + new Vector3(6, 0, 16)), "The space beside the reward alcove has no floor.");
    }

    [Test]
    public void LegacyLayoutsRetainTheirRectangularContainment()
    {
        var layout = ScriptableObject.CreateInstance<CollectorRoomLayout>();
        try
        {
            layout.origin = new Vector3(500, 0, 0); layout.roomSize = new Vector2(24, 26);
            Assert.True(layout.Contains(new Vector3(511, 0, 12)));
            Assert.False(layout.Contains(new Vector3(513, 0, 12)));
            Assert.False(layout.Contains(new Vector3(511.5f, 0, 0), 1));
        }
        finally { Object.DestroyImmediate(layout); }
    }
}
