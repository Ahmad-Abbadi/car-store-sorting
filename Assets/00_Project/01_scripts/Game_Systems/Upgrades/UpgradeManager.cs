using System.Collections.Generic;
using UnityEngine;

/// <summary> Scene bridge: buys upgrades and pushes their values into player systems. </summary>
public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance { get; private set; }

    [Header("Upgrades")]
    [SerializeField] private InventoryUpgradeSO inventoryUpgrade;
    [SerializeField] private ThrowUpgradeSO throwUpgrade;
    [SerializeField] private ReachUpgradeSO reachUpgrade;
    [SerializeField] private SprintUpgradeSO sprintUpgrade;
    [SerializeField] private AutoShelfUpgradeSO autoShelfUpgrade;

    [Tooltip("Read directly by the HighlightItemsPower on the player.")]
    [SerializeField] private HighlightItemsUpgradeSO highlightItemsUpgrade;

    [Tooltip("Read directly by the AutoCollectPower on the player.")]
    [SerializeField] private AutoCollectUpgradeSO autoCollectUpgrade;

    [Header("Targets")]
    [Tooltip("Player whose systems are upgraded. Falls back to Player.Instance.")]
    [SerializeField] private Player player;

    [Tooltip("Interaction ray the reach upgrade extends. Falls back to the first in scene.")]
    [SerializeField] private InteractionHandler interaction;

    private readonly List<UpgradeSO> _upgrades = new List<UpgradeSO>();
    private bool _started;

    /// <summary> Every assigned upgrade, in inspector order. </summary>
    public IReadOnlyList<UpgradeSO> Upgrades => _upgrades;

    private Player Player => player != null ? player : (player = Player.Instance);

    private InteractionHandler Interaction =>
        interaction != null ? interaction : (interaction = FindFirstObjectByType<InteractionHandler>());

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        AddUpgrade(inventoryUpgrade);
        AddUpgrade(throwUpgrade);
        AddUpgrade(reachUpgrade);
        AddUpgrade(sprintUpgrade);
        AddUpgrade(autoShelfUpgrade);
        AddUpgrade(highlightItemsUpgrade);
        AddUpgrade(autoCollectUpgrade);
    }

    private void OnEnable()
    {
        foreach (UpgradeSO upgrade in _upgrades) upgrade.OnLevelChanged += Apply;
        if (_started)
            foreach (UpgradeSO upgrade in _upgrades) Apply(upgrade);
    }

    private void OnDisable()
    {
        foreach (UpgradeSO upgrade in _upgrades) upgrade.OnLevelChanged -= Apply;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // Start, so every target system has finished its own Awake.
    private void Start()
    {
        _started = true;
        foreach (UpgradeSO upgrade in _upgrades) Apply(upgrade);
    }

    /// <summary> True when the upgrade has a next level the balance covers. </summary>
    public bool CanPurchase(UpgradeSO upgrade)
    {
        return isActiveAndEnabled && upgrade != null && _upgrades.Contains(upgrade) && !upgrade.IsMaxed
            && CoinsManager.Instance != null && CoinsManager.Instance.CanAfford(upgrade.NextCost);
    }

    /// <summary> Spends coins and raises the upgrade one level. False when refused. </summary>
    public bool TryPurchase(UpgradeSO upgrade)
    {
        if (!CanPurchase(upgrade) || !CoinsManager.Instance.TrySpend(upgrade.NextCost)) return false;

        upgrade.SetLevel(upgrade.Level + 1); // OnLevelChanged applies it
        return true;
    }

    /// <summary> Registers an assigned upgrade and loads its saved level. </summary>
    private void AddUpgrade(UpgradeSO upgrade)
    {
        if (upgrade == null || _upgrades.Contains(upgrade)) return;

        upgrade.Reload(); // SO state can outlive play sessions in the editor
        _upgrades.Add(upgrade);
    }

    /// <summary> Routes an upgrade's current values to its target system. Powers read their own SO. </summary>
    private void Apply(UpgradeSO upgrade)
    {
        if (upgrade == inventoryUpgrade) ApplyInventory();
        else if (upgrade == throwUpgrade) ApplyThrow();
        else if (upgrade == reachUpgrade) ApplyReach();
        else if (upgrade == sprintUpgrade) ApplySprint();
        else if (upgrade == autoShelfUpgrade) ApplyAutoShelf();
    }

    private void ApplyInventory()
    {
        InventoryHandler inventory = Player != null && Player.StashHandler != null ? Player.StashHandler.Inventory : null;
        if (inventory != null) inventory.SetCapacity(inventoryUpgrade.CurrentValues.capacity);
    }

    private void ApplyThrow()
    {
        if (Player == null || Player.ItemHandler == null) return;

        Player.ItemHandler.SetThrowForce(throwUpgrade.CurrentValues.throwForce);
    }

    private void ApplyReach()
    {
        if (Interaction != null) Interaction.SetMaxDistance(reachUpgrade.CurrentValues.distance *
            (TinyRiders.ThemedCollectionSession.Instance != null ? TinyRiders.ThemedCollectionSession.Instance.worldScale : 1f));
    }

    private void ApplySprint()
    {
        if (Player != null && Player.Input != null) Player.Input.SetSprintEnabled(sprintUpgrade.CurrentValues.unlocked);
    }

    private void ApplyAutoShelf()
    {
        SortingShelf.SetDistanceShelfingEnabled(autoShelfUpgrade.CurrentValues.enabled);
    }

    /// <summary>Clears every purchased upgrade before starting a fresh store.</summary>
    public void ResetForReplay()
    {
        foreach (UpgradeSO upgrade in _upgrades)
            if (upgrade != null) upgrade.ResetLevel();
        PlayerPrefs.Save();
    }

#if UNITY_EDITOR
    /// <summary> Test-only: sets every upgrade back to level 0. </summary>
    [ContextMenu("Reset Saved Upgrades")]
    private void ResetSavedUpgrades()
    {
        UpgradeSO[] all = {
            inventoryUpgrade, throwUpgrade, reachUpgrade, sprintUpgrade, autoShelfUpgrade,
            highlightItemsUpgrade, autoCollectUpgrade,
        };
        foreach (UpgradeSO upgrade in all)
            if (upgrade != null) upgrade.SetLevel(0); // fields, so it also works in edit mode
    }
#endif
}
