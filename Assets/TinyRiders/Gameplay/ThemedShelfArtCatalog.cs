using System;
using System.Linq;
using UnityEngine;

namespace TinyRiders
{
    [CreateAssetMenu(menuName = "Tiny Riders/Themed Shelf Art Catalog")]
    public sealed class ThemedShelfArtCatalog : ScriptableObject
    {
        [Serializable] public sealed class Entry
        {
            public string themeId;
            public GameObject prefab;
            public Material[] materials;
            public int decorationTriangles;
            public int signatureRevision;
            public int topperTriangles;
            public Vector3 topperSizeMeters;
            public float postHeightMeters;
        }
        public Entry[] entries = Array.Empty<Entry>();
        public Entry Find(string themeId) => entries.FirstOrDefault(e => e.themeId == themeId);
    }
}
