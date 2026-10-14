using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class UnityWheelImport
{
    const string Root="Assets/TinyRidersPremium/Shared/Wheels";
    static string Project=Directory.GetParent(Application.dataPath).FullName;
    [Serializable] public class Source { public Wheel[] components; public bool allTechnicalPass; }
    [Serializable] public class Wheel { public string wheelId,name; public int triangles; public float axleHeightForGroundM; }
    [Serializable] public class Result { public string status="COMPONENTS_REQUIRE_VISUAL_REVIEW"; public List<Row> wheels=new List<Row>(); public string[] captures; }
    [Serializable] public class Row { public string wheelId,prefab; public int triangles,meshRenderers,sourceMeshRenderers,missingUVs; public float[] dimensionsM; public float axleHeightForGroundM; }
    static Material Mat(string name,Color color,float smooth,float metal=0)
    {
        string path=Root+"/Materials/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(mat==null){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}
        mat.SetColor("_BaseColor",color.gamma);mat.SetFloat("_Smoothness",smooth);mat.SetFloat("_Metallic",metal);
        EditorUtility.SetDirty(mat);return mat;
    }
    static void Capture(Camera camera,string path)
    {
        var rt=new RenderTexture(1600,1250,24,RenderTextureFormat.ARGB32);rt.antiAliasing=4;
        camera.targetTexture=rt;camera.Render();var old=RenderTexture.active;RenderTexture.active=rt;
        var im=new Texture2D(1600,1250,TextureFormat.RGB24,false);im.ReadPixels(new Rect(0,0,1600,1250),0,0);im.Apply();
        File.WriteAllBytes(Path.Combine(Project,path),im.EncodeToPNG());camera.targetTexture=null;RenderTexture.active=old;
        rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(im);
    }
    static void CombineForPrefab(GameObject root,string wheelId)
    {
        // Keep every editable source part in Blender/FBX; the runtime prefab shares one
        // mesh with a submesh per material. Tread lugs do not need separate renderers.
        PrefabUtility.UnpackPrefabInstance(root,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
        var renderers=root.GetComponentsInChildren<MeshRenderer>();
        var groups=new Dictionary<Material,List<CombineInstance>>();
        foreach(var r in renderers){
            var mesh=r.GetComponent<MeshFilter>().sharedMesh;
            for(int i=0;i<r.sharedMaterials.Length;i++){
                var mat=r.sharedMaterials[i];if(!groups.ContainsKey(mat))groups[mat]=new List<CombineInstance>();
                groups[mat].Add(new CombineInstance{mesh=mesh,subMeshIndex=i,transform=root.transform.worldToLocalMatrix*r.transform.localToWorldMatrix});
            }
        }
        var mats=new List<Material>();var pieces=new List<CombineInstance>();var temp=new List<Mesh>();
        foreach(var entry in groups){
            var mesh=new Mesh();mesh.CombineMeshes(entry.Value.ToArray(),true,true);temp.Add(mesh);mats.Add(entry.Key);
            pieces.Add(new CombineInstance{mesh=mesh,subMeshIndex=0,transform=Matrix4x4.identity});
        }
        var combined=new Mesh{name=wheelId+"_RuntimeMesh"};combined.CombineMeshes(pieces.ToArray(),false,true);combined.RecalculateBounds();
        string path=Root+"/Meshes/"+wheelId+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(saved==null){AssetDatabase.CreateAsset(combined,path);saved=combined;}
        else {EditorUtility.CopySerialized(combined,saved);EditorUtility.SetDirty(saved);UnityEngine.Object.DestroyImmediate(combined);}
        foreach(var mesh in temp)UnityEngine.Object.DestroyImmediate(mesh);
        foreach(var r in renderers)UnityEngine.Object.DestroyImmediate(r.gameObject);
        var child=new GameObject(wheelId+"_SharedMesh");child.transform.SetParent(root.transform,false);
        child.AddComponent<MeshFilter>().sharedMesh=saved;child.AddComponent<MeshRenderer>().sharedMaterials=mats.ToArray();
    }
    public static string Main()
    {
        var src=Newtonsoft.Json.JsonConvert.DeserializeObject<Source>(File.ReadAllText(Path.Combine(Project,"TinyRidersPremium/Previews/Wheels/technical_audit.json")));
        if(!src.allTechnicalPass)throw new Exception("Blender wheel audit has failures; repair before importing.");
        foreach(string f in new[]{"Models","Materials","Meshes","Prefabs"})Directory.CreateDirectory(Path.Combine(Project,Root,f));
        foreach(var w in src.components)File.Copy(Path.Combine(Project,"TinyRidersPremium/Exports/Shared/Wheels/"+w.wheelId+".fbx"),Path.Combine(Project,Root,"Models",w.wheelId+".fbx"),true);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var mats=new Dictionary<string,Material>{
            {"Wheel_Rubber",Mat("Wheel_Rubber",new Color(.009f,.012f,.017f),.35f)},
            {"Wheel_Alloy",Mat("Wheel_Alloy",new Color(.34f,.39f,.46f),.66f,.65f)},
            {"Wheel_Graphite",Mat("Wheel_Graphite",new Color(.022f,.03f,.045f),.54f,.2f)},
            {"Wheel_Gold",Mat("Wheel_Gold",new Color(.80f,.42f,.028f),.64f,.45f)},
            {"Wheel_Ivory",Mat("Wheel_Ivory",new Color(.9f,.84f,.69f),.60f,.2f)}
        };
        var active=EditorSceneManager.GetActiveScene();if(active.isDirty && !string.IsNullOrEmpty(active.path))EditorSceneManager.SaveScene(active);
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var result=new Result();const float scale=20;
        for(int i=0;i<src.components.Length;i++) {
            var source=src.components[i];string f=Root+"/Models/"+source.wheelId+".fbx";
            var mi=(ModelImporter)AssetImporter.GetAtPath(f);mi.globalScale=1;mi.useFileScale=true;mi.isReadable=true;
            mi.importNormals=ModelImporterNormals.Import;mi.importCameras=false;mi.importLights=false;mi.importAnimation=false;mi.SaveAndReimport();
            var ob=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(f));ob.name=source.wheelId+"_"+source.name;
            ob.transform.rotation=Quaternion.Euler(0,180,0)*ob.transform.rotation;
            var row=new Row{wheelId=source.wheelId,prefab=Root+"/Prefabs/"+source.wheelId+".prefab"};bool first=true;Bounds b=default;
            foreach(var r in ob.GetComponentsInChildren<MeshRenderer>()) {
                var assigned=r.sharedMaterials;
                for(int k=0;k<assigned.Length;k++) {
                    if(assigned[k]==null || !mats.TryGetValue(assigned[k].name,out var match))throw new Exception("Unmapped wheel material on "+r.name);
                    assigned[k]=match;
                }
                r.sharedMaterials=assigned;row.meshRenderers++;
                var mesh=r.GetComponent<MeshFilter>().sharedMesh;row.triangles+=mesh.triangles.Length/3;
                if(mesh.uv.Length!=mesh.vertexCount)row.missingUVs++;
                if(first){b=r.bounds;first=false;}else b.Encapsulate(r.bounds);
            }
            row.dimensionsM=new[]{b.size.x,b.size.y,b.size.z};row.axleHeightForGroundM=-b.min.y;
            if(row.triangles!=source.triangles || row.missingUVs!=0 || Mathf.Abs(row.axleHeightForGroundM-source.axleHeightForGroundM)>.00001f)
                throw new Exception("Wheel import mismatch "+source.wheelId+": "+JsonUtility.ToJson(row));
            row.sourceMeshRenderers=row.meshRenderers;
            CombineForPrefab(ob,source.wheelId);row.meshRenderers=ob.GetComponentsInChildren<MeshRenderer>().Length;
            var runtime=ob.GetComponentInChildren<MeshFilter>().sharedMesh;
            if(runtime.triangles.Length/3!=row.triangles)throw new Exception("Wheel combination changed triangle count "+source.wheelId);
            PrefabUtility.SaveAsPrefabAsset(ob,row.prefab);result.wheels.Add(row);
            ob.transform.localScale*=scale;ob.transform.position=new Vector3((3-i%4)*.055f,(2-i/4)*.060f,0)*scale;
        }
        RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.55f,.58f,.64f);
        RenderSettings.ambientEquatorColor=new Color(.35f,.38f,.42f);RenderSettings.ambientGroundColor=new Color(.18f,.20f,.24f);RenderSettings.skybox=null;
        foreach(var e in new[]{new Vector3(35,160,0),new Vector3(30,-140,0)}){
            var l=new GameObject("WheelReviewLight").AddComponent<Light>();l.type=LightType.Directional;l.transform.rotation=Quaternion.Euler(e);l.intensity=e.y>0?.8f:.4f;
        }
        var camera=new GameObject("WheelLibraryCamera").AddComponent<Camera>();camera.tag="MainCamera";camera.orthographic=true;camera.orthographicSize=.095f*scale;
        camera.nearClipPlane=.01f;camera.farClipPlane=30;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.18f,.21f,.27f);
        Vector3 target=new Vector3(.0825f,.06f,0)*scale;camera.transform.position=target+new Vector3(0,.018f,.5f)*scale;camera.transform.LookAt(target);
        string front="TinyRidersPremium/Previews/Wheels/unity_library_front.png";Capture(camera,front);
        camera.transform.position=target+new Vector3(.16f,.1f,.5f)*scale;camera.transform.LookAt(target);
        string depth="TinyRidersPremium/Previews/Wheels/unity_library_depth.png";Capture(camera,depth);
        result.captures=new[]{front,depth};EditorSceneManager.SaveScene(scene,"Assets/TinyRidersPremium/PreviewScenes/WheelLibrary_Review.unity");AssetDatabase.SaveAssets();
        string json=Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented,
            new Newtonsoft.Json.JsonSerializerSettings{ReferenceLoopHandling=Newtonsoft.Json.ReferenceLoopHandling.Ignore});
        File.WriteAllText(Path.Combine(Project,"TinyRidersPremium/Previews/Wheels/unity_audit.json"),json);return json;
    }
}
