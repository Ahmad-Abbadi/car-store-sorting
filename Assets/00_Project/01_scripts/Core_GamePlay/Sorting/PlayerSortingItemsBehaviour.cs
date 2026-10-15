using UnityEngine;

/// <summary> Player-side sorting: turns the held item into a shelf item, and back. </summary>
public class PlayerSortingItemsBehaviour : MonoBehaviour
{
    [Tooltip("Player whose hand and stash are used.")]
    [SerializeField] private Player player;

    /// <summary> True when the hand holds a sortable item; outputs its data. </summary>
    public bool TryGetHeldSortable(out SortableItem_SO data)
    {
        data = null;
        if (player == null || player.ItemHandler == null) return false;

        HandItemBase held = player.ItemHandler.CurrentEquipped;
        if (held == null) return false;

        data = held.Data as SortableItem_SO;
        return data != null && data.PlacedOnShelfPrefab != null;
    }

    /// <summary> Spawns the held item's shelf form at the hand, then spends it from the stash. </summary>
    public bool TryGetPlacedItemFromHand(out PlacedOnShelfItemBase placed)
    {
        placed = null;
        if (!TryGetHeldSortable(out SortableItem_SO data)) return false;

        // Pose is read before the hand item is destroyed.
        player.ItemHandler.CurrentEquipped.transform.GetPositionAndRotation(out Vector3 position, out Quaternion rotation);

        placed = Instantiate(data.PlacedOnShelfPrefab, position, rotation);
        placed.Init(data);
        TinyRiders.ThemedWorldInstance.Stamp(placed,
            player.StashHandler?.Inventory?.InstanceIdAt(player.StashHandler.SelectedIndex));

        player.ItemHandler.Unequip(); // shelf form replaces it, so no sink

        // Removing from the stash equips the next item.
        if (player.StashHandler != null) player.StashHandler.TryRemove(data);

        return true;
    }

    /// <summary> Stashes a placed item and flies it into the hand. False if refused. </summary>
    public bool TryCollectPlacedItem(PlacedOnShelfItemBase placed)
    {
        if (placed == null || placed.Data == null || player == null || player.ItemCollector == null) return false;

        placed.transform.GetPositionAndRotation(out Vector3 position, out Quaternion rotation);
        if (!player.ItemCollector.TryCollectItem(placed.Data, position, rotation)) return false;
        player.ItemCollector.StampCollectedInstance(TinyRiders.ThemedWorldInstance.Read(placed));
        return true;
    }
}
