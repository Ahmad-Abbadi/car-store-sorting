using UnityEngine;
using UnityEditor;

public static class CustomAnchorHandler
    {
    private static Transform targetTransform;
    private static Vector3 anchorPosition;

    public static bool HasAnchor => targetTransform != null;

    public static void SetAnchor(Transform target, Vector3 position)
        {
        targetTransform = target;
        anchorPosition = position;

        if (Tools.current != Tool.Move && Tools.current != Tool.Rotate && Tools.current != Tool.Scale && Tools.current != Tool.Rect)
            {
            Tools.current = Tool.Move;
            }
        }

    public static void ClearAnchor()
        {
        targetTransform = null;
        }

    public static void DoSceneGUI()
        {
        if (targetTransform == null) return;

        Event e = Event.current;

        // Intercept the F key to focus the camera on the custom anchor
        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.F)
            {
            if (SceneView.lastActiveSceneView != null)
                {
                SceneView.lastActiveSceneView.Frame(new Bounds(anchorPosition, Vector3.one * 0.5f), false);
                e.Use();
                }
            }

        Handles.BeginGUI();
        GUI.Label(new Rect(10, 10, 400, 20), $"Anchor Active | Mode: {Tools.current} | W/E/R/T to switch | F to Focus");
        Handles.EndGUI();

        EditorGUI.BeginChangeCheck();

        switch (Tools.current)
            {
            case Tool.Move:
                DoPositionHandle();
                break;
            case Tool.Rotate:
                DoRotationHandle();
                break;
            case Tool.Scale:
                DoScaleHandle();
                break;
            case Tool.Rect:
                DoOffsetAnchorHandle();
                break;
            default:
                DoPositionHandle();
                break;
            }

        EditorGUI.EndChangeCheck();
        }

    private static void DoPositionHandle()
        {
        Vector3 newAnchorPosition = Handles.PositionHandle(anchorPosition, Quaternion.identity);

        if (newAnchorPosition != anchorPosition)
            {
            Undo.RecordObject(targetTransform, "Move Object via Anchor");

            Vector3 delta = newAnchorPosition - anchorPosition;
            targetTransform.position += delta;
            anchorPosition = newAnchorPosition;
            }
        }

    private static void DoRotationHandle()
        {
        Handles.color = Color.yellow;
        Handles.DrawDottedLine(anchorPosition, targetTransform.position, 2f);

        Quaternion currentRotation = targetTransform.rotation;
        Quaternion newRotation = Handles.RotationHandle(currentRotation, anchorPosition);

        if (newRotation != currentRotation)
            {
            Undo.RecordObject(targetTransform, "Rotate Object via Anchor");

            Quaternion deltaRotation = newRotation * Quaternion.Inverse(currentRotation);

            Vector3 offset = targetTransform.position - anchorPosition;
            targetTransform.position = anchorPosition + (deltaRotation * offset);

            targetTransform.rotation = newRotation;
            }
        }

    private static void DoScaleHandle()
        {
        Handles.color = Color.yellow;
        Handles.DrawDottedLine(anchorPosition, targetTransform.position, 2f);

        float size = HandleUtility.GetHandleSize(anchorPosition);
        Vector3 newScale = Handles.ScaleHandle(targetTransform.localScale, anchorPosition, targetTransform.rotation, size);

        if (newScale != targetTransform.localScale)
            {
            Undo.RecordObject(targetTransform, "Scale Object via Anchor");

            Vector3 currentScale = targetTransform.localScale;

            Vector3 scaleMultiplier = new Vector3(
                currentScale.x == 0 ? 0 : newScale.x / currentScale.x,
                currentScale.y == 0 ? 0 : newScale.y / currentScale.y,
                currentScale.z == 0 ? 0 : newScale.z / currentScale.z
            );

            Vector3 offset = targetTransform.position - anchorPosition;

            Vector3 localOffset = Quaternion.Inverse(targetTransform.rotation) * offset;
            localOffset.Scale(scaleMultiplier);
            Vector3 scaledOffset = targetTransform.rotation * localOffset;

            targetTransform.position = anchorPosition + scaledOffset;
            targetTransform.localScale = newScale;
            }
        }

    private static void DoOffsetAnchorHandle()
        {
        Handles.color = Color.cyan;
        Handles.DrawDottedLine(anchorPosition, targetTransform.position, 2f);

        Vector3 newAnchorPosition = Handles.PositionHandle(anchorPosition, Quaternion.identity);

        if (newAnchorPosition != anchorPosition)
            {
            anchorPosition = newAnchorPosition;
            }
        }
    }