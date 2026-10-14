using UnityEngine;

// Cached hand item + its use-interfaces, built once at equip.
internal struct EquippedItem
{
    public HandItemBase Item;
    public IItemClickable Clickable;
    public IItemHoldable Holdable;

    // Caches every use-interface on the item in one pass.
    public EquippedItem(HandItemBase item)
    {
        Item = item;
        Clickable = null;
        Holdable = null;

        if (item == null) return;

        item.TryGetComponent(out Clickable);
        item.TryGetComponent(out Holdable);
    }

    // True when an item is actually equipped.
    public bool IsValid => Item != null;

    // Explicit "nothing equipped" value.
    public static EquippedItem None => new EquippedItem(null);
}
