using UnityEngine;
namespace TinyRiders
{
    /// <summary>Culls the real themed meshes outside the view; never substitutes generic car art.</summary>
    public sealed class ThemedVisibility : MonoBehaviour
    {
        public float visibleDistance = 220f;
        private Camera view;
        private float nextUpdate;
        private readonly Plane[] frustumPlanes = new Plane[6];
        private static readonly RaycastHit[] OcclusionHits = new RaycastHit[16];
        private const int CarLayerMask = 1 << 8;
        private void LateUpdate()
        {
            if (Time.unscaledTime < nextUpdate) return;
            nextUpdate = Time.unscaledTime + .15f;
            if (view == null) view = Camera.main;
            if (view == null) return;
            GeometryUtility.CalculateFrustumPlanes(view, frustumPlanes);
            foreach (var group in ItemVisualGroup.All)
            {
                if (group == null || group.GetComponent<ThemedWorldInstance>() == null || group.GetComponent<HandItemBase>() != null) continue;
                Collider collider = group.GetComponent<Collider>();
                if (collider == null) { group.SetFar(false); continue; }
                Bounds bounds = collider.bounds;
                bool outside = !GeometryUtility.TestPlanesAABB(frustumPlanes, bounds);
                bool far = outside || (group.transform.position - view.transform.position).sqrMagnitude > visibleDistance * visibleDistance;
                if (!far) far = FullyOccluded(group, bounds);
                group.SetFar(far);
            }
        }

        private bool FullyOccluded(ItemVisualGroup group, Bounds bounds)
        {
            Vector3 right = view.transform.right;
            Vector3 up = view.transform.up;
            // Keep the whole car visible if even one sample has a clear view.
            for (int i = 0; i < 5; i++)
            {
                Vector3 offset = i == 1 ? right : i == 2 ? -right : i == 3 ? up : i == 4 ? -up : Vector3.zero;
                Vector3 axis = i < 3 ? right : up;
                Vector3 sample = ClampToBounds(bounds.center + offset * ProjectedExtent(bounds.extents, axis) * .65f, bounds);
                Vector3 delta = sample - view.transform.position;
                float distance = delta.magnitude;
                if (distance <= .01f) return false;
                int count = Physics.RaycastNonAlloc(view.transform.position, delta / distance, OcclusionHits,
                    distance - .01f, CarLayerMask, QueryTriggerInteraction.Ignore);
                if (count >= OcclusionHits.Length) return false;

                bool blockedByCar = false;
                for (int hitIndex = 0; hitIndex < count; hitIndex++)
                {
                    ItemVisualGroup blocker = OcclusionHits[hitIndex].collider.GetComponentInParent<ItemVisualGroup>();
                    if (blocker != null && blocker != group)
                    {
                        blockedByCar = true;
                        break;
                    }
                }
                if (!blockedByCar) return false;
            }
            return true;
        }

        private static float ProjectedExtent(Vector3 extents, Vector3 axis) =>
            Vector3.Dot(extents, new Vector3(Mathf.Abs(axis.x), Mathf.Abs(axis.y), Mathf.Abs(axis.z)));

        private static Vector3 ClampToBounds(Vector3 point, Bounds bounds) => new Vector3(
            Mathf.Clamp(point.x, bounds.min.x, bounds.max.x),
            Mathf.Clamp(point.y, bounds.min.y, bounds.max.y),
            Mathf.Clamp(point.z, bounds.min.z, bounds.max.z));

        private void OnDisable()
        {
            foreach (var group in ItemVisualGroup.All) if (group != null) group.SetFar(false);
        }
    }
}
