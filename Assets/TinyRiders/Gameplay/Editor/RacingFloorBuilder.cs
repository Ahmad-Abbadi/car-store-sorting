using System;
using System.Collections.Generic;
using System.Linq;
using TinyRiders;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>Editor-authored, collision-free painted racing floor for the collector shop.</summary>
public static class RacingFloorBuilder
{
    public const string ArtRoot = "Assets/TinyRiders/Art/RacingFloor";
    public const string DecorationName = "Racing Floor Graphics";
    const float TrackY = .003f;
    const float PaintY = .006f;
    const float HalfWidth = 7.5f;
    const float HalfDepth = 9f;
    const float Radius = 2.4f;
    const float LaneWidth = 2.4f;

    [MenuItem("Tools/Tiny Riders/Collector Shop/Apply Racing Floor")]
    public static void ApplyToCurrentRoom()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Apply the racing floor in Edit mode.");
        var scene = SceneManager.GetActiveScene();
        if (scene.path != CompactCollectionBuilder.ScenePath)
            throw new InvalidOperationException("Open the collector Level scene first.");
        var session = Object.FindFirstObjectByType<ThemedCollectionSession>();
        var interior = scene.GetRootGameObjects().Single(g => g.name == "Car Store Generated")
            .transform.Find("Crowded Shop Interior");
        if (session == null || interior == null)
            throw new InvalidOperationException("The crowded collector room is required.");
        Apply(interior, session.roomLayout);
        foreach (string guid in AssetDatabase.FindAssets("", new[] { ArtRoot }))
            AssetDatabase.SaveAssetIfDirty(AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guid)));
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    /// <summary>Shared by the targeted migration and the complete room builder.</summary>
    public static void Apply(Transform interior, CollectorRoomLayout layout)
    {
        if (layout == null || Vector2.Distance(layout.roomSize, new Vector2(24, 26)) > .01f)
            throw new InvalidOperationException("The racing floor is authored for the 24 x 26 collector room.");
        var floor = interior.Find("Store Floor");
        if (floor == null || floor.GetComponent<BoxCollider>() == null)
            throw new InvalidOperationException("The original solid Store Floor must exist.");
        // Validate the authored background before changing any scene objects or materials.
        const string backgroundPath = ArtRoot + "/Toy Town Background.png";
        var background = AssetDatabase.LoadAssetAtPath<Texture2D>(backgroundPath);
        if (background == null)
            throw new InvalidOperationException("Generate the Toy Town Background.png using ArtSource/RacingFloor/Scripts/build_toy_town.py first.");
        var importer = (TextureImporter)AssetImporter.GetAtPath(backgroundPath);
        if (!importer.mipmapEnabled || importer.anisoLevel != 8 || importer.maxTextureSize != 2048
            || importer.wrapMode != TextureWrapMode.Clamp || importer.filterMode != FilterMode.Trilinear
            || !importer.sRGBTexture || importer.alphaSource != TextureImporterAlphaSource.None
            || importer.textureCompression != TextureImporterCompression.CompressedHQ || importer.isReadable)
        {
            importer.mipmapEnabled = true;
            importer.anisoLevel = 8;
            importer.maxTextureSize = 2048;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Trilinear;
            importer.sRGBTexture = true;
            importer.alphaSource = TextureImporterAlphaSource.None;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.isReadable = false;
            importer.SaveAndReimport();
            background = AssetDatabase.LoadAssetAtPath<Texture2D>(backgroundPath);
        }
        EnsureFolder(ArtRoot);
        // Calibrated beneath the shop's bright ambient lighting so cream retains its warm tint.
        var cream = Material("Warm Cream Satin", "B2AA96", .29f);
        var teal = Material("Teal Track Satin", "568386", .32f);
        var ivory = Material("Ivory Lane Paint", "F4ECD8", .27f);
        var coral = Material("Coral Corner Paint", "D88570", .28f);
        var ink = Material("Deep Teal Trim", "355F64", .3f);
        // The room's strong ambient light needs a neutral tint to retain the printed palette.
        var town = Material("Toy Town Satin", "B3B3B3", .26f);
        town.SetTexture("_BaseMap", background);
        town.SetTextureScale("_BaseMap", Vector2.one);
        town.SetTextureOffset("_BaseMap", Vector2.zero);
        EditorUtility.SetDirty(town);
        var grain = SurfaceGrain();
        cream.SetTexture("_BaseMap", grain);
        cream.SetTextureScale("_BaseMap", new Vector2(12.3f, 13.3f));
        teal.SetTexture("_BaseMap", grain);
        teal.SetTextureScale("_BaseMap", Vector2.one * .5f);
        EditorUtility.SetDirty(cream);
        EditorUtility.SetDirty(teal);
        // Change the renderer only. The collider, transform, layer and physics material stay intact.
        Undo.RecordObject(floor.GetComponent<Renderer>(), "Apply racing floor material");
        floor.GetComponent<Renderer>().sharedMaterial = cream;

        var root = interior.Find(DecorationName);
        if (root == null)
        {
            root = new GameObject(DecorationName).transform;
            Undo.RegisterCreatedObjectUndo(root.gameObject, "Create racing floor graphics");
            root.SetParent(interior, false);
        }
        root.SetPositionAndRotation(layout.origin, Quaternion.identity);
        root.localScale = Vector3.one;
        root.gameObject.layer = 2; // Ignore Raycast: painted graphics never intercept picking.

        // Full-sheet artwork is registered to the floor extents; existing track paint overlays it.
        var townSurface = new Geometry();
        townSurface.Rect(-12.3f, -13.3f, 12.3f, 13.3f, .001f);
        for (int i = 0; i < townSurface.uvs.Count; i++)
        {
            var p = townSurface.uvs[i];
            townSurface.uvs[i] = new Vector2((p.x + 12.3f) / 24.6f, (p.y + 13.3f) / 26.6f);
        }
        Publish(root, "Toy Town Background", townSurface, town);

        var road = new Geometry();
        Band(road, -.5f * LaneWidth, .5f * LaneWidth, TrackY);
        road.Rect(-1.2f, -7.8f, 1.2f, 7.8f, TrackY);

        var lines = new Geometry();
        // Continuous outer edge, inner edge interrupted at the two central-straight junctions.
        Band(lines, 1.085f, 1.135f, PaintY);
        Band(lines, -1.135f, -1.085f, PaintY, true);
        lines.Rect(-1.135f, -7.8f, -1.085f, 7.8f, PaintY);
        lines.Rect(1.085f, -7.8f, 1.135f, 7.8f, PaintY);
        var center = RoundedLoop(0);
        float perimeter = Length(center);
        int dashCount = Mathf.RoundToInt(perimeter / 1.8f);
        for (int i = 0; i < dashCount; i++)
        {
            float distance = (i + .5f) * perimeter / dashCount;
            var p = Along(center, distance);
            if (Mathf.Abs(p.x) < 1.8f && Mathf.Abs(p.y) > 8.8f) continue;
            Dash(lines, center, distance, .72f, .065f);
        }
        for (float z = -2.8f; z < 7f; z += 1.8f)
            lines.Rect(-.0325f, z, .0325f, z + .72f, PaintY);

        // Small two-row start / finish checker; track paint supplies the darker squares.
        for (int row = 0; row < 2; row++)
            for (int col = 0; col < 10; col++)
                if ((row + col) % 2 == 0)
                    lines.Rect(-1.1f + col * .22f, -9.65f + row * .22f,
                        -.88f + col * .22f, -9.43f + row * .22f, PaintY);
        for (int i = 0; i < 3; i++)
        {
            float z = -7.1f + i * 1.25f;
            float x = i % 2 == 0 ? -.78f : .3f;
            lines.Rect(x, z, x + .48f, z + .045f, PaintY);
            lines.Rect(x, z, x + .045f, z + .34f, PaintY);
            lines.Rect(x + .435f, z, x + .48f, z + .34f, PaintY);
        }

        var accents = new Geometry();
        // Short coral curb segments float outside the ivory edge without looking like obstacles.
        for (int corner = 0; corner < 4; corner++)
            for (int segment = 0; segment < 3; segment++)
                ArcBand(accents, corner, corner * 90 + 22 + segment * 16,
                    corner * 90 + 32 + segment * 16, 1.25f, 1.40f, TrackY);

        var trim = new Geometry();
        trim.Rect(-11.85f, -12.85f, 11.85f, -12.79f, TrackY);
        trim.Rect(-11.85f, 12.79f, 11.85f, 12.85f, TrackY);
        trim.Rect(-11.85f, -12.79f, -11.79f, 12.79f, TrackY);
        trim.Rect(11.79f, -12.79f, 11.85f, 12.79f, TrackY);
        // Slim end caps frame the checker without adding words or competing signage.
        trim.Rect(-1.2f, -9.73f, 1.2f, -9.68f, PaintY);
        trim.Rect(-1.2f, -9.18f, 1.2f, -9.13f, PaintY);

        Publish(root, "Track", road, teal);
        Publish(root, "Lane Markings", lines, ivory);
        Publish(root, "Coral Curbs", accents, coral);
        Publish(root, "Perimeter Trim", trim, ink);
        EditorUtility.SetDirty(floor.GetComponent<Renderer>());
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = path.Substring(0, path.LastIndexOf('/'));
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, path.Substring(path.LastIndexOf('/') + 1));
    }

    static Material Material(string name, string hex, float smoothness)
    {
        string path = ArtRoot + "/" + name + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
            AssetDatabase.CreateAsset(mat, path);
        }
        ColorUtility.TryParseHtmlString("#" + hex, out var color);
        mat.SetColor("_BaseColor", color);
        mat.SetFloat("_Metallic", 0);
        mat.SetFloat("_Smoothness", smoothness);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static Texture2D SurfaceGrain()
    {
        const string path = ArtRoot + "/Satin Surface Grain.asset";
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (texture != null) return texture;
        const int size = 128;
        texture = new Texture2D(size, size, TextureFormat.RGB24, true, false)
        { name = "Satin Surface Grain", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear, anisoLevel = 4 };
        var pixels = new Color[size * size];
        var random = new System.Random(719);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                // Periodic low-amplitude variation; no seams and no gritty asphalt noise.
                float wave = Mathf.Sin(x * 2 * Mathf.PI / size) * Mathf.Cos(y * 4 * Mathf.PI / size);
                float value = .982f + wave * .008f + (float)random.NextDouble() * .009f;
                pixels[y * size + x] = new Color(value, value, value, 1);
            }
        texture.SetPixels(pixels);
        texture.Apply(true, true);
        AssetDatabase.CreateAsset(texture, path);
        return texture;
    }

    static Vector2 Corner(int corner) => new Vector2(
        (corner == 0 || corner == 3 ? 1 : -1) * (HalfWidth - Radius),
        (corner < 2 ? 1 : -1) * (HalfDepth - Radius));

    static List<Vector2> RoundedLoop(float offset)
    {
        var points = new List<Vector2>();
        for (int corner = 0; corner < 4; corner++)
            for (int step = 0; step <= 32; step++)
            {
                float angle = (corner * 90 + step * 90f / 32) * Mathf.Deg2Rad;
                points.Add(Corner(corner) + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (Radius + offset));
            }
        return points;
    }

    static void Band(Geometry geo, float inside, float outside, float y, bool junctionGaps = false)
    {
        var a = RoundedLoop(inside); var b = RoundedLoop(outside);
        for (int i = 0; i < a.Count; i++)
        {
            int j = (i + 1) % a.Count;
            if (junctionGaps && Mathf.Abs(a[i].y - a[j].y) < .001f && a[i].x * a[j].x < 0)
            {
                float z0 = Mathf.Min(a[i].y, b[i].y), z1 = Mathf.Max(a[i].y, b[i].y);
                geo.Rect(Mathf.Min(a[i].x, a[j].x), z0, -1.135f, z1, y);
                geo.Rect(1.135f, z0, Mathf.Max(a[i].x, a[j].x), z1, y);
            }
            else geo.Quad(a[i], a[j], b[j], b[i], y);
        }
    }

    static void ArcBand(Geometry geo, int corner, float from, float to, float inside, float outside, float y)
    {
        for (int step = 0; step < 8; step++)
        {
            float t0 = Mathf.Lerp(from, to, step / 8f) * Mathf.Deg2Rad;
            float t1 = Mathf.Lerp(from, to, (step + 1) / 8f) * Mathf.Deg2Rad;
            var a = new Vector2(Mathf.Cos(t0), Mathf.Sin(t0));
            var b = new Vector2(Mathf.Cos(t1), Mathf.Sin(t1));
            var center = Corner(corner);
            geo.Quad(center + a * (Radius + inside), center + b * (Radius + inside),
                center + b * (Radius + outside), center + a * (Radius + outside), y);
        }
    }

    static float Length(List<Vector2> points)
    {
        float total = 0;
        for (int i = 0; i < points.Count; i++) total += Vector2.Distance(points[i], points[(i + 1) % points.Count]);
        return total;
    }

    static Vector2 Along(List<Vector2> points, float distance)
    {
        distance = Mathf.Repeat(distance, Length(points));
        for (int i = 0; i < points.Count; i++)
        {
            var a = points[i]; var b = points[(i + 1) % points.Count];
            float length = Vector2.Distance(a, b);
            if (distance <= length) return Vector2.Lerp(a, b, distance / length);
            distance -= length;
        }
        return points[0];
    }

    static void Dash(Geometry geo, List<Vector2> path, float center, float length, float width)
    {
        for (int i = 0; i < 6; i++)
        {
            float d0 = center - length * .5f + length * i / 6;
            float d1 = center - length * .5f + length * (i + 1) / 6;
            var a = Along(path, d0); var b = Along(path, d1);
            var tangent = (b - a).normalized; var n = new Vector2(-tangent.y, tangent.x) * width * .5f;
            geo.Quad(a - n, b - n, b + n, a + n, PaintY);
        }
    }

    static void Publish(Transform root, string name, Geometry geometry, Material material)
    {
        string path = ArtRoot + "/" + name + ".asset";
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (mesh == null) { mesh = new Mesh { name = name }; AssetDatabase.CreateAsset(mesh, path); }
        mesh.Clear();
        mesh.SetVertices(geometry.vertices);
        mesh.SetUVs(0, geometry.uvs);
        mesh.SetTriangles(geometry.triangles, 0);
        mesh.RecalculateNormals(); mesh.RecalculateBounds(); mesh.RecalculateTangents();
        EditorUtility.SetDirty(mesh);
        var child = root.Find(name);
        if (child == null)
        {
            child = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)).transform;
            child.SetParent(root, false);
        }
        child.localPosition = Vector3.zero; child.localRotation = Quaternion.identity; child.localScale = Vector3.one;
        child.gameObject.layer = 2;
        // Preserve shared buffers (no per-instance static-batching copies).
        GameObjectUtility.SetStaticEditorFlags(child.gameObject, StaticEditorFlags.OccludeeStatic);
        child.GetComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = child.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = true;
    }

    sealed class Geometry
    {
        public readonly List<Vector3> vertices = new List<Vector3>();
        public readonly List<Vector2> uvs = new List<Vector2>();
        public readonly List<int> triangles = new List<int>();
        public void Rect(float x0, float z0, float x1, float z1, float y) =>
            Quad(new Vector2(x0,z0), new Vector2(x1,z0), new Vector2(x1,z1), new Vector2(x0,z1), y);
        public void Quad(Vector2 a, Vector2 b, Vector2 c, Vector2 d, float y)
        {
            int first = vertices.Count;
            foreach (var p in new[] { a, b, c, d }) { vertices.Add(new Vector3(p.x,y,p.y)); uvs.Add(p); }
            bool up = Vector3.Cross(vertices[first+1]-vertices[first], vertices[first+2]-vertices[first]).y > 0;
            triangles.AddRange(up ? new[] {first,first+1,first+2,first,first+2,first+3}
                : new[] {first,first+2,first+1,first,first+3,first+2});
        }
    }
}
