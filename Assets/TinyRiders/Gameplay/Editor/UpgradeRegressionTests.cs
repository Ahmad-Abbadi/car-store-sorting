using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TinyRiders;
using UnityEditor;
using UnityEditor.TestTools;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public class UpgradeRegressionTests
{
    const string Backup = "UpgradeRegression.backup.";
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static UpgradeSO[] Assets => AssetDatabase.FindAssets("t:UpgradeSO")
        .Select(g => AssetDatabase.LoadAssetAtPath<UpgradeSO>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
    static T Field<T>(object target, string name) => (T)target.GetType().GetField(name, Private).GetValue(target);
    static void Set(object target, string name, object value) => target.GetType().GetField(name, Private).SetValue(target, value);
    static object Owner(WorldItemBase item) => typeof(WorldItemBase).GetProperty("PowerOwner", Private).GetValue(item);
    static IEnumerator Tick(float seconds)
    {
        float end = Time.realtimeSinceStartup + seconds;
        while (Time.realtimeSinceStartup < end) yield return null;
    }

    [Test]
    public void UpgradeAssets_HaveUniqueSaveIdsAndValidPrices()
    {
        var assets = Assets;
        Assert.AreEqual(7, assets.Length);
        Assert.AreEqual(7, assets.Select(a => a.SaveId).Distinct().Count());
        foreach (var upgrade in assets)
        {
            Assert.False(string.IsNullOrWhiteSpace(upgrade.SaveId), upgrade.name);
            Assert.Greater(upgrade.MaxLevel, 0, upgrade.name);
            for (int level = 1; level <= upgrade.MaxLevel; level++)
                Assert.GreaterOrEqual(upgrade.GetCost(level), 0, upgrade.name);
            Assert.AreEqual(0, upgrade.GetCost(0));
            Assert.AreEqual(0, upgrade.GetCost(upgrade.MaxLevel + 1));
        }
    }

    [UnityTest, Timeout(180000)]
    public IEnumerator AllUpgrades_PurchasesPersistenceGameplayAndPowerLifecycle()
    {
        // The EditMode runner starts in an empty bootstrap scene and restores the
        // user's scene setup when the run finishes.
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene(CompactCollectionBuilder.ScenePath);
        foreach (var upgrade in Assets)
        {
            string key = "Upgrade_" + upgrade.SaveId;
            SessionState.SetInt(Backup + key, PlayerPrefs.HasKey(key) ? PlayerPrefs.GetInt(key) : -1);
        }
        SessionState.SetBool(Backup + "active", true);
        // Existing scene-load isolation redirects level, wallet and position saves before Start.
        SessionState.SetBool("TinyRiders.Compact.QA.Active", true);
        yield return new EnterPlayMode();
        Application.runInBackground = true;
        yield return Tick(.5f);
        yield return Exercise();
        yield return new ExitPlayMode();
    }

    static IEnumerator Exercise()
    {
        var session = ThemedCollectionSession.Instance;
        Assert.NotNull(session);
        session.suspendPersistenceForTests = true;
        Time.timeScale = 1;
        int frame = Time.frameCount;
        yield return Tick(.1f);
        Assert.Greater(Time.frameCount, frame, "The player loop must advance during verification.");
        var manager = UpgradeManager.Instance;
        var coins = CoinsManager.Instance;
        var player = Player.Instance;
        Assert.NotNull(manager); Assert.NotNull(coins); Assert.NotNull(player);
        Assert.AreEqual(7, manager.Upgrades.Count);
        var inventory = player.StashHandler.Inventory;
        inventory.Clear();
        foreach (var upgrade in manager.Upgrades) upgrade.SetLevel(0);
        coins.Remove(coins.Coins);
        Assert.False(manager.TryPurchase(null));

        var panel = Object.FindFirstObjectByType<UpgradePanelUI>();
        Assert.NotNull(panel);
        panel.Open();
        Assert.True(panel.IsOpen);
        var cards = Object.FindObjectsByType<UpgradeCardUI>(FindObjectsSortMode.None)
            .Where(c => c.Upgrade != null).ToArray();
        Assert.AreEqual(7, cards.Length);
        foreach (var upgrade in manager.Upgrades)
        {
            var card = cards.Single(c => c.Upgrade == upgrade);
            while (!upgrade.IsMaxed)
            {
                int level = upgrade.Level, price = upgrade.NextCost;
                coins.Remove(coins.Coins);
                if (price > 0)
                {
                    coins.Add(price - 1);
                    Assert.False(manager.TryPurchase(upgrade), upgrade.name + " must refuse insufficient funds");
                    Assert.AreEqual(level, upgrade.Level);
                    Assert.AreEqual(price - 1, coins.Coins);
                    Assert.False(card.BuyButton.interactable);
                    coins.Add(1);
                }
                Assert.True(card.BuyButton.interactable, upgrade.name);
                card.BuyButton.onClick.Invoke();
                Assert.AreEqual(level + 1, upgrade.Level, upgrade.name);
                Assert.AreEqual(0, coins.Coins, upgrade.name + " must charge exactly once");
                Assert.AreEqual(upgrade.Level, PlayerPrefs.GetInt("Upgrade_" + upgrade.SaveId));
                upgrade.Reload();
                Assert.AreEqual(level + 1, upgrade.Level, "Reload must retain purchases");
                Assert.False(upgrade.GetDescription().Contains("{value}"));
                CheckApplied(upgrade, player, session);
            }
            Assert.False(card.BuyButton.interactable);
            coins.Add(100);
            Assert.False(manager.TryPurchase(upgrade));
            Assert.AreEqual(100, coins.Coins, "MAX must not spend coins");
        }
        panel.Close();
        Assert.False(panel.IsOpen);
        Time.timeScale = 1;
        var foreign = Object.Instantiate(manager.Upgrades[0]);
        try
        {
            SetForeignId(foreign);
            foreign.ResetLevel();
            Assert.False(manager.TryPurchase(foreign), "Unregistered upgrades have no gameplay target");
        }
        finally { PlayerPrefs.DeleteKey("Upgrade_UpgradeRegression.foreign"); Object.Destroy(foreign); }

        var reach = manager.Upgrades.OfType<ReachUpgradeSO>().Single();
        manager.enabled = false;
        Assert.False(manager.TryPurchase(reach));
        reach.SetLevel(0);
        manager.enabled = true;
        CheckApplied(reach, player, session);

        yield return CheckPowers(player, manager);
        manager.ResetForReplay();
        foreach (var upgrade in manager.Upgrades)
        {
            Assert.AreEqual(0, upgrade.Level);
            Assert.False(PlayerPrefs.HasKey("Upgrade_" + upgrade.SaveId));
            CheckApplied(upgrade, player, session);
        }
    }

    static void SetForeignId(UpgradeSO upgrade) => typeof(UpgradeSO).GetField("saveId", Private)
        .SetValue(upgrade, "UpgradeRegression.foreign");

    static void CheckApplied(UpgradeSO upgrade, Player player, ThemedCollectionSession session)
    {
        if (upgrade is InventoryUpgradeSO bag)
            Assert.AreEqual(bag.CurrentValues.capacity, player.StashHandler.Inventory.Capacity);
        else if (upgrade is ThrowUpgradeSO throwing)
            Assert.AreEqual(throwing.CurrentValues.throwForce, Field<float>(player.ItemHandler, "throwForce"));
        else if (upgrade is ReachUpgradeSO reach)
            Assert.AreEqual(reach.CurrentValues.distance * session.worldScale,
                Object.FindFirstObjectByType<InteractionHandler>().MaxDistance, .001f);
        else if (upgrade is SprintUpgradeSO sprint)
            Assert.AreEqual(sprint.CurrentValues.unlocked, player.Input.SprintEnabled);
        else if (upgrade is AutoShelfUpgradeSO shelf)
            Assert.AreEqual(shelf.CurrentValues.enabled, SortingShelf.DistanceShelfingEnabled);
        else if (upgrade is HighlightItemsUpgradeSO highlight)
            Assert.AreEqual(highlight.CurrentValues.unlocked, Object.FindFirstObjectByType<HighlightItemsPower>().IsUnlocked);
        else if (upgrade is AutoCollectUpgradeSO collect)
            Assert.AreEqual(collect.CurrentValues.unlocked, Object.FindFirstObjectByType<AutoCollectPower>().IsUnlocked);
    }

    static IEnumerator CheckPowers(Player player, UpgradeManager manager)
    {
        var inventory = player.StashHandler.Inventory;
        var collect = Object.FindFirstObjectByType<AutoCollectPower>();
        var highlight = Object.FindFirstObjectByType<HighlightItemsPower>();
        Assert.NotNull(collect); Assert.NotNull(highlight);
        Assert.AreSame(manager.Upgrades.OfType<AutoCollectUpgradeSO>().Single(), Field<AutoCollectUpgradeSO>(collect, "upgrade"));
        Assert.AreSame(manager.Upgrades.OfType<HighlightItemsUpgradeSO>().Single(), Field<HighlightItemsUpgradeSO>(highlight, "upgrade"));
        var world = WorldItemBase.All.First(w => w.gameObject.activeInHierarchy);
        var data = world.Data;
        Assert.True(player.ItemCollector.TryCollectItem(world));
        yield return Tick(.5f);
        // The compact collection has two copies per design. Add isolated extra candidates
        // to verify the power respects free slots even when many matches are available.
        for (int i = 0; i < 4; i++)
            Object.Instantiate(data.WorldItem, new Vector3(500 + i * 2, 10, 0), Quaternion.identity);
        player.Input.Enable();
        player.Input.SendMessage("OnApplicationFocus", true);
        highlight.RequestUse();
        highlight.SendMessage("Update");
        Assert.True(highlight.IsActive);
        Assert.True(WorldItemBase.All.Any(w => ReferenceEquals(Owner(w), highlight)));
        highlight.enabled = false;
        yield return Tick(.1f);
        Assert.False(highlight.IsActive);
        Assert.Greater(highlight.CooldownRemaining, 0);
        Assert.False(WorldItemBase.All.Any(w => ReferenceEquals(Owner(w), highlight)));
        highlight.enabled = true;

        inventory.SetCapacity(inventory.Count);
        collect.RequestUse(); collect.SendMessage("Update");
        Assert.False(collect.IsActive, "A full stash must not launch items or consume the power");
        Assert.AreEqual(0, collect.CooldownRemaining);
        inventory.SetCapacity(inventory.Count + 1);
        collect.RequestUse(); collect.SendMessage("Update");
        Assert.True(collect.IsActive);
        Assert.AreEqual(1, WorldItemBase.All.Count(w => ReferenceEquals(Owner(w), collect)), "Only free slots may be launched");
        int before = inventory.Count;
        yield return Tick(2f);
        Assert.AreEqual(before + 1, inventory.Count);
        Assert.False(collect.IsActive);
        Assert.Greater(collect.CooldownRemaining, 0);
        Assert.False(WorldItemBase.All.Any(w => ReferenceEquals(Owner(w), collect)));
        typeof(ItemPowerBase).GetField("_cooldownEnd", Private).SetValue(collect, 0f);
        inventory.SetCapacity(inventory.Count + 3);
        collect.RequestUse(); collect.SendMessage("Update");
        Assert.True(collect.IsActive);
        collect.enabled = false;
        yield return Tick(.1f);
        Assert.False(collect.IsActive, "Disabling a power must cancel its flights");
        Assert.False(WorldItemBase.All.Any(w => ReferenceEquals(Owner(w), collect)));
        int cancelledCount = inventory.Count;
        yield return Tick(2f);
        Assert.AreEqual(cancelledCount, inventory.Count, "Disabled flights must not collect later");
        collect.enabled = true;
    }

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        if (EditorApplication.isPlaying)
        {
            if (ThemedCollectionSession.Instance != null) ThemedCollectionSession.Instance.suspendPersistenceForTests = true;
            yield return new ExitPlayMode();
        }
        SessionState.SetBool("TinyRiders.Compact.QA.Active", false);
        if (SessionState.GetBool(Backup + "active", false))
        {
            foreach (var upgrade in Assets)
            {
                string key = "Upgrade_" + upgrade.SaveId;
                int previous = SessionState.GetInt(Backup + key, -1);
                if (previous < 0) PlayerPrefs.DeleteKey(key); else PlayerPrefs.SetInt(key, previous);
                upgrade.Reload();
            }
            SessionState.SetBool(Backup + "active", false);
        }
        PlayerPrefs.Save();
    }
}
