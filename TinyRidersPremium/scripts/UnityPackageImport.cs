using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class UnityPackageImport
{
    const string Root="Assets/TinyRidersPremium";
    static string Project=Directory.GetParent(Application.dataPath).FullName;
    static Material Make(string name,string texture,Color color,bool clear=false)
    {
        string path=Root+"/Packaging/Materials/"+name+".mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        var shader=Shader.Find(clear?"TinyRiders/PET Blister":texture==null?"Universal Render Pipeline/Lit":"Universal Render Pipeline/Unlit");
        if(m==null){m=new Material(shader);AssetDatabase.CreateAsset(m,path);} else m.shader=shader;
        m.SetColor("_BaseColor",color);
        if(texture!=null)m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Packaging/Textures/"+texture));
        if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",clear?.90f:.18f);
        if(clear){
            m.SetFloat("_EdgeOpacity",.48f);m.SetFloat("_Gloss",160);
            m.SetOverrideTag("RenderType","Transparent");
            m.renderQueue=(int)RenderQueue.Transparent;
        }
        EditorUtility.SetDirty(m);return m;
    }
    [Serializable] class Audit {
        public string status="PACKAGE_CANDIDATE_REQUIRES_VISUAL_REVIEW";
        public int packageMeshRenderers,carMeshRenderers,missingUVs,missingMaterials=0;
        public Vector3 cardSizeMetres,carSizeMetres;
        public string[] captures;
    }
    public static string Main()
    {
        foreach(var f in new[]{"Models","Textures","Materials","Prefabs"})Directory.CreateDirectory(Path.Combine(Project,Root,"Packaging",f));
        File.Copy(Path.Combine(Project,"TinyRidersPremium/Exports/Packaging/PKG_Master.fbx"),Path.Combine(Project,Root,"Packaging/Models/PKG_Master.fbx"),true);
        foreach(var f in new[]{"TC_master_front.png","TC_master_back.png"})File.Copy(Path.Combine(Project,"TinyRidersPremium/Textures/Packaging/Candidates",f),Path.Combine(Project,Root,"Packaging/Textures",f),true);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var modelPath=Root+"/Packaging/Models/PKG_Master.fbx";
        var mi=(ModelImporter)AssetImporter.GetAtPath(modelPath);mi.importCameras=false;mi.importLights=false;mi.importAnimation=false;
        mi.importNormals=ModelImporterNormals.Import;mi.isReadable=true;mi.SaveAndReimport();
        foreach(var f in new[]{"TC_master_front.png","TC_master_back.png"}){
            var ti=(TextureImporter)AssetImporter.GetAtPath(Root+"/Packaging/Textures/"+f);ti.sRGBTexture=true;ti.maxTextureSize=2048;
            ti.textureCompression=TextureImporterCompression.Uncompressed;ti.wrapMode=TextureWrapMode.Clamp;ti.SaveAndReimport();
        }
        var mats=new Dictionary<string,Material>{
            {"PKG_Front_Print",Make("PKG_Front_Print","TC_master_front.png",Color.white)},
            {"PKG_Back_Print",Make("PKG_Back_Print","TC_master_back.png",Color.white)},
            {"PKG_Cardboard_Edge",Make("PKG_Cardboard_Edge",null,new Color(.65f,.55f,.4f))},
            {"PKG_Clear_PET",Make("PKG_Clear_PET",null,new Color(.85f,.94f,1,.035f),true)}
        };
        var active=EditorSceneManager.GetActiveScene();if(active.isDirty && !string.IsNullOrEmpty(active.path))EditorSceneManager.SaveScene(active);
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var product=new GameObject("TinyRiders_PackageMaster_Candidate");
        var pack=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(modelPath));pack.transform.SetParent(product.transform,false);
        pack.transform.rotation=Quaternion.Euler(0,180,0)*pack.transform.rotation;
        var audit=new Audit();
        foreach(var r in pack.GetComponentsInChildren<MeshRenderer>()){
            var materials=r.sharedMaterials;
            for(int i=0;i<materials.Length;i++){
                if(materials[i]!=null && mats.TryGetValue(materials[i].name,out var m))materials[i]=m;
                else throw new Exception("Unmatched package material "+materials[i]?.name);
            }
            r.sharedMaterials=materials;audit.packageMeshRenderers++;
            var mesh=r.GetComponent<MeshFilter>().sharedMesh;if(mesh.uv.Length!=mesh.vertexCount)audit.missingUVs++;
            if(r.name.Contains("Card_"))audit.cardSizeMetres=r.bounds.size;
        }
        if(audit.packageMeshRenderers!=5)throw new Exception("Unexpected package mesh count: "+audit.packageMeshRenderers);
        var car=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/Candidates/M03_Blue23.prefab"));
        car.transform.SetParent(product.transform,false);car.transform.position=new Vector3(-.00069f,.0325f,-.035f);
        Bounds carBounds=default;bool first=true;
        foreach(var r in car.GetComponentsInChildren<Renderer>()){
            audit.carMeshRenderers++;if(first){carBounds=r.bounds;first=false;}else carBounds.Encapsulate(r.bounds);
            foreach(var material in r.sharedMaterials)if(material==null)audit.missingMaterials++;
        }
        audit.carSizeMetres=carBounds.size;
        PrefabUtility.SaveAsPrefabAsset(product,Root+"/Packaging/Prefabs/BlueRacer_PackageCandidate.prefab");
        // Studio enlargement only; the saved product remains at physical size.
        product.transform.localScale=Vector3.one*10;
        var ground=GameObject.CreatePrimitive(PrimitiveType.Cube);ground.name="PackageStudioGround";
        ground.transform.position=new Vector3(0,-.015f,0);ground.transform.localScale=new Vector3(30,.02f,30);
        ground.GetComponent<Renderer>().sharedMaterial=Make("PackageStudio",null,new Color(.3f,.34f,.4f));UnityEngine.Object.DestroyImmediate(ground.GetComponent<Collider>());
        RenderSettings.skybox=null;RenderSettings.ambientMode=AmbientMode.Trilight;
        RenderSettings.ambientSkyColor=new Color(.36f,.37f,.4f);RenderSettings.ambientEquatorColor=new Color(.22f,.23f,.25f);RenderSettings.ambientGroundColor=new Color(.12f,.13f,.15f);
        foreach(var spec in new[]{new Vector3(42,-35,0),new Vector3(55,125,0)}){
            var l=new GameObject("PackageStudioLight").AddComponent<Light>();l.type=LightType.Directional;l.transform.rotation=Quaternion.Euler(spec);
            l.intensity=spec.y<0?.9f:.3f;l.shadows=LightShadows.Soft;l.shadowBias=.005f;l.shadowNormalBias=.01f;
        }
        var camera=new GameObject("PackagePreviewCamera").AddComponent<Camera>();camera.tag="MainCamera";camera.orthographic=true;
        camera.orthographicSize=1.18f;camera.nearClipPlane=.01f;camera.farClipPlane=30;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.3f,.34f,.4f);
        var paths=new List<string>();
        foreach(var shot in new[]{("front",new Vector3(0,.90f,-5)),("three_quarter",new Vector3(-2.6f,2.1f,-4.8f)),("back",new Vector3(0,.9f,5))}){
            camera.transform.position=shot.Item2;camera.transform.LookAt(new Vector3(0,.9f,shot.Item1=="three_quarter"?-.25f:0));
            var rt=new RenderTexture(900,1200,24,RenderTextureFormat.ARGB32);rt.antiAliasing=4;camera.targetTexture=rt;camera.Render();
            var previous=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(900,1200,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,900,1200),0,0);image.Apply();
            string p="TinyRidersPremium/Previews/Packaging/unity_package_"+shot.Item1+".png";File.WriteAllBytes(Path.Combine(Project,p),image.EncodeToPNG());paths.Add(p);
            camera.targetTexture=null;RenderTexture.active=previous;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(image);
        }
        camera.transform.position=new Vector3(-2.6f,2.1f,-4.8f);camera.transform.LookAt(new Vector3(0,.9f,-.25f));
        audit.captures=paths.ToArray();EditorSceneManager.SaveScene(scene,Root+"/PreviewScenes/PackageMaster_CandidateReview.unity");AssetDatabase.SaveAssets();
        string json=JsonUtility.ToJson(audit,true);File.WriteAllText(Path.Combine(Project,"TinyRidersPremium/Previews/Packaging/unity_package_audit.json"),json);return json;
    }
}
