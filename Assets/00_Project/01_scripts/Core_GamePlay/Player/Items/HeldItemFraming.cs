using System.Collections.Generic;
using UnityEngine;

/// <summary>Socket-local presentation pose; scale belongs only to the spawned hand model.</summary>
public readonly struct HeldItemPose
{
    public readonly Vector3 Position;
    public readonly Quaternion Rotation;
    public readonly Vector3 Scale;
    public HeldItemPose(Vector3 position, Quaternion rotation, Vector3 scale)
    { Position = position; Rotation = rotation; Scale = scale; }
}

/// <summary>Caches model bounds once, then fits their corners into a camera viewport rectangle.</summary>
internal sealed class HeldItemFraming
{
    // Leave the name and inventory count unobstructed below the model.
    internal static readonly Rect Viewport = Rect.MinMaxRect(.58f, .16f, .98f, .60f);
    // Camera-relative angle chosen in Play mode: front/side view with a slight tilt.
    internal static readonly Vector3 DefaultRotationOffset = new Vector3(-3.659882f, 88.439224f, 5.780028f);
    internal readonly Vector3 OriginalScale;
    internal readonly bool IsThemedCar;
    readonly Vector3[] corners;

    internal HeldItemFraming(Transform root)
    {
        OriginalScale = root.localScale;
        IsThemedCar = root.GetComponentInChildren<TinyRiders.ThemedCarIdentity>(true) != null;
        var points = new List<Vector3>();
        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            if (!renderer.enabled) continue;
            var bounds = renderer.localBounds;
            var matrix = root.worldToLocalMatrix * renderer.localToWorldMatrix;
            for (int i = 0; i < 8; i++)
            {
                var sign = new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1);
                points.Add(matrix.MultiplyPoint3x4(bounds.center + Vector3.Scale(bounds.extents, sign)));
            }
        }
        corners = points.ToArray();
    }

    internal bool TryResolve(Camera camera, Transform socket, Vector3 authoredRotation, out HeldItemPose pose)
        => TryResolve(camera, socket, authoredRotation, Quaternion.identity, out pose);

    internal bool TryResolve(Camera camera, Transform socket, Vector3 authoredRotation, Quaternion inspectionRotation, out HeldItemPose pose)
    {
        pose = default;
        if (camera == null || camera.orthographic || socket == null || corners.Length == 0) return false;
        var rotation = inspectionRotation * Quaternion.Euler(authoredRotation + DefaultRotationOffset);
        var scale = Vector3.Scale(OriginalScale, socket.lossyScale);
        var bounds = new Bounds(rotation * Vector3.Scale(corners[0], scale), Vector3.zero);
        foreach (var point in corners) bounds.Encapsulate(rotation * Vector3.Scale(point, scale));
        if (bounds.size.sqrMagnitude < .00000001f) return false;

        Vector3 lower = camera.transform.InverseTransformPoint(camera.ViewportToWorldPoint(new Vector3(Viewport.xMin, Viewport.yMin, 1f)));
        Vector3 upper = camera.transform.InverseTransformPoint(camera.ViewportToWorldPoint(new Vector3(Viewport.xMax, Viewport.yMax, 1f)));
        float left = lower.x, right = upper.x, bottom = lower.y, top = upper.y;
        float cx = (left + right) * .5f, cy = (bottom + top) * .5f;
        float depth = Mathf.Max(.42f, camera.nearClipPlane + .3f);
        float factor = float.PositiveInfinity;
        foreach (var point in corners)
        {
            Vector3 p = rotation * Vector3.Scale(point, scale) - bounds.center;
            Limit(ref factor, (cx - left) * depth, left * p.z - p.x);
            Limit(ref factor, (right - cx) * depth, p.x - right * p.z);
            Limit(ref factor, (cy - bottom) * depth, bottom * p.z - p.y);
            Limit(ref factor, (top - cy) * depth, p.y - top * p.z);
            Limit(ref factor, depth - camera.nearClipPlane - .03f, -p.z);
        }
        if (float.IsInfinity(factor) || float.IsNaN(factor) || factor <= 0f) return false;
        factor *= .98f;
        Vector3 cameraPosition = new Vector3(cx * depth, cy * depth, depth) - bounds.center * factor;
        pose = new HeldItemPose(socket.InverseTransformPoint(camera.transform.TransformPoint(cameraPosition)),
            Quaternion.Inverse(socket.rotation) * camera.transform.rotation * rotation, OriginalScale * factor);
        return true;
    }

    static void Limit(ref float factor, float available, float extent)
    { if (extent > 0f) factor = Mathf.Min(factor, available / extent); }
}
