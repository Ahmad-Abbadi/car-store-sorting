using UnityEngine;
using UnityEditor;
using UnityEditor.ShortcutManagement;

[InitializeOnLoad]
public static class CustomVertexSnapper
    {
    public static bool IsActive { get; private set; }
    private const float SnapThresholdPixels = 20f;

    private static VertexOctree currentOctree;
    private static GameObject currentSelectedObject;

    static CustomVertexSnapper()
        {
        SceneView.duringSceneGui += OnSceneGUI;
        Selection.selectionChanged += OnSelectionChanged;
        }

    [Shortcut("Custom Tools/Toggle Custom Vertex Snapper", KeyCode.V, ShortcutModifiers.Alt)]
    private static void ToggleSnapper()
        {
        SetToolState(!IsActive);
        }

    private static void SetToolState(bool state)
        {
        IsActive = state;

        if (IsActive)
            {
            Tools.hidden = true;
            BuildOctreeForSelection();
            Debug.Log("Custom Vertex Snapper: ACTIVATED.");
            }
        else
            {
            Tools.hidden = false;
            CustomAnchorHandler.ClearAnchor();
            Debug.Log("Custom Vertex Snapper: DEACTIVATED.");
            }

        SceneView.RepaintAll();
        }

    private static void OnSelectionChanged()
        {
        GameObject newSelection = Selection.activeGameObject;

        if (IsActive && newSelection != currentSelectedObject)
            {
            SetToolState(false);
            }

        currentSelectedObject = newSelection;
        }

    private static void BuildOctreeForSelection()
        {
        currentOctree = null;
        if (currentSelectedObject == null) return;

        MeshFilter[] meshFilters = currentSelectedObject.GetComponentsInChildren<MeshFilter>();

        if (meshFilters != null && meshFilters.Length > 0)
            {
            currentOctree = new VertexOctree(meshFilters);
            }
        }

    private static void OnSceneGUI(SceneView sceneView)
        {
        if (!IsActive) return;

        Event e = Event.current;

        if (CustomAnchorHandler.HasAnchor)
            {
            CustomAnchorHandler.DoSceneGUI();
            return;
            }

        Handles.BeginGUI();
        GUI.Label(new Rect(10, 10, 300, 20), "Select a vertex to place the custom anchor");
        Handles.EndGUI();

        if (currentOctree == null || currentSelectedObject == null) return;

        Ray mouseRay = HandleUtility.GUIPointToWorldRay(e.mousePosition);

        if (currentOctree.GetClosestVertex(mouseRay, out Vector3 closestVertex, out float distanceToRay))
            {
            Vector2 guiPoint = HandleUtility.WorldToGUIPoint(closestVertex);
            if (Vector2.Distance(guiPoint, e.mousePosition) <= SnapThresholdPixels)
                {
                Handles.color = Color.green;
                Handles.DrawWireDisc(closestVertex, sceneView.camera.transform.forward, 0.05f * HandleUtility.GetHandleSize(closestVertex));

                if (e.type == EventType.MouseDown && e.button == 0)
                    {
                    CustomAnchorHandler.SetAnchor(currentSelectedObject.transform, closestVertex);
                    e.Use();
                    }
                }
            }

        if (e.type == EventType.MouseMove)
            {
            sceneView.Repaint();
            }
        }
    }