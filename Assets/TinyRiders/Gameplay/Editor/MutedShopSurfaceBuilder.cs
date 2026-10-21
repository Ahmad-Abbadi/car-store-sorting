using System;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>Decorative shop surfaces, authored entirely in the Editor.</summary>
public static class MutedShopSurfaceBuilder
{
    public const string Root = "Assets/TinyRiders/Art/MutedShopSurfaces";
    public const string Evidence = "IntegrationEvidence/CompactScatter/MutedSurfaces";

    public static Material CeilingMaterial(bool dark) => AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/Slate Ceiling " + (dark ? "Dark" : "Bright") + ".mat");

    [MenuItem("Tools/Tiny Riders/Collector Shop/Apply Muted Teal And Slate Surfaces")]
    public static void Apply()
    {
        ApplySurfaces();
        CollectorPromenadeBuilder.FinishLighting();
        CollectorPromenadeBuilder.RefreshLightingReflections();
    }

    public static void ApplySurfaces()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != CompactCollectionBuilder.ScenePath)
            throw new InvalidOperationException("Open Level in Edit mode first.");
        Directory.CreateDirectory(Root + "/Materials"); Directory.CreateDirectory(Root + "/Meshes");
        AssetDatabase.Refresh();
        var plaster = Import("Painted Plaster BaseColor");
        var acoustic = Import("Slate Acoustic BaseColor");
        var upper = Material("Teal Grey Plaster", plaster, "Wall Plaster", Color.white, Color.black);
        var lower = Material("Dusty Teal Plaster", plaster, "Wall Plaster", new Color(.62f, .82f, .80f), Color.black);
        Material("Slate Ceiling Bright", acoustic, "Ceiling Acoustic", Color.white, new Color(.8f, .8f, .8f));
        Material("Slate Ceiling Dark", acoustic, "Ceiling Acoustic", Color.white, new Color(.10f, .11f, .12f));
        var room = GameObject.Find("Collector Promenade").transform;
        foreach (var renderer in room.GetComponentsInChildren<MeshRenderer>())
        {
            bool ceiling = renderer.name == "Store Ceiling";
            if (!ceiling && renderer.name != "Gallery wall" && renderer.name != "Teal dado") continue;
            renderer.sharedMaterial = ceiling ? CeilingMaterial(LightingThemeSettings.Current == LightingTheme.Dark)
                : renderer.name == "Teal dado" ? lower : upper;
            ApplyMetricUVs(renderer);
            EditorUtility.SetDirty(renderer);
        }
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
    }

    static Texture2D Import(string name)
    {
        string path = Root + "/" + name + ".png";
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) throw new InvalidOperationException("Generated surface texture missing: " + path);
        // Preserve the generated source; normalize the GPU texture to a 2048-square power-of-two asset.
        importer.textureType = TextureImporterType.Default; importer.sRGBTexture = true;
        importer.wrapMode = TextureWrapMode.Repeat; importer.filterMode = FilterMode.Trilinear;
        importer.mipmapEnabled = true; importer.anisoLevel = 8; importer.maxTextureSize = 2048;
        importer.npotScale = TextureImporterNPOTScale.ToLarger;
        importer.alphaSource = TextureImporterAlphaSource.None; importer.textureCompression = TextureImporterCompression.CompressedHQ;
        importer.isReadable = false; importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    static Material Material(string name, Texture2D color, string detail, Color tint, Color emission)
    {
        string path = Root + "/Materials/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name }; AssetDatabase.CreateAsset(material, path); }
        material.SetTexture("_BaseMap", color); material.SetColor("_BaseColor", tint);
        material.SetTextureScale("_BaseMap", Vector2.one); material.SetTextureOffset("_BaseMap", Vector2.zero);
        material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(ShopInteriorPolishBuilder.ArtRoot + "/Textures/" + detail + " Normal.png"));
        material.EnableKeyword("_NORMALMAP"); material.SetFloat("_BumpScale", .12f);
        material.SetTexture("_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>(ShopInteriorPolishBuilder.ArtRoot + "/Textures/" + detail + " Smoothness.png"));
        material.EnableKeyword("_METALLICSPECGLOSSMAP"); material.SetFloat("_Metallic", 0); material.SetFloat("_Smoothness", .22f);
        material.SetFloat("_SpecularHighlights", 0); material.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
        material.SetFloat("_EnvironmentReflections", 0); material.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
        material.SetTexture("_EmissionMap", color); material.SetColor("_EmissionColor", emission);
        if (emission.maxColorComponent > 0) material.EnableKeyword("_EMISSION"); else material.DisableKeyword("_EMISSION");
        material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
        EditorUtility.SetDirty(material); return material;
    }

    static void ApplyMetricUVs(MeshRenderer renderer)
    {
        var filter = renderer.GetComponent<MeshFilter>();
        var p = renderer.transform.localPosition;
        string id = renderer.name + " " + p.x.ToString("0.###", CultureInfo.InvariantCulture) + " "
            + p.y.ToString("0.###", CultureInfo.InvariantCulture) + " " + p.z.ToString("0.###", CultureInfo.InvariantCulture);
        string path = Root + "/Meshes/" + id + ".asset";
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (mesh == null) { mesh = Object.Instantiate(filter.sharedMesh); mesh.name = "Muted Surface " + id; AssetDatabase.CreateAsset(mesh, path); }
        var vertices = mesh.vertices; var normals = mesh.normals; var uv = new Vector2[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            var world = renderer.transform.TransformPoint(vertices[i]) - new Vector3(500, 0, 0);
            var n = renderer.transform.TransformDirection(normals[i]);
            // One texture repeat per two metres, on every face and through both alcoves.
            uv[i] = Mathf.Abs(n.y) > .5f ? new Vector2(world.x, world.z) * .5f
                : Mathf.Abs(n.x) > .5f ? new Vector2(world.z, world.y) * .5f : new Vector2(world.x, world.y) * .5f;
        }
        mesh.uv = uv; mesh.RecalculateTangents(); filter.sharedMesh = mesh;
        MeshUtility.SetMeshCompression(mesh, ModelImporterMeshCompression.Off);
        EditorUtility.SetDirty(mesh); EditorUtility.SetDirty(filter);
    }
}
