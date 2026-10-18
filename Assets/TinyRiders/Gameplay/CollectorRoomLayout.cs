using System;
using UnityEngine;

namespace TinyRiders
{
    [CreateAssetMenu(menuName = "Tiny Riders/Collector Room Layout")]
    public sealed class CollectorRoomLayout : ScriptableObject
    {
        public int revision = 2;
        public Vector2 roomSize = new Vector2(36, 44);
        public Vector3 origin = new Vector3(500, 0, 0);
        public Vector3 spawn = new Vector3(500, .05f, -20.5f);
        public float spawnPitch;
        public float centralAisle = 4;
        public float minimumAisle = 1.8f;
        public float ceilingHeight = 4.6f;
        public bool walkThroughLooseCars;
        [Tooltip("Optional local floor rectangles for shops with wings or alcoves. Empty uses roomSize.")]
        public Bounds[] floorRegions = Array.Empty<Bounds>();
        public Bounds[] pileRegions = Array.Empty<Bounds>();
        public Bounds[] pileExclusions = Array.Empty<Bounds>();
        public Section[] sections = Array.Empty<Section>();
        public ShelfPose[] shelves = Array.Empty<ShelfPose>();

        [Serializable] public sealed class Section
        {
            public string title;
            public string[] themes;
            public Color color;
            public Vector3 center;
            public Bounds[] sortingAreas;
        }

        [Serializable] public sealed class ShelfPose
        {
            public string saveId;
            public int section;
            public Vector3 position;
            public float yaw;
        }

        public bool Contains(Vector3 world, float margin = 0)
        {
            var p = world - origin;
            if (floorRegions != null && floorRegions.Length > 0)
            {
                foreach (var region in floorRegions)
                    if (p.x >= region.min.x + margin && p.x <= region.max.x - margin &&
                        p.z >= region.min.z + margin && p.z <= region.max.z - margin) return true;
                return false;
            }
            return Mathf.Abs(p.x) <= roomSize.x * .5f - margin &&
                   Mathf.Abs(p.z) <= roomSize.y * .5f - margin;
        }
    }
}
