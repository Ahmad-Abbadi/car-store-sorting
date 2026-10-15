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
            return Mathf.Abs(p.x) <= roomSize.x * .5f - margin &&
                   Mathf.Abs(p.z) <= roomSize.y * .5f - margin;
        }
    }
}
