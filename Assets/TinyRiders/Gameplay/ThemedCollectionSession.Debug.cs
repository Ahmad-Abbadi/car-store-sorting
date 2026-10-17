#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TinyRiders
{
    public sealed partial class ThemedCollectionSession
    {
        private bool debugSorting;

        /// <summary> F9 test cheat: restart the collection in place, retaining coins, upgrades and instance IDs. </summary>
        public bool DebugResetCollection()
        {
            if (!Application.isPlaying || !ready || debugSorting) return false;
            if (pileOrigin == null || definitions.Values.Any(e => e.sortingItem == null || e.sortingItem.WorldItem == null))
            { Report("Debug: Cannot reset collection — pile origin or car prefab is missing"); return false; }
            debugSorting = true;
            try
            {
                var activeIds = new HashSet<string>(records.Values.Where(r => definitions.ContainsKey(r.collectibleId)).Select(r => r.id));
                DebugRemoveHeldInstances(activeIds);
                foreach (var shelf in shelves.Values.Where(s => s != null && s.gameObject.scene == gameObject.scene))
                    for (int spot = 0; spot < shelf.SpotCount; spot++)
                        if (activeIds.Contains(ThemedWorldInstance.Read(shelf.GetOccupant(spot)) ?? ""))
                            shelf.DebugRemoveForSort(spot, false);
                foreach (var world in live.Values.Concat(pool).Where(w => w != null).Distinct().ToArray())
                {
                    world.gameObject.SetActive(false);
                    Destroy(world.gameObject);
                }
                live.Clear();
                pool.Clear();
                foreach (var record in records.Values.Where(r => activeIds.Contains(r.id)))
                {
                    record.state = "pending";
                    record.shelfId = null;
                    record.slotId = null;
                    record.position = Vector3.zero;
                    record.euler = Vector3.zero;
                }
                discovered.Clear();
                completed.Clear();
                State.discovered.Clear();
                State.completedThemes.Clear();
                var save = FindFirstObjectByType<LevelSaveManager>();
                save?.DebugResetCollectionProgress();
                // F9 returns the entire collection, even in scenes that normally refill a smaller visible pile.
                bool spawnAll = spawnEntireCollectionAtStart;
                try { spawnEntireCollectionAtStart = true; Refill(false); }
                finally { spawnEntireCollectionAtStart = spawnAll; }
                Message = "Debug: Collection reset — all cars back in the pile";
                Publish();
                foreach (var ui in FindObjectsByType<CarStoreUI>(FindObjectsSortMode.None)) ui.DebugResumeCollection();
                LevelSaveManager.MarkDirty();
                save?.Save();
                return true;
            }
            finally { debugSorting = false; }
        }

        /// <summary> F8 test cheat: sort the whole active collection, preserving permanent instance IDs. </summary>
        public bool DebugSortAll()
        {
            if (!Application.isPlaying || !ready || debugSorting) return false;
            debugSorting = true;
            try
            {
                var remaining = records.Values.Where(r => definitions.ContainsKey(r.collectibleId) && r.state != "sorted")
                    .OrderBy(r => r.id, StringComparer.Ordinal).ToArray();
                if (remaining.Length == 0) return true;

                // Sorted occupants stay in place. Only known, unsorted instances may be reclaimed.
                var movingIds = new HashSet<string>(remaining.Select(r => r.id));
                var slots = new List<(SortingShelf shelf, int spot)>();
                var removals = new List<(SortingShelf shelf, int spot)>();
                foreach (var shelf in shelves.Values.Where(s => s != null && s.gameObject.scene == gameObject.scene)
                    .OrderBy(s => s.SaveId, StringComparer.Ordinal))
                    for (int spot = 0; spot < shelf.SpotCount; spot++)
                    {
                        if (shelf.GetSpot(spot) == null) continue;
                        var occupant = shelf.GetOccupant(spot);
                        if (occupant != null)
                        {
                            if (!movingIds.Contains(ThemedWorldInstance.Read(occupant))) continue;
                            removals.Add((shelf, spot));
                        }
                        slots.Add((shelf, spot));
                    }

                // Find a full matching before touching cars, inventory, records or saves.
                // Reassigning earlier matches also handles shelves with different accessory clearances.
                var candidates = remaining.Select(r => Enumerable.Range(0, slots.Count)
                    .Where(i => slots[i].shelf.DebugCanPlaceInstant(definitions[r.collectibleId].sortingItem, slots[i].spot))
                    .ToArray()).ToArray();
                var owners = Enumerable.Repeat(-1, slots.Count).ToArray();
                bool Assign(int car, bool[] visited)
                {
                    foreach (int slot in candidates[car])
                    {
                        if (visited[slot]) continue;
                        visited[slot] = true;
                        if (owners[slot] >= 0 && !Assign(owners[slot], visited)) continue;
                        owners[slot] = car;
                        return true;
                    }
                    return false;
                }
                for (int car = 0; car < remaining.Length; car++)
                    if (!Assign(car, new bool[slots.Count]))
                    {
                        Report("Debug: Cannot sort all cars — insufficient compatible shelf space");
                        return false;
                    }

                foreach (var source in removals) source.shelf.DebugRemoveForSort(source.spot);
                for (int slot = 0; slot < owners.Length; slot++)
                {
                    if (owners[slot] < 0) continue;
                    var record = remaining[owners[slot]];
                    var destination = slots[slot];
                    if (!destination.shelf.DebugPlaceInstant(definitions[record.collectibleId].sortingItem, destination.spot, record.id))
                        throw new InvalidOperationException("A prevalidated debug sorting slot became unavailable: " + record.id);
                    record.state = "sorted";
                    record.shelfId = destination.shelf.SaveId;
                    record.slotId = destination.shelf.GetSpot(destination.spot).name;
                    discovered.Add(record.collectibleId);
                }

                // Disable sources immediately; deferred Destroy cannot leave a second interactive copy this frame.
                foreach (string id in movingIds)
                    if (live.TryGetValue(id, out var world))
                    {
                        live.Remove(id);
                        if (world == null) continue;
                        world.gameObject.SetActive(false);
                        Destroy(world.gameObject);
                    }

                DebugRemoveHeldInstances(movingIds);
                State.discovered = discovered.OrderBy(id => id, StringComparer.Ordinal).ToList();
                foreach (var theme in definitions.Values.Select(e => e.themeId).Distinct())
                {
                    var counts = ThemeCounts(theme);
                    if (counts.total > 0 && counts.sorted == counts.total) completed.Add(theme);
                }
                State.completedThemes = completed.OrderBy(id => id, StringComparer.Ordinal).ToList();
                Message = "Debug: All cars sorted";
                LevelSaveManager.MarkDirty();
                Publish();
                var save = FindFirstObjectByType<LevelSaveManager>();
                // MarkStoreCompleted saves once; the normal UI completion check later becomes a no-op save.
                if (save != null && completeStoreWhenSorted && Sorted == Total && !save.StoreCompleted)
                    LevelSaveManager.MarkStoreCompleted();
                else save?.Save();
                return true;
            }
            finally { debugSorting = false; }
        }

        private void DebugRemoveHeldInstances(HashSet<string> ids)
        {
            var player = Player.Instance;
            var stash = player != null ? player.StashHandler : null;
            var inventory = stash != null ? stash.Inventory : null;
            if (inventory != null)
            {
                var keptItems = new List<ItemDataSO>();
                var keptIds = new List<string>();
                for (int i = 0; i < inventory.Count; i++)
                    if (!ids.Contains(inventory.InstanceIdAt(i) ?? ""))
                    { keptItems.Add(inventory.GetItemAt(i)); keptIds.Add(inventory.InstanceIdAt(i)); }
                if (keptItems.Count != inventory.Count)
                {
                    player.ItemCollector?.DebugCancelPickupFlight();
                    player.ItemHandler?.Unequip();
                    stash.LoadFromSave(keptItems, 0);
                    inventory.SetInstanceIds(keptIds);
                    if (inventory.Count > 0) inventory.NotifyEquipped(0);
                }
            }
            State.heldOrder.RemoveAll(id => ids.Contains(id ?? ""));
            State.selection = stash != null ? stash.SelectedIndex : 0;
        }
    }
}
#endif
