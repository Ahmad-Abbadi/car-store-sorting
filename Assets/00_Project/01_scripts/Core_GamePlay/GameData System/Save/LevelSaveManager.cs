using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary> Saves the current level's world items, shelves and held items to PlayerPrefs. </summary>
[DefaultExecutionOrder(-100)] // restores before StashHandler, tracker and culling Start
public class LevelSaveManager : MonoBehaviour
{
    private const int PositionScale = 1000; // mm precision
    private const int RotationScale = 10;   // 0.1 degree precision
    private const int WorldStride = 7;      // id + position xyz + euler xyz
    private const int SaveVersion = 3;      // stable authored copy ids and store completion

    [Tooltip("Maps saved item ids back to item data.")]
    [SerializeField] private ItemDatabaseSO database;

    [Tooltip("Parent for restored world items; keep at unit scale. Scene root when empty.")]
    [SerializeField] private Transform worldItemsContainer;

    [Tooltip("PlayerPrefs key the level save is stored under.")]
    [SerializeField] private string saveKey = "LevelSave";

    [Tooltip("Min seconds between autosaves while there are unsaved changes. 0 disables autosave.")]
    [SerializeField, Min(0)] private float autosaveInterval = 60f;

    private readonly List<(int spot, SortableItem_SO data)> _shelfBuffer = new List<(int spot, SortableItem_SO data)>();
    private readonly Dictionary<string, int> _indexById = new Dictionary<string, int>();
    private readonly Dictionary<int, WorldItemBase> _authored = new Dictionary<int, WorldItemBase>();
    private SortingShelf[] _shelves = new SortingShelf[0]; // scene shelves never change
    private static LevelSaveManager _instance;
    private float _nextAutosave;
    private bool _ready;         // false until Load ran; blocks early saves
    private bool _levelFinished; // blocks saving once the level is won
    private bool _dirty;         // something changed since the last save
    private bool _storeCompleted;
    private float _elapsedSeconds;
    private bool _preserveUnreadableSave;

    public bool StoreCompleted => _storeCompleted;
    public float ElapsedSeconds => _elapsedSeconds;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    /// <summary> Clears completion for a new debug collection run; the caller saves after rebuilding the pile. </summary>
    internal void DebugResetCollectionProgress()
    {
        _storeCompleted = false;
        _elapsedSeconds = 0f;
        _levelFinished = false;
        MarkDirty();
    }
#endif

    /// <summary>Persists completion without clearing the finished store.</summary>
    public static void MarkStoreCompleted()
    {
        if (_instance == null || _instance._storeCompleted) return;
        _instance._storeCompleted = true;
        MarkDirty();
        _instance.Save();
    }

    /// <summary> Flags unsaved changes; the next autosave writes them. </summary>
    public static void MarkDirty()
    {
        if (_instance != null && _instance._ready) _instance._dirty = true;
    }

    private void Awake() => _instance = this;

    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }

    private void OnEnable() => LevelsManager.OnLevelCompleted += HandleLevelCompleted;

    private void OnDisable() => LevelsManager.OnLevelCompleted -= HandleLevelCompleted;

    private void Start()
    {
        if (database == null) Debug.LogWarning($"{name}: no item database assigned; level save disabled.", this);

        _shelves = FindObjectsByType<SortingShelf>(FindObjectsSortMode.None);
        RecordAuthored();
        Load();
        _nextAutosave = Time.unscaledTime + autosaveInterval;
        _ready = true;
        _dirty = false; // restoring isn't a change
    }

    private void Update()
    {
        if (_ready && !_storeCompleted && GameFlowManager.Instance != null &&
            GameFlowManager.Instance.currentState == GameFlowManager.GameState.Playing)
        {
            _elapsedSeconds += Time.deltaTime;
            _dirty = true;
        }
        if (!_dirty || autosaveInterval <= 0f || Time.unscaledTime < _nextAutosave) return;

        _nextAutosave = Time.unscaledTime + autosaveInterval;
        Save();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused && _dirty) Save();
    }

    private void OnApplicationFocus(bool focused)
    {
        if (!focused && _dirty) Save();
    }

    private void OnApplicationQuit()
    {
        if (_dirty) Save();
    }

    /// <summary> Writes the current level state to PlayerPrefs. </summary>
    [ContextMenu("Save Now")]
    public void Save()
    {
        if (TinyRiders.ThemedCollectionSession.Instance != null && TinyRiders.ThemedCollectionSession.Instance.suspendPersistenceForTests) return;
        if (!_ready || _levelFinished || database == null || _preserveUnreadableSave) return;

        PlayerPrefs.SetString(saveKey, JsonUtility.ToJson(Capture()));
        PlayerPrefs.Save();
        _dirty = false;
    }

    /// <summary> Deletes the level save. </summary>
    [ContextMenu("Clear Save")]
    public void Clear()
    {
        PlayerPrefs.DeleteKey(saveKey);
        PlayerPrefs.Save();
        _storeCompleted = false;
        _elapsedSeconds = 0f;
    }

    /// <summary> Wipes the save once the level is won; replays start fresh. </summary>
    private void HandleLevelCompleted(int levelNumber)
    {
        if (TinyRiders.ThemedCollectionSession.Instance != null) { Save(); return; }
        _levelFinished = true;
        Clear();
    }

    // ------------------------------------------------------------------ capture

    /// <summary> Snapshots world items, shelves, held items and progress. </summary>
    private LevelSaveData Capture()
    {
        LevelSaveData data = new LevelSaveData { version = SaveVersion, scene = gameObject.scene.name };
        _indexById.Clear();
        if (TinyRiders.ThemedCollectionSession.Instance != null)
        {
            data.themed = TinyRiders.ThemedCollectionSession.Instance.Capture();
            data.completed = _storeCompleted;
            data.elapsedSeconds = _elapsedSeconds;
            return data;
        }

        CaptureWorld(data);
        CaptureShelves(data);
        CaptureHeld(data);

        SortingProgressTracker tracker = SortingProgressTracker.Instance;
        if (tracker != null)
        {
            data.collected = tracker.Collected;
            data.total = tracker.Total;
        }
        data.completed = _storeCompleted;
        data.elapsedSeconds = _elapsedSeconds;
        return data;
    }

    /// <summary> Saves removed/moved authored items as keys and every other item as a pose. </summary>
    private void CaptureWorld(LevelSaveData data)
    {
        // Managed flags only: this runs over thousands of items.
        foreach (KeyValuePair<int, WorldItemBase> pair in _authored)
        {
            WorldItemBase item = pair.Value;
            bool gone = item == null || !item.IsRegistered;
            if (!gone && !item.Moved) continue;

            data.removed.Add(pair.Key);
            if (!gone) AddPose(item, data); // moved: saved like a spawned item
        }

        // Own scene only, so another loaded scene's items never leak in.
        Scene own = gameObject.scene;
        foreach (WorldItemBase item in WorldItemBase.All)
            if (!item.IsAuthored && item.gameObject.scene == own) AddPose(item, data);
    }

    /// <summary> Appends an item's type, position and rotation. </summary>
    private void AddPose(WorldItemBase item, LevelSaveData data)
    {
        int index = IndexOf(item.Data, data);
        if (index < 0) return;

        item.transform.GetPositionAndRotation(out Vector3 position, out Quaternion rotation);
        Vector3 euler = rotation.eulerAngles;

        data.world.Add(index);
        data.world.Add(Mathf.RoundToInt(position.x * PositionScale));
        data.world.Add(Mathf.RoundToInt(position.y * PositionScale));
        data.world.Add(Mathf.RoundToInt(position.z * PositionScale));
        data.world.Add(Mathf.RoundToInt(euler.x * RotationScale));
        data.world.Add(Mathf.RoundToInt(euler.y * RotationScale));
        data.world.Add(Mathf.RoundToInt(euler.z * RotationScale));
    }

    /// <summary> Asks every shelf for its placed items. </summary>
    private void CaptureShelves(LevelSaveData data)
    {
        foreach (SortingShelf shelf in _shelves)
        {
            if (shelf == null) continue;

            shelf.GetPlacedItems(_shelfBuffer);
            if (_shelfBuffer.Count == 0) continue;

            if (string.IsNullOrEmpty(shelf.SaveId))
            {
                Debug.LogWarning($"{shelf.name}: shelf has no save id; its items are not saved.", shelf);
                continue;
            }

            ShelfSaveData save = new ShelfSaveData { id = shelf.SaveId };
            foreach ((int spot, SortableItem_SO item) in _shelfBuffer)
            {
                int index = IndexOf(item, data);
                if (index < 0) continue;

                save.items.Add(spot);
                save.items.Add(index);
            }
            data.shelves.Add(save);
        }
    }

    /// <summary> Saves the player's stash contents and selected slot. </summary>
    private void CaptureHeld(LevelSaveData data)
    {
        StashHandler stash = Player.Instance != null ? Player.Instance.StashHandler : null;
        if (stash == null || stash.Inventory == null) return;

        foreach (ItemDataSO item in stash.Inventory.Items)
        {
            int index = IndexOf(item, data);
            if (index >= 0) data.held.Add(index);
        }
        data.selected = stash.SelectedIndex;
    }

    /// <summary> Item's index in the id table, added if new; -1 if not in the database. </summary>
    private int IndexOf(ItemDataSO item, LevelSaveData data)
    {
        if (item == null) return -1;
        if (_indexById.TryGetValue(item.SaveId, out int index)) return index;

        if (!database.TryGet(item.SaveId, out _))
        {
            Debug.LogWarning($"{name}: '{item.SaveId}' is missing from the item database; not saved.", this);
            return -1;
        }

        index = data.ids.Count;
        data.ids.Add(item.SaveId);
        _indexById.Add(item.SaveId, index);
        return index;
    }

    // ------------------------------------------------------------------ authored items

    /// <summary> Keys every scene-placed world item by type and authored position. </summary>
    private void RecordAuthored()
    {
        _authored.Clear();

        foreach (WorldItemBase item in WorldItemBase.All)
        {
            if (item.gameObject.scene != gameObject.scene || item.Data == null) continue;

            int key = item.AuthoredSaveId > 0 ? item.AuthoredSaveId : AuthoredKey(item.Data.SaveId, item.transform.position);
            if (item.AuthoredSaveId > 0 && _authored.ContainsKey(key))
                Debug.LogError($"Duplicate authored item id {key} on {item.name}.", item);
            while (_authored.ContainsKey(key)) key++; // legacy collision fallback

            _authored.Add(key, item);
            item.IsAuthored = true;
        }
    }

    /// <summary> Stable FNV-1a hash of the item id and millimeter position. </summary>
    private static int AuthoredKey(string id, Vector3 position)
    {
        unchecked
        {
            uint hash = 2166136261;
            foreach (char c in id ?? string.Empty) hash = (hash ^ c) * 16777619;
            hash = (hash ^ (uint)Mathf.RoundToInt(position.x * PositionScale)) * 16777619;
            hash = (hash ^ (uint)Mathf.RoundToInt(position.y * PositionScale)) * 16777619;
            hash = (hash ^ (uint)Mathf.RoundToInt(position.z * PositionScale)) * 16777619;
            return (int)hash;
        }
    }

    // ------------------------------------------------------------------ restore

    /// <summary> Restores this scene's save; discards another level's or format's save. </summary>
    private void Load()
    {
        if (database == null) return;

        string json = PlayerPrefs.GetString(saveKey, string.Empty);
        if (string.IsNullOrEmpty(json)) return;

        LevelSaveData data;
        try
        {
            data = JsonUtility.FromJson<LevelSaveData>(json);
        }
        catch (ArgumentException e)
        {
            if (TinyRiders.ThemedCollectionSession.Instance != null)
            { _preserveUnreadableSave = true; Debug.LogWarning("Themed save could not be read; original data retained. " + e.Message); return; }
            Debug.LogWarning($"{name}: corrupt level save discarded. {e.Message}", this);
            Clear();
            return;
        }

        // Only the current level and format are kept; anything else is stale.
        if (data == null || data.version != SaveVersion || data.scene != gameObject.scene.name)
        {
            if (TinyRiders.ThemedCollectionSession.Instance != null) { _preserveUnreadableSave = true; return; }
            Clear();
            return;
        }

        _storeCompleted = data.completed;
        _elapsedSeconds = Mathf.Max(0f, data.elapsedSeconds);
        if (TinyRiders.ThemedCollectionSession.Instance != null)
        {
            if (data.themed != null && data.themed.schema != 1) { _preserveUnreadableSave = true; return; }
            TinyRiders.ThemedCollectionSession.Instance.Restore(data.themed);
            return;
        }

        ItemDataSO[] items = ResolveIds(data.ids);
        RestoreWorld(data.removed, data.world, items);
        RestoreShelves(data.shelves, items);
        RestoreHeld(data, items);

        if (SortingProgressTracker.Instance != null)
            SortingProgressTracker.Instance.LoadFromSave(data.collected, data.total);
    }

    /// <summary> Maps the saved id table to item data; missing ids become null. </summary>
    private ItemDataSO[] ResolveIds(List<string> ids)
    {
        ItemDataSO[] items = new ItemDataSO[ids.Count];
        for (int i = 0; i < ids.Count; i++)
            if (!database.TryGet(ids[i], out items[i]))
                Debug.LogWarning($"{name}: saved item '{ids[i]}' not found in the database.", this);
        return items;
    }

    /// <summary> Removes saved-away authored items, then spawns the saved ones. </summary>
    private void RestoreWorld(List<int> removed, List<int> world, ItemDataSO[] items)
    {
        foreach (int key in removed)
        {
            if (!_authored.TryGetValue(key, out WorldItemBase authored) || authored == null) continue;

            authored.gameObject.SetActive(false); // hides it from scans before Destroy runs
            Destroy(authored.gameObject);
        }

        for (int i = 0; i + WorldStride <= world.Count; i += WorldStride)
        {
            ItemDataSO item = At(items, world[i]);
            if (item == null || item.WorldItem == null) continue;

            Vector3 position = new Vector3(world[i + 1], world[i + 2], world[i + 3]) / PositionScale;
            Vector3 euler = new Vector3(world[i + 4], world[i + 5], world[i + 6]) / RotationScale;
            Instantiate(item.WorldItem, position, Quaternion.Euler(euler), worldItemsContainer);
        }
    }

    /// <summary> Refills each shelf by its save id. </summary>
    private void RestoreShelves(List<ShelfSaveData> shelves, ItemDataSO[] items)
    {
        Dictionary<string, ShelfSaveData> byId = new Dictionary<string, ShelfSaveData>();
        foreach (ShelfSaveData save in shelves)
            if (save != null && !string.IsNullOrEmpty(save.id)) byId[save.id] = save;

        foreach (SortingShelf shelf in _shelves)
        {
            if (shelf == null || shelf.gameObject.scene != gameObject.scene || string.IsNullOrEmpty(shelf.SaveId)) continue;
            if (!byId.TryGetValue(shelf.SaveId, out ShelfSaveData save)) continue;

            for (int i = 0; i + 1 < save.items.Count; i += 2)
                shelf.RestoreItem(save.items[i], At(items, save.items[i + 1]) as SortableItem_SO);
        }
    }

    /// <summary> Loads the saved stash in place of the starting items. </summary>
    private void RestoreHeld(LevelSaveData data, ItemDataSO[] items)
    {
        StashHandler stash = Player.Instance != null ? Player.Instance.StashHandler : null;
        if (stash == null) return;

        List<ItemDataSO> held = new List<ItemDataSO>();
        foreach (int index in data.held)
        {
            ItemDataSO item = At(items, index);
            if (item != null) held.Add(item);
        }
        stash.LoadFromSave(held, data.selected);
    }

    private static ItemDataSO At(ItemDataSO[] items, int index) =>
        index >= 0 && index < items.Length ? items[index] : null;
}
