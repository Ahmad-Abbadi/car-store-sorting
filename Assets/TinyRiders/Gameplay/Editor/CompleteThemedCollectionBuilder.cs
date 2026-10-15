using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using TinyRiders;
using TinyRiders.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Adds the missing cross-product without regenerating any existing themed artwork.</summary>
public static class CompleteThemedCollectionBuilder
{
    const string Art = "Assets/TinyRiders/ThemedCollection";
    const string Data = Art + "/Data/Themes/";
    const string Gameplay = "Assets/TinyRiders/Gameplay";
    const string Release = "complete_15_shapes_v1";
    static bool Accessory(Transform t)
    {
        for (; t != null; t = t.parent) if (t.name == "ThemeAccessories") return true;
        return false;
    }
    static Bounds BoundsOf(GameObject root)
    {
        var renderers = root.GetComponentsInChildren<Renderer>().Where(r => r.enabled).ToArray();
        if (renderers.Length == 0) throw new Exception("No visible car: " + root.name);
        var bounds = renderers[0].bounds;
        foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
        return bounds;
    }
    static T Required<T>(string path) where T : UnityEngine.Object
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null) throw new Exception("Missing required asset: " + path);
        return asset;
    }
    static Material Material(string theme, string role) => Required<Material>(Art + "/Materials/Themes/" + theme + "/" + role + ".mat");

    static Material Decals(string theme, string baseId)
    {
        string target = Art + "/Materials/Themes/" + theme + "/" + baseId + "_Decals.mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(target);
        if (existing != null) return existing;
        var material = new Material(Required<Material>(AssetDatabase.FindAssets("t:Material", new[] { Art + "/Materials/Themes/" + theme })
            .Select(AssetDatabase.GUIDToAssetPath).First(p => p.EndsWith("_Decals.mat", StringComparison.Ordinal))));
        string texturePath = Art + "/Textures/Themes/" + theme + "/" + baseId + "_ThemeDecals.png";
        var importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);
        if (importer == null) throw new Exception("Generate missing UV artwork first: " + texturePath);
        importer.alphaIsTransparency = true; importer.mipmapEnabled = true;
        importer.wrapMode = TextureWrapMode.Repeat; importer.textureCompression = TextureImporterCompression.Compressed;
        var web = importer.GetPlatformTextureSettings("WebGL");
        web.overridden = true; web.maxTextureSize = 512; web.format = TextureImporterFormat.DXT5;
        importer.SetPlatformTextureSettings(web); importer.SaveAndReimport();
        var texture = Required<Texture2D>(texturePath);
        material.SetTexture("_BaseMap", texture);
        if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
        material.name = baseId + "_Decals"; AssetDatabase.CreateAsset(material, target);
        return material;
    }

    static void AssignMaterials(GameObject root, string theme, int car)
    {
        string baseId = "C" + car.ToString("000");
        var decals = Decals(theme, baseId);
        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true).Where(r => !Accessory(r.transform)))
        {
            bool wheel = false, character = false;
            for (var t = renderer.transform; t != null; t = t.parent)
            { wheel |= t.name.Contains("Wheel_"); character |= t.name.StartsWith("Racer_", StringComparison.Ordinal); }
            var materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i] == null) throw new Exception("Missing source material " + root.name);
                string path = AssetDatabase.GetAssetPath(materials[i]);
                if (character && !path.StartsWith(Art + "/Materials/Themes/", StringComparison.Ordinal)) continue;
                string role = Path.GetFileNameWithoutExtension(path);
                if (role.Contains("Decals")) materials[i] = decals;
                else if (path.StartsWith(Art + "/Materials/Themes/", StringComparison.Ordinal))
                {
                    // Per-base paint roles vary; use the target theme's same material role.
                    string common = role.StartsWith("C", StringComparison.Ordinal) && role.EndsWith("_Paint", StringComparison.Ordinal)
                        ? (new[] {3,4,5,9,12,13,15}.Contains(car) ? "Neutral" : "Paint") : role;
                    var target = AssetDatabase.LoadAssetAtPath<Material>(Art + "/Materials/Themes/" + theme + "/" + common + ".mat");
                    if (target == null && File.Exists(Art + "/Textures/Themes/" + theme + "/" + common + "Palette.png"))
                    {
                        target = new Material(Material(theme, common == "BodyMetal" ? "Paint" : "Neutral")); target.name = common;
                        var texture = Required<Texture2D>(Art + "/Textures/Themes/" + theme + "/" + common + "Palette.png");
                        target.SetTexture("_BaseMap", texture);
                        if (target.HasProperty("_MainTex")) target.SetTexture("_MainTex", texture);
                        AssetDatabase.CreateAsset(target, Art + "/Materials/Themes/" + theme + "/" + common + ".mat");
                    }
                    if (target == null) throw new Exception("Unknown theme material role: " + role);
                    materials[i] = target;
                }
                else if (path.StartsWith(ThemedCarBuilder.Source + "/Materials/", StringComparison.Ordinal))
                {
                    bool secondary = renderer.name.Contains("FrontBumper") || renderer.name.Contains("RearBumper") || renderer.name.Contains("Spoiler");
                    if (role == "Color") materials[i] = Material(theme, wheel ? "WheelAccent" : secondary ? "Secondary" : car == 13 ? "Neutral" : "Paint");
                    else if (role == "Metal") materials[i] = Material(theme, "Trim");
                    else if (role == "Emission") materials[i] = Material(theme, "WheelInset");
                    else if (role.StartsWith("Light_", StringComparison.Ordinal))
                    {
                        var light = AssetDatabase.LoadAssetAtPath<Material>(Art + "/Materials/Themes/" + theme + "/" + role + ".mat");
                        if (light != null) materials[i] = light;
                    }
                }
            }
            renderer.sharedMaterials = materials;
            PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
        }
    }

    // Find a real painted triangle under a vertical ray, avoiding glass, decal planes and wheels.
    static float PaintedHeight(GameObject root, float x, float z)
    {
        float best = float.NegativeInfinity;
        foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>().Where(r => r.enabled && !Accessory(r.transform)))
        {
            var filter = renderer.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null) continue;
            var mesh = filter.sharedMesh; var vertices = mesh.vertices; var materials = renderer.sharedMaterials;
            for (int s = 0; s < Math.Min(mesh.subMeshCount, materials.Length); s++)
            {
                string name = materials[s].name;
                if (name != "Paint" && name != "Neutral" && name != "BodyMetal" && name != "Color" && name != "Metal") continue;
                var triangles = mesh.GetTriangles(s);
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    var a = renderer.transform.TransformPoint(vertices[triangles[i]]);
                    var b = renderer.transform.TransformPoint(vertices[triangles[i+1]]);
                    var c = renderer.transform.TransformPoint(vertices[triangles[i+2]]);
                    float d = (b.z-c.z)*(a.x-c.x)+(c.x-b.x)*(a.z-c.z);
                    if (Mathf.Abs(d) < .000001f) continue;
                    float u = ((b.z-c.z)*(x-c.x)+(c.x-b.x)*(z-c.z))/d;
                    float v = ((c.z-a.z)*(x-c.x)+(a.x-c.x)*(z-c.z))/d;
                    if (u < 0 || v < 0 || u+v > 1) continue;
                    best = Mathf.Max(best, u*a.y+v*b.y+(1-u-v)*c.y);
                }
            }
        }
        if (float.IsNegativeInfinity(best)) throw new Exception("No painted mounting surface: " + root.name);
        return best;
    }

    static void AttachKit(GameObject root, string theme, string baseId, Bounds bodyBounds, JArray profiles)
    {
        var old = root.transform.Find("ThemeAccessories");
        if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
        var group = new GameObject("ThemeAccessories"); group.transform.SetParent(root.transform, false);
        var kit = (GameObject)PrefabUtility.InstantiatePrefab(Required<GameObject>("Assets/TinyRiders/Art/ThemeAccessories/Prefabs/" + theme + "/KIT_" + theme + ".prefab"));
        kit.transform.SetParent(group.transform, false);
        var kitBounds = BoundsOf(kit);
        float scale = Mathf.Min(bodyBounds.size.z*.26f/Mathf.Max(kitBounds.size.x,kitBounds.size.z), bodyBounds.size.x*.65f/kitBounds.size.x, bodyBounds.size.y*.42f/kitBounds.size.y);
        var profile = profiles.FirstOrDefault(p => (string)p["baseCarId"] == baseId);
        if (profile != null)
        {
            kit.transform.localPosition = new Vector3((float)profile["localPosition"][0],(float)profile["localPosition"][1],(float)profile["localPosition"][2]);
            kit.transform.localEulerAngles = new Vector3((float)profile["localRotation"][0],(float)profile["localRotation"][1],(float)profile["localRotation"][2]);
        }
        else
        {
            // C013 is a truck: its cab roof is forward of the rear chassis deck.
            float z = baseId == "C013" ? .72f : -.8f;
            float y = PaintedHeight(root, 0, z);
            float dz = (PaintedHeight(root,0,z+.06f)-PaintedHeight(root,0,z-.06f))/.12f;
            float dx = (PaintedHeight(root,.06f,z)-PaintedHeight(root,-.06f,z))/.12f;
            kit.transform.localPosition = new Vector3(0,y-.015f,z);
            kit.transform.localEulerAngles = new Vector3(-Mathf.Atan(dz)*Mathf.Rad2Deg,0,Mathf.Atan(dx)*Mathf.Rad2Deg);
        }
        kit.transform.localScale = Vector3.one*scale;
        PrefabUtility.RecordPrefabInstancePropertyModifications(kit.transform);
    }

    [MenuItem("Tools/Tiny Riders/Sorting/Complete All 15 Shapes Per Theme")]
    public static void Complete()
    {
        if (EditorApplication.isPlaying) throw new Exception("Exit Play mode first");
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var catalog = Required<ThemedCarCatalog>(Data + "ThemedCarCatalog.asset");
        var portable = JObject.Parse(File.ReadAllText(Data + "variant_catalog.json"));
        var records = (JArray)portable["entries"];
        var profiles = JArray.Parse(File.ReadAllText("TinyRidersProduction/ThemeAccessoryPass/fitting_profiles.json"));
        var recipes = (JArray)JObject.Parse(File.ReadAllText(Data + "theme_recipes.json"))["themes"];
        var entries = catalog.entries.ToList(); var added = new List<string>();
        foreach (var theme in recipes)
        {
            string themeId = (string)theme["id"];
            for (int car = 1; car <= 15; car++)
            {
                string baseId = "C"+car.ToString("000"), id = "TR-"+baseId+"-"+themeId+"-V01";
                if (entries.Any(e => e.collectibleId == id)) continue;
                var reference = entries.FirstOrDefault(e => e.baseCarId == baseId);
                string source = reference != null ? AssetDatabase.GetAssetPath(reference.prefab) : "Assets/TinyRiders/Prefabs/BaseCars/"+baseId+"/"+baseId+".prefab";
                string path = Art+"/Prefabs/Themed/"+themeId+"/"+id+".prefab";
                GameObject root = File.Exists(path) ? PrefabUtility.LoadPrefabContents(path) : (GameObject)PrefabUtility.InstantiatePrefab(Required<GameObject>(source));
                bool loaded = File.Exists(path);
                try
                {
                    if (!loaded) PrefabUtility.UnpackPrefabInstance(root,PrefabUnpackMode.OutermostRoot,InteractionMode.AutomatedAction);
                    root.name = id;
                    var old = root.transform.Find("ThemeAccessories"); if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
                    foreach (var t in root.GetComponentsInChildren<Transform>(true))
                        if (t.name.EndsWith("_Spoiler",StringComparison.Ordinal) || t.name.EndsWith("_Turbine",StringComparison.Ordinal) || t.name.EndsWith("_Engine",StringComparison.Ordinal))
                        { t.gameObject.SetActive(false); PrefabUtility.RecordPrefabInstancePropertyModifications(t.gameObject); }
                    var identity = root.GetComponent<ThemedCarIdentity>() ?? root.AddComponent<ThemedCarIdentity>();
                    identity.collectibleId=id;identity.baseCarId=baseId;identity.themeId=themeId;identity.colorwayId="V01";identity.contentRevision=1;
                    AssignMaterials(root,themeId,car);
                    var body = BoundsOf(root); AttachKit(root,themeId,baseId,body,profiles);
                    var saved = PrefabUtility.SaveAsPrefabAsset(root,path);
                    string preview = Art+"/Preview/Thumbnails/"+themeId+"/"+id+".png";
                    ThemeAccessoryBuilder.Render(saved,preview,body,"Front",256);
                    AssetDatabase.ImportAsset(preview,ImportAssetOptions.ForceSynchronousImport);
                    var entry = new ThemedCarCatalog.Entry{collectibleId=id,baseCarId=baseId,themeId=themeId,colorwayId="V01",displayName=(string)theme["name"]+" · "+baseId,prefab=saved,preview=Required<Texture2D>(preview),releaseId=Release,enabled=true};
                    entries.Add(entry);added.Add(id);
                    if (!records.Any(r=>(string)r["collectibleId"]==id)) records.Add(new JObject{["collectibleId"]=id,["baseCarId"]=baseId,["themeId"]=themeId,["colorwayId"]="V01",["displayName"]=entry.displayName,["prefabPath"]=path,["previewPath"]=preview,["thumbnailPath"]=preview,["sourcePrefabPath"]=source,["contentRevision"]=1,["releaseId"]=Release});
                }
                finally { if (loaded) PrefabUtility.UnloadPrefabContents(root); else UnityEngine.Object.DestroyImmediate(root); }
            }
            // Reconcile a prior catalog checkpoint even if its portable JSON write was interrupted.
            foreach(var entry in entries.Where(e=>e.releaseId==Release&&!records.Any(r=>(string)r["collectibleId"]==e.collectibleId)))
                records.Add(new JObject{["collectibleId"]=entry.collectibleId,["baseCarId"]=entry.baseCarId,["themeId"]=entry.themeId,["colorwayId"]="V01",["displayName"]=entry.displayName,["prefabPath"]=AssetDatabase.GetAssetPath(entry.prefab),["previewPath"]=AssetDatabase.GetAssetPath(entry.preview),["thumbnailPath"]=AssetDatabase.GetAssetPath(entry.preview),["contentRevision"]=1,["releaseId"]=Release});
            // Checkpoint after each theme so an interrupted authoring pass can resume.
            catalog.entries=entries.ToArray();EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();
            WriteJson(Data+"variant_catalog.json",portable.ToString());
            Debug.Log("COLLECTION_COMPLETED_THEME "+themeId+" "+entries.Count);
        }
        var registry=JObject.Parse(File.ReadAllText(Data+"id_registry.json"));
        if(registry["additionalReleases"]==null)registry["additionalReleases"]=new JObject();
        registry["additionalReleases"][Release]=new JArray(entries.Where(e=>e.releaseId==Release).Select(e=>e.collectibleId));
        WriteJson(Data+"id_registry.json",registry.ToString());
        foreach(var theme in recipes)theme["cars"]=new JArray(Enumerable.Range(1,15));
        WriteJson(Data+"theme_recipes.json",new JObject{["schemaVersion"]=1,["themes"]=recipes}.ToString());
        AssetDatabase.SaveAssets();AssetDatabase.Refresh();
        File.WriteAllText("IntegrationEvidence/collection_completion.json",new JObject{["designs"]=entries.Count,["themes"]=recipes.Count,["shapesPerTheme"]=15,["copiesPerDesign"]=2,["physicalCars"]=entries.Count*2,["addedThisRun"]=new JArray(added)}.ToString());
        Debug.Log("COMPLETE_COLLECTION_ART_READY "+entries.Count);
    }

    static void WriteJson(string path,string text)
    {
        // Unity may hold an imported TextAsset in a mapped view on Windows. Replace its contents
        // through the Editor file API instead of truncating the live mapped file.
        string temporary="IntegrationEvidence/"+Path.GetFileName(path)+".writing";
        File.WriteAllText(temporary,text);FileUtil.ReplaceFile(temporary,path);
        if(File.Exists(temporary))File.Delete(temporary);
    }

    public static void RepairNewShapeMounts()
    {
        var catalog=Required<ThemedCarCatalog>(Data+"ThemedCarCatalog.asset");
        var profiles=JArray.Parse(File.ReadAllText("TinyRidersProduction/ThemeAccessoryPass/fitting_profiles.json"));
        foreach(var entry in catalog.entries.Where(e=>(e.baseCarId=="C013"||e.baseCarId=="C006")&&e.releaseId==Release))
        {
            string path=AssetDatabase.GetAssetPath(entry.prefab);var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var old=root.transform.Find("ThemeAccessories");if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
                var body=BoundsOf(root);AttachKit(root,entry.themeId,entry.baseCarId,body,profiles);
                var saved=PrefabUtility.SaveAsPrefabAsset(root,path);
                ThemeAccessoryBuilder.Render(saved,AssetDatabase.GetAssetPath(entry.preview),body,"Front",256);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        AssetDatabase.SaveAssets();AssetDatabase.Refresh();
    }

    public static void RepairMaterialReferences()
    {
        var catalog=Required<ThemedCarCatalog>(Data+"ThemedCarCatalog.asset");int repaired=0;
        foreach(var entry in catalog.entries.Where(e=>e.releaseId==Release))
        {
            bool foreign=entry.prefab.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials)
                .Select(AssetDatabase.GetAssetPath).Any(p=>p.StartsWith(Art+"/Materials/Themes/",StringComparison.Ordinal)&&!p.Contains("/"+entry.themeId+"/"));
            if(!foreign)continue;
            var root=PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(entry.prefab));
            try
            {
                AssignMaterials(root,entry.themeId,int.Parse(entry.baseCarId.Substring(1)));
                var saved=PrefabUtility.SaveAsPrefabAsset(root,AssetDatabase.GetAssetPath(entry.prefab));
                ThemeAccessoryBuilder.Render(saved,AssetDatabase.GetAssetPath(entry.preview),BoundsOf(root),"Front",256);repaired++;
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        AssetDatabase.SaveAssets();AssetDatabase.Refresh();Debug.Log("COLLECTION_MATERIAL_REFERENCES_REPAIRED "+repaired);
    }

    static void Set(UnityEngine.Object obj,string name,UnityEngine.Object value)
    {var so=new SerializedObject(obj);so.FindProperty(name).objectReferenceValue=value;so.ApplyModifiedPropertiesWithoutUndo();}
    static T Asset<T>(string path) where T:ScriptableObject
    {var a=AssetDatabase.LoadAssetAtPath<T>(path);if(a==null){a=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(a,path);}return a;}

    [MenuItem("Tools/Tiny Riders/Sorting/Update Completed Collection Gameplay")]
    public static void Integrate()
    {
        if(EditorApplication.isPlaying)throw new Exception("Exit Play mode first");
        var current=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(current.path=="Assets/TinyRiders/Scenes/ThemedSorting.unity"&&current.isDirty)EditorSceneManager.SaveScene(current);
        var scene=EditorSceneManager.OpenScene("Assets/TinyRiders/Scenes/ThemedSorting.unity");
        var session=UnityEngine.Object.FindFirstObjectByType<ThemedCollectionSession>();
        var catalog=session.catalog;
        foreach(var entry in catalog.entries.Where(e=>e.sortingItem==null))
        {
            var item=Asset<SortableItem_SO>(Gameplay+"/Items/"+entry.collectibleId+".asset");
            var so=new SerializedObject(item);so.FindProperty("saveId").stringValue=entry.collectibleId;so.FindProperty("displayName").stringValue=entry.displayName;so.ApplyModifiedPropertiesWithoutUndo();
            item.Catagory=Required<Catagory_SO>(Gameplay+"/Themes/"+entry.themeId+".asset");
            item.CameraFixed=true;item.HeldPosition=new Vector3(1.6f,-2.1f,7.8f);item.HeldRotation=new Vector3(0,150,0);
            var world=ThemedSortingBuilder.Wrap(entry,item,"World",out var size);
            var held=ThemedSortingBuilder.Wrap(entry,item,"Held",out _);
            var shelf=ThemedSortingBuilder.Wrap(entry,item,"Shelf",out _);
            if(size.x>4.176f||size.y>4.86f||size.z>5.832f)throw new Exception("Car exceeds shelf clearance: "+entry.collectibleId+" "+size);
            Set(item,"worldItem",world.GetComponent<WorldItemBase>());Set(item,"handItem",held.GetComponent<HandItemBase>());
            item.PlacedOnShelfPrefab=shelf.GetComponent<PlacedOnShelfItemBase>();
            entry.sortingItem=item;entry.displaySize=size;EditorUtility.SetDirty(item);
        }
        foreach(var theme in catalog.themes)
        {
            var category=Required<Catagory_SO>(Gameplay+"/Themes/"+theme.id+".asset");
            category.Items=catalog.entries.Where(e=>e.themeId==theme.id).Select(e=>e.sortingItem).ToList();EditorUtility.SetDirty(category);
            var shelves=UnityEngine.Object.FindObjectsByType<SortingShelf>(FindObjectsSortMode.None).Where(s=>s.GetComponent<ThemeShelfBinding>()?.themeId==theme.id).OrderBy(s=>s.SaveId,StringComparer.Ordinal).ToList();
            int needed=Mathf.CeilToInt(catalog.entries.Count(e=>e.enabled&&e.themeId==theme.id)*2/18f);
            for(int n=shelves.Count+1;n<=needed;n++)
            {
                var copy=UnityEngine.Object.Instantiate(shelves[0].gameObject);copy.name="Theme Shelf "+theme.id+" "+n;
                var shelf=copy.GetComponent<SortingShelf>();var so=new SerializedObject(shelf);so.FindProperty("saveId").stringValue=theme.id+"-S"+n.ToString("D3");so.ApplyModifiedPropertiesWithoutUndo();shelves.Add(shelf);
            }
        }
        var database=Required<ItemDatabaseSO>(Gameplay+"/ThemedItemDatabase.asset");
        var dbso=new SerializedObject(database);var items=dbso.FindProperty("items");items.arraySize=catalog.entries.Length;
        for(int i=0;i<items.arraySize;i++)items.GetArrayElementAtIndex(i).objectReferenceValue=catalog.entries[i].sortingItem;
        dbso.ApplyModifiedPropertiesWithoutUndo();
        session.copiesPerDesign=2;session.visiblePileLimit=108;EditorUtility.SetDirty(catalog);
        var settings=ThemedSortingAuthoringSettings.Load();settings.copiesPerDesign=2;settings.visiblePileLimit=108;EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();BakeThemedPile.Run();FinishThemedScene.Run();
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        Debug.Log("COMPLETE_COLLECTION_GAMEPLAY_READY 600 designs, 1200 cars, 80 shelves");
    }
}
