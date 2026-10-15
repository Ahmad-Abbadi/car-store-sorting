using UnityEngine;
namespace TinyRiders
{
    [DisallowMultipleComponent]
    public sealed class ThemeShelfBinding : MonoBehaviour
    {
        public string themeId;
        public Vector3 clearance;
        public bool Accepts(SortableItem_SO item, out string reason)
        {
            reason = null;
            var entry = ThemedCollectionSession.Instance?.Definition(item);
            if (entry == null || !entry.enabled || entry.prefab == null) reason = "Collectible unavailable";
            else if (entry.themeId != themeId) reason = "Match the car's theme to this shelf";
            else if (entry.displaySize.x > clearance.x || entry.displaySize.y > clearance.y || entry.displaySize.z > clearance.z)
                reason = "This accessory needs more display clearance";
            return reason == null;
        }
    }
}
