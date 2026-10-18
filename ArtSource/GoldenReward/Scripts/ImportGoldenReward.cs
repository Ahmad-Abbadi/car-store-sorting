using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class ImportGoldenReward
{
    const string A = "Assets/TinyRiders/Art/GoldenReward/";
    const string O = "ArtSource/GoldenReward/";
    static readonly Dictionary<string, Material> Mats = new Dictionary<string, Material>();
    static readonly List<string> Report = new List<string>();
    static void SaveOwnedAssets()
    {
        foreach(var guid in AssetDatabase.FindAssets("",new[]{A.TrimEnd('/')}))
        {
            var asset=AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guid));
            if(asset!=null)AssetDatabase.SaveAssetIfDirty(asset);
        }
    }
    static T Persist<T>(T obj, string path) where T : UnityEngine.Object
    {
        var old = AssetDatabase.LoadAssetAtPath<T>(path);
        if (old != null) { EditorUtility.CopySerialized(obj, old); UnityEngine.Object.DestroyImmediate(obj); EditorUtility.SetDirty(old); return old; }
        AssetDatabase.CreateAsset(obj, path); return obj;
    }
    static Material Mat(string name, Color color, float metal, float rough, string texture = null, float emission = 0)
    {
        var m = new Material(Shader.Find("Universal Render Pipeline/Lit")); m.name = "TR_" + name;
        m.SetColor("_BaseColor", color); m.SetFloat("_Metallic", metal); m.SetFloat("_Smoothness", 1-rough);
        if (texture != null) m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(A+"Textures/"+texture));
        if (emission > 0) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", color*emission); m.globalIlluminationFlags=MaterialGlobalIlluminationFlags.BakedEmissive; }
        if (name == "Cover")
        {
            m.SetFloat("_Surface",1);m.SetFloat("_Blend",0);m.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);m.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite",0);m.SetFloat("_Cull",(float)CullMode.Back);m.SetOverrideTag("RenderType","Transparent");m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");m.renderQueue=3000;
            m.SetShaderPassEnabled("ShadowCaster",false);
            m.SetFloat("_SpecularHighlights",0);m.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
            m.SetFloat("_EnvironmentReflections",0);m.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
            m.SetFloat("_BlendModePreserveSpecular",0);
        }
        m.enableInstancing=true;string key=m.name; Mats[key]=Persist(m,A+"Materials/"+key+".mat");return Mats[key];
    }
    static void Materials()
    {
        Mat("Gold",new Color(.92f,.65f,.22f),.88f,.23f); Mat("GoldEdge",new Color(1,.79f,.40f),.83f,.19f);
        Mat("Charcoal",new Color(.065f,.075f,.09f),.25f,.29f);Mat("Rubber",new Color(.08f,.085f,.095f),0,.60f);
        Mat("Window",new Color(.035f,.05f,.075f),.12f,.24f);Mat("Checker",Color.white,.12f,.34f,"TR_Checker_BaseColor.png");
        Mat("Grille",Color.white,.15f,.4f,"TR_Grille_BaseColor.png");Mat("WarmLight",new Color(1,.77f,.40f),.1f,.24f,null,1.2f);
        Mat("RedLight",new Color(.9f,.025f,.012f),.1f,.24f,null,1.1f);Mat("Fabric",new Color(.50f,.035f,.075f),0,.88f);
        Mat("Cover",new Color(.75f,.88f,1,.025f),0,.12f);Mat("Studio",new Color(.12f,.15f,.20f),.1f,.48f);
    }
    static Transform Find(Transform root, string name) { return root.GetComponentsInChildren<Transform>(true).First(t=>t.name==name); }
    static Bounds BoundsOf(GameObject root)
    {
        var rs=root.GetComponentsInChildren<Renderer>(true); var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b;
    }
    static GameObject MakePrefab(string name, string[] groupNames)
    {
        var model=AssetDatabase.LoadAssetAtPath<GameObject>(A+"Models/"+name+".fbx");
        if(model==null)throw new Exception("Missing FBX "+name);
        var source=UnityEngine.Object.Instantiate(model);source.name="TemporaryGoldenImport";
        var root=new GameObject(name);
        // Normalize the converted model into identity-root metric meshes. Determine forward from the actual grille.
        Quaternion correction=name.Contains("Accessories")?Quaternion.Euler(0,180,0):Quaternion.identity;
        var front=source.GetComponentsInChildren<Transform>().FirstOrDefault(t=>t.name=="Front grille" || t.name=="Base shield rim");
        if(front != null && front.position.z<0) correction=Quaternion.Euler(0,180,0);
        if(front!=null)Report.Add(name+" imported front="+front.position.ToString("F6")+" correction="+correction.eulerAngles);
        var matrix=Matrix4x4.Rotate(correction);
        Transform geometry=null;
        if(name.Contains("Display")){geometry=new GameObject("Geometry").transform;geometry.SetParent(root.transform,false);}
        foreach(var groupName in groupNames)
        {
            var src=groupName==name?source.transform:Find(source.transform,groupName);
            var dst=new GameObject(groupName).transform;
            dst.SetParent(geometry!=null && new[]{"Base","Columns","Canopy","CrownPlaques","SpotlightHousings"}.Contains(groupName)?geometry:root.transform,false);
            dst.position=matrix.MultiplyPoint3x4(src.position);
            if(groupName.StartsWith("Wheel_"))dst.name=groupName.Substring(0,groupName.LastIndexOf('_'))+(dst.position.x<0?"_L":"_R");
            var renderers=src.GetComponentsInChildren<MeshRenderer>();
            var byMat=new Dictionary<Material,List<CombineInstance>>();
            foreach(var renderer in renderers)
            {
                var mf=renderer.GetComponent<MeshFilter>(); if(mf==null || mf.sharedMesh==null)continue;
                var imported=renderer.sharedMaterials;
                for(int s=0;s<mf.sharedMesh.subMeshCount;s++)
                {
                    string matName=imported[s].name.Split('.')[0];
                    if(!Mats.TryGetValue(matName,out var mat))throw new Exception("Unmapped material "+matName);
                    if(!byMat.ContainsKey(mat))byMat[mat]=new List<CombineInstance>();
                    byMat[mat].Add(new CombineInstance{mesh=mf.sharedMesh,subMeshIndex=s,transform=dst.worldToLocalMatrix*matrix*renderer.localToWorldMatrix});
                }
            }
            var intermediate=new List<Mesh>();var pieces=new List<CombineInstance>();var materials=new List<Material>();
            foreach(var pair in byMat)
            {
                var part=new Mesh();part.indexFormat=IndexFormat.UInt32;part.CombineMeshes(pair.Value.ToArray(),true,true);intermediate.Add(part);
                pieces.Add(new CombineInstance{mesh=part,transform=Matrix4x4.identity});materials.Add(pair.Key);
            }
            if(pieces.Count>0)
            {
                var final=new Mesh();final.name=name+"_"+groupName;final.indexFormat=IndexFormat.UInt32;final.CombineMeshes(pieces.ToArray(),false,true);final.RecalculateBounds();
                var saved=Persist(final,A+"Models/"+final.name+".asset");dst.gameObject.AddComponent<MeshFilter>().sharedMesh=saved;
                var mr=dst.gameObject.AddComponent<MeshRenderer>();mr.sharedMaterials=materials.ToArray();mr.lightProbeUsage=LightProbeUsage.Off;mr.reflectionProbeUsage=ReflectionProbeUsage.BlendProbes;
                if(groupName=="GlassCover"){mr.shadowCastingMode=ShadowCastingMode.Off;mr.receiveShadows=false;}
            }
            foreach(var m in intermediate)UnityEngine.Object.DestroyImmediate(m);
            if(groupName=="TurntablePivot")
            {
                // Keep platter independent of the future rotation pivot and placement anchor.
                var platter=new GameObject("Platter");platter.transform.SetParent(dst,false);
                var mf=dst.GetComponent<MeshFilter>();var mr=dst.GetComponent<MeshRenderer>();
                platter.AddComponent<MeshFilter>().sharedMesh=mf.sharedMesh;platter.AddComponent<MeshRenderer>().sharedMaterials=mr.sharedMaterials;
                UnityEngine.Object.DestroyImmediate(mf);UnityEngine.Object.DestroyImmediate(mr);
                var anch=new GameObject("Anchor_Car");anch.transform.SetParent(dst,false);anch.transform.position=matrix.MultiplyPoint3x4(Find(source.transform,"Anchor_Car").position);
            }
        }
        var b=BoundsOf(root);Report.Add(name+" size="+b.size.ToString("F6")+" min="+b.min.ToString("F6")+" rootScale="+root.transform.localScale);
        var rs=root.GetComponentsInChildren<MeshRenderer>();long triangles=root.GetComponentsInChildren<MeshFilter>().Sum(f=>(long)f.sharedMesh.triangles.Length/3);
        Report.Add(name+" triangles="+triangles+" renderers="+rs.Length+" materialSlots="+rs.Sum(r=>r.sharedMaterials.Length)+" materials="+rs.SelectMany(r=>r.sharedMaterials).Distinct().Count()+" missingMaterials="+rs.Sum(r=>r.sharedMaterials.Count(m=>m==null))+" missingUV="+root.GetComponentsInChildren<MeshFilter>().Count(f=>f.sharedMesh.uv.Length!=f.sharedMesh.vertexCount));
        var prefab=PrefabUtility.SaveAsPrefabAsset(root,A+"Prefabs/"+name+".prefab");UnityEngine.Object.DestroyImmediate(source);UnityEngine.Object.DestroyImmediate(root);return prefab;
    }
    static Cubemap StudioCube()
    {
        int n=64;var cube=new Cubemap(n,TextureFormat.RGBAHalf,true);cube.name="TR_StudioReflection";
        for(int face=0;face<6;face++)
        {
            var pixels=new Color[n*n];
            for(int y=0;y<n;y++)for(int x=0;x<n;x++)
            {
                float u=(x+.5f)/n*2-1,v=(y+.5f)/n*2-1;Vector3 d;
                switch(face){case 0:d=new Vector3(1,-v,-u);break;case 1:d=new Vector3(-1,-v,u);break;case 2:d=new Vector3(u,1,v);break;case 3:d=new Vector3(u,-1,-v);break;case 4:d=new Vector3(u,-v,1);break;default:d=new Vector3(-u,-v,-1);break;}
                d.Normalize();float luminance=.12f+Mathf.Max(d.y,0)*.23f;
                float key=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.72f,.95f,Vector3.Dot(d,new Vector3(-.5f,.65f,.7f).normalized)));
                float rim=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.80f,.97f,Vector3.Dot(d,new Vector3(.8f,.3f,-.6f).normalized)));
                luminance+=key*1.2f+rim*.8f;
                pixels[y*n+x]=new Color(luminance,luminance*.96f,luminance*.89f,1);
            }
            cube.SetPixels(pixels,(CubemapFace)face);
        }
        cube.Apply(true,false);return Persist(cube,A+"Textures/TR_StudioReflection.asset");
    }
    static void Layer(GameObject g,int layer){foreach(var t in g.GetComponentsInChildren<Transform>(true))t.gameObject.layer=layer;}
    static void Capture(Camera cam,string filename)
    {
        // Supersample the camera capture without altering the project's render-scale or quality assets.
        var rt=new RenderTexture(2800,2200,24,RenderTextureFormat.ARGB32);cam.targetTexture=rt;cam.Render();
        var resolved=new RenderTexture(1400,1100,0,RenderTextureFormat.ARGB32);Graphics.Blit(rt,resolved);var old=RenderTexture.active;RenderTexture.active=resolved;
        var image=new Texture2D(resolved.width,resolved.height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,resolved.width,resolved.height),0,0);image.Apply();File.WriteAllBytes(O+"Renders/"+filename+".png",image.EncodeToPNG());
        RenderTexture.active=old;cam.targetTexture=null;UnityEngine.Object.DestroyImmediate(image);rt.Release();resolved.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(resolved);
    }
    public static string Main()
    {
        if(EditorApplication.isPlaying)throw new Exception("Art importer requires stopped Editor");
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        foreach(var path in Directory.GetFiles(A+"Textures","*.png"))
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(path.Replace('\\','/'));importer.sRGBTexture=true;importer.textureType=TextureImporterType.Default;importer.wrapMode=TextureWrapMode.Repeat;importer.maxTextureSize=512;importer.mipmapEnabled=true;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
        }
        foreach(var path in Directory.GetFiles(A+"Models","*.fbx"))
        {
            var importer=(ModelImporter)AssetImporter.GetAtPath(path.Replace('\\','/'));importer.globalScale=1;importer.useFileScale=true;importer.bakeAxisConversion=true;importer.importAnimation=false;importer.isReadable=true;importer.importCameras=false;importer.importLights=false;importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;importer.SaveAndReimport();
        }
        var previous=SceneManager.GetActiveScene();
        var preview=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(preview);
        try
        {
        Materials();
        var car=MakePrefab("TR_GoldenCar_Reward",new[]{"Body","Cabin","Trim","Wing","Wheel_Front_L","Wheel_Rear_L","Wheel_Front_R","Wheel_Rear_R"});
        var display=MakePrefab("TR_GoldenReward_Display",new[]{"Base","Columns","Canopy","CrownPlaques","SpotlightHousings","TurntablePivot","GlassCover","AccentEmission"});
        var accessories=MakePrefab("TR_GoldenReward_Accessories",new[]{"TR_GoldenReward_Accessories"});
        var assembly=new GameObject("TR_GoldenReward_Filled_ArtPreset");var d=(GameObject)PrefabUtility.InstantiatePrefab(display);d.transform.SetParent(assembly.transform,false);
        var c=(GameObject)PrefabUtility.InstantiatePrefab(car);c.transform.SetParent(Find(d.transform,"Anchor_Car"),false);
        var filled=PrefabUtility.SaveAsPrefabAsset(assembly,A+"Prefabs/TR_GoldenReward_Filled_ArtPreset.prefab");UnityEngine.Object.DestroyImmediate(assembly);
        var reflection=StudioCube();SaveOwnedAssets();
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.32f,.34f,.39f);RenderSettings.defaultReflectionMode=DefaultReflectionMode.Custom;RenderSettings.customReflectionTexture=reflection;RenderSettings.reflectionIntensity=1;
            RenderSettings.fog=false;RenderSettings.skybox=null;
            var displayInstance=(GameObject)PrefabUtility.InstantiatePrefab(display,preview);
            var carInstance=(GameObject)PrefabUtility.InstantiatePrefab(car,preview);carInstance.transform.SetParent(Find(displayInstance.transform,"Anchor_Car"),false);
            var optional=(GameObject)PrefabUtility.InstantiatePrefab(accessories,preview);optional.SetActive(false);
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="PreviewFloor";floor.transform.position=new Vector3(0,-.008f,0);floor.transform.localScale=new Vector3(8,.012f,8);floor.GetComponent<Renderer>().sharedMaterial=Mats["TR_Studio"];UnityEngine.Object.DestroyImmediate(floor.GetComponent<Collider>());
            var camObject=new GameObject("GoldenReward_PreviewCamera");var cam=camObject.AddComponent<Camera>();camObject.transform.position=new Vector3(.64f,.2625f,1.12f);camObject.transform.LookAt(new Vector3(0,.195f,0));cam.orthographic=false;cam.fieldOfView=26;cam.orthographicSize=.34f;cam.nearClipPlane=.005f;cam.farClipPlane=20;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.065f,.08f,.105f);cam.allowHDR=true;cam.cullingMask=1<<30;
            cam.scene=preview;
            cam.overrideSceneCullingMask=EditorSceneManager.GetSceneCullingMask(preview);
            foreach(var data in new[]{new[]{35f,-35f,0f,.9f},new[]{35f,145f,0f,.55f},new[]{65f,70f,0f,.3f}})
            {
                var lightObject=new GameObject("Preview soft light");var light=lightObject.AddComponent<Light>();light.type=LightType.Directional;light.intensity=data[3];light.color=new Color(1,.94f,.83f);light.shadows=data[3]>.8f?LightShadows.Soft:LightShadows.None;light.shadowBias=.001f;light.shadowNormalBias=.005f;light.shadowNearPlane=.01f;light.cullingMask=1<<30;light.transform.rotation=Quaternion.Euler(data[0],data[1],data[2]);
            }
            var probe=new GameObject("Preview local reflection").AddComponent<ReflectionProbe>();probe.mode=ReflectionProbeMode.Custom;probe.customBakedTexture=reflection;probe.size=Vector3.one*3;probe.transform.position=new Vector3(0,.3f,0);probe.boxProjection=false;probe.intensity=1;
            foreach(var go in preview.GetRootGameObjects())Layer(go,30);
            carInstance.SetActive(false);EditorSceneManager.SaveScene(preview,A+"PreviewScenes/TR_GoldenReward_Empty.unity");Capture(cam,"unity_empty_display");
            carInstance.SetActive(true);EditorSceneManager.SaveScene(preview,A+"PreviewScenes/TR_GoldenReward_Filled.unity");Capture(cam,"unity_filled_display");
            cam.orthographic=true;cam.transform.position=new Vector3(.48f,.40f,.64f);cam.transform.LookAt(new Vector3(0,.075f,0));cam.orthographicSize=.24f;displayInstance.SetActive(false);
            carInstance.transform.SetParent(null,true);carInstance.transform.position=Vector3.zero;carInstance.SetActive(true);Capture(cam,"unity_car");
            displayInstance.SetActive(true);carInstance.transform.SetParent(Find(displayInstance.transform,"Anchor_Car"),false);
            var comparison=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TinyRiders/Gameplay/Compact/Prefabs/Held_TR-C001-TH_FIRE-V01.prefab");
            var other=UnityEngine.Object.Instantiate(comparison);other.name="Existing collectible scale reference";other.transform.position=new Vector3(.48f,0,0);Layer(other,30);
            cam.transform.position=new Vector3(.75f,.52f,1.1f);cam.transform.LookAt(new Vector3(.15f,.18f,0));cam.orthographicSize=.43f;Capture(cam,"unity_scale_check");
            UnityEngine.Object.DestroyImmediate(other);
            cam.orthographic=false;cam.transform.position=new Vector3(.64f,.2625f,1.12f);cam.transform.LookAt(new Vector3(0,.195f,0));cam.orthographicSize=.34f;
            EditorSceneManager.SaveScene(preview,A+"PreviewScenes/TR_GoldenReward_Filled.unity");
            Report.Add("Preview lights=3 directional (1 shadow caster); no per-accent realtime lights; 1 custom local reflection probe; no volumes, bloom or particles.");
            Report.Add("Textures=2 x 512x512 sRGB base color, 1 x 64 cubemap HDR reflection; constants for metallic and smoothness (smoothness = 1 - Blender roughness). Cover: separate transparent renderer, alpha .025, no shadows.");
        }
        finally { EditorSceneManager.CloseScene(preview,true);SceneManager.SetActiveScene(previous); }
        File.WriteAllLines(O+"unity_audit.txt",Report);SaveOwnedAssets();return string.Join("\n",Report);
    }
}
