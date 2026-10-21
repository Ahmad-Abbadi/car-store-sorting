using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Build-time, single-material distant versions of the authored cars.</summary>
public sealed class CarSpatialCatalog : ScriptableObject
{
    [Serializable]
    public struct Entry
    {
        public ItemDataSO item;
        public Mesh mesh;
    }

    public Material material;
    public Entry[] entries;

    private Dictionary<ItemDataSO, Mesh> lookup;

    public bool TryGet(ItemDataSO item, out Mesh mesh)
    {
        if (lookup == null)
        {
            lookup = new Dictionary<ItemDataSO, Mesh>();
            if (entries != null)
                foreach (var entry in entries)
                    if (entry.item != null && entry.mesh != null) lookup[entry.item] = entry.mesh;
        }
        return lookup.TryGetValue(item, out mesh);
    }
}
