using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary> Places objects in rings from outer to inner, then stacks layers upward. </summary>
public class AutoCircularGrid : MonoBehaviour
{
    /// <summary> Normal fits one layer; Fixed uses outerRadius and stacks extras. </summary>
    public enum CircleMode { Normal, Fixed }

    /// <summary> None keeps rotation; Outward/Inward face away from or toward center. </summary>
    public enum FacingMode { None, Outward, Inward }

    private const float MinDistance = 0.01f;
    private const float Epsilon = 0.0001f;
    private const int MaxSearchRings = 1000;

    [Tooltip("When off, the circular grid is not applied.")]
    public bool Enabled = true;
    [SerializeField] private CircleMode mode = CircleMode.Normal;
    [Tooltip("Fixed mode only. Radius of the outer ring. Extras stack upward in layers.")]
    [SerializeField] private float outerRadius = 3f;
    [Tooltip("Distance between objects along each ring.")]
    [SerializeField] private float spacing = 1f;
    [Tooltip("Distance between a ring and the next inner ring.")]
    [SerializeField] private float ringSpacing = 1f;
    [Tooltip("Height between stacked layers.")]
    [SerializeField] private float layerHeight = 1f;
    [Tooltip("None leaves rotation untouched. Center object faces this transform's forward.")]
    [SerializeField] private FacingMode facing = FacingMode.None;
    [Tooltip("Euler offset in degrees, applied on top of the facing direction.")]
    [SerializeField] private Vector3 rotationOffset = Vector3.zero;
    [Tooltip("Leave empty to use direct children.")]
    [SerializeField] private List<Transform> objects = new List<Transform>();

    /// <summary> Arranges all targets into rings, outer to inner, bottom layer first. </summary>
    [ContextMenu("Apply Circular Grid")]
    public void ApplyCircularGrid()
    {
        if (!Enabled) return;
        List<Transform> targets = GetTargets();
        int count = targets.Count;
        if (count == 0) return;

        List<Vector3> slots = BuildLayerSlots(GetOuterRadius(count));
        int perLayer = slots.Count;

        for (int i = 0; i < count; i++)
        {
            int layer = i / perLayer;
            Vector3 slot = slots[i % perLayer];
            Vector3 offset = slot + Vector3.up * (layer * layerHeight);
            SetTransform(targets[i], transform.position + transform.rotation * offset, GetRotation(slot));
        }
    }

    /// <summary> Returns the facing rotation for a slot, or null when facing is None. </summary>
    private Quaternion? GetRotation(Vector3 slot)
    {
        if (facing == FacingMode.None) return null;

        Vector3 direction = slot.sqrMagnitude > Epsilon ? slot.normalized : Vector3.forward;
        if (facing == FacingMode.Inward && slot.sqrMagnitude > Epsilon) direction = -direction;

        return transform.rotation * Quaternion.LookRotation(direction, Vector3.up) * Quaternion.Euler(rotationOffset);
    }

    /// <summary> True when the grid uses the Fixed outer radius. </summary>
    public bool IsFixedMode => mode == CircleMode.Fixed;

    /// <summary> Returns object count for the outer N rings; center counts as last ring. </summary>
    public int GetOuterRingsCapacity(int rings)
    {
        List<int> ringSizes = GetRingSizes(Mathf.Max(0f, outerRadius));
        int capacity = 0;
        for (int i = 0; i < ringSizes.Count && i < rings; i++) capacity += ringSizes[i];
        if (rings > ringSizes.Count) capacity++;
        return capacity;
    }

    /// <summary> Returns one layer's local offsets: outer ring first, center last. </summary>
    private List<Vector3> BuildLayerSlots(float radius)
    {
        List<Vector3> slots = new List<Vector3>();
        float step = Mathf.Max(MinDistance, ringSpacing);
        float r = radius;

        foreach (int ringCount in GetRingSizes(radius))
        {
            float angleStep = 2f * Mathf.PI / ringCount;
            for (int j = 0; j < ringCount; j++)
            {
                float angle = j * angleStep;
                slots.Add(new Vector3(Mathf.Sin(angle) * r, 0f, Mathf.Cos(angle) * r));
            }
            r -= step;
        }

        slots.Add(Vector3.zero);
        return slots;
    }

    /// <summary> Returns object counts per ring, outer to inner, excluding center. </summary>
    private List<int> GetRingSizes(float radius)
    {
        List<int> sizes = new List<int>();
        float step = Mathf.Max(MinDistance, ringSpacing);

        for (float r = radius; r + Epsilon >= step; r -= step)
        {
            int ringCount = GetRingCount(r);
            if (ringCount < 2) break;
            sizes.Add(ringCount);
        }
        return sizes;
    }

    /// <summary> Returns how many objects fit on a ring of this radius. </summary>
    private int GetRingCount(float radius)
    {
        float circumference = 2f * Mathf.PI * radius;
        return Mathf.FloorToInt(circumference / Mathf.Max(MinDistance, spacing) + Epsilon);
    }

    /// <summary> Returns the outer radius for the current mode. </summary>
    private float GetOuterRadius(int count)
    {
        if (mode == CircleMode.Fixed) return Mathf.Max(0f, outerRadius);

        float step = Mathf.Max(MinDistance, ringSpacing);
        for (int rings = 0; rings < MaxSearchRings; rings++)
        {
            float radius = rings * step;
            if (BuildLayerSlots(radius).Count >= count) return radius;
        }
        return MaxSearchRings * step;
    }

    /// <summary> Moves and optionally rotates a target with Undo, skipping unchanged values. </summary>
    private void SetTransform(Transform target, Vector3 position, Quaternion? rotation)
    {
        bool samePosition = target.position == position;
        bool sameRotation = !rotation.HasValue || target.rotation == rotation.Value;
        if (samePosition && sameRotation) return;
#if UNITY_EDITOR
        Undo.RecordObject(target, "Apply Circular Grid");
#endif
        target.position = position;
        if (rotation.HasValue) target.rotation = rotation.Value;
    }

    /// <summary> Returns assigned objects, or direct children when the list is empty. </summary>
    private List<Transform> GetTargets()
    {
        List<Transform> targets = new List<Transform>();
        foreach (Transform target in objects)
        {
            if (target != null && target != transform) targets.Add(target);
        }
        if (targets.Count > 0) return targets;

        foreach (Transform child in transform) targets.Add(child);
        return targets;
    }

#if UNITY_EDITOR
    /// <summary> Reapplies the circular grid live when Inspector values change. </summary>
    private void OnValidate()
    {
        EditorApplication.delayCall += () =>
        {
            if (this == null || !gameObject.scene.IsValid()) return;
            ApplyCircularGrid();
        };
    }

    /// <summary> Shows outerRadius and rotationOffset only when relevant. </summary>
    [CustomEditor(typeof(AutoCircularGrid))]
    private class AutoCircularGridEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            bool isFixed = serializedObject.FindProperty(nameof(mode)).enumValueIndex == (int)CircleMode.Fixed;
            bool isFacing = serializedObject.FindProperty(nameof(facing)).enumValueIndex != (int)FacingMode.None;

            List<string> excluded = new List<string>();
            if (!isFixed) excluded.Add(nameof(outerRadius));
            if (!isFacing) excluded.Add(nameof(rotationOffset));
            DrawPropertiesExcluding(serializedObject, excluded.ToArray());

            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}
