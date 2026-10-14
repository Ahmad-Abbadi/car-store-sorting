using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary> Stacks objects into a 3D square pyramid anchored at a chosen pivot. </summary>
public class AutoPyramid : MonoBehaviour
{
    /// <summary> Normal fits the smallest base; Fixed uses a set base size. </summary>
    public enum PyramidMode { Normal, Fixed }

    /// <summary> Base point placed on this transform; Custom uses customPivot. </summary>
    public enum PyramidPivot { Center, FrontLeft, Front, FrontRight, Left, Right, BackLeft, Back, BackRight, Custom }

    [Tooltip("When off, the pyramid is not applied.")]
    public bool Enabled = true;
    [SerializeField] private PyramidMode mode = PyramidMode.Normal;
    [SerializeField] private PyramidPivot pivot = PyramidPivot.Center;
    [Tooltip("Normalized pivot on the base. (0,0) = front-left, (1,1) = back-right.")]
    [SerializeField] private Vector2 customPivot = new Vector2(0.5f, 0.5f);
    [Tooltip("Fixed mode only. Base layer is baseSize x baseSize. Extras are left in place.")]
    [SerializeField] private int baseSize = 3;
    [Tooltip("Cell size in world units (X = width, Y = depth along Z).")]
    [SerializeField] private Vector2 cellSize = Vector2.one;
    [Tooltip("Height between stacked layers.")]
    [SerializeField] private float layerHeight = 1f;
    [Tooltip("Leave empty to use direct children.")]
    [SerializeField] private List<Transform> objects = new List<Transform>();

    /// <summary> Arranges all targets into the pyramid, bottom layer first. </summary>
    [ContextMenu("Apply Pyramid")]
    public void ApplyPyramid()
    {
        if (!Enabled) return;
        List<Transform> targets = GetTargets();
        int count = targets.Count;
        if (count == 0) return;

        int size = GetBaseSize(count);
        Vector2 pivotPoint = GetPivotPoint();
        float width = (size - 1) * cellSize.x;
        float depth = (size - 1) * cellSize.y;
        float baseStartX = -pivotPoint.x * width;
        float baseStartZ = depth - pivotPoint.y * depth;

        int index = 0;
        for (int layer = 0; layer < size && index < count; layer++)
        {
            int layerSize = size - layer;
            float startX = baseStartX + layer * 0.5f * cellSize.x;
            float startZ = baseStartZ - layer * 0.5f * cellSize.y;

            for (int i = 0; i < layerSize * layerSize && index < count; i++, index++)
            {
                int column = i % layerSize;
                int row = i / layerSize;
                Vector3 offset = new Vector3(startX + column * cellSize.x, layer * layerHeight, startZ - row * cellSize.y);
                SetPosition(targets[index], transform.position + transform.rotation * offset);
            }
        }
    }

    /// <summary> Moves a target with Undo support, skipping unchanged positions. </summary>
    private void SetPosition(Transform target, Vector3 position)
    {
        if (target.position == position) return;
#if UNITY_EDITOR
        Undo.RecordObject(target, "Apply Pyramid");
#endif
        target.position = position;
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

    /// <summary> Returns the base size for the current mode. </summary>
    private int GetBaseSize(int count)
    {
        if (mode == PyramidMode.Fixed) return Mathf.Max(1, baseSize);

        int size = 0;
        int capacity = 0;
        while (capacity < count)
        {
            size++;
            capacity += size * size;
        }
        return size;
    }

    /// <summary> Returns the normalized pivot for the selected preset. </summary>
    private Vector2 GetPivotPoint()
    {
        switch (pivot)
        {
            case PyramidPivot.FrontLeft: return new Vector2(0f, 0f);
            case PyramidPivot.Front: return new Vector2(0.5f, 0f);
            case PyramidPivot.FrontRight: return new Vector2(1f, 0f);
            case PyramidPivot.Left: return new Vector2(0f, 0.5f);
            case PyramidPivot.Right: return new Vector2(1f, 0.5f);
            case PyramidPivot.BackLeft: return new Vector2(0f, 1f);
            case PyramidPivot.Back: return new Vector2(0.5f, 1f);
            case PyramidPivot.BackRight: return new Vector2(1f, 1f);
            case PyramidPivot.Custom: return customPivot;
            default: return new Vector2(0.5f, 0.5f);
        }
    }

#if UNITY_EDITOR
    /// <summary> Reapplies the pyramid live when Inspector values change. </summary>
    private void OnValidate()
    {
        EditorApplication.delayCall += () =>
        {
            if (this == null || !gameObject.scene.IsValid()) return;
            ApplyPyramid();
        };
    }

    /// <summary> Shows customPivot and baseSize only when relevant. </summary>
    [CustomEditor(typeof(AutoPyramid))]
    private class AutoPyramidEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            bool isCustom = serializedObject.FindProperty(nameof(pivot)).enumValueIndex == (int)PyramidPivot.Custom;
            bool isFixed = serializedObject.FindProperty(nameof(mode)).enumValueIndex == (int)PyramidMode.Fixed;

            List<string> excluded = new List<string>();
            if (!isCustom) excluded.Add(nameof(customPivot));
            if (!isFixed) excluded.Add(nameof(baseSize));
            DrawPropertiesExcluding(serializedObject, excluded.ToArray());

            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}
