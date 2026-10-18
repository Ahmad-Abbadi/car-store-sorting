using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TinyRiders;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>Repeatable native LOD bake and detail-only quality settings.</summary>
public static class ItemDetailBuilder
{
    public const string CatalogPath = "Assets/TinyRiders/Gameplay/Compact/CompactCarCatalog.asset";
    public const string SetPath = "Assets/00_Project/03_scriptableObjects/Resources/ItemLodSet.asset";
    public const string MeshRoot = "Assets/TinyRiders/Gameplay/DetailMeshes";
    public const string TextureGroup = "World Detail";
    public const string Evidence = "IntegrationEvidence/ItemDetail";

    public static GameObject[] CatalogPrefabs()
    {
        var catalog = AssetDatabase.LoadAssetAtPath<ThemedCarCatalog>(CatalogPath);
        return catalog.entries.Where(e => e.enabled && e.sortingItem != null)
            .SelectMany(e => new[] { e.sortingItem.WorldItem.gameObject, e.sortingItem.PlacedOnShelfPrefab.gameObject,
                e.sortingItem.HandItem != null ? e.sortingItem.HandItem.gameObject : null }).Where(p => p != null)
            .Distinct().ToArray();
    }

    [MenuItem("Tools/Items/Bake Item LODs")]
    public static void Bake()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play mode before baking.");
        if (!AssetDatabase.IsValidFolder(MeshRoot)) AssetDatabase.CreateFolder("Assets/TinyRiders/Gameplay", "DetailMeshes");
        var meshes = CatalogPrefabs().SelectMany(p => p.GetComponentsInChildren<MeshFilter>(true))
            .Select(f => f.sharedMesh).Where(m => m != null).Distinct()
            .OrderBy(m => AssetDatabase.GetAssetPath(m)).ThenBy(m => m.name).ToArray();
        var entries = new List<ItemLodSet.Entry>();
        var report = new System.Text.StringBuilder("mesh,highTriangles,mediumTriangles,lowTriangles\n");
        long[] totals = new long[3];
        foreach (var source in meshes)
        {
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(source, out string guid, out long id);
            string path = MeshRoot + "/" + guid + "_" + id + ".asset";
            var copy = Object.Instantiate(source);
            try
            {
                copy.name = source.name + " (Detail LODs)";
                if (source.triangles.Length / 3 > 64) MeshLodUtility.GenerateMeshLods(copy, 2);
                var levels = new List<MeshLodRange[]>();
                var buffers = new List<int[]>();
                for (int sub = 0; sub < copy.subMeshCount; sub++)
                {
                    var triangles = new int[3][];
                    triangles[0] = source.GetIndices(sub);
                    var ranges = new MeshLodRange[3];
                    for (int level = 1; level < 3; level++)
                    {
                        // Do not let the simplifier erase a small material part or decal.
                        var reduced = level < copy.lodCount ? copy.GetIndices(sub, level, true) : triangles[level - 1];
                        triangles[level] = reduced.Length >= 24 && reduced.Length <= triangles[level - 1].Length
                            ? reduced : triangles[level - 1];
                    }
                    uint start = 0;
                    for (int level = 0; level < 3; level++)
                    {
                        ranges[level] = new MeshLodRange { indexStart = start, indexCount = (uint)triangles[level].Length };
                        start += (uint)triangles[level].Length;
                    }
                    buffers.Add(triangles.SelectMany(t => t).ToArray());
                    levels.Add(ranges);
                }
                // Unity may reorder LOD0 triangles during generation. Restore the exact
                // source triangle list while keeping only its generated lower levels.
                copy.lodCount = 1;
                for (int sub = 0; sub < copy.subMeshCount; sub++) copy.SetIndices(buffers[sub], MeshTopology.Triangles, sub, false);
                copy.lodCount = 3;
                for (int sub = 0; sub < copy.subMeshCount; sub++) copy.SetLods(levels[sub], sub, MeshUpdateFlags.Default);
                copy.bounds = source.bounds;
                if (!source.vertices.SequenceEqual(copy.vertices)) throw new InvalidOperationException("LOD0 vertices changed: " + source.name);
                for (int sub = 0; sub < copy.subMeshCount; sub++)
                    if (!source.GetIndices(sub).SequenceEqual(copy.GetIndices(sub))) throw new InvalidOperationException("LOD0 indices changed: " + source.name);
                var baked = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if (baked == null) { AssetDatabase.CreateAsset(copy, path); baked = copy; }
                else { EditorUtility.CopySerialized(copy, baked); EditorUtility.SetDirty(baked); }
                entries.Add(new ItemLodSet.Entry { mesh = source, bakedMesh = baked });
                var counts = Enumerable.Range(0, 3).Select(l => levels.Sum(s => (long)s[l].indexCount) / 3).ToArray();
                for (int i = 0; i < 3; i++) totals[i] += counts[i];
                report.AppendLine(source.name + "," + string.Join(",", counts));
            }
            finally { if (!AssetDatabase.Contains(copy)) Object.DestroyImmediate(copy); }
        }
        var set = AssetDatabase.LoadAssetAtPath<ItemLodSet>(SetPath);
        if (set == null) { set = ScriptableObject.CreateInstance<ItemLodSet>(); AssetDatabase.CreateAsset(set, SetPath); }
        set.SetEntries(entries); EditorUtility.SetDirty(set); AssetDatabase.SaveAssets();
        Directory.CreateDirectory(Evidence);
        File.WriteAllText(Evidence + "/mesh-coverage.csv", report.ToString());
        File.WriteAllText(Evidence + "/bake-summary.txt", "Covered " + entries.Count + "/" + meshes.Length
            + " unique meshes; High/Medium/Low triangles: " + string.Join("/", totals) + "; missing: 0\n");
    }

    [MenuItem("Tools/Items/Configure Detail Settings")]
    public static void Configure()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit mode only.");
        int previous = QualitySettings.GetQualityLevel();
        var high = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset>("Assets/Settings/High_URP.asset");
        if (!TextureMipmapLimitGroups.HasGroup(TextureGroup)) TextureMipmapLimitGroups.CreateGroup(TextureGroup);
        try
        {
            foreach (var name in new[] { "Low", "Medium", "High" })
            {
                var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset>("Assets/Settings/" + name + "_URP.asset");
                if (asset != high) EditorUtility.CopySerialized(high, asset);
                asset.name = name + "_URP";
                asset.renderScale = name == "High" ? 1 : name == "Medium" ? .9f : .8f;
                asset.msaaSampleCount = name == "High" ? 4 : name == "Medium" ? 2 : 1;
                EditorUtility.SetDirty(asset);
                QualitySettings.SetQualityLevel(Array.IndexOf(QualitySettings.names, name), true);
                QualitySettings.globalTextureMipmapLimit = 0;
                QualitySettings.SetTextureMipmapLimitSettings(TextureGroup, new TextureMipmapLimitSettings
                {
                    limitBiasMode = TextureMipmapLimitBiasMode.OverrideGlobalLimit,
                    limitBias = name == "High" ? 0 : name == "Medium" ? 1 : 2
                });
            }
        }
        finally { QualitySettings.SetQualityLevel(previous, true); }
        AssetDatabase.SaveAssets();
    }

    public static void ConfigureWorldTextures(IEnumerable<Scene> scenes)
    {
        var prefabMaterials = CatalogPrefabs().SelectMany(p => p.GetComponentsInChildren<MeshRenderer>(true)).SelectMany(r => r.sharedMaterials);
        var sceneRenderers = scenes.SelectMany(s => s.GetRootGameObjects()).SelectMany(g => g.GetComponentsInChildren<Renderer>(true)).ToArray();
        var uiMaterials = sceneRenderers.Where(r => r.GetComponentInParent<Canvas>() != null || r.GetComponent<TMPro.TMP_Text>() != null)
            .SelectMany(r => r.sharedMaterials);
        var exempt = new HashSet<Texture>(Textures(uiMaterials));
        var worldMaterials = sceneRenderers.Where(r => r.GetComponentInParent<Canvas>() == null && r.GetComponent<TMPro.TMP_Text>() == null)
            .SelectMany(r => r.sharedMaterials).Concat(prefabMaterials);
        var paths = Textures(worldMaterials).Where(t => !exempt.Contains(t)).Select(AssetDatabase.GetAssetPath).Distinct().OrderBy(p => p).ToArray();
        int changed = 0, included = 0;
        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (string path in paths)
            {
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                // Palette colors and shelf artwork contain sorting information: keep them exact.
                if (importer == null || !importer.mipmapEnabled || importer.textureType == TextureImporterType.Sprite
                    || path.IndexOf("Palette", StringComparison.OrdinalIgnoreCase) >= 0
                    || path.IndexOf("/Shelves/", StringComparison.OrdinalIgnoreCase) >= 0
                    || path.IndexOf("/Fonts", StringComparison.OrdinalIgnoreCase) >= 0) continue;
                included++;
                if (importer.mipmapLimitGroupName == TextureGroup && !importer.ignoreMipmapLimit) continue;
                importer.mipmapLimitGroupName = TextureGroup;
                importer.ignoreMipmapLimit = false;
                importer.SaveAndReimport(); changed++;
            }
        }
        finally { AssetDatabase.StopAssetEditing(); }
        Directory.CreateDirectory(Evidence);
        File.WriteAllText(Evidence + "/texture-coverage.txt", "World textures grouped: " + included + "; reimported: " + changed
            + ". UI, fonts, shelf labels, and color palettes retain full resolution.\n");
    }

    private static IEnumerable<Texture> Textures(IEnumerable<Material> materials) => materials.Where(m => m != null).Distinct()
        .SelectMany(m => m.GetTexturePropertyNames().Select(m.GetTexture)).Where(t => t is Texture2D).Distinct();

    public static void ConfigureMenu(Scene scene)
    {
        var cars = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true))
            .Single(t => t.name == "Staged Collection Cars");
        if (cars.GetComponent<ItemDetailDisplay>() == null) cars.gameObject.AddComponent<ItemDetailDisplay>();
        EditorSceneManager.MarkSceneDirty(scene);
    }
}
