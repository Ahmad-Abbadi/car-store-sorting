using System;
using System.Collections.Generic;
using UnityEngine;

// Non-visual inventory: a list of ItemData with add/remove/swap.
public class InventoryHandler : MonoBehaviour
{
    [Tooltip("Max items held. 0 = unlimited.")]
    [SerializeField] private int capacity = 0;

    [Tooltip("Items currently held.")]
    [SerializeField] private List<ItemDataSO> items = new List<ItemDataSO>();
    [SerializeField] private List<string> instanceIds = new List<string>();
    public string InstanceIdAt(int index) => index >= 0 && index < instanceIds.Count ? instanceIds[index] : null;
    public void SetInstanceIds(IEnumerable<string> ids)
    {
        instanceIds = new List<string>(ids);
        while (instanceIds.Count < items.Count) instanceIds.Add(null);
    }
    public void SetLastInstanceId(string id)
    {
        while (instanceIds.Count < items.Count) instanceIds.Add(null);
        if (items.Count > 0) instanceIds[items.Count - 1] = id;
    }

    // Fired whenever the contents change.
    public event Action OnChanged;

    // Fired when an item is equipped, with its index.
    public event Action<int> OnItemEquip;

    // Read-only view of the held items.
    public IReadOnlyList<ItemDataSO> Items => items;

    // Number of items held.
    public int Count => items.Count;

    // True when capacity is set and reached.
    public bool IsFull => capacity > 0 && items.Count >= capacity;

    // Max slots. 0 = unlimited.
    public int Capacity => capacity;

    /// <summary> Sets max slots (0 = unlimited). Held items above it are kept. </summary>
    public void SetCapacity(int value)
    {
        value = Mathf.Max(0, value);
        if (value == capacity) return;

        capacity = value;
        OnChanged?.Invoke(); // lets the stash UI rebuild its slots
    }

    // Adds an item. False if null or full.
    public bool TryAddItem(ItemDataSO item)
    {
        if (item == null || IsFull) return false;

        items.Add(item);
        instanceIds.Add(null);
        OnChanged?.Invoke();
        return true;
    }

    /// <summary> Replaces all items, ignoring capacity. Used by save restore. </summary>
    public void SetItems(IEnumerable<ItemDataSO> newItems)
    {
        items.Clear();
        instanceIds.Clear();
        foreach (ItemDataSO item in newItems)
            if (item != null) { items.Add(item); instanceIds.Add(null); }

        OnChanged?.Invoke();
    }

    // Removes the first matching item. False if absent.
    public bool RemoveItem(ItemDataSO item)
    {
        int index = items.IndexOf(item);
        if (item == null || index < 0) return false;
        items.RemoveAt(index);
        if (index < instanceIds.Count) instanceIds.RemoveAt(index);

        OnChanged?.Invoke();
        return true;
    }

    // Removes the item at index. False if out of range.
    public bool RemoveAt(int index)
    {
        if (!InRange(index)) return false;

        items.RemoveAt(index);
        if (index < instanceIds.Count) instanceIds.RemoveAt(index);
        OnChanged?.Invoke();
        return true;
    }

    // Swaps two items' positions. False if either index invalid.
    public bool SwapItems(int indexA, int indexB)
    {
        if (indexA == indexB || !InRange(indexA) || !InRange(indexB)) return false;

        (items[indexA], items[indexB]) = (items[indexB], items[indexA]);
        while (instanceIds.Count < items.Count) instanceIds.Add(null);
        (instanceIds[indexA], instanceIds[indexB]) = (instanceIds[indexB], instanceIds[indexA]);
        OnChanged?.Invoke();
        return true;
    }

    // True when the item is present.
    public bool Contains(ItemDataSO item) => item != null && items.Contains(item);

    // Empties the inventory.
    public void Clear()
    {
        if (items.Count == 0) return;

        items.Clear();
        instanceIds.Clear();
        OnChanged?.Invoke();
    }

    // Fires OnItemEquip for the item at index.
    public void NotifyEquipped(int index)
    {
        if (!InRange(index)) return;

        OnItemEquip?.Invoke(index);
    }

    public ItemDataSO GetItemAt(int index)
    {
        if(!InRange(index)) return null; 
        return items[index];
    }
    private bool InRange(int index) => index >= 0 && index < items.Count;
}
