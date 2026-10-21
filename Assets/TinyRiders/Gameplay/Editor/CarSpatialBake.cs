using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TinyRiders;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Bakes real car geometry and color maps into one distant-car material.</summary>
public static class CarSpatialBake
{
    private const string Root = "Assets/TinyRiders/Gameplay/Rendering/SpatialCars";
    private const string CatalogPath = "Assets/00_Project/03_scriptableObjects/Resources/CarSpatialCatalog.asset";
    private const int Tile = 128;
    private const int Columns = 32;
    private const int Rows = 16;
    private const int Width = Tile * Columns;
    private const int Height = Tile * Rows;

    [MenuItem("Tools/Toy Car Sort/Rendering/Bake Spatial Car Proxies")]
    public static void Bake()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play mode first.");
        Directory.CreateDirectory(Root);
        var source = AssetDatabase.LoadAssetAtPath<ThemedCarCatalog>(ItemDetailBuilder.CatalogPath);
        if (source == null) throw new InvalidOperationException("Car catalog missing.");
        var entries = source.entries.Where(e => e.enabled).OrderBy(e => e.collectibleId, StringComparer.Ordinal).ToArray();
        var materials = entries.SelectMany(e => e.sortingItem.WorldItem.GetComponentsInChildren<MeshRenderer>(true))
            .Where(r => r.enabled).SelectMany(r => r.sharedMaterials).Where(m => m != null && m.renderQueue < 2500)
            .Distinct().ToArray();
        var maps = materials.Select(BaseMap).Where(t => t != null).Distinct().OrderBy(AssetDatabase.GetAssetPath, StringComparer.Ordinal).ToArray();
        if (maps.Length + 1 > Columns * Rows) throw new InvalidOperationException("Car atlas is full.");
        var tiles = new Dictionary<Texture, int>();
        for (int i = 0; i < maps.Length; i++) tiles[maps[i]] = i + 1;
        var atlas = new Texture2D(Width, Height, TextureFormat.RGBA32, true, false);
        var white = Enumerable.Repeat(Color.white, Tile * Tile).ToArray();
        atlas.SetPixels(0, 0, Tile, Tile, white);
        var previous = RenderTexture.active;
        try
        {
            for (int i = 0; i < maps.Length; i++)
            {
                var target = RenderTexture.GetTemporary(Tile, Tile, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                try
                {
                    Graphics.Blit(maps[i], target);
                    RenderTexture.active = target;
                    int index = i + 1;
                    atlas.ReadPixels(new Rect(0, 0, Tile, Tile), index % Columns * Tile, index / Columns * Tile, false);
                }
                finally { RenderTexture.ReleaseTemporary(target); }
            }
        }
        finally { RenderTexture.active = previous; }
        atlas.Apply(true, false);
        string atlasPath = Root + "/CarAtlas.png";
        File.WriteAllBytes(atlasPath, atlas.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(atlas);
        AssetDatabase.ImportAsset(atlasPath, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(atlasPath);
        importer.maxTextureSize = Width;
        importer.mipmapEnabled = true;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        importer.isReadable = false;
        importer.SaveAndReimport();
        var materialPath = Root + "/CarSpatial.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        var shader = Shader.Find("TinyRiders/Car Spatial Proxy");
        if (shader == null) throw new InvalidOperationException("Car spatial shader missing.");
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, materialPath);
        }
        material.shader = shader;
        material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(atlasPath));
        material.SetVector("_TileSize", new Vector4((float)Tile / Width, (float)Tile / Height, 0, 0));
        EditorUtility.SetDirty(material);

        var catalog = AssetDatabase.LoadAssetAtPath<CarSpatialCatalog>(CatalogPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<CarSpatialCatalog>();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
        }
        var output = new CarSpatialCatalog.Entry[entries.Length];
        var lodSet = AssetDatabase.LoadAssetAtPath<ItemLodSet>(ItemDetailBuilder.SetPath);
        long totalVertices = 0, totalTriangles = 0;
        for (int i = 0; i < entries.Length; i++)
        {
            var prefab = entries[i].sortingItem.WorldItem.gameObject;
            Mesh proxy = Proxy(prefab, tiles, lodSet);
            string path = Root + "/Car_" + i.ToString("D3") + ".asset";
            var saved = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (saved == null) { AssetDatabase.CreateAsset(proxy, path); saved = proxy; }
            else { EditorUtility.CopySerialized(proxy, saved); EditorUtility.SetDirty(saved); UnityEngine.Object.DestroyImmediate(proxy); }
            output[i] = new CarSpatialCatalog.Entry { item = entries[i].sortingItem, mesh = saved };
            totalVertices += saved.vertexCount;
            totalTriangles += saved.GetIndexCount(0) / 3;
            MeshUtility.SetMeshCompression(saved, ModelImporterMeshCompression.High);
            EditorUtility.SetDirty(saved);
            if ((i + 1) % 50 == 0) Debug.Log("SPATIAL_CARS " + (i + 1) + "/" + entries.Length);
        }
        catalog.material = material;
        catalog.entries = output;
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        Debug.Log($"SPATIAL_CARS_READY cars={output.Length} maps={maps.Length} vertices={totalVertices} triangles={totalTriangles}");
    }

    private static Texture BaseMap(Material material) => material.HasProperty("_BaseMap") ? material.GetTexture("_BaseMap") : null;

    private static Mesh Proxy(GameObject prefab, Dictionary<Texture, int> tiles, ItemLodSet lodSet)
    {
        var vertices = new List<Vector3>();
        var normals = new List<Vector3>();
        var uv = new List<Vector2>();
        var tile = new List<Vector2>();
        var colors = new List<Color32>();
        var triangles = new List<int>();
        Matrix4x4 local = prefab.transform.worldToLocalMatrix;
        foreach (var renderer in prefab.GetComponentsInChildren<MeshRenderer>(true))
        {
            if (!renderer.enabled) continue;
            var filter = renderer.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null) continue;
            Mesh mesh = filter.sharedMesh;
            if (lodSet != null && lodSet.TryGet(mesh, out var entry) && entry.bakedMesh != null)
                mesh = entry.bakedMesh;
            var positions = mesh.vertices;
            var sourceNormals = mesh.normals;
            var sourceUv = mesh.uv;
            var sourceColors = mesh.colors32;
            Matrix4x4 matrix = local * filter.transform.localToWorldMatrix;
            Matrix4x4 normalMatrix = matrix.inverse.transpose;
            var shared = renderer.sharedMaterials;
            for (int sub = 0; sub < shared.Length && sub < mesh.subMeshCount; sub++)
            {
                Material material = shared[sub];
                if (material == null || material.renderQueue >= 2500) continue;
                Texture map = BaseMap(material);
                int tileIndex = map != null && tiles.TryGetValue(map, out int index) ? index : 0;
                Vector2 tileOrigin = new Vector2((float)(tileIndex % Columns) / Columns, (float)(tileIndex / Columns) / Rows);
                Vector2 scale = material.HasProperty("_BaseMap") ? material.GetTextureScale("_BaseMap") : Vector2.one;
                Vector2 offset = material.HasProperty("_BaseMap") ? material.GetTextureOffset("_BaseMap") : Vector2.zero;
                Color tint = material.HasProperty("_BaseColor") ? material.GetColor("_BaseColor") : Color.white;
                int[] indices = mesh.GetIndices(sub, Mathf.Min(2, mesh.lodCount - 1), true);
                var remap = new Dictionary<int, int>();
                foreach (int sourceIndex in indices)
                {
                    if (!remap.TryGetValue(sourceIndex, out int targetIndex))
                    {
                        targetIndex = vertices.Count;
                        remap.Add(sourceIndex, targetIndex);
                        vertices.Add(matrix.MultiplyPoint3x4(positions[sourceIndex]));
                        normals.Add(sourceNormals.Length == positions.Length ? normalMatrix.MultiplyVector(sourceNormals[sourceIndex]).normalized : Vector3.up);
                        Vector2 source = sourceUv.Length == positions.Length ? sourceUv[sourceIndex] : Vector2.zero;
                        uv.Add(Vector2.Scale(source, scale) + offset);
                        tile.Add(tileOrigin);
                        Color sourceColor = sourceColors.Length == positions.Length ? sourceColors[sourceIndex] : Color.white;
                        colors.Add((Color32)(sourceColor * tint));
                    }
                    triangles.Add(targetIndex);
                }
                if (matrix.determinant < 0)
                    for (int t = triangles.Count - indices.Length; t < triangles.Count; t += 3)
                    { int swap = triangles[t + 1]; triangles[t + 1] = triangles[t + 2]; triangles[t + 2] = swap; }
            }
        }
        if (vertices.Count == 0) throw new InvalidOperationException("Empty distant car: " + prefab.name);
        var result = new Mesh { name = prefab.name + " Distant", indexFormat = vertices.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
        result.SetVertices(vertices);
        result.SetNormals(normals);
        result.SetUVs(0, uv);
        result.SetUVs(1, tile);
        result.SetColors(colors);
        result.SetTriangles(triangles, 0);
        result.RecalculateBounds();
        if (triangles.Count < 3000) return result;

        // The source's Low LOD still carries thousands of tiny parts. Reduce the
        // single atlas mesh again, then discard vertices no longer referenced.
        MeshLodUtility.GenerateMeshLods(result, 4);
        int[] reduced = result.GetIndices(0, 4, true);
        var compactPositions = new List<Vector3>();
        var compactNormals = new List<Vector3>();
        var compactUv = new List<Vector2>();
        var compactTiles = new List<Vector2>();
        var compactColors = new List<Color32>();
        var compactIndices = new int[reduced.Length];
        var used = new Dictionary<int, int>();
        for (int i = 0; i < reduced.Length; i++)
        {
            int source = reduced[i];
            if (!used.TryGetValue(source, out int destination))
            {
                destination = compactPositions.Count;
                used.Add(source, destination);
                compactPositions.Add(vertices[source]);
                compactNormals.Add(normals[source]);
                compactUv.Add(uv[source]);
                compactTiles.Add(tile[source]);
                compactColors.Add(colors[source]);
            }
            compactIndices[i] = destination;
        }
        var compact = new Mesh { name = prefab.name + " Distant", indexFormat =
            compactPositions.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
        compact.SetVertices(compactPositions);
        compact.SetNormals(compactNormals);
        compact.SetUVs(0, compactUv);
        compact.SetUVs(1, compactTiles);
        compact.SetColors(compactColors);
        compact.SetTriangles(compactIndices, 0);
        compact.RecalculateBounds();
        UnityEngine.Object.DestroyImmediate(result);
        return compact;
    }
}
