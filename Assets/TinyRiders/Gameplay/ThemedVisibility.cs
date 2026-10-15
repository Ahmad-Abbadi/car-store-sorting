using UnityEngine;
namespace TinyRiders
{
    /// <summary>Culls the real themed meshes outside the view; never substitutes generic car art.</summary>
    public sealed class ThemedVisibility : MonoBehaviour
    {
        public float visibleDistance = 220f;
        private Camera view;
        private float nextUpdate;
        private void LateUpdate()
        {
            if (Time.unscaledTime < nextUpdate) return;
            nextUpdate = Time.unscaledTime + .15f;
            if (view == null) view = Camera.main;
            if (view == null) return;
            foreach (var group in ItemVisualGroup.All)
            {
                if (group == null || group.GetComponent<ThemedWorldInstance>() == null || group.GetComponent<HandItemBase>() != null) continue;
                Vector3 point = view.WorldToViewportPoint(group.transform.position + Vector3.up);
                bool outside = point.z < -6 || point.x < -.2f || point.x > 1.2f || point.y < -.3f || point.y > 1.3f;
                group.SetFar(outside || (group.transform.position - view.transform.position).sqrMagnitude > visibleDistance * visibleDistance);
            }
        }
        private void OnDisable()
        {
            foreach (var group in ItemVisualGroup.All) if (group != null) group.SetFar(false);
        }
    }
}
