using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary> Baked lower-detail triangle lists per item mesh; made by Tools > Items > Bake Item LODs. </summary>
[CreateAssetMenu(menuName = "Sorting Template/Item LOD Set", fileName = "ItemLodSet")]
public class ItemLodSet : ScriptableObject
{
    /// <summary> One mesh's LOD triangles, indexing the mesh's own vertices. </summary>
    [Serializable]
    public class Entry
    {
        public Mesh mesh;
        public int[] lod1; // built-in simplifier; empty means reuse LOD0
        public int[] lod2; // big parts only; empty means reuse LOD1
    }

    [SerializeField] private List<Entry> entries = new List<Entry>();

    private Dictionary<Mesh, Entry> _lookup;

    /// <summary> Replaces all entries; used by the editor baker. </summary>
    public void SetEntries(List<Entry> newEntries)
    {
        entries = newEntries;
        _lookup = null;
    }

    /// <summary> Number of baked meshes. </summary>
    public int Count => entries.Count;

    /// <summary> Finds the baked LODs for a mesh. </summary>
    public bool TryGet(Mesh mesh, out Entry entry)
    {
        if (_lookup == null)
        {
            _lookup = new Dictionary<Mesh, Entry>();
            foreach (Entry e in entries)
                if (e != null && e.mesh != null) _lookup[e.mesh] = e;
        }
        return _lookup.TryGetValue(mesh, out entry);
    }
}
