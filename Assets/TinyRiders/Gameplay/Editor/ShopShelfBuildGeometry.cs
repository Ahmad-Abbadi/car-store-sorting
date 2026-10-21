using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Omit disabled source buffers already represented by exact shelf batches.</summary>
public sealed class ShopShelfBuildGeometry : IProcessSceneWithReport
{
    public int callbackOrder => -980;

    public void OnProcessScene(Scene scene, BuildReport report)
    {
        if (report != null) StripSourceBuffers(scene);
    }

    internal static int StripSourceBuffers(Scene scene)
    {
        int removed = 0;
        // The menu deliberately strips gameplay components from its visual snapshot.
        // Identify the authored source/cache pair by structure in both kinds of scene.
        foreach (var shelf in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true))
            .Where(t => t.Find("TR_Shelf_Base_01") != null && t.Find("Batched Shelf Art") != null))
        {
            var source = shelf.Find("TR_Shelf_Base_01");
            var batch = shelf.Find("Batched Shelf Art");
            if (source == null || batch == null) continue;
            var originals = source.GetComponentsInChildren<MeshRenderer>(true);
            // Inactive cinematic stages can become visible later. Keep their complete
            // cache and omit the disabled source buffers just as for active stages.
            var replacements = batch.GetComponentsInChildren<MeshRenderer>(true)
                .Where(r => r.enabled).ToArray();
            if (originals.Length == 0 || originals.Any(r => r.enabled) || replacements.Length == 0) continue;
            var filters = originals.Select(r => r.GetComponent<MeshFilter>()).ToArray();
            if (filters.Any(f => f == null || f.sharedMesh == null ||
                !AssetDatabase.GetAssetPath(f.sharedMesh).StartsWith("Assets/TinyRiders/Art/Shelves/", StringComparison.Ordinal))) continue;
            if (replacements.Any(r => r.GetComponent<MeshFilter>()?.sharedMesh == null ||
                !AssetDatabase.GetAssetPath(r.GetComponent<MeshFilter>().sharedMesh).StartsWith("Assets/TinyRiders/Gameplay/Rendering/Shelf_", StringComparison.Ordinal))) continue;

            // The batching generator verifies exact triangle counts and geometry bounds.
            // Guard against a manually changed or incomplete replacement before pruning.
            Bounds BoundsOf(MeshRenderer[] renderers)
            {
                bool found = false;
                Bounds bounds = default;
                foreach (var renderer in renderers)
                    foreach (var vertex in renderer.GetComponent<MeshFilter>().sharedMesh.vertices)
                    {
                        var point = renderer.transform.TransformPoint(vertex);
                        if (!found) { bounds = new Bounds(point, Vector3.zero); found = true; }
                        else bounds.Encapsulate(point);
                    }
                return bounds;
            }
            var before = BoundsOf(originals);
            var after = BoundsOf(replacements);
            if (Vector3.Distance(before.min, after.min) > .002f || Vector3.Distance(before.max, after.max) > .002f)
                throw new InvalidOperationException("Shelf source and batch bounds differ: " + shelf.name);
            // A connected source prefab keeps its original FBX subassets reachable,
            // even when the instance's MeshFilters no longer reference them.
            if (PrefabUtility.IsAnyPrefabInstanceRoot(source.gameObject))
                PrefabUtility.UnpackPrefabInstance(source.gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            foreach (var filter in filters)
            {
                filter.sharedMesh = null;
                PrefabUtility.RecordPrefabInstancePropertyModifications(filter);
                removed++;
            }
        }
        return removed;
    }
}
