using System;
using System.Collections.Generic;

/// <summary> Compact level save: authored items as removals, others as poses. </summary>
[Serializable]
public class LevelSaveData
{
    public int version; // save format; other versions are discarded
    public string scene; // scene this save belongs to

    public List<string> ids = new List<string>(); // item save id table

    // Keys of scene-authored world items that were removed or moved.
    public List<int> removed = new List<int>();

    // 7 ints per spawned or moved world item: id index, position (mm), euler (0.1 deg).
    public List<int> world = new List<int>();

    public List<ShelfSaveData> shelves = new List<ShelfSaveData>();

    public List<int> held = new List<int>(); // id indices, stash order
    public int selected;

    public int collected; // progress tracker counts
    public int total;
    public bool completed;
    public float elapsedSeconds;
    public TinyRiders.ThemedSessionSave themed;
}

/// <summary> One shelf's placed items. </summary>
[Serializable]
public class ShelfSaveData
{
    public string id;

    // Pairs of (spot, id index), oldest placement first.
    public List<int> items = new List<int>();
}
