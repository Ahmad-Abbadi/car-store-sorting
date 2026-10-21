using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using TinyRiders;
using Unity.Collections;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

/// <summary>Bakes exact, per-car opaque batches. Source art, transparent sorting and gameplay transforms are retained.</summary>
public static class CarVisualBatchBuilder
{
    public const string Root = "Assets/TinyRiders/Gameplay/Rendering/CarBatches";
    public const string Evidence = "IntegrationEvidence/CarBatching";
    const int Version = 4;
    public static bool SkipForBenchmark;
    static ItemLodSet lods;

    sealed class Part
    {
        public MeshRenderer renderer;
        public Mesh mesh;
        public Matrix4x4 matrix;
        public int[] submeshes;
    }

    static bool IsOpaque(Material material) => material != null && material.shader.name == "Universal Render Pipeline/Lit"
        && material.renderQueue < 2500 && (!material.HasProperty("_Surface") || material.GetFloat("_Surface") == 0);

    static bool Eligible(MeshRenderer r)
    {
        var mesh = r.GetComponent<MeshFilter>()?.sharedMesh;
        // Unity permits mesh reads in Edit mode even when runtime Read/Write is disabled.
        // Requiring isReadable here would silently exclude the project's optimized FBX imports.
        return r.enabled && r.gameObject.activeInHierarchy && mesh != null && !r.HasPropertyBlock()
            && r.additionalVertexStreams == null && r.lightmapIndex < 0 && r.realtimeLightmapIndex < 0
            && !mesh.HasVertexAttribute(VertexAttribute.TexCoord3)
            // Reindexing is safe for Lit; custom shaders may depend on SV_VertexID.
            && r.sharedMaterials.All(m => m != null && m.shader.name == "Universal Render Pipeline/Lit")
            && r.sharedMaterials.Length == mesh.subMeshCount && Enumerable.Range(0,mesh.subMeshCount).All(s => mesh.GetTopology(s) == MeshTopology.Triangles)
            && r.GetComponents<Component>().All(c => c is Transform || c is MeshFilter || c is MeshRenderer)
            && !r.GetComponentsInParent<Transform>().Any(t => t.GetComponent<Animator>() != null || t.GetComponent<Animation>() != null || t.GetComponent<LODGroup>() != null);
    }

    static string StateKey(MeshRenderer r)
    {
        var mesh = r.GetComponent<MeshFilter>().sharedMesh;
        return string.Join("|", new object[] { r.gameObject.layer, r.shadowCastingMode, r.receiveShadows, r.lightProbeUsage,
            r.reflectionProbeUsage, r.probeAnchor != null ? r.probeAnchor.GetInstanceID() : 0, r.renderingLayerMask,
            r.motionVectorGenerationMode, r.allowOcclusionWhenDynamic, r.lightProbeProxyVolumeOverride != null ? r.lightProbeProxyVolumeOverride.GetInstanceID() : 0,
            r.rendererPriority, r.sortingLayerID, r.sortingOrder,
            // Theme accessories vary, while the car body can share exact geometry across all forty themes.
            r.GetComponentsInParent<Transform>().Any(t => t.name == "ThemeAccessories") ? "accessory" : "body",
            string.Join(";", mesh.GetVertexAttributes().Select(a => a.attribute + ":" + a.dimension)) });
    }

    static Mesh SourceMesh(Mesh mesh) => mesh != null && lods != null && lods.TryGet(mesh, out var entry) && entry.bakedMesh != null ? entry.bakedMesh : mesh;
    static int[] Indices(Mesh mesh, int sub, int level) => lods != null && lods.TryGet(mesh, out var entry)
        ? entry.GetIndices(level, sub) : mesh.GetIndices(sub, Mathf.Min(level, mesh.lodCount - 1), true);

    // Concatenate vertices once per source renderer; never weld vertices or regenerate LOD triangles.
    static Mesh Combine(List<Part> parts, string name, out Material[] materials)
    {
        var positions = new List<Vector3>(); var normals = new List<Vector3>(); var tangents = new List<Vector4>();
        var colors = new List<Color>(); var smooth = new List<Vector3>();
        var uv = Enumerable.Range(0, 8).Select(_ => new List<Vector4>()).ToArray();
        var dimensions = new int[8];
        bool hasNormals = parts.Any(p => SourceMesh(p.mesh).HasVertexAttribute(VertexAttribute.Normal));
        bool hasTangents = parts.Any(p => SourceMesh(p.mesh).HasVertexAttribute(VertexAttribute.Tangent));
        bool hasColors = parts.Any(p => SourceMesh(p.mesh).HasVertexAttribute(VertexAttribute.Color));
        var materialList = new List<Material>();
        var triangles = new List<List<int>[]>();
        foreach (var part in parts)
        {
            var source = SourceMesh(part.mesh);
            int offset = positions.Count;
            var vertices = source.vertices; var ns = source.normals; var ts = source.tangents; var cs = source.colors;
            var normalMatrix = part.matrix.inverse.transpose;
            var outline = new Vector3[vertices.Length];
            // Match SharedOutline's original per-renderer smoothing BEFORE applying the part's transform.
            foreach (var group in vertices.Select((v, i) => (v, i)).GroupBy(p => p.v))
            {
                Vector3 sum = Vector3.zero;
                foreach (var p in group) if (ns.Length > p.i) sum += ns[p.i];
                sum.Normalize();
                foreach (var p in group) outline[p.i] = normalMatrix.MultiplyVector(sum).normalized;
            }
            for (int i = 0; i < vertices.Length; i++)
            {
                positions.Add(part.matrix.MultiplyPoint3x4(vertices[i])); smooth.Add(outline[i]);
                if (hasNormals) normals.Add(ns.Length > i ? normalMatrix.MultiplyVector(ns[i]).normalized : Vector3.zero);
                if (hasTangents)
                {
                    Vector4 t = ts.Length > i ? ts[i] : Vector4.zero;
                    Vector3 direction = part.matrix.MultiplyVector(new Vector3(t.x, t.y, t.z)).normalized;
                    tangents.Add(new Vector4(direction.x, direction.y, direction.z, t.w * (part.matrix.determinant < 0 ? -1 : 1)));
                }
                if (hasColors) colors.Add(cs.Length > i ? cs[i] : Color.white);
            }
            for (int channel = 0; channel < 8; channel++)
            {
                if (channel == 3) continue; // QuickOutline's reserved smooth-normal channel.
                var values = new List<Vector4>(); source.GetUVs(channel, values);
                if (source.HasVertexAttribute(VertexAttribute.TexCoord0 + channel))
                    dimensions[channel] = Math.Max(dimensions[channel], source.GetVertexAttributeDimension(VertexAttribute.TexCoord0 + channel));
                for (int i = 0; i < vertices.Length; i++) uv[channel].Add(values.Count > i ? values[i] : Vector4.zero);
            }
            foreach (int sub in part.submeshes)
            {
                Material material = part.renderer.sharedMaterials[sub];
                int m = materialList.IndexOf(material);
                if (m < 0) { m = materialList.Count; materialList.Add(material); triangles.Add(new[] { new List<int>(), new List<int>(), new List<int>() }); }
                for (int level = 0; level < 3; level++)
                {
                    int[] indices = Indices(part.mesh, sub, level);
                    // Negative part scales reverse winding when folded into the car's local space.
                    for (int i = 0; i < indices.Length; i += 3)
                    {
                        triangles[m][level].Add(offset + indices[i]);
                        triangles[m][level].Add(offset + indices[i + (part.matrix.determinant < 0 ? 2 : 1)]);
                        triangles[m][level].Add(offset + indices[i + (part.matrix.determinant < 0 ? 1 : 2)]);
                    }
                }
            }
        }
        var mesh = new Mesh { name = name, indexFormat = positions.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
        mesh.SetVertices(positions);
        if (hasNormals) mesh.SetNormals(normals);
        if (hasTangents) mesh.SetTangents(tangents);
        if (hasColors) mesh.SetColors(colors);
        for (int channel = 0; channel < 8; channel++)
        {
            if (dimensions[channel] == 2) mesh.SetUVs(channel, uv[channel].Select(v => new Vector2(v.x, v.y)).ToList());
            else if (dimensions[channel] == 3) mesh.SetUVs(channel, uv[channel].Select(v => new Vector3(v.x, v.y, v.z)).ToList());
            else if (dimensions[channel] > 0) mesh.SetUVs(channel, uv[channel]);
        }
        mesh.SetUVs(3, smooth);
        mesh.subMeshCount = materialList.Count;
        for (int sub = 0; sub < triangles.Count; sub++) mesh.SetIndices(triangles[sub].SelectMany(t => t).ToArray(), MeshTopology.Triangles, sub, false);
        mesh.lodCount = 3;
        for (int sub = 0; sub < triangles.Count; sub++)
        {
            uint start = 0;
            var ranges = triangles[sub].Select(t => { var r = new MeshLodRange { indexStart = start, indexCount = (uint)t.Count }; start += (uint)t.Count; return r; }).ToArray();
            mesh.SetLods(ranges, sub, MeshUpdateFlags.Default);
        }
        mesh.RecalculateBounds(); materials = materialList.ToArray(); return mesh;
    }

    static Mesh SaveMesh(Mesh mesh, string path)
    {
        mesh = CompactUnusedVertices(mesh);
        EnsureFolder(Root + "/Meshes");
        // Content-address native mesh buffers, independently of theme material assignments.
        // This also shares unchanged glass/decal slices across world/held/shelf wrappers and themes.
        using (var bytes = new MemoryStream())
        using (var writer = new BinaryWriter(bytes))
        {
            void Vector(Vector4 v) { writer.Write(v.x); writer.Write(v.y); writer.Write(v.z); writer.Write(v.w); }
            writer.Write(mesh.vertexCount); writer.Write(mesh.subMeshCount); writer.Write(mesh.lodCount);
            foreach (var attribute in mesh.GetVertexAttributes()) { writer.Write((int)attribute.attribute); writer.Write(attribute.dimension); }
            foreach (var v in mesh.vertices) Vector(v);
            foreach (var v in mesh.normals) Vector(v);
            foreach (var v in mesh.tangents) Vector(v);
            foreach (var c in mesh.colors) Vector(c);
            for (int channel = 0; channel < 8; channel++)
            {
                var values = new List<Vector4>(); mesh.GetUVs(channel, values); writer.Write(values.Count);
                foreach (var value in values) Vector(value);
            }
            Vector(mesh.bounds.center); Vector(mesh.bounds.size);
            for (int sub = 0; sub < mesh.subMeshCount; sub++)
            for (int level = 0; level < mesh.lodCount; level++)
            {
                var indices = mesh.GetIndices(sub, level, true); writer.Write(indices.Length);
                foreach (int index in indices) writer.Write(index);
            }
            writer.Flush();
            using (var hash = SHA256.Create())
                path = Root + "/Meshes/" + BitConverter.ToString(hash.ComputeHash(bytes.ToArray())).Replace("-", "").ToLowerInvariant() + ".asset";
        }
        var saved = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (saved == null) { AssetDatabase.CreateAsset(mesh, path); return mesh; }
        Object.DestroyImmediate(mesh); return saved;
    }

    // Preserve every byte of each referenced vertex. The union spans every native detail level;
    // a vertex used only by a distant LOD must survive. Bounds remain authored for sorting/culling.
    static Mesh CompactUnusedVertices(Mesh source)
    {
        var indices = Enumerable.Range(0, source.subMeshCount).Select(s =>
            Enumerable.Range(0, source.lodCount).Select(l => source.GetIndices(s, l, true)).ToArray()).ToArray();
        var retained = indices.SelectMany(s => s).SelectMany(l => l).Distinct().OrderBy(i => i).ToArray();
        if (retained.Length == source.vertexCount) return source;
        var map = new int[source.vertexCount];
        for (int i = 0; i < retained.Length; i++) map[retained[i]] = i;
        var result = new Mesh { name = source.name, indexFormat = source.indexFormat };
        result.SetVertexBufferParams(retained.Length, source.GetVertexAttributes());
        using (var data = Mesh.AcquireReadOnlyMeshData(source))
        for (int stream = 0; stream < source.vertexBufferCount; stream++)
        {
            int stride = source.GetVertexBufferStride(stream);
            var input = data[0].GetVertexData<byte>(stream);
            using (var output = new NativeArray<byte>(retained.Length * stride, Allocator.Temp))
            {
                for (int vertex = 0; vertex < retained.Length; vertex++)
                    NativeArray<byte>.Copy(input, retained[vertex] * stride, output, vertex * stride, stride);
                result.SetVertexBufferData(output, 0, 0, output.Length, stream);
            }
        }
        result.subMeshCount = source.subMeshCount;
        for (int sub = 0; sub < indices.Length; sub++)
            result.SetIndices(indices[sub].SelectMany(l => l).Select(i => map[i]).ToArray(), MeshTopology.Triangles, sub, false);
        result.lodCount = source.lodCount;
        for (int sub = 0; sub < indices.Length; sub++)
        {
            uint start = 0;
            result.SetLods(indices[sub].Select(l => { var range = new MeshLodRange { indexStart = start, indexCount = (uint)l.Length }; start += (uint)l.Length; return range; }).ToArray(), sub);
            var descriptor = result.GetSubMesh(sub);
            descriptor.bounds = source.GetSubMesh(sub).bounds;
            result.SetSubMesh(sub, descriptor, MeshUpdateFlags.DontRecalculateBounds);
        }
        result.bounds = source.bounds;
        Object.DestroyImmediate(source);
        return result;
    }

    static void BakeLocalOutlineNormals(Mesh mesh)
    {
        var vertices = mesh.vertices; var normals = mesh.normals;
        var smooth = normals.ToArray();
        if (smooth.Length != vertices.Length) throw new InvalidOperationException("Outline source normals are incomplete: " + mesh.name);
        foreach (var group in vertices.Select((v, i) => (v, i)).GroupBy(p => p.v))
        {
            if (group.Count() == 1) continue;
            Vector3 sum = Vector3.zero;
            foreach (var p in group) if (p.i < normals.Length) sum += normals[p.i];
            sum.Normalize();
            foreach (var p in group) smooth[p.i] = sum;
        }
        mesh.SetUVs(3, smooth);
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = path.Substring(0, path.LastIndexOf('/'));
        EnsureFolder(parent); AssetDatabase.CreateFolder(parent, path.Substring(path.LastIndexOf('/') + 1));
    }

    public static GameObject BakeVisual(GameObject source, string folder)
    {
        EnsureFolder(folder);
        lods = AssetDatabase.LoadAssetAtPath<ItemLodSet>(ItemDetailBuilder.SetPath);
        var root = PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(source));
        try
        {
            // Gameplay wrappers own physics; Wrap() also strips these from the original nested art.
            foreach (var body in root.GetComponentsInChildren<Rigidbody>(true)) Object.DestroyImmediate(body);
            foreach (var collider in root.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(collider);
            // Runtime-driven art must retain its authored hierarchy and renderers.
            if (root.GetComponentsInChildren<MonoBehaviour>(true).Any(b => !(b is ThemedCarIdentity)))
                return PrefabUtility.SaveAsPrefabAsset(root, folder + "/Visual.prefab");
            var candidates = root.GetComponentsInChildren<MeshRenderer>().Where(Eligible)
                .Where(r => r.sharedMaterials.Any(IsOpaque)).ToArray();
            int batch = 0;
            foreach (var group in candidates.GroupBy(StateKey))
            {
                var parts = group.Select(r => new Part { renderer = r, mesh = r.GetComponent<MeshFilter>().sharedMesh,
                    matrix = root.transform.worldToLocalMatrix * r.transform.localToWorldMatrix,
                    submeshes = Enumerable.Range(0, r.sharedMaterials.Length).Where(i => IsOpaque(r.sharedMaterials[i])).ToArray() }).ToList();
                if (parts.Sum(p => p.submeshes.Length) <= parts.SelectMany(p => p.submeshes.Select(s => p.renderer.sharedMaterials[s])).Distinct().Count()) continue;
                var combined = Combine(parts, "CarBatch Opaque " + batch, out var materials);
                combined = SaveMesh(combined, folder + "/Opaque" + batch + ".asset");
                var target = new GameObject("Batched Opaque " + batch++, typeof(MeshFilter), typeof(MeshRenderer), typeof(CarBatchGeometry));
                target.transform.SetParent(root.transform, false); target.layer = parts[0].renderer.gameObject.layer;
                var renderer = target.GetComponent<MeshRenderer>(); EditorUtility.CopySerialized(parts[0].renderer, renderer);
                renderer.sharedMaterials = materials; target.GetComponent<MeshFilter>().sharedMesh = combined;
                renderer.ResetLocalBounds(); renderer.ResetBounds();
                foreach (var part in parts)
                {
                    var sourceRenderer = part.renderer; var filter = sourceRenderer.GetComponent<MeshFilter>();
                    int[] other = Enumerable.Range(0, sourceRenderer.sharedMaterials.Length).Except(part.submeshes).ToArray();
                    if (other.Length == 0) { Object.DestroyImmediate(sourceRenderer); Object.DestroyImmediate(filter); continue; }
                    // Retain the original transform AND bounds for glass/decal sorting. Do not merge transparent surfaces.
                    Mesh slice = Object.Instantiate(SourceMesh(part.mesh)); slice.name = "CarBatch Transparent " + sourceRenderer.name;
                    // Include normals from the complete original part, even vertices excluded by slicing.
                    BakeLocalOutlineNormals(slice);
                    slice.lodCount = 1; slice.subMeshCount = other.Length;
                    for (int sub = 0; sub < other.Length; sub++) slice.SetIndices(Indices(part.mesh, other[sub], 0).Concat(Indices(part.mesh, other[sub], 1)).Concat(Indices(part.mesh, other[sub], 2)).ToArray(), MeshTopology.Triangles, sub, false);
                    slice.lodCount = 3;
                    for (int sub = 0; sub < other.Length; sub++)
                    {
                        uint start = 0;
                        slice.SetLods(Enumerable.Range(0, 3).Select(l => { uint count = (uint)Indices(part.mesh, other[sub], l).Length; var r = new MeshLodRange { indexStart = start, indexCount = count }; start += count; return r; }).ToArray(), sub, MeshUpdateFlags.Default);
                    }
                    slice.bounds = SourceMesh(part.mesh).bounds;
                    filter.sharedMesh = SaveMesh(slice, folder + "/Transparent" + Array.IndexOf(candidates, sourceRenderer) + ".asset");
                    sourceRenderer.gameObject.AddComponent<CarBatchGeometry>();
                    sourceRenderer.sharedMaterials = other.Select(i => sourceRenderer.sharedMaterials[i]).ToArray();
                }
            }
            root.name = source.name;
            ValidateCounts(source, root);
            return PrefabUtility.SaveAsPrefabAsset(root, folder + "/Visual.prefab");
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    [MenuItem("Tools/Toy Car Sort/Rendering/Bake Exact Car Batches")]
    public static void BakeAll()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play mode before baking");
        EnsureFolder(Root); Directory.CreateDirectory(Evidence);
        var catalog = AssetDatabase.LoadAssetAtPath<ThemedCarCatalog>(ItemDetailBuilder.CatalogPath);
        var report = new JArray();
        foreach (var entry in catalog.entries.Where(e => e.enabled).OrderBy(e => e.collectibleId, StringComparer.Ordinal))
        {
            string sourcePath = AssetDatabase.GetAssetPath(entry.prefab);
            string folder = Root + "/" + AssetDatabase.AssetPathToGUID(sourcePath);
            string fingerprint = Fingerprint(entry.prefab);
            var visual = AssetDatabase.LoadAssetAtPath<GameObject>(folder + "/Visual.prefab");
            if (visual == null || AssetImporter.GetAtPath(folder + "/Visual.prefab").userData != fingerprint)
            {
                visual = BakeVisual(entry.prefab, folder);
                var importer = AssetImporter.GetAtPath(folder + "/Visual.prefab");
                importer.userData = fingerprint; AssetDatabase.WriteImportSettingsIfDirty(importer.assetPath);
            }
            ValidateCounts(entry.prefab, visual);
            foreach (var wrapper in new[] { entry.sortingItem.WorldItem.gameObject, entry.sortingItem.HandItem.gameObject, entry.sortingItem.PlacedOnShelfPrefab.gameObject })
                ReplaceVisual(wrapper, visual);
            report.Add(new JObject { ["id"] = entry.collectibleId, ["source"] = sourcePath, ["generated"] = AssetDatabase.GetAssetPath(visual),
                ["drawsBefore"] = entry.prefab.GetComponentsInChildren<MeshRenderer>().Sum(r => r.sharedMaterials.Length),
                ["drawsAfter"] = visual.GetComponentsInChildren<MeshRenderer>().Sum(r => r.sharedMaterials.Length) });
            if (report.Count % 50 == 0) Debug.Log("CAR_BATCHING " + report.Count + "/600");
        }
        RegisterLods(); AssetDatabase.SaveAssets();
        File.WriteAllText(Evidence + "/geometry-bake.json", new JObject { ["version"] = Version, ["cars"] = report,
            ["drawsBefore"] = report.Sum(r => (int)r["drawsBefore"]), ["drawsAfter"] = report.Sum(r => (int)r["drawsAfter"]) }.ToString());
    }

    static void ReplaceVisual(GameObject wrapper, GameObject visual)
    {
        var current = new SerializedObject(wrapper.GetComponent<ItemVisualGroup>());
        var currentRoot = (Transform)current.FindProperty("visualRoot").objectReferenceValue;
        if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(currentRoot.gameObject) == AssetDatabase.GetAssetPath(visual)) return;
        var root = PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(wrapper));
        try
        {
            var serialized = new SerializedObject(root.GetComponent<ItemVisualGroup>());
            var old = (Transform)serialized.FindProperty("visualRoot").objectReferenceValue;
            var replacement = (GameObject)PrefabUtility.InstantiatePrefab(visual, root.scene);
            foreach (var body in replacement.GetComponentsInChildren<Rigidbody>(true)) Object.DestroyImmediate(body);
            foreach (var collider in replacement.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(collider);
            replacement.name = old.name; replacement.transform.SetParent(old.parent, false);
            replacement.transform.SetSiblingIndex(old.GetSiblingIndex());
            replacement.SetActive(old.gameObject.activeSelf);
            replacement.transform.localPosition = old.localPosition; replacement.transform.localRotation = old.localRotation;
            replacement.transform.localScale = old.localScale;
            foreach (var t in replacement.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = old.gameObject.layer;
            serialized.FindProperty("visualRoot").objectReferenceValue = replacement.transform; serialized.ApplyModifiedPropertiesWithoutUndo();
            Object.DestroyImmediate(old.gameObject);
            PrefabUtility.SaveAsPrefabAsset(root, AssetDatabase.GetAssetPath(wrapper));
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    [MenuItem("Tools/Toy Car Sort/Rendering/Restore Authored Car Visuals")]
    public static void RestoreOriginalVisuals()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play mode first");
        var catalog = AssetDatabase.LoadAssetAtPath<ThemedCarCatalog>(ItemDetailBuilder.CatalogPath);
        foreach (var entry in catalog.entries.Where(e => e.enabled))
            foreach (var wrapper in new[] { entry.sortingItem.WorldItem.gameObject, entry.sortingItem.HandItem.gameObject, entry.sortingItem.PlacedOnShelfPrefab.gameObject })
                ReplaceVisual(wrapper, entry.prefab);
        RegisterLods(); AssetDatabase.SaveAssets();
    }

    public static void RegisterLods()
    {
        var set = AssetDatabase.LoadAssetAtPath<ItemLodSet>(ItemDetailBuilder.SetPath);
        var so = new SerializedObject(set); var field = so.FindProperty("entries");
        var entries = new List<ItemLodSet.Entry>();
        for (int i = 0; i < field.arraySize; i++)
        {
            var e = field.GetArrayElementAtIndex(i);
            var mesh = (Mesh)e.FindPropertyRelative("mesh").objectReferenceValue;
            if (mesh != null && !AssetDatabase.GetAssetPath(mesh).StartsWith(Root + "/", StringComparison.Ordinal))
                entries.Add(new ItemLodSet.Entry { mesh = mesh, bakedMesh = (Mesh)e.FindPropertyRelative("bakedMesh").objectReferenceValue,
                    lod1 = ReadArray(e.FindPropertyRelative("lod1")), lod2 = ReadArray(e.FindPropertyRelative("lod2")) });
        }
        // Previous content-addressed meshes may remain in the cache; never root unused versions in Resources.
        foreach (var mesh in ItemDetailBuilder.CatalogPrefabs().SelectMany(p => p.GetComponentsInChildren<MeshFilter>(true))
            .Select(f => f.sharedMesh).Where(m => m != null && AssetDatabase.GetAssetPath(m).StartsWith(Root + "/", StringComparison.Ordinal))
            .Distinct().OrderBy(AssetDatabase.GetAssetPath, StringComparer.Ordinal))
        {
            if (mesh != null) entries.Add(new ItemLodSet.Entry { mesh = mesh, bakedMesh = mesh });
        }
        set.SetEntries(entries); EditorUtility.SetDirty(set);
    }
    static int[] ReadArray(SerializedProperty p) => Enumerable.Range(0, p.arraySize).Select(i => p.GetArrayElementAtIndex(i).intValue).ToArray();

    /// <summary>Exact buffers still used by generated visuals, including unmerged parts and their authored LODs.</summary>
    public static string[] PrecisionMeshPaths()
    {
        if (!AssetDatabase.IsValidFolder(Root)) return Array.Empty<string>();
        var set = AssetDatabase.LoadAssetAtPath<ItemLodSet>(ItemDetailBuilder.SetPath);
        var paths = new HashSet<string>(StringComparer.Ordinal);
        void Add(Mesh mesh) { if (mesh != null) { string path = AssetDatabase.GetAssetPath(mesh); if (!string.IsNullOrEmpty(path)) paths.Add(path); } }
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { Root }))
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
            foreach (var filter in prefab.GetComponentsInChildren<MeshFilter>(true))
            {
                Add(filter.sharedMesh);
                if (filter.sharedMesh != null && set != null && set.TryGet(filter.sharedMesh, out var entry))
                { Add(entry.mesh); Add(entry.bakedMesh); }
            }
        }
        return paths.OrderBy(p => p, StringComparer.Ordinal).ToArray();
    }

    public static void ValidateCounts(GameObject original, GameObject generated)
    {
        Dictionary<Material, long> Counts(GameObject root, int level)
        {
            var counts = new Dictionary<Material, long>();
            foreach (var r in root.GetComponentsInChildren<MeshRenderer>().Where(r => r.enabled))
            {
                var mesh = r.GetComponent<MeshFilter>()?.sharedMesh;
                if (mesh == null) continue;
                for (int slot = 0; slot < r.sharedMaterials.Length && mesh.subMeshCount > 0; slot++)
                {
                    int sub = Math.Min(slot, mesh.subMeshCount - 1);
                    var mat = r.sharedMaterials[slot];
                    counts.TryGetValue(mat, out long count);
                    counts[mat] = count + Indices(mesh, sub, level).LongLength;
                }
            }
            return counts;
        }
        for (int level = 0; level < 3; level++)
        {
            var a = Counts(original, level); var b = Counts(generated, level);
            if (a.Count != b.Count || a.Any(pair => !b.TryGetValue(pair.Key, out long count) || count != pair.Value))
                throw new InvalidOperationException("Car batching changed material geometry at LOD" + level + ": " + original.name);
        }
    }

    static string Fingerprint(GameObject source)
    {
        lods = AssetDatabase.LoadAssetAtPath<ItemLodSet>(ItemDetailBuilder.SetPath);
        var paths = source.GetComponentsInChildren<MeshFilter>(true).Select(f => SourceMesh(f.sharedMesh))
            .Where(m => m != null).Select(AssetDatabase.GetAssetPath).Distinct().OrderBy(p => p, StringComparer.Ordinal);
        return Version + ":" + AssetDatabase.GetAssetDependencyHash(AssetDatabase.GetAssetPath(source)) + ":" +
            string.Join(";", paths.Select(p => AssetDatabase.GetAssetDependencyHash(p).ToString()));
    }
}

// Ordinary Web builds get the same validated, incremental bake as the release menu command.
public sealed class CarVisualBatchBuildProcessor : IPreprocessBuildWithReport
{
    public int callbackOrder => -1000;
    public void OnPreprocessBuild(BuildReport report)
    {
        if (report.summary.platform == BuildTarget.WebGL && !CarVisualBatchBuilder.SkipForBenchmark)
            CarVisualBatchBuilder.BakeAll();
    }
}
