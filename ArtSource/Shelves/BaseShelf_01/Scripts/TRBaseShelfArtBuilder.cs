using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Art authoring only. No component from this class is attached to any prefab.
[InitializeOnLoad]
public static class TRBaseShelfArtBuilder
{
    const string Art = "Assets/TinyRiders/Art/Shelves/BaseShelf_01";
    const string Src = "ArtSource/Shelves/BaseShelf_01";
    static TRBaseShelfArtBuilder() { EditorApplication.delayCall += Bootstrap; }
    static void Bootstrap()
    {
        if (!File.Exists(Src + "/request_measure.txt")) return;
        try {
            EditorApplication.ExecuteMenuItem("Window/Pipeline/Start Server");
            Measure();
            File.Delete(Src + "/request_measure.txt");
        } catch (Exception e) { File.WriteAllText(Src + "/unity_error.txt",e.ToString()); Debug.LogException(e); }
    }
    [Serializable] public class CarInfo { public string path; public Vector3 size; public Vector3 center; public Vector3 min; }
    [Serializable] public class CarReport { public CarInfo[] cars; public CarInfo[] representatives; public Vector3 maxSize; public float recommendedScale; }
    static Bounds VisibleBounds(GameObject o)
    {
        bool found=false; Bounds b=default;
        foreach(var r in o.GetComponentsInChildren<Renderer>(false)) {
            if (!r.enabled || (!(r is MeshRenderer) && !(r is SkinnedMeshRenderer))) continue;
            if (!found) { b=r.bounds; found=true; } else b.Encapsulate(r.bounds);
        }
        if (!found) throw new Exception("No car mesh: "+o.name);
        return b;
    }
    [MenuItem("Tools/Tiny Riders/Base Shelf/Measure Existing Cars")]
    public static void Measure()
    {
        var list=new List<CarInfo>();
        var paths=AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/00_Project/GeneratedCarStore/Prefabs"}).Select(AssetDatabase.GUIDToAssetPath).Where(p=>Path.GetFileName(p).StartsWith("Shelf_TR-C")).OrderBy(p=>p).ToArray();
        foreach(var p in paths) {
            var o=PrefabUtility.LoadPrefabContents(p);
            try { var b=VisibleBounds(o); list.Add(new CarInfo{path=p,size=b.size,center=b.center,min=b.min}); }
            finally { PrefabUtility.UnloadPrefabContents(o); }
        }
        if(list.Count==0) throw new Exception("No established shelf-car prefabs found.");
        var max=new Vector3(list.Max(c=>c.size.x),list.Max(c=>c.size.y),list.Max(c=>c.size.z));
        var reps=new[]{list.OrderByDescending(c=>c.size.x).First(),list.OrderByDescending(c=>c.size.y).First(),list.First(c=>c.path.Contains("CUS")),list.First(c=>c.path.Contains("STD"))}.GroupBy(c=>c.path).Select(g=>g.First()).ToList();
        foreach(var c in list.Where(c=>c.path.Contains("CUS")).Reverse()) { if(reps.Count>=4)break; if(!reps.Any(r=>r.path==c.path))reps.Add(c); }
        var report=new CarReport{cars=list.ToArray(),representatives=reps.ToArray(),maxSize=max,recommendedScale=Mathf.Ceil(Mathf.Max((max.x+.04f)/.15466f,(max.z+.06f)/.216f,(max.y+.04f)/.18f)*10)/10};
        File.WriteAllText(Src+"/car_measurements.json",JsonUtility.ToJson(report,true));
        Debug.Log("BASE_SHELF_MEASURED "+list.Count+" cars; max "+max+"; uniform shelf scale "+report.recommendedScale);
    }
    static Material Material(string name,Color color,float smooth,float metal,string texture=null,bool printed=false,bool emission=false)
    {
        string p=Art+"/Materials/"+name+".mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(p);
        var shader=Shader.Find(printed?"TinyRiders/PrintedPaint":"Universal Render Pipeline/Lit");
        if(!shader)throw new Exception("Missing shader "+name);
        if(!m) { m=new Material(shader); AssetDatabase.CreateAsset(m,p); }
        m.shader=shader; m.SetColor("_BaseColor",color); m.SetFloat("_Smoothness",smooth); m.SetFloat("_Metallic",metal);
        if(texture!=null) m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"/Textures/"+texture+".png"));
        if(emission) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor",new Color(1,.48f,.095f)*2); }
        EditorUtility.SetDirty(m); return m;
    }
    static Transform Group(string name,Transform parent) {var g=new GameObject(name).transform; g.SetParent(parent,false); return g;}
    static Transform Find(Transform root,string n)=>root.GetComponentsInChildren<Transform>(true).First(t=>t.name==n);
    static Dictionary<string,Material> Materials()
    {
        var d=new Dictionary<string,Material>();
        void Add(Material m) { d[m.name]=m; }
        Add(Material("MAT_Frame_Charcoal",new Color(.17f,.20f,.25f),.42f,.18f));
        Add(Material("MAT_SidePanels_Red",new Color(.72f,.12f,.16f),.62f,.12f,"TEX_SidePanel",true));
        Add(Material("MAT_Trays_LightGray",new Color(.71f,.75f,.8f),.52f,.28f));
        Add(Material("MAT_Trim_Dark",new Color(.09f,.11f,.14f),.4f,.12f));
        Add(Material("MAT_Header",Color.white,.4f,0,"TEX_Header_Blank"));
        Add(Material("MAT_Printed_Left",Color.white,.4f,0,"TEX_Printed_Left"));
        Add(Material("MAT_Printed_Right",Color.white,.4f,0,"TEX_Printed_Right"));
        Add(Material("MAT_Accent_Warm",new Color(1,.65f,.25f),.4f,0,null,false,true));
        return d;
    }
    [MenuItem("Tools/Tiny Riders/Base Shelf/Build Prefab and Previews")]
    public static void Build()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play mode before building art previews.");
        for(int i=SceneManager.sceneCount-1;i>=0;i--) {
            var old=SceneManager.GetSceneAt(i);
            if(old.path.StartsWith(Art+"/PreviewScenes/"))EditorSceneManager.CloseScene(old,true);
        }
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var modelPath=Art+"/Models/TR_Shelf_Base_01.fbx";
        var imp=(ModelImporter)AssetImporter.GetAtPath(modelPath);
        imp.globalScale=1; imp.useFileScale=true; imp.importCameras=false; imp.importLights=false; imp.importAnimation=false; imp.isReadable=true; imp.materialImportMode=ModelImporterMaterialImportMode.ImportStandard; imp.SaveAndReimport();
        foreach(var p in Directory.GetFiles(Art+"/Textures","*.png")) {
            var ti=(TextureImporter)AssetImporter.GetAtPath(p.Replace('\\','/')); ti.textureType=TextureImporterType.Default; ti.alphaIsTransparency=false; ti.mipmapEnabled=true; ti.maxTextureSize=2048; ti.textureCompression=TextureImporterCompression.Uncompressed; ti.wrapMode=TextureWrapMode.Clamp; ti.SaveAndReimport();
        }
        var mats=Materials();
        var model=AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        var imported=(GameObject)PrefabUtility.InstantiatePrefab(model); PrefabUtility.UnpackPrefabInstance(imported,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
        var root=new GameObject("TR_Shelf_Base_01");
        var sourceGeo=Find(imported.transform,"Geometry"); sourceGeo.SetParent(root.transform,true);
        var back=Find(root.transform,"BackPanel").GetComponentsInChildren<Renderer>().First().bounds.center;
        var lip=Find(root.transform,"FrontLip_01").GetComponent<Renderer>().bounds.center;
        if(back.z>lip.z)sourceGeo.RotateAround(Vector3.zero,Vector3.up,180);
        UnityEngine.Object.DestroyImmediate(imported);
        foreach(var r in root.GetComponentsInChildren<Renderer>()) {
            r.sharedMaterials=r.sharedMaterials.Select(m=> { if(!m || !mats.TryGetValue(m.name,out var mapped))throw new Exception("Unmapped material: "+(m?m.name:"null"));return mapped; }).ToArray();
            r.shadowCastingMode=ShadowCastingMode.On; r.receiveShadows=true;
        }
        var markerRoot=Group("PlacementMarkers",root.transform);
        float s=3f, angle=3f;
        for(int r=0;r<3;r++) {
            var row=Group("Row_"+(r+1).ToString("00"),markerRoot);
            for(int c=0;c<6;c++) {
                var mk=Group($"Slot_R{r+1:00}_C{c+1:00}",row);
                mk.localPosition=new Vector3((.5f-(c+.5f)/6)*s,(new[]{.82f,.565f,.31f}[r]+Mathf.Tan(angle*Mathf.Deg2Rad)*.005f)*s,-.005f*s);
                mk.localRotation=Quaternion.Euler(angle,0,0);
            }
        }
        var bounds=VisibleBounds(root);
        if(bounds.size.x<3.5f || bounds.size.x>3.8f || bounds.size.y<3.5f || bounds.size.y>3.8f)throw new Exception("FBX scale mismatch: "+bounds);
        var prefab=PrefabUtility.SaveAsPrefabAsset(root,Art+"/Prefabs/TR_Shelf_Base_01.prefab");
        UnityEngine.Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        BuildPreviews(prefab);
    }
    static void Light(string name,Vector3 pos,Vector3 target,float intensity,Color color,Transform parent)
    {
        var t=Group(name,parent); t.position=pos; t.LookAt(target); var l=t.gameObject.AddComponent<Light>(); l.type=LightType.Directional; l.color=color; l.intensity=intensity; l.shadows=LightShadows.Soft; l.cullingMask=1<<31;
    }
    static void Capture(Camera cam,string name)
    {
        var otherLights=UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Where(l=>l.enabled && l.gameObject.scene!=cam.gameObject.scene).ToArray();
        foreach(var l in otherLights)l.enabled=false;
        foreach(var go in cam.gameObject.scene.GetRootGameObjects())foreach(var t in go.GetComponentsInChildren<Transform>(true)) { t.gameObject.layer=31; if(PrefabUtility.IsPartOfPrefabInstance(t))PrefabUtility.RecordPrefabInstancePropertyModifications(t.gameObject); }
        var rt=new RenderTexture(1400,1400,24,RenderTextureFormat.ARGB32); rt.antiAliasing=4;
        var prev=RenderTexture.active; cam.targetTexture=rt;
        var req=new UniversalRenderPipeline.SingleCameraRequest(); req.destination=rt;
        RenderPipeline.SubmitRenderRequest(cam,req);
        RenderTexture.active=rt; var tex=new Texture2D(1400,1400,TextureFormat.RGB24,false); tex.ReadPixels(new Rect(0,0,1400,1400),0,0); tex.Apply();
        File.WriteAllBytes(Src+"/Renders/"+name+".png",tex.EncodeToPNG());
        cam.targetTexture=null; RenderTexture.active=prev; UnityEngine.Object.DestroyImmediate(tex); rt.Release(); UnityEngine.Object.DestroyImmediate(rt);
        foreach(var l in otherLights)l.enabled=true;
    }
    static void BuildPreviews(GameObject prefab)
    {
        var previous=SceneManager.GetActiveScene();
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive); SceneManager.SetActiveScene(scene);
        var shelf=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);
        var studio=Group("ArtPreviewStudio",null);
        RenderSettings.ambientMode=AmbientMode.Trilight; RenderSettings.ambientSkyColor=new Color(.25f,.28f,.33f); RenderSettings.ambientEquatorColor=new Color(.14f,.17f,.21f); RenderSettings.ambientGroundColor=new Color(.08f,.09f,.11f); RenderSettings.skybox=null; RenderSettings.fog=false;
        Light("Key",new Vector3(4,7,5),new Vector3(0,1.8f,0),.9f,new Color(1,.9f,.8f),studio);
        Light("Fill",new Vector3(-4,4,3),new Vector3(0,1.8f,0),.35f,new Color(.8f,.88f,1),studio);
        Light("Rim",new Vector3(-1,6,-3),new Vector3(0,1.8f,0),.5f,new Color(1,1,1),studio);
        var floor=GameObject.CreatePrimitive(PrimitiveType.Cube); floor.name="NeutralGround"; floor.transform.SetParent(studio); floor.transform.position=new Vector3(0,-.055f,0); floor.transform.localScale=new Vector3(200,.1f,200); UnityEngine.Object.DestroyImmediate(floor.GetComponent<Collider>()); floor.GetComponent<Renderer>().sharedMaterial=Material("MAT_PreviewGround",new Color(.35f,.39f,.46f),.2f,0);
        var cam=Group("ArtPreviewCamera",studio).gameObject.AddComponent<Camera>(); cam.transform.position=new Vector3(6,5.1f,9); cam.transform.LookAt(new Vector3(0,1.8f,0)); cam.orthographic=true; cam.orthographicSize=2.55f; cam.nearClipPlane=.05f; cam.farClipPlane=300; cam.clearFlags=CameraClearFlags.SolidColor; cam.backgroundColor=new Color(.19f,.22f,.28f); cam.allowHDR=true;
        cam.cullingMask=1<<31;
        cam.gameObject.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing=false;
        EditorSceneManager.SaveScene(scene,Art+"/PreviewScenes/TR_Shelf_Empty_ArtPreview.unity");
        Capture(cam,"D_Unity_Empty_EmissionOn");
        var accent=AssetDatabase.LoadAssetAtPath<Material>(Art+"/Materials/MAT_Accent_Warm.mat"); var e=accent.GetColor("_EmissionColor"); accent.SetColor("_EmissionColor",Color.black); Capture(cam,"D_Unity_Empty_EmissionOff"); accent.SetColor("_EmissionColor",e);
        EditorSceneManager.SaveScene(scene,Art+"/PreviewScenes/TR_Shelf_Empty_ArtPreview.unity");
        var report=JsonUtility.FromJson<CarReport>(File.ReadAllText(Src+"/car_measurements.json"));
        var previews=Group("ExistingCarPreviewWrappers",null); var results=new List<string>();
        var slots=shelf.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("Slot_")).OrderBy(t=>t.name).ToArray();
        for(int i=0;i<18;i++) {
            var ci=report.representatives[i%report.representatives.Length];
            var wrapper=Group("Preview_"+Path.GetFileNameWithoutExtension(ci.path),previews);
            wrapper.position=slots[i].position; wrapper.rotation=slots[i].rotation;
            var car=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ci.path),scene); car.transform.SetParent(wrapper,false);
            // Position only: keep each prefab's established scale and supplier assets untouched.
            car.transform.localRotation=Quaternion.Euler(0,180,0);
            car.transform.localPosition=new Vector3(ci.center.x,-ci.min.y,ci.center.z);
            PrefabUtility.RecordPrefabInstancePropertyModifications(car.transform);
            results.Add(slots[i].name+": "+ci.path);
        }
        EditorSceneManager.SaveScene(scene,Art+"/PreviewScenes/TR_Shelf_Filled_ArtPreview.unity");
        Capture(cam,"D_Unity_Filled");
        cam.transform.position=new Vector3(0,2.1f,10); cam.transform.LookAt(new Vector3(0,1.8f,0)); Capture(cam,"C_Unity_Fit_Front");
        cam.transform.position=new Vector3(4,3.6f,6); cam.transform.LookAt(new Vector3(0,2.5f,0)); cam.orthographicSize=1.55f; Capture(cam,"C_Unity_Fit_Closeup");
        var b=VisibleBounds(shelf); var trs=shelf.GetComponentsInChildren<Transform>();
        int triangles=shelf.GetComponentsInChildren<MeshFilter>().Sum(m=>m.sharedMesh.triangles.Length/3);
        var bad=shelf.GetComponentsInChildren<Renderer>().Where(r=>r.sharedMaterials.Any(m=>!m || !m.shader || m.shader.name=="Hidden/InternalErrorShader")).Select(r=>r.name).ToArray();
        File.WriteAllText(Src+"/unity_validation.json",JsonUtility.ToJson(new Validation{dimensions=b.size,center=b.center,triangles=triangles,markers=slots.Length,rootScale=shelf.transform.localScale,rootEuler=shelf.transform.eulerAngles,missingMaterialRenderers=bad,carCount=report.cars.Length,maxCar=report.maxSize,widthClearance=.464f-report.maxSize.x,depthClearance=.648f-report.maxSize.z,heightClearance=.54f-report.maxSize.y},true));
        File.WriteAllLines(Src+"/preview_car_slots.txt",results);
        cam.transform.position=new Vector3(6,5.1f,9); cam.transform.LookAt(new Vector3(0,1.8f,0)); cam.orthographicSize=2.55f;
        EditorSceneManager.SaveScene(scene,Art+"/PreviewScenes/TR_Shelf_Filled_ArtPreview.unity");
        Selection.activeObject=shelf; SceneView.lastActiveSceneView?.Frame(new Bounds(new Vector3(0,1.8f,0),new Vector3(4,4,1)),false);
        AssetDatabase.SaveAssets();
        File.WriteAllText(Src+"/unity_complete.txt","Prefab, 2 preview scenes, camera captures, and car measurements created in the existing Unity Editor.");
        Debug.Log("BASE_SHELF_UNITY_COMPLETE");
    }
    [Serializable] class Validation {public Vector3 dimensions,center,rootScale,rootEuler,maxCar;public int triangles,markers,carCount;public string[] missingMaterialRenderers;public float widthClearance,depthClearance,heightClearance;}
}
