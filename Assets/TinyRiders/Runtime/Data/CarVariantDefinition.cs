using UnityEngine;

namespace TinyRiders
{
    [CreateAssetMenu(menuName = "Tiny Riders/Variant Definition")]
    public sealed class CarVariantDefinition : ScriptableObject
    {
        public string variantId;
        public string baseCarId;
        public string buildCode;
        public string liveryCode;
        public string displayName;
        public string baseCarDisplayName;
        public string buildDisplayName;
        public string colorwayName;
        public string collectionId;
        public string setId;
        public string[] colorTags;
        public string releaseId;
        public int contentRevision;
        public int displayOrder;
        public GameObject prefab;
        public Sprite thumbnail;
    }
}
