using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class UnityCandidateImport
{
    const string Root="Assets/TinyRidersPremium";
    static string Project=Directory.GetParent(Application.dataPath).FullName;
    static Color Linear(float r,float g,float b) => new Color(r,g,b).gamma;
    static Material Mat(string name,Color color,float smooth,float metal=0,string texture=null)
    {
        string path=Root+"/Materials/"+name+".mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(m==null) { m=new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m,path); }
        m.SetColor("_BaseColor",color); m.SetFloat("_Smoothness",smooth); m.SetFloat("_Metallic",metal);
        m.SetTexture("_BaseMap",texture==null?null:AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/"+texture));
        EditorUtility.SetDirty(m); return m;
    }
    [Serializable] public class Audit
    {
        public string status="CANDIDATE_REQUIRES_VISUAL_REVIEW";
        public int meshRenderers,triangles,missingMaterials,missingUVs;
        public Vector3 sizeMetres;
        public float groundMinimum;
        public string[] captures;
    }
    public static string Main(string mid="M03")
    {
        var labels=new Dictionary<string,string>{{"M01","Pocket"},{"M02","Parcel"},{"M03","Blue23"},{"M04","Wedge"}};
        if(!labels.ContainsKey(mid)) throw new ArgumentException("Unconfigured master "+mid);
        foreach(var f in new[]{"Models","Textures","Materials","Prefabs/Candidates","PreviewScenes","Settings"}) Directory.CreateDirectory(Path.Combine(Project,Root,f));
        string pipelinePath=Root+"/Settings/ArtPreview_URP.asset";
        if(!File.Exists(pipelinePath)) AssetDatabase.CopyAsset("Assets/Settings/Performance_URP.asset",pipelinePath);
        var pipeline=AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset>(pipelinePath);
        var settings=new SerializedObject(pipeline);
        settings.FindProperty("m_MainLightShadowsSupported").boolValue=true;
        settings.FindProperty("m_MainLightShadowmapResolution").intValue=2048;
        settings.FindProperty("m_SoftShadowsSupported").boolValue=true;
        settings.FindProperty("m_AdditionalLightsRenderingMode").intValue=1;
        settings.FindProperty("m_AdditionalLightsPerObjectLimit").intValue=4;
        settings.FindProperty("m_RenderScale").floatValue=1;
        settings.FindProperty("m_MSAA").intValue=4;
        settings.FindProperty("m_ShadowDepthBias").floatValue=.005f;
        settings.FindProperty("m_ShadowNormalBias").floatValue=.01f;
        settings.ApplyModifiedPropertiesWithoutUndo();
        if(QualitySettings.renderPipeline!=pipeline) QualitySettings.renderPipeline=pipeline;
        File.Copy(Path.Combine(Project,"TinyRidersPremium/Exports/Candidates/"+mid+"_candidate.fbx"),Path.Combine(Project,Root,"Models/"+mid+"_candidate.fbx"),true);
        foreach(var f in new[]{mid+"_body.png",mid+"_cabin.png"}) File.Copy(Path.Combine(Project,"TinyRidersPremium/Textures/Candidates",f),Path.Combine(Project,Root,"Textures",f),true);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        foreach(var f in new[]{mid+"_body.png",mid+"_cabin.png"}) {
            var ti=(TextureImporter)AssetImporter.GetAtPath(Root+"/Textures/"+f);
            ti.sRGBTexture=true; ti.maxTextureSize=2048; ti.mipmapEnabled=true;
            ti.textureCompression=TextureImporterCompression.Uncompressed; ti.wrapMode=TextureWrapMode.Clamp; ti.SaveAndReimport();
        }
        var mi=(ModelImporter)AssetImporter.GetAtPath(Root+"/Models/"+mid+"_candidate.fbx");
        mi.globalScale=1; mi.useFileScale=true; mi.importCameras=false; mi.importLights=false; mi.importAnimation=false;
        mi.importNormals=ModelImporterNormals.Import; mi.isReadable=true; mi.SaveAndReimport();
        var mats=new Dictionary<string,Material> {
            {"M03_Paint_Print",Mat("M03_Paint_Print",Color.white,.71f,0,"M03_body.png")},
            {"M03_Cabin_Print",Mat("M03_Cabin_Print",Color.white,.70f,0,"M03_cabin.png")},
            {"M03_Paint_Solid",Mat("M03_Paint_Solid",Linear(.007f,.12f,.86f),.71f)},
            {"Rubber_Satin",Mat("Rubber_Satin",Linear(.009f,.011f,.015f),.38f)},
            {"Wheel_Aluminum",Mat("Wheel_Aluminum",Linear(.36f,.42f,.50f),.72f,.72f)},
            {"Wheel_DeepGraphite",Mat("Wheel_DeepGraphite",Linear(.027f,.035f,.05f),.66f,.3f)},
            {"Underbody_Graphite",Mat("Underbody_Graphite",Linear(.023f,.028f,.038f),.5f)}
        };
        if(mid=="M01") {
            mats["M01_Paint_Print"]=Mat("M01_Paint_Print",Color.white,.68f,0,"M01_body.png");
            mats["M01_Cabin_Print"]=Mat("M01_Cabin_Print",Color.white,.70f,0,"M01_cabin.png");
            mats["M01_Paint_Solid"]=Mat("M01_Paint_Solid",Linear(.86f,.035f,.022f),.68f);
            mats["M01_Ivory_Plastic"]=Mat("M01_Ivory_Plastic",Linear(.91f,.85f,.71f),.66f);
            mats["M01_SatinAlloy"]=Mat("M01_SatinAlloy",Linear(.31f,.34f,.38f),.63f,.55f);
            mats["M01_Graphite"]=Mat("M01_Graphite",Linear(.018f,.021f,.025f),.5f);
        }
        if(mid=="M02") {
            mats["M02_Paint_Print"]=Mat("M02_Paint_Print",Color.white,.66f,0,"M02_body.png");
            mats["M02_Cabin_Print"]=Mat("M02_Cabin_Print",Color.white,.70f,0,"M02_cabin.png");
            mats["M02_Paint_Solid"]=Mat("M02_Paint_Solid",Linear(1f,.565f,.027f),.66f);
            mats["M02_Graphite"]=Mat("M02_Graphite",Linear(.018f,.025f,.031f),.52f);
            mats["M02_WheelGold"]=Mat("M02_WheelGold",Linear(.84f,.43f,.022f),.63f,.3f);
        }
        if(mid=="M04") {
            mats["M04_Paint_Print"]=Mat("M04_Paint_Print",Color.white,.68f,0,"M04_body.png");
            mats["M04_Cabin_Print"]=Mat("M04_Cabin_Print",Color.white,.70f,0,"M04_cabin.png");
            mats["M04_Paint_Solid"]=Mat("M04_Paint_Solid",Linear(.94f,.188f,.008f),.68f);
            mats["M04_Graphite"]=Mat("M04_Graphite",Linear(.015f,.022f,.03f),.53f);
            foreach(string name in new[]{"Wheel_Rubber","Wheel_Alloy","Wheel_Graphite","Wheel_Gold","Wheel_Ivory"})
                mats[name]=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Shared/Wheels/Materials/"+name+".mat");
        }
        var active=EditorSceneManager.GetActiveScene();
        if(active.isDirty && !string.IsNullOrEmpty(active.path)) EditorSceneManager.SaveScene(active);
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var car=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Models/"+mid+"_candidate.fbx"));
        car.name=mid+"_Candidate";
        car.transform.rotation=Quaternion.Euler(0,180,0)*car.transform.rotation;
        var audit=new Audit(); Bounds bounds=default; bool first=true;
        foreach(var r in car.GetComponentsInChildren<MeshRenderer>()) {
            var assigned=r.sharedMaterials;
            for(int i=0;i<assigned.Length;i++) {
                if(assigned[i]!=null && mats.TryGetValue(assigned[i].name,out var match)) assigned[i]=match;
                else throw new Exception("Unmatched imported material: "+(assigned[i]==null?"NULL":assigned[i].name));
            }
            r.sharedMaterials=assigned; audit.meshRenderers++;
            if(first) {bounds=r.bounds;first=false;} else bounds.Encapsulate(r.bounds);
            var mesh=r.GetComponent<MeshFilter>().sharedMesh;
            audit.triangles+=mesh.triangles.Length/3;
            if(mesh.uv.Length!=mesh.vertexCount) audit.missingUVs++;
        }
        audit.sizeMetres=bounds.size; audit.groundMinimum=bounds.min.y;
        if(bounds.size.x<.08f || bounds.size.x>.12f || Math.Abs(bounds.min.y)>.0005f) throw new Exception("Unexpected import size or grounding: "+bounds);
        PrefabUtility.SaveAsPrefabAsset(car,Root+"/Prefabs/Candidates/"+mid+"_"+labels[mid]+".prefab");
        // The saved prefab is in metres. The studio enlarges it for stable realtime shadows.
        const float studioScale=20;
        car.transform.localScale*=studioScale;
        var floor=GameObject.CreatePrimitive(PrimitiveType.Cube); floor.name="StudioGround";
        floor.transform.position=new Vector3(0,-.002f,0)*studioScale;floor.transform.localScale=new Vector3(2,.0035f,2)*studioScale;
        floor.GetComponent<Renderer>().sharedMaterial=Mat("Studio",new Color(.32f,.34f,.38f),.15f);
        UnityEngine.Object.DestroyImmediate(floor.GetComponent<Collider>());
        RenderSettings.ambientMode=AmbientMode.Trilight; RenderSettings.ambientSkyColor=new Color(.36f,.37f,.40f);
        RenderSettings.ambientEquatorColor=new Color(.22f,.23f,.25f); RenderSettings.ambientGroundColor=new Color(.12f,.13f,.15f);
        RenderSettings.skybox=null;
        foreach(var spec in new[]{new Vector3(42,-35,0),new Vector3(55,125,0)}) {
            var l=new GameObject("StudioLight").AddComponent<Light>(); l.type=LightType.Directional;
            l.transform.rotation=Quaternion.Euler(spec); l.intensity=spec.y<0?.9f:.30f; l.color=Color.white;
            l.shadows=LightShadows.Soft; l.shadowBias=.005f; l.shadowNormalBias=.01f;
        }
        var camera=new GameObject("CandidatePreviewCamera").AddComponent<Camera>(); camera.tag="MainCamera";
        camera.orthographic=true; camera.orthographicSize=.063f*studioScale; camera.nearClipPlane=.01f;camera.farClipPlane=10;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.30f,.35f,.43f);
        var paths=new List<string>();
        foreach(var entry in new[]{("three_quarter",new Vector3(-.16f,.105f,-.15f)),("rear",new Vector3(.16f,.08f,-.15f)),("side",new Vector3(0,.025f,-.20f))}) {
            camera.transform.position=entry.Item2*studioScale; camera.transform.LookAt(new Vector3(0,.023f,0)*studioScale);
            var rt=new RenderTexture(1200,900,24,RenderTextureFormat.ARGB32); rt.antiAliasing=4;
            camera.targetTexture=rt;camera.Render(); var previous=RenderTexture.active;RenderTexture.active=rt;
            var image=new Texture2D(1200,900,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,1200,900),0,0);image.Apply();
            string p="TinyRidersPremium/Previews/"+mid+"/unity_"+entry.Item1+".png";
            File.WriteAllBytes(Path.Combine(Project,p),image.EncodeToPNG());paths.Add(p);
            camera.targetTexture=null;RenderTexture.active=previous;rt.Release();
            UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(image);
        }
        camera.transform.position=new Vector3(-.16f,.105f,-.15f)*studioScale;camera.transform.LookAt(new Vector3(0,.023f,0)*studioScale);
        audit.captures=paths.ToArray();
        EditorSceneManager.SaveScene(scene,Root+"/PreviewScenes/"+mid+"_CandidateReview.unity"); AssetDatabase.SaveAssets();
        string json=JsonUtility.ToJson(audit,true);File.WriteAllText(Path.Combine(Project,"TinyRidersPremium/Previews/"+mid+"/unity_audit.json"),json);
        return json;
    }
}
