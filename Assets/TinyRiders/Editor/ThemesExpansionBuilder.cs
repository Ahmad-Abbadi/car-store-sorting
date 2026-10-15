using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace TinyRiders.Editor {
public static class ThemesExpansionBuilder {
    const string D="TinyRidersProduction/ThemesExpansion01", R="Assets/TinyRiders/ThemedCollection", A="Assets/TinyRiders/Art/ThemeAccessories";
    static readonly Dictionary<string,Texture2D> TextureCache=new Dictionary<string,Texture2D>();
    static readonly Dictionary<string,string> CanonicalMaterials=Directory.GetFiles(D+"/baseline/Files/"+R+"/Materials/Themes","*.mat",SearchOption.AllDirectories).OrderBy(p=>p,StringComparer.Ordinal).GroupBy(Path.GetFileName).ToDictionary(g=>g.Key,g=>g.First().Substring((D+"/baseline/Files/").Length).Replace('\\','/'));
    static Vector3 V(JToken j)=>new Vector3((float)j[0],(float)j[1],(float)j[2]);
    static JArray J(Vector3 v)=>new JArray(v.x,v.y,v.z);
    static void Folder(string p)=>Directory.CreateDirectory(Path.GetDirectoryName(p));
    static Color Hex(string s){ColorUtility.TryParseHtmlString(s,out var c);return c;}
    static bool Accessory(Transform t){for(;t!=null;t=t.parent)if(t.name=="ThemeAccessories")return true;return false;}
    static string PathOf(Transform t,Transform root)=>AnimationUtility.CalculateTransformPath(t,root);
    static Bounds BoundsOf(GameObject g){var rs=g.GetComponentsInChildren<Renderer>().Where(r=>r.enabled).ToArray();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b;}
    static Bounds BaseBounds(string id){var p=JArray.Parse(File.ReadAllText(D+"/fitting_profiles.json")).Single(x=>(string)x["baseCarId"]==id);var b=new Bounds();b.SetMinMax(V(p["sourceBoundsMin"]),V(p["sourceBoundsMax"]));return b;}
    static Texture2D Texture(string p,bool alpha=false){if(TextureCache.TryGetValue(p,out var cached))return cached;AssetDatabase.ImportAsset(p,ImportAssetOptions.ForceSynchronousImport);var ti=(TextureImporter)AssetImporter.GetAtPath(p);var wrap=alpha?TextureWrapMode.Repeat:TextureWrapMode.Clamp;int max=alpha?2048:1024;bool changed=ti.textureType!=TextureImporterType.Default||!ti.sRGBTexture||ti.alphaIsTransparency!=alpha||!ti.mipmapEnabled||ti.wrapMode!=wrap||ti.maxTextureSize!=max||ti.textureCompression!=TextureImporterCompression.Compressed;ti.textureType=TextureImporterType.Default;ti.sRGBTexture=true;ti.alphaIsTransparency=alpha;ti.mipmapEnabled=true;ti.wrapMode=wrap;ti.maxTextureSize=max;ti.textureCompression=TextureImporterCompression.Compressed;if(changed)ti.SaveAndReimport();return TextureCache[p]=AssetDatabase.LoadAssetAtPath<Texture2D>(p);}
    static GameObject Kit(string theme){
        string fbx=A+"/Meshes/"+theme+"/ACC_"+theme+"_Primary.fbx";AssetDatabase.ImportAsset(fbx,ImportAssetOptions.ForceSynchronousImport);
        var mi=(ModelImporter)AssetImporter.GetAtPath(fbx);mi.globalScale=1;mi.importCameras=false;mi.importLights=false;mi.importAnimation=false;mi.addCollider=false;mi.isReadable=true;mi.materialImportMode=ModelImporterMaterialImportMode.None;mi.SaveAndReimport();
        var tex=Texture(A+"/Textures/"+theme+"/AccessoryAtlas.png");string mp=A+"/Materials/"+theme+"/Accessory.mat";Folder(mp);var mat=AssetDatabase.LoadAssetAtPath<Material>(mp);if(mat==null){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,mp);}mat.SetTexture("_BaseMap",tex);mat.SetColor("_BaseColor",Color.white);mat.SetFloat("_Smoothness",.32f);mat.SetFloat("_Metallic",.04f);mat.globalIlluminationFlags=MaterialGlobalIlluminationFlags.EmissiveIsBlack;mat.DisableKeyword("_EMISSION");EditorUtility.SetDirty(mat);
        string path=A+"/Prefabs/"+theme+"/KIT_"+theme+".prefab";Folder(path);bool exists=File.Exists(path);var g=exists?PrefabUtility.LoadPrefabContents(path):new GameObject("KIT_"+theme);
        try {if(!exists){var model=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(fbx));model.name="AccessoryGeometry";model.transform.SetParent(g.transform,false);}foreach(var rr in g.GetComponentsInChildren<Renderer>(true))rr.sharedMaterials=rr.sharedMaterials.Select(_=>mat).ToArray();return PrefabUtility.SaveAsPrefabAsset(g,path);}finally{if(exists)PrefabUtility.UnloadPrefabContents(g);else UnityEngine.Object.DestroyImmediate(g);}
    }
    static Material Derived(Material original,string theme,JToken recipe,Dictionary<string,Material> cache){
        string old=AssetDatabase.GetAssetPath(original);if(!old.StartsWith(R+"/Materials/Themes/"))return original;
        string name=Path.GetFileName(old),path=R+"/Materials/Themes/"+theme+"/"+name;if(cache.TryGetValue(path,out var prior))return prior;
        // Shared new materials must not depend on which base car a targeted build visits first.
        // Use one preserved template per material role, then explicitly apply this release's finish.
        original=AssetDatabase.LoadAssetAtPath<Material>(CanonicalMaterials[name]);
        Folder(path);var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m==null){m=new Material(original);AssetDatabase.CreateAsset(m,path);}else m.CopyPropertiesFromMaterial(original);
        foreach(string prop in new[]{"_BaseMap","_MainTex"})if(m.HasProperty(prop)){
            string tp=AssetDatabase.GetAssetPath(original.GetTexture(prop));if(tp.StartsWith(R+"/Textures/Themes/")){string np=R+"/Textures/Themes/"+theme+"/"+Path.GetFileName(tp);m.SetTexture(prop,Texture(np,np.EndsWith("_ThemeDecals.png")));}
        }
        m.DisableKeyword("_EMISSION");if(m.HasProperty("_EmissionMap"))m.SetTexture("_EmissionMap",null);if(m.HasProperty("_EmissionColor"))m.SetColor("_EmissionColor",Color.black);
        m.globalIlluminationFlags=MaterialGlobalIlluminationFlags.EmissiveIsBlack;if(m.HasProperty("_Metallic"))m.SetFloat("_Metallic",name=="Trim.mat"?(float)recipe["trimMetallic"]:name.Contains("Decals")||name.StartsWith("Light_")?0:(float)recipe["metallic"]);
        Color tint=name.StartsWith("Light_")?Hex((string)recipe["light"]):name=="WheelInset.mat"?Hex((string)recipe["trim"]):Color.white;
        if(m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",tint);if(m.HasProperty("_Color"))m.SetColor("_Color",tint);if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",name.Contains("Decals")?.2f:.32f);EditorUtility.SetDirty(m);cache[path]=m;return m;
    }
    public static void CaptureBaseline(){
        var old=JObject.Parse(File.ReadAllText(D+"/baseline/Files/"+R+"/Data/Themes/variant_catalog.json"));
        foreach(var e in old["entries"]){string file=D+"/baseline/Renders/"+e["collectibleId"]+".png";if(File.Exists(file))continue;ThemeAccessoryBuilder.Render(AssetDatabase.LoadAssetAtPath<GameObject>((string)e["prefabPath"]),file,BaseBounds((string)e["baseCarId"]));}
        ThemeAccessoryBuilder.PreserveFoundationMaterialFlags();Debug.Log("EXPANSION_BASELINE_CAPTURED");
    }
    public static void Build(string theme,string bases="ALL",bool render=true){
        TextureCache.Clear();
        var recipe=JObject.Parse(File.ReadAllText(D+"/texture_recipes.json"))["themes"].Single(t=>(string)t["id"]==theme);
        var plan=JArray.Parse(File.ReadAllText(D+"/variant_plan.json")).Where(t=>(string)t["themeId"]==theme&&(bases=="ALL"||bases.Split(',').Contains((string)t["baseCarId"])));
        var kit=Kit(theme);var kb=BoundsOf(kit);var profiles=JArray.Parse(File.ReadAllText(D+"/fitting_profiles.json"));var cache=new Dictionary<string,Material>();
        var records=File.Exists(D+"/products.json")?JArray.Parse(File.ReadAllText(D+"/products.json")):new JArray();
        foreach(var spec in plan){string id=(string)spec["variantId"],baseId=(string)spec["baseCarId"],refPath=(string)spec["approvedReferencePrefab"],path=R+"/Prefabs/Themed/"+theme+"/"+id+".prefab";Folder(path);
            bool exists=File.Exists(path);GameObject root=exists?PrefabUtility.LoadPrefabContents(path):(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(refPath));
            try {
                if(!exists)PrefabUtility.UnpackPrefabInstance(root,PrefabUnpackMode.OutermostRoot,InteractionMode.AutomatedAction);root.name=id;
                var old=root.transform.Find("ThemeAccessories");if(!exists&&old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
                var reference=AssetDatabase.LoadAssetAtPath<GameObject>(refPath);var rr=reference.GetComponentsInChildren<Renderer>(true).Where(r=>!Accessory(r.transform)).ToDictionary(r=>PathOf(r.transform,reference.transform));
                foreach(var r in root.GetComponentsInChildren<Renderer>(true).Where(r=>!Accessory(r.transform))){var src=rr[PathOf(r.transform,root.transform)];r.sharedMaterials=src.sharedMaterials.Select(m=>Derived(m,theme,recipe,cache)).ToArray();PrefabUtility.RecordPrefabInstancePropertyModifications(r);}
                var identity=root.GetComponent<ThemedCarIdentity>();identity.collectibleId=id;identity.themeId=theme;identity.baseCarId=baseId;identity.colorwayId="V01";identity.contentRevision=1;
                var visibility=new JArray();
                if(File.Exists(D+"/fit_overrides.json"))foreach(var change in JArray.Parse(File.ReadAllText(D+"/fit_overrides.json")).Where(x=>(string)x["variantId"]==id)){
                    string node=(string)change["path"];var part=root.transform.Find(node);if(part==null||!part.name.EndsWith("_FogLight"))throw new Exception("Not a separately removable fog-light module "+id+" "+node);
                    part.gameObject.SetActive(false);PrefabUtility.RecordPrefabInstancePropertyModifications(part.gameObject);visibility.Add(change.DeepClone());
                }
                var ar=root.transform.Find("ThemeAccessories");if(ar==null){ar=new GameObject("ThemeAccessories").transform;ar.SetParent(root.transform,false);}
                var prop=ar.Find("KIT_"+theme);if(prop==null){prop=((GameObject)PrefabUtility.InstantiatePrefab(kit)).transform;prop.SetParent(ar,false);prop.name="KIT_"+theme;}
                var profile=profiles.Single(x=>(string)x["baseCarId"]==baseId);var b=BaseBounds(baseId);float scale=Mathf.Min(b.size.z*.26f/Mathf.Max(kb.size.x,kb.size.z),b.size.x*.65f/kb.size.x,b.size.y*.42f/kb.size.y);
                prop.localPosition=V(profile["localPosition"]);prop.localEulerAngles=V(profile["localRotation"]);prop.localScale=Vector3.one*scale;PrefabUtility.RecordPrefabInstancePropertyModifications(prop);
                var saved=PrefabUtility.SaveAsPrefabAsset(root,path);string preview=R+"/Preview/Themes/"+theme+"/"+id+".png";
                if(render){ThemeAccessoryBuilder.Render(saved,preview,b);foreach(string view in new[]{"Side","Elevated","Gameplay"})ThemeAccessoryBuilder.Render(saved,D+"/Views/"+view+"/"+id+".png",b,view,view=="Gameplay"?256:384);ThemeAccessoryBuilder.Render(saved,D+"/Views/TextureOnly/"+id+".png",b,"Front",384,false);ThemeAccessoryBuilder.Render(saved,D+"/Views/Clay/"+id+".png",b,"Front",384,true,true);}
                var expanded=BoundsOf(root);var rec=new JObject{["collectibleId"]=id,["variantId"]=id,["baseCarId"]=baseId,["themeId"]=theme,["colorwayId"]="V01",["releaseId"]="themes_expansion_01",["displayName"]=(string)recipe["name"]+" - "+baseId,["prefabPath"]=path,["previewPath"]=preview,["thumbnailPath"]=R+"/Preview/Thumbnails/"+theme+"/"+id+".png",["contentRevision"]=1,["approvedReferencePrefab"]=refPath,["accessoryKitId"]="KIT_"+theme,["accessoryFitRecord"]=D+"/products.json",["localPosition"]=J(prop.localPosition),["localRotation"]=J(prop.localEulerAngles),["localScale"]=J(prop.localScale),["optionalPartOverrides"]=new JArray(),["boundsMin"]=J(expanded.min),["boundsMax"]=J(expanded.max),["materialPaths"]=new JArray(root.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).Select(AssetDatabase.GetAssetPath).Distinct()),["validationState"]="imported_pending_visual_QA",["shelfCompatibility"]="unsupported: unmodified source car already exceeds separate small shelf envelope"};
                rec["optionalPartOverrides"]=visibility;
                foreach(var e in records.Where(e=>(string)e["variantId"]==id).ToArray())e.Remove();records.Add(rec);
            }finally{if(exists)PrefabUtility.UnloadPrefabContents(root);else UnityEngine.Object.DestroyImmediate(root);}
        }
        File.WriteAllText(D+"/products.json",records.ToString(Formatting.Indented));AssetDatabase.SaveAssets();AssetDatabase.Refresh();ThemeAccessoryBuilder.PreserveFoundationMaterialFlags();Debug.Log("EXPANSION_BUILT "+theme+" "+bases);
    }
    public static void Publish(){
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var products=JArray.Parse(File.ReadAllText(D+"/products.json"));var catalogPath=R+"/Data/Themes/variant_catalog.json";var cat=JObject.Parse(File.ReadAllText(catalogPath));var entries=(JArray)cat["entries"];
        var runtime=AssetDatabase.LoadAssetAtPath<ThemedCarCatalog>(R+"/Data/Themes/ThemedCarCatalog.asset");var list=runtime.entries.ToList();
        foreach(var p in products){
            string id=(string)p["collectibleId"];
            if((string)p["releaseId"]!="themes_expansion_01")throw new Exception("Unexpected expansion release "+id);
            var existing=entries.FirstOrDefault(e=>(string)e["collectibleId"]==id);
            if(existing==null)entries.Add(p.DeepClone());
            else {if((string)existing["releaseId"]!="themes_expansion_01")throw new Exception("Protected catalog collision "+id);existing["displayName"]=p["displayName"].DeepClone();}
            var runtimeEntry=list.FirstOrDefault(e=>e.collectibleId==id);
            if(runtimeEntry==null)list.Add(new ThemedCarCatalog.Entry{collectibleId=id,baseCarId=(string)p["baseCarId"],themeId=(string)p["themeId"],colorwayId="V01",contentRevision=1,displayName=(string)p["displayName"],prefab=AssetDatabase.LoadAssetAtPath<GameObject>((string)p["prefabPath"]),preview=AssetDatabase.LoadAssetAtPath<Texture2D>((string)p["thumbnailPath"])});
            else runtimeEntry.displayName=(string)p["displayName"];
        }
        runtime.entries=list.ToArray();EditorUtility.SetDirty(runtime);File.WriteAllText(catalogPath,cat.ToString(Formatting.Indented));
        var recipesPath=R+"/Data/Themes/theme_recipes.json";var recipes=JObject.Parse(File.ReadAllText(recipesPath));foreach(var t in JObject.Parse(File.ReadAllText(D+"/texture_recipes.json"))["themes"])if(!recipes["themes"].Any(x=>(string)x["id"]==(string)t["id"]))((JArray)recipes["themes"]).Add(t.DeepClone());File.WriteAllText(recipesPath,recipes.ToString(Formatting.Indented));
        // The original release and immutableIds are intentionally retained as their original membership.
        var regPath=R+"/Data/Themes/id_registry.json";var reg=JObject.Parse(File.ReadAllText(regPath));if(reg["additionalReleases"]==null)reg["additionalReleases"]=new JObject();reg["additionalReleases"]["themes_expansion_01"]=new JArray(products.Select(p=>(string)p["collectibleId"]));File.WriteAllText(regPath,reg.ToString(Formatting.Indented));
        AssetDatabase.SaveAssets();AssetDatabase.Refresh();ThemeAccessoryBuilder.PreserveFoundationMaterialFlags();Debug.Log("EXPANSION_PUBLISHED "+products.Count);
    }
    public static void Validate(string suffix="first"){
        var errors=new List<string>();int meshCount=0;var products=JArray.Parse(File.ReadAllText(D+"/products.json"));var ids=new HashSet<string>();
        foreach(var p in products){string id=(string)p["variantId"];if(!ids.Add(id))errors.Add("Duplicate ID "+id);var g=AssetDatabase.LoadAssetAtPath<GameObject>((string)p["prefabPath"]);var src=AssetDatabase.LoadAssetAtPath<GameObject>((string)p["approvedReferencePrefab"]);if(g==null){errors.Add("Missing "+id);continue;}
            if(g.GetComponentsInChildren<Transform>(true).Count(t=>t.name=="ThemeAccessories")!=1)errors.Add("Accessory root count "+id);var ar=g.transform.Find("ThemeAccessories");if(ar.childCount!=1)errors.Add("Kit count "+id);
            var original=src.GetComponentsInChildren<Transform>(true).Where(t=>!Accessory(t)).ToDictionary(t=>PathOf(t,src.transform));var body=g.GetComponentsInChildren<Transform>(true).Where(t=>!Accessory(t)).ToArray();if(body.Length!=original.Count)errors.Add("Body hierarchy count "+id);
            foreach(var t in body){string path=PathOf(t,g.transform);if(!original.TryGetValue(path,out var before)){errors.Add("Extra body node "+id+" "+path);continue;}bool allowedHidden=p["optionalPartOverrides"].Any(x=>(string)x["path"]==path)&&t.name.EndsWith("_FogLight")&&!t.gameObject.activeSelf;if(t.localPosition!=before.localPosition||t.localRotation!=before.localRotation||t.localScale!=before.localScale||(t.gameObject.activeSelf!=before.gameObject.activeSelf&&!allowedHidden))errors.Add("Physical config changed "+id+" "+path);var mf=t.GetComponent<MeshFilter>();if(mf!=null){meshCount++;if(mf.sharedMesh!=before.GetComponent<MeshFilter>().sharedMesh)errors.Add("Body mesh changed "+id+" "+path);}}
            foreach(var r in g.GetComponentsInChildren<Renderer>(true))foreach(var m in r.sharedMaterials){if(m==null||m.shader==null||m.shader.name.Contains("InternalError")){errors.Add("Bad material "+id);continue;}var mp=AssetDatabase.GetAssetPath(m);if(mp.EndsWith("/Decals_1.mat")||mp.EndsWith("/Decals_2.mat"))errors.Add("Supplier graphics "+id);if(mp.StartsWith(R+"/Materials/Themes/")&&!mp.Contains("/"+p["themeId"]+"/"))errors.Add("Wrong theme material "+id);}
            var s=ar.GetChild(0).localScale;if(Mathf.Abs(s.x-s.y)>1e-5||Mathf.Abs(s.x-s.z)>1e-5)errors.Add("Nonuniform accessory "+id);if(!File.Exists((string)p["previewPath"]))errors.Add("Missing preview "+id);
        }
        File.WriteAllText(D+"/unity_validation_"+suffix+".json",JsonConvert.SerializeObject(new{productCount=products.Count,themeCount=products.Select(p=>(string)p["themeId"]).Distinct().Count(),bodyMeshReferencesChecked=meshCount,errors,passed=errors.Count==0},Formatting.Indented));if(errors.Count>0)throw new Exception(string.Join("\n",errors.Take(20)));Debug.Log("EXPANSION_VALIDATED "+products.Count);
    }
    public static void CaptureOldAfter(){var old=JObject.Parse(File.ReadAllText(D+"/baseline/Files/"+R+"/Data/Themes/variant_catalog.json"));foreach(var e in old["entries"])ThemeAccessoryBuilder.Render(AssetDatabase.LoadAssetAtPath<GameObject>((string)e["prefabPath"]),D+"/Nonregression/"+e["collectibleId"]+".png",BaseBounds((string)e["baseCarId"]));ThemeAccessoryBuilder.PreserveFoundationMaterialFlags();}
}
}
