using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Applies a one-time ("baked") random nudge to a set of GameObjects: a random
/// position offset and a random rotation, each within a min/max range you choose.
/// If <see cref="targets"/> is empty, it falls back to this object's direct children.
/// Run it from the component's context menu (⋮ → "Apply Baked Nudge") in the editor.
/// </summary>
public class BakedNudge : MonoBehaviour
{
    [Header("Targets (empty = use my children)")]
    [SerializeField] private List<GameObject> targets = new List<GameObject>();

    [Header("Random Position Offset")]
    [SerializeField] private Vector3 positionMin = new Vector3(-0.1f, 0f, -0.1f);
    [SerializeField] private Vector3 positionMax = new Vector3(0.1f, 0f, 0.1f);

    [Header("Random Rotation (euler degrees)")]
    [SerializeField] private Vector3 rotationMin = Vector3.zero;
    [SerializeField] private Vector3 rotationMax = new Vector3(0f, 360f, 0f);

    [Header("Options")]
    [Tooltip("Nudge in local space instead of world space.")]
    [SerializeField] private bool useLocalSpace = false;

    [ContextMenu("Apply Baked Nudge")]
    public void ApplyNudge()
    {
        List<Transform> toNudge = GatherTargets();
        if (toNudge.Count == 0)
        {
            Debug.LogWarning("[BakedNudge] No targets and no children to nudge.", this);
            return;
        }

#if UNITY_EDITOR
        UnityEditor.Undo.RecordObjects(toNudge.ToArray(), "Baked Nudge");
#endif

        foreach (Transform t in toNudge)
        {
            Vector3 offset = new Vector3(
                Random.Range(positionMin.x, positionMax.x),
                Random.Range(positionMin.y, positionMax.y),
                Random.Range(positionMin.z, positionMax.z));

            Vector3 euler = new Vector3(
                Random.Range(rotationMin.x, rotationMax.x),
                Random.Range(rotationMin.y, rotationMax.y),
                Random.Range(rotationMin.z, rotationMax.z));

            if (useLocalSpace)
            {
                t.localPosition += offset;
                t.localRotation *= Quaternion.Euler(euler);
            }
            else
            {
                t.position += offset;
                t.rotation *= Quaternion.Euler(euler);
            }

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(t);
#endif
        }

        Debug.Log($"[BakedNudge] Nudged {toNudge.Count} object(s).", this);
    }

    private List<Transform> GatherTargets()
    {
        List<Transform> result = new List<Transform>();

        if (targets != null && targets.Count > 0)
        {
            foreach (GameObject go in targets)
            {
                if (go != null)
                    result.Add(go.transform);
            }
        }
        else
        {
            // Fallback: direct children of this object.
            foreach (Transform child in transform)
                result.Add(child);
        }

        return result;
    }
}
