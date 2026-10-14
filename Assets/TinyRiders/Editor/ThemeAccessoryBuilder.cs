using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace TinyRiders.Editor
{
public static class ThemeAccessoryBuilder
{
    public const string Art="Assets/TinyRiders/Art/ThemeAccessories",Prod="TinyRidersProduction/ThemeAccessoryPass",Cars=ThemedCarBuilder.Root;
    static Vector3 V(JToken j)=>new Vector3((float)j[0],(float)j[1],(float)j[2]);
    static float[] A(Vector3 v)=>new[]{v.x,v.y,v.z};
    static void Folder(string file)=>Directory.CreateDirectory(Path.GetDirectoryName(file));
    public static void PreserveFoundationMaterialFlags()
    {
        // Unity's preview/import validation can mark zero-emission supplier-style materials dirty.
        // Restore only the original keyword/GI flags; never change artwork or other properties.
        string baseline=Prod+"/baseline/Collection/Materials";
        foreach(string file in Directory.GetFiles(baseline,"*.mat",SearchOption.AllDirectories))
        {
            string original=File.ReadAllText(file),path=Cars+"/Materials"+file.Substring(baseline.Length).Replace('\\','/');
            if(!original.Contains("m_ValidKeywords: []")||!original.Contains("m_InvalidKeywords: []"))continue;
            var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(mat==null)continue;
            var block=Regex.Match(original,@"m_ValidKeywords:([\s\S]*?)  m_InvalidKeywords:").Groups[1].Value;
            var keywords=block.Split('\n').Select(s=>s.Trim()).Where(s=>s.StartsWith("- ")).Select(s=>s.Substring(2)).ToArray();
            var flags=(MaterialGlobalIlluminationFlags)int.Parse(Regex.Match(original,@"m_LightmapFlags: (\d+)").Groups[1].Value);
            if(!mat.shaderKeywords.OrderBy(x=>x).SequenceEqual(keywords.OrderBy(x=>x))||mat.globalIlluminationFlags!=flags)
            {
                if(mat.shaderKeywords.Except(keywords).Any(k=>k!="_EMISSION")||keywords.Except(mat.shaderKeywords).Any()||(mat.globalIlluminationFlags!=flags&&mat.globalIlluminationFlags!=(flags|MaterialGlobalIlluminationFlags.EmissiveIsBlack)))
                    throw new InvalidOperationException("Unexpected manual material flags preserved: "+path);
                mat.shaderKeywords=keywords;mat.globalIlluminationFlags=flags;EditorUtility.SetDirty(mat);AssetDatabase.SaveAssetIfDirty(mat);
            }
        }
    }
    static Bounds BoundsOf(GameObject g,bool bodyOnly=false)
    {
        var rs=g.GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled&&(!bodyOnly||!UnderAccessories(r.transform))).ToArray();
        if(rs.Length==0)throw new Exception("No visible mesh "+g.name);
        var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b;
    }
    static bool UnderAccessories(Transform t){for(;t!=null;t=t.parent)if(t.name=="ThemeAccessories")return true;return false;}
    static Bounds BaseBounds(string baseId)
    {
        var p=JArray.Parse(File.ReadAllText(Prod+"/fitting_profiles.json")).Single(x=>(string)x["baseCarId"]==baseId);
        var b=new Bounds();b.SetMinMax(V(p["sourceBoundsMin"]),V(p["sourceBoundsMax"]));return b;
    }
    public static void Render(GameObject prefab,string path,Bounds baseline,string view="Front",int size=512,bool accessories=true,bool clay=false)
    {
        var preview=new PreviewRenderUtility();GameObject instance=null;Material gray=null;
        try{
            instance=UnityEngine.Object.Instantiate(prefab);var ar=instance.transform.Find("ThemeAccessories");if(ar!=null)ar.gameObject.SetActive(accessories);
            if(clay){gray=new Material(Shader.Find("Universal Render Pipeline/Lit"));gray.SetColor("_BaseColor",new Color(.52f,.55f,.59f));gray.SetFloat("_Smoothness",.25f);foreach(var r in instance.GetComponentsInChildren<Renderer>())r.sharedMaterials=r.sharedMaterials.Select(m=>gray).ToArray();}
            preview.AddSingleGO(instance);var cam=preview.camera;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.91f,.93f,.95f,1);cam.nearClipPlane=.01f;cam.farClipPlane=100;
            Vector3 direction=view=="Side"?new Vector3(1,.10f,0):view=="Elevated"?new Vector3(.65f,1.8f,1):view=="Reverse"?new Vector3(-1,.8f,-1.25f):new Vector3(1,.8f,1.25f);
            var center=baseline.center+Vector3.up*baseline.size.y*.13f;
            cam.orthographic=true;cam.orthographicSize=baseline.extents.magnitude*.99f;cam.transform.position=center+direction.normalized*14;cam.transform.LookAt(center);
            if(view=="Gameplay"){cam.orthographic=false;cam.fieldOfView=60;cam.transform.position=center+new Vector3(1,.65f,1.2f).normalized*Mathf.Max(baseline.size.x,baseline.size.z)*1.5f;cam.transform.LookAt(center);}
            preview.ambientColor=new Color(.48f,.48f,.48f);preview.lights[0].intensity=1.25f;preview.lights[0].transform.rotation=Quaternion.Euler(35,-35,0);preview.lights[1].intensity=.8f;preview.lights[1].transform.rotation=Quaternion.Euler(40,150,0);
            preview.BeginStaticPreview(new Rect(0,0,size,size));cam.Render();var tex=preview.EndStaticPreview();Folder(path);File.WriteAllBytes(path,tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);
        }finally{preview.Cleanup();if(instance!=null)UnityEngine.Object.DestroyImmediate(instance);if(gray!=null)UnityEngine.Object.DestroyImmediate(gray);}
    }
    public static void CaptureBefore()
    {
        var cat=JsonUtility.FromJson<ThemedCarBuilder.Catalog>(File.ReadAllText(Cars+"/Data/Themes/variant_catalog.json"));
        foreach(var e in cat.entries){string path=Prod+"/before_after/before/"+e.collectibleId+".png";if(File.Exists(path))continue;var p=AssetDatabase.LoadAssetAtPath<GameObject>(e.prefabPath);Render(p,path,BaseBounds(e.baseCarId));}
        Debug.Log("ACCESSORY_BEFORE_CAPTURED 150");
    }
    static GameObject ImportKit(string theme)
    {
        string fbx=Art+"/Meshes/"+theme+"/ACC_"+theme+"_Primary.fbx";
        AssetDatabase.ImportAsset(fbx,ImportAssetOptions.ForceSynchronousImport);
        var imp=(ModelImporter)AssetImporter.GetAtPath(fbx);imp.globalScale=1;imp.importCameras=false;imp.importLights=false;imp.importAnimation=false;imp.addCollider=false;imp.isReadable=true;imp.materialImportMode=ModelImporterMaterialImportMode.None;imp.SaveAndReimport();
        string atlas=Art+"/Textures/"+theme+"/AccessoryAtlas.png";AssetDatabase.ImportAsset(atlas,ImportAssetOptions.ForceSynchronousImport);
        var ti=(TextureImporter)AssetImporter.GetAtPath(atlas);ti.textureType=TextureImporterType.Default;ti.sRGBTexture=true;ti.mipmapEnabled=true;ti.maxTextureSize=1024;ti.textureCompression=TextureImporterCompression.Compressed;ti.wrapMode=TextureWrapMode.Clamp;ti.SaveAndReimport();
        string matpath=Art+"/Materials/"+theme+"/Accessory.mat";Folder(matpath);var mat=AssetDatabase.LoadAssetAtPath<Material>(matpath);
        if(mat==null){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,matpath);}
        var recipe=JObject.Parse(File.ReadAllText(Cars+"/Data/Themes/theme_recipes.json"))["themes"].Single(t=>(string)t["id"]==theme);
        mat.SetColor("_BaseColor",Color.white);mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(atlas));mat.SetFloat("_Smoothness",(float)recipe["smoothness"]);mat.SetFloat("_Metallic",(float)recipe["metallic"]*.6f);EditorUtility.SetDirty(mat);
        var model=AssetDatabase.LoadAssetAtPath<GameObject>(fbx);var go=new GameObject("KIT_"+theme);
        var imported=UnityEngine.Object.Instantiate(model);imported.name="AccessoryGeometry";imported.transform.SetParent(go.transform,false);
        try{
            foreach(var r in go.GetComponentsInChildren<Renderer>(true))r.sharedMaterials=r.sharedMaterials.Select(x=>mat).ToArray();
            var b=BoundsOf(go);if(b.size.y<.1f||b.size.y>2||b.size.x>3||b.size.z>3)throw new Exception("Pilot export scale/axis failure "+theme+" "+b);
            string path=Art+"/Prefabs/"+theme+"/KIT_"+theme+".prefab";Folder(path);return PrefabUtility.SaveAsPrefabAsset(go,path);
        }finally{UnityEngine.Object.DestroyImmediate(go);}
    }
    public static void Apply(string theme,string bases="ALL")
    {
        var kit=ImportKit(theme);var cat=JObject.Parse(File.ReadAllText(Cars+"/Data/Themes/variant_catalog.json"));
        var profiles=JArray.Parse(File.ReadAllText(Prod+"/fitting_profiles.json"));
        var records=File.Exists(Prod+"/variant_assignments.json")?JArray.Parse(File.ReadAllText(Prod+"/variant_assignments.json")):new JArray();
        var catalog=AssetDatabase.LoadAssetAtPath<ThemedCarCatalog>(Cars+"/Data/Themes/ThemedCarCatalog.asset");
        foreach(var e in cat["entries"].Where(e=>(string)e["themeId"]==theme&&(bases=="ALL"||bases.Split(',').Contains((string)e["baseCarId"]))))
        {
            string id=(string)e["collectibleId"],baseId=(string)e["baseCarId"],path=(string)e["prefabPath"];
            if(!File.Exists(Prod+"/before_after/before/"+id+".png"))throw new Exception("Capture before before attaching "+id);
            var p=PrefabUtility.LoadPrefabContents(path);
            try{
                var profile=profiles.Single(x=>(string)x["baseCarId"]==baseId);var b=BaseBounds(baseId);
                var ar=p.transform.Find("ThemeAccessories");if(ar==null){var root=new GameObject("ThemeAccessories");ar=root.transform;ar.SetParent(p.transform,false);}
                // Idempotent: reuse the existing shared kit instance, creating only when missing.
                var old=ar.Find("KIT_"+theme);GameObject prop;
                if(old!=null)prop=old.gameObject;else{prop=(GameObject)PrefabUtility.InstantiatePrefab(kit);prop.name="KIT_"+theme;prop.transform.SetParent(ar,false);}
                var kb=BoundsOf(kit);float scale=Mathf.Min(b.size.z*.26f/Mathf.Max(kb.size.x,kb.size.z),b.size.x*.65f/kb.size.x,b.size.y*.42f/kb.size.y);
                prop.transform.localPosition=V(profile["localPosition"]);prop.transform.localEulerAngles=V(profile["localRotation"]);prop.transform.localScale=Vector3.one*scale;
                PrefabUtility.RecordPrefabInstancePropertyModifications(prop.transform);
                p.GetComponent<ThemedCarIdentity>().contentRevision=3;var saved=PrefabUtility.SaveAsPrefabAsset(p,path);
                string after=Prod+"/before_after/after/"+id+".png";Render(saved,after,b);
                Render(saved,Prod+"/views/Side/"+id+".png",b,"Side");Render(saved,Prod+"/views/Elevated/"+id+".png",b,"Elevated");
                Render(saved,Prod+"/views/Clay/"+id+".png",b,"Front",384,true,true);
                Render(saved,Prod+"/views/Gameplay/"+id+".png",b,"Gameplay",256);
                var expanded=BoundsOf(p);var rec=new JObject{["variantId"]=id,["baseCarId"]=baseId,["buildIdentity"]=(string)profile["buildIdentity"],["themeId"]=theme,["kitId"]="KIT_"+theme,["accessoryId"]="ACC-"+theme+"-PRIMARY",["profileId"]=(string)profile["profileId"],["localPosition"]=JArray.FromObject(A(prop.transform.localPosition)),["localRotation"]=JArray.FromObject(A(prop.transform.localEulerAngles)),["localScale"]=JArray.FromObject(A(prop.transform.localScale)),["optionalPartOverrides"]=new JArray(),["previousRevision"]=2,["newRevision"]=3,["beforePreviewPath"]=Prod+"/before_after/before/"+id+".png",["afterPreviewPath"]=after,["boundsMin"]=JArray.FromObject(A(expanded.min)),["boundsMax"]=JArray.FromObject(A(expanded.max)),["fitStatus"]="assembled_pending_visual_review"};
                foreach(var prior in records.Where(r=>(string)r["variantId"]==id).ToArray())prior.Remove();records.Add(rec);
                e["contentRevision"]=3;e["accessoryKitId"]="KIT_"+theme;e["accessoryFitRecord"]="TinyRidersProduction/ThemeAccessoryPass/variant_assignments.json";
                catalog.entries.Single(x=>x.collectibleId==id).contentRevision=3;
            }finally{PrefabUtility.UnloadPrefabContents(p);}
        }
        EditorUtility.SetDirty(catalog);File.WriteAllText(Cars+"/Data/Themes/variant_catalog.json",cat.ToString(Formatting.Indented));File.WriteAllText(Prod+"/variant_assignments.json",records.ToString(Formatting.Indented));AssetDatabase.SaveAssets();AssetDatabase.Refresh();
        PreserveFoundationMaterialFlags();
        Debug.Log("ACCESSORY_THEME_APPLIED "+theme);
    }
}
}
