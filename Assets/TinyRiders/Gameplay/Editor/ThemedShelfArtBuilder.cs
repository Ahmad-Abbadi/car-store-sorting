using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using TinyRiders;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>Import Blender-authored cabinet art and replace presentation without changing gameplay identity.</summary>
public static class ThemedShelfArtBuilder
{
    public const string Root = "Assets/TinyRiders/Art/Shelves/Themed";
    public const string CatalogPath = Root + "/ThemedShelfArtCatalog.asset";
    public const string Evidence = "IntegrationEvidence/SignatureShelves";
    public static ThemedShelfArtCatalog Catalog => AssetDatabase.LoadAssetAtPath<ThemedShelfArtCatalog>(CatalogPath);
    public static GameObject ArtForTheme(string id)
    {
        var catalog = Catalog;
        if (catalog == null) return ExactThemeShelfBuilder.EnsurePrefab();
        var entry = catalog.Find(id);
        if (entry == null || entry.prefab == null) throw new InvalidOperationException("Missing themed shelf art: " + id);
        return entry.prefab;
    }
    public static bool HasThemedArt(SortingShelf shelf)
    {
        var art = shelf.transform.Find("TR_Shelf_Base_01");
        return art != null && art.GetComponentsInChildren<Transform>(true).Any(t => t.name == "ThemeDecorations");
    }
    static Material Material(string name, Color color, float metallic, float smoothness)
    {
        string path = Root + "/Materials/" + name + ".mat";
        var result = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (result == null) { result = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(result, path); }
        result.name = name; result.SetColor("_BaseColor", color); result.SetFloat("_Metallic", metallic); result.SetFloat("_Smoothness", smoothness);
        result.enableInstancing = true; EditorUtility.SetDirty(result); return result;
    }
    static Texture2D Texture(string path, bool srgb, int size = 1024, bool normal = false)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
        importer.sRGBTexture = srgb; importer.maxTextureSize = size; importer.mipmapEnabled = true;
        importer.filterMode = FilterMode.Trilinear; importer.anisoLevel = 4;
        importer.alphaSource = srgb ? TextureImporterAlphaSource.None : TextureImporterAlphaSource.FromInput;
        importer.wrapMode = TextureWrapMode.Clamp; importer.textureCompression = TextureImporterCompression.Compressed;
        importer.SaveAndReimport(); return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
    static Transform Find(Transform root, string name) => root.GetComponentsInChildren<Transform>(true).Single(t => t.name == name);

    [MenuItem("Tools/Tiny Riders/Themed Shelves/Import Blender Art")]
    public static void ImportAll()
    {
        ImportThemes(null);
    }

    public static void ImportThemes(string[] themeIds)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play mode first.");
        AssetDatabase.Refresh();
        var carCatalog = AssetDatabase.LoadAssetAtPath<ThemedCarCatalog>(CompactCollectionBuilder.CatalogPath);
        var themes = carCatalog.themes.Where(t => t.enabled && (themeIds == null || themeIds.Contains(t.id))).ToArray();
        // Verify the entire Blender output before replacing any Unity assets.
        foreach (var theme in themes)
            foreach (var file in new[] { Root + "/Models/" + theme.id + ".fbx", Root + "/Textures/" + theme.id + "_BaseColor.png", Root + "/Textures/" + theme.id + "_Surface.png", "ArtSource/Shelves/SignatureShelves/Reports/" + theme.id + ".json", Root + "/Textures/" + theme.id + "_Signature_BaseColor.png", Root + "/Textures/" + theme.id + "_Signature_Normal.png", Root + "/Textures/" + theme.id + "_Signature_Surface.png" })
                if (!File.Exists(file)) throw new FileNotFoundException("Incomplete Blender shelf output", file);
        var frame = Material("Shelf_CommonFrame", new Color(.025f,.038f,.055f), .1f, .52f);
        var tray = Material("Shelf_CommonTray", new Color(.36f,.43f,.47f), .22f, .6f);
        var entries = Catalog == null ? new List<ThemedShelfArtCatalog.Entry>() : Catalog.entries.Where(e => !themes.Any(t => t.id == e.themeId)).ToList();
        foreach (var theme in themes.OrderBy(t => t.id, StringComparer.Ordinal))
        {
            var report = JObject.Parse(File.ReadAllText("ArtSource/Shelves/SignatureShelves/Reports/" + theme.id + ".json"));
            int decoration = (int)report["decorationTriangles"];
            if (decoration > 8000 || (int)report["slots"] != 15 || (int)report["signatureRevision"] != 1) throw new InvalidOperationException("Blender budget or slot mismatch: " + theme.id);
            var paint = Material("Shelf_" + theme.id, Color.white, 0, 1);
            paint.SetTexture("_BaseMap", Texture(Root + "/Textures/" + theme.id + "_BaseColor.png", true));
            paint.SetTexture("_MetallicGlossMap", Texture(Root + "/Textures/" + theme.id + "_Surface.png", false));
            paint.EnableKeyword("_METALLICSPECGLOSSMAP");
            var signature = Material("Signature_" + theme.id, Color.white, 0, 1);
            signature.SetTexture("_BaseMap", Texture(Root + "/Textures/" + theme.id + "_Signature_BaseColor.png", true, 2048));
            signature.SetTexture("_BumpMap", Texture(Root + "/Textures/" + theme.id + "_Signature_Normal.png", false, 2048, true));
            signature.SetFloat("_BumpScale", .35f); signature.EnableKeyword("_NORMALMAP");
            signature.SetTexture("_MetallicGlossMap", Texture(Root + "/Textures/" + theme.id + "_Signature_Surface.png", false, 2048));
            signature.EnableKeyword("_METALLICSPECGLOSSMAP");
            var materials = new[] { frame, tray, paint, signature };
            string modelPath = Root + "/Models/" + theme.id + ".fbx";
            var importer = (ModelImporter)AssetImporter.GetAtPath(modelPath);
            importer.globalScale = 1; importer.isReadable = true; importer.importAnimation = false; importer.addCollider = false; importer.SaveAndReimport();
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            var imported = (GameObject)PrefabUtility.InstantiatePrefab(source);
            var cabinet = (GameObject)PrefabUtility.InstantiatePrefab(ExactThemeShelfBuilder.EnsurePrefab());
            try
            {
                PrefabUtility.UnpackPrefabInstance(imported, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                PrefabUtility.UnpackPrefabInstance(cabinet, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                cabinet.name = "TR_Shelf_Base_01";
                Object.DestroyImmediate(cabinet.transform.Find("Geometry").gameObject);
                var geometry = Find(imported.transform, "Geometry");
                geometry.SetParent(cabinet.transform, true);
                if (Find(geometry, "BackPanel").GetComponentsInChildren<Renderer>().First().bounds.center.z > Find(geometry, "FrontLip_01").GetComponent<Renderer>().bounds.center.z)
                    geometry.RotateAround(Vector3.zero, Vector3.up, 180);
                foreach (var renderer in geometry.GetComponentsInChildren<MeshRenderer>(true))
                {
                    renderer.sharedMaterials = renderer.sharedMaterials.Select(m => materials.SingleOrDefault(candidate => m != null && candidate.name == m.name)
                        ?? throw new InvalidOperationException("Unmapped Blender material: " + (m == null ? "null" : m.name) + " / " + theme.id)).ToArray();
                    renderer.enabled = true;
                }
                if (ExactThemeShelfBuilder.Markers(cabinet).Length != 15) throw new InvalidOperationException("Placement marker mismatch.");
                string prefabPath = Root + "/Prefabs/Shelf_" + theme.id + ".prefab";
                var prefab = PrefabUtility.SaveAsPrefabAsset(cabinet, prefabPath);
                entries.Add(new ThemedShelfArtCatalog.Entry {themeId=theme.id,prefab=prefab,materials=materials,decorationTriangles=decoration,
                    signatureRevision=1,topperTriangles=(int)report["topperTriangles"],postHeightMeters=(float)report["postHeightMeters"],
                    topperSizeMeters=new Vector3((float)report["topperSizeMeters"][0],(float)report["topperSizeMeters"][2],(float)report["topperSizeMeters"][1])});
            }
            finally { Object.DestroyImmediate(cabinet); Object.DestroyImmediate(imported); }
        }
        var catalog = Catalog;
        if (catalog == null) { catalog = ScriptableObject.CreateInstance<ThemedShelfArtCatalog>(); AssetDatabase.CreateAsset(catalog, CatalogPath); }
        catalog.entries = entries.OrderBy(e=>e.themeId,StringComparer.Ordinal).ToArray(); EditorUtility.SetDirty(catalog); AssetDatabase.SaveAssets();
        Debug.Log("THEMED_SHELF_ART_IMPORTED: " + entries.Count + " Blender designs");
    }

    [MenuItem("Tools/Tiny Riders/Themed Shelves/Apply To Current Room")]
    public static void Apply()
    {
        ApplyThemes(null);
    }

    public static void ApplyThemes(string[] themeIds)
    {
        var scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != CompactCollectionBuilder.ScenePath) throw new InvalidOperationException("Open Level in Edit mode.");
        ExactThemeShelfBuilder.ValidateScene();
        var shelves = Object.FindObjectsByType<SortingShelf>(FindObjectsSortMode.None).Where(s=>themeIds==null||themeIds.Contains(s.GetComponent<ThemeShelfBinding>().themeId)).OrderBy(s=>s.SaveId,StringComparer.Ordinal).ToArray();
        if (Catalog == null || Catalog.entries.Length != 40) throw new InvalidOperationException("Import all 40 designs first.");
        Directory.CreateDirectory(Evidence);
        string backup=Evidence + "/Level_before_signature_art.unity";
        if (!File.Exists(backup)) EditorSceneManager.SaveScene(scene, backup, true);
        foreach (var shelf in shelves)
        {
            var old = shelf.transform.Find("TR_Shelf_Base_01");
            var prefab = ArtForTheme(shelf.GetComponent<ThemeShelfBinding>().themeId);
            var art = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene); art.name = "TR_Shelf_Base_01";
            art.transform.SetParent(shelf.transform,false); art.transform.localPosition=old.localPosition;art.transform.localRotation=old.localRotation;art.transform.localScale=old.localScale;
            foreach (var t in art.GetComponentsInChildren<Transform>(true)) {t.gameObject.layer=2;PrefabUtility.RecordPrefabInstancePropertyModifications(t.gameObject);}
            var markers=ExactThemeShelfBuilder.Markers(art); var so=new SerializedObject(shelf);var spots=so.FindProperty("placingSpots");
            for(int i=0;i<shelf.SpotCount;i++)
            {
                var marker=markers.Single(t=>t.name==shelf.GetSpot(i).name);
                if(Vector3.Distance(marker.position,shelf.GetSpot(i).position)>.0001f||Quaternion.Angle(marker.rotation,shelf.GetSpot(i).rotation)>.01f)
                    throw new InvalidOperationException("Themed art moved a gameplay slot: "+shelf.SaveId);
                spots.GetArrayElementAtIndex(i).objectReferenceValue=marker;
            }
            so.ApplyModifiedPropertiesWithoutUndo();Object.DestroyImmediate(old.gameObject);
            var label=shelf.transform.Find("Theme Header").GetComponent<TextMeshPro>();
            label.rectTransform.sizeDelta=new Vector2(.88f,label.rectTransform.sizeDelta.y);
            label.enableAutoSizing=true;label.fontSizeMax=1.05f;label.fontSizeMin=.58f;EditorUtility.SetDirty(label);
        }
        ThemedShelfBatcher.Apply(shelves);
        ThemedShelfSignClearance.Apply();
        ExactThemeShelfBuilder.ValidateScene();
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        Audit();
    }

    public static void Audit()
    {
        ExactThemeShelfBuilder.ValidateScene();
        var shelves=Object.FindObjectsByType<SortingShelf>(FindObjectsSortMode.None);
        if(shelves.Length!=80||shelves.Any(s=>!HasThemedArt(s)))throw new InvalidOperationException("Themed shelf coverage mismatch.");
        foreach(var group in shelves.GroupBy(s=>s.GetComponent<ThemeShelfBinding>().themeId))
        {
            if(group.Count()!=2)throw new InvalidOperationException("Theme pair missing: "+group.Key);
            var a=group.First().transform.Find("Batched Shelf Art").GetComponentsInChildren<MeshFilter>();
            var b=group.Last().transform.Find("Batched Shelf Art").GetComponentsInChildren<MeshFilter>();
            int expected=Catalog.Find(group.Key).signatureRevision>0?4:3;
            if(a.Length!=expected||!a.Select(m=>m.sharedMesh).SequenceEqual(b.Select(m=>m.sharedMesh)))throw new InvalidOperationException("Theme pair does not share batched meshes: "+group.Key);
            foreach(var shelf in group)
            {
                var art=shelf.transform.Find("TR_Shelf_Base_01");
                if(art.GetComponentsInChildren<Collider>(true).Length!=0||art.GetComponentsInChildren<Light>(true).Length!=0)throw new InvalidOperationException("Unexpected art collider or light.");
            }
        }
        var renderers=shelves.SelectMany(s=>s.GetComponentsInChildren<MeshRenderer>()).Where(r=>r.enabled).ToArray();
        var textures=renderers.SelectMany(r=>r.sharedMaterials).Distinct().SelectMany(m=>m.GetTexturePropertyNames().Select(n=>m.GetTexture(n))).Where(t=>t!=null).Distinct().ToArray();
        File.WriteAllText(Evidence+"/rendering_after.json",Newtonsoft.Json.JsonConvert.SerializeObject(new {shelves=shelves.Length,themes=Catalog.entries.Length,slots=shelves.Sum(s=>s.SpotCount),signatureShelves=shelves.Count(s=>Catalog.Find(s.GetComponent<ThemeShelfBinding>().themeId).signatureRevision>0),renderers=renderers.Length,triangles=renderers.Sum(r=>r.GetComponent<MeshFilter>().sharedMesh.triangles.Length/3),materials=renderers.SelectMany(r=>r.sharedMaterials).Distinct().Count(),textures=textures.Length,textureBytes=textures.Sum(t=>UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(t)),maximumDecorationTriangles=Catalog.entries.Max(e=>e.decorationTriangles),pairsShareMeshes=true},Newtonsoft.Json.Formatting.Indented));
    }
}
