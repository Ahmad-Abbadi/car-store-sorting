using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary> Tiles objects on a flat X/Z grid anchored at a chosen pivot. </summary>
public class AutoGridding : MonoBehaviour
{
    /// <summary> Normal fits a near-square grid; Fixed uses set columns and rows. </summary>
    public enum GridMode { Normal, Fixed }

    /// <summary> Grid point placed on this transform; Custom uses customPivot. </summary>
    public enum GridPivot { Center, FrontLeft, Front, FrontRight, Left, Right, BackLeft, Back, BackRight, Custom }

    [Tooltip("When off, the grid is not applied.")]
    public bool Enabled = true;
    [SerializeField] private GridMode mode = GridMode.Normal;
    [SerializeField] private GridPivot pivot = GridPivot.Center;
    [Tooltip("Normalized pivot. (0,0) = front-left, (1,1) = back-right.")]
    [SerializeField] private Vector2 customPivot = new Vector2(0.5f, 0.5f);
    [Tooltip("Fixed mode only. X = columns, Y = rows. Extras stack upward in layers.")]
    [SerializeField] private Vector2Int fixedSize = new Vector2Int(3, 3);
    [Tooltip("Cell size in world units (X = width, Y = depth along Z).")]
    [SerializeField] private Vector2 cellSize = Vector2.one;
    [Tooltip("Fixed mode only. Height between stacked layers.")]
    [SerializeField] private float layerHeight = 1f;
    [Tooltip("Leave empty to use direct children.")]
    [SerializeField] private List<Transform> objects = new List<Transform>();

    /// <summary> Arranges all targets into the grid. </summary>
    [ContextMenu("Apply Grid")]
    public void ApplyGrid()
    {
        if (!Enabled) return;
        List<Transform> targets = GetTargets();
        int count = targets.Count;
        if (count == 0) return;

        GetDimensions(count, out int columns, out int rows);

        Vector2 pivotPoint = GetPivotPoint();
        float width = (columns - 1) * cellSize.x;
        float depth = (rows - 1) * cellSize.y;
        float startX = -pivotPoint.x * width;
        float startZ = depth - pivotPoint.y * depth;
        int perLayer = columns * rows;

        for (int i = 0; i < count; i++)
        {
            int layer = i / perLayer;
            int indexInLayer = i % perLayer;
            int column = indexInLayer % columns;
            int row = indexInLayer / columns;
            Vector3 offset = new Vector3(startX + column * cellSize.x, layer * layerHeight, startZ - row * cellSize.y);
            Vector3 position = transform.position + transform.rotation * offset;

            if (targets[i].position == position) continue;
#if UNITY_EDITOR
            Undo.RecordObject(targets[i], "Apply Grid");
#endif
            targets[i].position = position;
        }
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

    /// <summary> Computes column and row counts for the current mode. </summary>
    private void GetDimensions(int count, out int columns, out int rows)
    {
        if (mode == GridMode.Fixed)
        {
            columns = Mathf.Max(1, fixedSize.x);
            rows = Mathf.Max(1, fixedSize.y);
            return;
        }

        columns = Mathf.CeilToInt(Mathf.Sqrt(count));
        rows = Mathf.CeilToInt(count / (float)columns);
    }

    /// <summary> Returns the normalized pivot for the selected preset. </summary>
    private Vector2 GetPivotPoint()
    {
        switch (pivot)
        {
            case GridPivot.FrontLeft: return new Vector2(0f, 0f);
            case GridPivot.Front: return new Vector2(0.5f, 0f);
            case GridPivot.FrontRight: return new Vector2(1f, 0f);
            case GridPivot.Left: return new Vector2(0f, 0.5f);
            case GridPivot.Right: return new Vector2(1f, 0.5f);
            case GridPivot.BackLeft: return new Vector2(0f, 1f);
            case GridPivot.Back: return new Vector2(0.5f, 1f);
            case GridPivot.BackRight: return new Vector2(1f, 1f);
            case GridPivot.Custom: return customPivot;
            default: return new Vector2(0.5f, 0.5f);
        }
    }

#if UNITY_EDITOR
    /// <summary> Reapplies the grid live when Inspector values change. </summary>
    private void OnValidate()
    {
        EditorApplication.delayCall += () =>
        {
            if (this == null || !gameObject.scene.IsValid()) return;
            ApplyGrid();
        };
    }

    /// <summary> Shows customPivot only when the pivot is set to Custom. </summary>
    [CustomEditor(typeof(AutoGridding))]
    private class AutoGriddingEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            SerializedProperty pivotProperty = serializedObject.FindProperty(nameof(pivot));
            bool isCustom = pivotProperty.enumValueIndex == (int)GridPivot.Custom;

            if (isCustom) DrawDefaultInspector();
            else DrawPropertiesExcluding(serializedObject, nameof(customPivot));

            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}
