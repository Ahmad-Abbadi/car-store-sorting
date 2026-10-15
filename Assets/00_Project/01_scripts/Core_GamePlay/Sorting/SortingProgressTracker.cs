using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary> Counts right-placed items against every item found at level start. </summary>
public class SortingProgressTracker : MonoBehaviour
{
    public static SortingProgressTracker Instance { get; private set; }

    /// <summary> Fired with (collected, total) whenever either changes. </summary>
    public static event Action<int, int> OnProgressChanged;

    /// <summary> Items currently sitting on their right shelf. </summary>
    public int Collected { get; private set; }

    /// <summary> Every item in the level, counted once at start. </summary>
    public int Total { get; private set; }

    private readonly List<(int spot, SortableItem_SO data)> _shelfBuffer = new List<(int, SortableItem_SO)>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnEnable()
    {
        SortingShelf.OnRightItemPlaced += HandleRightItemPlaced;
        SortingShelf.OnRightItemRemoved += HandleRightItemRemoved;
    }

    private void OnDisable()
    {
        SortingShelf.OnRightItemPlaced -= HandleRightItemPlaced;
        SortingShelf.OnRightItemRemoved -= HandleRightItemRemoved;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // Waits a frame so the save is restored first.
    private IEnumerator Start()
    {
        yield return null;
        Recount();
    }

    /// <summary> Shows saved counts until the Start recount replaces them. </summary>
    public void LoadFromSave(int collected, int total)
    {
        Collected = Mathf.Max(0, collected);
        Total = Mathf.Max(0, total);
        Notify();
    }

    /// <summary> Counts every item and right-placed item from the live level. </summary>
    private void Recount()
    {
        if (TinyRiders.ThemedCollectionSession.Instance != null)
        {
            LoadFromSave(TinyRiders.ThemedCollectionSession.Instance.Sorted, TinyRiders.ThemedCollectionSession.Instance.Total);
            return;
        }
        int total = 0;
        int collected = 0;

        // Inactive included: culling may have disabled some items.
        total += FindObjectsByType<SortableWorldItem>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;

        foreach (PlacedOnShelfItemBase item in FindObjectsByType<PlacedOnShelfItemBase>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (item.Shelf != null) total++;

        if (StashHandler.CurrentStash != null)
            foreach (ItemDataSO held in StashHandler.CurrentStash)
                if (held is SortableItem_SO) total++;

        foreach (SortingShelf shelf in FindObjectsByType<SortingShelf>(FindObjectsSortMode.None))
        {
            shelf.GetPlacedItems(_shelfBuffer);
            foreach (var placed in _shelfBuffer)
                if (shelf.IsCorrectItem(placed.data)) collected++;
        }

        Total = total;
        Collected = collected;
        Notify();
    }

    private void HandleRightItemPlaced(SortingShelf shelf, SortableItem_SO data)
    {
        if (TinyRiders.ThemedCollectionSession.Instance != null) return;
        Collected++;
        Notify();
    }

    private void HandleRightItemRemoved(SortingShelf shelf, SortableItem_SO data)
    {
        if (TinyRiders.ThemedCollectionSession.Instance != null) return;
        Collected = Mathf.Max(0, Collected - 1);
        Notify();
    }

    private void Notify() => OnProgressChanged?.Invoke(Collected, Total);
}
