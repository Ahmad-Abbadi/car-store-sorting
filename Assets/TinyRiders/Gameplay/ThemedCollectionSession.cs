using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace TinyRiders
{
    [Serializable] public sealed class ThemedInstanceRecord
    {
        public string id, collectibleId, state = "pending", shelfId, slotId;
        public Vector3 position, euler;
    }
    [Serializable] public sealed class ThemedSessionSave
    {
        public int schema = 1, seed;
        public int pileLayoutRevision;
        public List<ThemedInstanceRecord> instances = new List<ThemedInstanceRecord>();
        public List<string> discovered = new List<string>();
        public List<string> completedThemes = new List<string>();
        public List<string> heldOrder = new List<string>();
        public int selection;
    }
    /// <summary>Finite visible pile over an extensible, deterministic physical collection. Uses the template's inventory, shelves and save coordinator.</summary>
    [DefaultExecutionOrder(-150)]
    public sealed class ThemedCollectionSession : MonoBehaviour
    {
        public static ThemedCollectionSession Instance { get; private set; }
        public ThemedCarCatalog catalog;
        public int seed = 73481;
        public float worldScale = 9f;
        [Min(1)] public int copiesPerDesign = 2;
        [Range(12, 180)] public int visiblePileLimit = 72;
        public Transform pileOrigin;
        public TextAsset pileLayout;
        public Vector3 pileSpacing = new Vector3(5.8f, 3f, 6.8f);
        public bool suspendPersistenceForTests;
        public string Message { get; private set; } = "Pick a car, then match its theme to a shelf";
        public event Action Changed;
        public ThemedSessionSave State { get; private set; } = new ThemedSessionSave();
        private readonly Dictionary<string, ThemedCarCatalog.Entry> definitions = new Dictionary<string, ThemedCarCatalog.Entry>();
        private readonly Dictionary<string, ThemedInstanceRecord> records = new Dictionary<string, ThemedInstanceRecord>();
        private readonly Dictionary<string, WorldItemBase> live = new Dictionary<string, WorldItemBase>();
        private readonly Dictionary<string, SortingShelf> shelves = new Dictionary<string, SortingShelf>();
        private readonly HashSet<string> discovered = new HashSet<string>();
        private readonly HashSet<string> completed = new HashSet<string>();
        private readonly List<WorldItemBase> pool = new List<WorldItemBase>();
        [Serializable] private class Layout { public int revision; public ThemedInstanceRecord[] placements; }
        private readonly Dictionary<string, ThemedInstanceRecord> baked = new Dictionary<string, ThemedInstanceRecord>();
        private Dictionary<string, int> pileOrder;
        private int pileOrderSeed;
        public bool Recycle(WorldItemBase item)
        {
            if (item == null || Definition(item.Data) == null || pool.Count >= 24) return false;
            item.gameObject.SetActive(false);
            if (item.Body != null) item.Body.isKinematic = true;
            item.GetComponent<ThemedWorldInstance>()?.SetId(null);
            pool.Add(item);
            return true;
        }
        private float nextMaintenance;
        private bool ready;
        public IEnumerable<ThemedCarCatalog.Entry> ActiveDefinitions => definitions.Values;
        public int Total => records.Values.Count(r => definitions.ContainsKey(r.collectibleId));
        public int Sorted => records.Values.Count(r => r.state == "sorted" && definitions.ContainsKey(r.collectibleId));
        public int UniqueDiscovered => discovered.Count(id => definitions.ContainsKey(id));
        public int LivePileCount => live.Count;
        public int Capacity => shelves.Values.Sum(s => s.SpotCount);
        private void Awake()
        {
            Instance = this;
            if (catalog == null) { Debug.LogError("Themed sorting catalog is missing", this); enabled = false; return; }
            State.seed = seed;
            State.pileLayoutRevision = 1;
            if (pileLayout != null)
                foreach (var pose in JsonUtility.FromJson<Layout>(pileLayout.text).placements) baked[pose.id] = pose;
            RefreshContent();
            foreach (var s in FindObjectsByType<SortingShelf>(FindObjectsSortMode.None))
                if (s.GetComponent<ThemeShelfBinding>() != null && !shelves.TryAdd(s.SaveId, s)) Debug.LogError("Duplicate shelf ID: " + s.SaveId);
        }
        public void RefreshContent()
        {
            pileOrder = null;
            definitions.Clear();
            foreach (var e in catalog.entries ?? Array.Empty<ThemedCarCatalog.Entry>())
                if (e != null && e.enabled && e.prefab != null && e.sortingItem != null &&
                    catalog.themes.Any(t => t.id == e.themeId && t.enabled))
                {
                    if (!definitions.TryAdd(e.collectibleId, e)) Debug.LogError("Duplicate collectible ID: " + e.collectibleId);
                }
            MergeContent();
            Publish();
        }
        private System.Collections.IEnumerator Start()
        {
            // The save coordinator restores in Start; never materialize the initial pile before that restoration.
            yield return null;
            ready = true; Refill(); Publish();
        }
        private void OnDestroy() { if (Instance == this) Instance = null; }
        public ThemedCarCatalog.Entry Definition(ItemDataSO item) => item != null && definitions.TryGetValue(item.SaveId, out var e) ? e : null;
        public string ThemeName(string id) => catalog.themes.FirstOrDefault(t => t.id == id)?.displayName ?? id;
        private void MergeContent()
        {
            // IDs contain permanent design IDs, never catalog indices. Existing records survive release toggles.
            foreach (var e in definitions.Values.OrderBy(e => e.collectibleId, StringComparer.Ordinal))
                for (int copy = 1; copy <= copiesPerDesign; copy++)
                {
                    string id = "TRWORLD-" + State.seed + ":" + e.collectibleId + ":" + copy.ToString("D4");
                    if (records.ContainsKey(id)) continue;
                    var record = new ThemedInstanceRecord { id = id, collectibleId = e.collectibleId };
                    State.instances.Add(record); records.Add(id, record);
                }
            // A completed theme can gain new designs when a collection release is added.
            completed.RemoveWhere(theme => { var counts = ThemeCounts(theme); return counts.total > 0 && counts.sorted < counts.total; });
            State.completedThemes = completed.OrderBy(x => x, StringComparer.Ordinal).ToList();
        }
        public void Report(string message) { Message = message; Changed?.Invoke(); }
        public void Held(string id)
        {
            if (string.IsNullOrEmpty(id) || !records.TryGetValue(id, out var r)) return;
            r.state = "held"; r.shelfId = null; r.slotId = null; live.Remove(id);
            LevelSaveManager.MarkDirty(); Publish();
        }
        public void Dropped(WorldItemBase item, string id)
        {
            if (item == null || string.IsNullOrEmpty(id) || !records.TryGetValue(id, out var r)) return;
            ThemedWorldInstance.Stamp(item, id); r.state = "world"; live[id] = item;
            r.shelfId = null; r.slotId = null; LevelSaveManager.MarkDirty(); Publish();
        }
        public bool CanPlace(string id) => !string.IsNullOrEmpty(id) && records.TryGetValue(id, out var r) && r.state != "sorted";
        public void Placed(PlacedOnShelfItemBase item, SortingShelf shelf, int spot)
        {
            string id = ThemedWorldInstance.Read(item);
            if (!CanPlace(id)) return;
            var r = records[id]; r.state = "sorted"; r.shelfId = shelf.SaveId; r.slotId = shelf.GetSpot(spot).name;
            live.Remove(id); bool first = discovered.Add(r.collectibleId);
            State.discovered = discovered.OrderBy(x => x, StringComparer.Ordinal).ToList();
            var e = definitions[r.collectibleId];
            Report(first ? "Discovered " + e.displayName : "Sorted · " + ThemeName(e.themeId));
            int themeTotal = ThemeCounts(e.themeId).total;
            if (themeTotal > 0 && ThemeCounts(e.themeId).sorted == themeTotal && completed.Add(e.themeId))
                Report("Collection complete · " + ThemeName(e.themeId));
            State.completedThemes = completed.OrderBy(x => x, StringComparer.Ordinal).ToList();
            Publish(); LevelSaveManager.MarkDirty();
            // Occupancy is claimed before this checkpoint; animation interruption cannot duplicate it.
            FindFirstObjectByType<LevelSaveManager>()?.Save();
        }
        public (int sorted, int total) ThemeCounts(string theme)
        {
            int total = 0, sorted = 0;
            foreach (var r in records.Values)
                if (definitions.TryGetValue(r.collectibleId, out var e) && e.themeId == theme)
                { total++; if (r.state == "sorted") sorted++; }
            return (sorted, total);
        }
        private void Publish()
        {
            SortingProgressTracker.Instance?.LoadFromSave(Sorted, Total);
            Changed?.Invoke();
        }
        private void Update()
        {
            if (!ready || Time.unscaledTime < nextMaintenance) return;
            nextMaintenance = Time.unscaledTime + .4f;
            foreach (var pair in live.ToArray())
            {
                if (pair.Value == null) { live.Remove(pair.Key); continue; }
                var t = pair.Value.transform;
                if (t.position.y < -20f) { t.SetPositionAndRotation(pileOrigin.position + Vector3.up * 5, Quaternion.identity); if (pair.Value.Body != null) pair.Value.Body.isKinematic = true; }
                records[pair.Key].position = t.position; records[pair.Key].euler = t.eulerAngles;
            }
            Refill();
        }
        private uint Rank(string id)
        {
            unchecked { uint hash = (uint)State.seed; foreach (char c in id) hash = (hash ^ c) * 16777619; return hash; }
        }
        private Vector3 PilePosition(int cell)
        {
            uint noise = Rank("cell" + cell);
            float angle = cell * 2.399963f;
            float radius = 25f + cell / 24 * 5.5f;
            return pileOrigin.position + new Vector3(Mathf.Cos(angle) * radius, .05f, Mathf.Sin(angle) * radius);
        }
        private void Refill()
        {
            if (pileOrigin == null || live.Count >= visiblePileLimit) return;
            if (pileOrder == null || pileOrderSeed != State.seed)
            {
                pileOrder = ThemedPileSequence.InstanceIds(definitions.Values, copiesPerDesign, State.seed)
                    .Select((id, index) => new { id, index }).ToDictionary(x => x.id, x => x.index);
                pileOrderSeed = State.seed;
            }
            foreach (var r in records.Values.Where(r => (r.state == "pending" || r.state == "world") &&
                         !live.ContainsKey(r.id) && definitions.ContainsKey(r.collectibleId))
                         .OrderBy(r => r.state == "world" ? 0 : 1)
                         .ThenBy(r => pileOrder.TryGetValue(r.id, out var index) ? index : int.MaxValue)
                         .ThenBy(r => r.id, StringComparer.Ordinal))
            {
                if (live.Count >= visiblePileLimit) break;
                bool fresh = r.state == "pending";
                int cell = 0;
                // Never create an inaccessible copy underneath an existing car.
                while (cell < visiblePileLimit * 3 && live.Values.Any(w => w != null &&
                    (w.transform.position - PilePosition(cell)).sqrMagnitude < pileSpacing.x * pileSpacing.x * .65f)) cell++;
                ThemedInstanceRecord pose = null;
                bool hasBaked = fresh && baked.TryGetValue(r.id, out pose);
                Vector3 p = hasBaked ? pileOrigin.position + pose.position : fresh ? PilePosition(cell) : r.position;
                Quaternion q = hasBaked ? Quaternion.Euler(pose.euler) : fresh ? Quaternion.Euler(0, (Rank(r.id) % 360), 0) : Quaternion.Euler(r.euler);
                var source = definitions[r.collectibleId].sortingItem.WorldItem;
                var item = pool.FirstOrDefault(w => w != null && w.Data == source.Data);
                if (item != null)
                {
                    pool.Remove(item); item.transform.SetPositionAndRotation(p, q);
                    item.Frozen = true; item.Moved = false; item.ThrowControlled = false; item.RestPosition = p;
                    item.gameObject.SetActive(true);
                }
                else item = Instantiate(source, p, q);
                Dropped(item, r.id);
            }
        }
        public ThemedSessionSave Capture()
        {
            foreach (var pair in live)
                if (pair.Value != null && records.TryGetValue(pair.Key, out var r))
                { r.position = pair.Value.transform.position; r.euler = pair.Value.transform.eulerAngles; }
            State.heldOrder.Clear();
            var stash = Player.Instance?.StashHandler;
            if (stash?.Inventory != null)
                for (int i = 0; i < stash.Inventory.Count; i++) State.heldOrder.Add(stash.Inventory.InstanceIdAt(i));
            State.selection = stash != null ? stash.SelectedIndex : 0;
            return State;
        }
        public void Restore(ThemedSessionSave save)
        {
            if (save == null) return;
            if (save.schema != 1) { Debug.LogWarning("Unsupported themed save retained without loading"); return; }
            State = save; records.Clear(); discovered.Clear(); completed.Clear();
            foreach (string id in save.discovered ?? new List<string>()) discovered.Add(id);
            foreach (string id in save.completedThemes ?? new List<string>()) completed.Add(id);
            foreach (var r in save.instances ?? new List<ThemedInstanceRecord>())
                if (r != null && !string.IsNullOrEmpty(r.id) && !records.ContainsKey(r.id))
                {
                    // Upgrade only the old unsorted staging layout; held/sorted progress and identities remain intact.
                    if (save.pileLayoutRevision < 1 && r.state == "world" && baked.TryGetValue(r.id, out var pose))
                    { r.position = pileOrigin.position + pose.position; r.euler = pose.euler; }
                    records.Add(r.id, r);
                }
            State.pileLayoutRevision = 1;
            State.instances = records.Values.ToList(); MergeContent();
            var usedSlots = new HashSet<string>();
            foreach (var r in records.Values.Where(r => r.state == "sorted" && definitions.ContainsKey(r.collectibleId)))
            {
                if (shelves.TryGetValue(r.shelfId ?? "", out var s))
                {
                    int spot = -1;
                    for (int i = 0; i < s.SpotCount; i++) if (s.GetSpot(i).name == r.slotId) { spot = i; break; }
                    string slotKey = s.SaveId + "/" + r.slotId;
                    if (spot >= 0 && !usedSlots.Contains(slotKey) && s.CanAccept(definitions[r.collectibleId].sortingItem, spot, out _) &&
                        s.RestoreItem(spot, definitions[r.collectibleId].sortingItem))
                    { ThemedWorldInstance.Stamp(s.GetOccupant(spot), r.id); usedSlots.Add(slotKey); continue; }
                }
                r.state = "pending"; r.shelfId = null; r.slotId = null;
            }
            var held = new List<ItemDataSO>(); var heldIds = new List<string>();
            foreach (string id in save.heldOrder ?? new List<string>())
                if (!heldIds.Contains(id ?? "") && records.TryGetValue(id ?? "", out var r) && r.state == "held" && definitions.TryGetValue(r.collectibleId, out var e))
                { held.Add(e.sortingItem); heldIds.Add(id); }
            foreach (var r in records.Values) if (r.state == "held" && !heldIds.Contains(r.id) && definitions.ContainsKey(r.collectibleId)) r.state = "pending";
            var stash = Player.Instance?.StashHandler;
            stash?.LoadFromSave(held, save.selection); stash?.Inventory?.SetInstanceIds(heldIds);
            Publish();
        }
    }
}
