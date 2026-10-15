using System;
using UnityEngine;
namespace TinyRiders
{
    [CreateAssetMenu(menuName = "Tiny Riders/Themed Car Catalog")]
    public sealed class ThemedCarCatalog : ScriptableObject
    {
        [Serializable] public class Entry
        {
            public string collectibleId, baseCarId, themeId, colorwayId, displayName;
            public int contentRevision = 1;
            public GameObject prefab;
            public Texture2D preview;
            [Header("Sorting integration (source artwork remains unchanged)")]
            public SortableItem_SO sortingItem;
            public Vector3 displaySize;
            public bool enabled = true;
            public string releaseId;
            public string unavailableReason;
        }
        public int schemaVersion = 1;
        public Entry[] entries;
        [Serializable] public class Theme
        {
            public string id, displayName, motif, shelfGroup;
            public Color color = Color.white;
            public bool enabled = true;
        }
        public Theme[] themes = new Theme[0];
    }
}
