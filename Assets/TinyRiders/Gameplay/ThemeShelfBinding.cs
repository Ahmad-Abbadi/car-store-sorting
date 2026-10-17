using UnityEngine;
namespace TinyRiders
{
    [DisallowMultipleComponent]
    public sealed class ThemeShelfBinding : MonoBehaviour
    {
        public string themeId;
        public Vector3 clearance;

        /// <summary>Fit the complete car into this slot, centered and resting on its deck.</summary>
        public bool TryFitDisplay(Transform display, out Vector3 position, out Vector3 scale)
        {
            position = Vector3.zero;
            scale = display.localScale;
            if (display.parent == null || clearance.x <= 0f || clearance.y <= 0f || clearance.z <= 0f) return false;
            var parentScale = display.parent.lossyScale;
            if (parentScale.x <= 0f || parentScale.y <= 0f || parentScale.z <= 0f) return false;
            var available = new Vector3(clearance.x / parentScale.x, clearance.y / parentScale.y, clearance.z / parentScale.z);
            bool found = false;
            Bounds bounds = default;
            foreach (var renderer in display.GetComponentsInChildren<Renderer>(true))
            {
                if (!renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
                var matrix = Matrix4x4.Scale(scale) * display.worldToLocalMatrix * renderer.localToWorldMatrix;
                var local = renderer.localBounds;
                for (int i = 0; i < 8; i++)
                {
                    var sign = new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1);
                    var point = matrix.MultiplyPoint3x4(local.center + Vector3.Scale(local.extents, sign));
                    if (!found) { bounds = new Bounds(point, Vector3.zero); found = true; }
                    else bounds.Encapsulate(point);
                }
            }
            if (!found || bounds.size.x < .00001f || bounds.size.y < .00001f || bounds.size.z < .00001f) return false;
            float floorGap = .005f / parentScale.y;
            float factor = Mathf.Min(available.x * .96f / bounds.size.x,
                (available.y * .96f - floorGap) / bounds.size.y, available.z * .96f / bounds.size.z);
            if (factor <= 0f || float.IsNaN(factor) || float.IsInfinity(factor)) return false;
            scale *= factor;
            position = new Vector3(-bounds.center.x * factor, floorGap - bounds.min.y * factor, -bounds.center.z * factor);
            return true;
        }
        public bool Accepts(SortableItem_SO item, out string reason)
        {
            reason = null;
            var entry = ThemedCollectionSession.Instance?.Definition(item);
            if (entry == null || !entry.enabled || entry.prefab == null) reason = "Collectible unavailable";
            else if (entry.displaySize.x > clearance.x || entry.displaySize.y > clearance.y || entry.displaySize.z > clearance.z)
                reason = "This accessory needs more display clearance";
            return reason == null;
        }
    }
}
