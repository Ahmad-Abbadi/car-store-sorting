using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary> Spawns prefab instances to fill the grid's outer rings, going inward. </summary>
[RequireComponent(typeof(AutoCircularGrid))]
public class AutoCircularFill : MonoBehaviour
{
    [Tooltip("When off, Fill does nothing.")]
    public bool Enabled = true;
    [SerializeField] private GameObject prefab;
    [Tooltip("Outer rings to fill, going inward. The center counts as the last ring.")]
    [Min(0)] [SerializeField] private int ringCount = 1;
    [SerializeField, HideInInspector] private List<GameObject> spawned = new List<GameObject>();

#if UNITY_EDITOR
    /// <summary> Replaces previously spawned instances and fills the outer rings. </summary>
    [ContextMenu("Fill")]
    public void Fill()
    {
        if (!Enabled) return;
        AutoCircularGrid grid = GetComponent<AutoCircularGrid>();
        if (prefab == null)
        {
            Debug.LogWarning($"{name}: assign a prefab before filling.", this);
            return;
        }
        if (!grid.IsFixedMode)
        {
            Debug.LogWarning($"{name}: set AutoCircularGrid to Fixed mode before filling.", this);
            return;
        }

        Undo.SetCurrentGroupName("Fill Circular Grid");
        int undoGroup = Undo.GetCurrentGroup();

        Clear();
        int count = grid.GetOuterRingsCapacity(ringCount);
        for (int i = 0; i < count; i++)
        {
            GameObject instance = PrefabUtility.InstantiatePrefab(prefab, transform) as GameObject;
            if (instance == null) instance = Instantiate(prefab, transform);

            // Spawned first in hierarchy so they take the outer ring slots.
            instance.transform.SetSiblingIndex(i);
            Undo.RegisterCreatedObjectUndo(instance, "Fill Circular Grid");
            spawned.Add(instance);
        }

        grid.ApplyCircularGrid();
        Undo.CollapseUndoOperations(undoGroup);
    }

    /// <summary> Deletes the instances this component spawned. </summary>
    [ContextMenu("Clear")]
    public void Clear()
    {
        Undo.RecordObject(this, "Clear Circular Fill");
        foreach (GameObject instance in spawned)
        {
            if (instance != null) Undo.DestroyObjectImmediate(instance);
        }
        spawned.Clear();
    }

    /// <summary> Adds Fill and Clear buttons to the Inspector. </summary>
    [CustomEditor(typeof(AutoCircularFill))]
    private class AutoCircularFillEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            AutoCircularFill fill = (AutoCircularFill)target;
            int spawnedCount = fill.spawned.FindAll(instance => instance != null).Count;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Spawned", spawnedCount.ToString());
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Fill")) fill.Fill();
            if (GUILayout.Button("Clear")) fill.Clear();
            EditorGUILayout.EndHorizontal();
        }
    }
#endif
}
