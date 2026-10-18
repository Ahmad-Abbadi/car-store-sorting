using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using TinyRiders.Quests;
using Object = UnityEngine.Object;

public static class BoardReview
{
    const string Output = "IntegrationEvidence/CollectorBoard";
    public static string Detail(string name)
    {
        var board=Object.FindFirstObjectByType<CollectorQuestBoard>();
        var go=new GameObject("Temporary badge review camera");var camera=go.AddComponent<Camera>();
        camera.fieldOfView=38;camera.nearClipPlane=.02f;camera.farClipPlane=80;
        go.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing=false;
        go.transform.position=board.transform.TransformPoint(new Vector3(-.64f,2.33f,-.91f));
        go.transform.LookAt(board.transform.TransformPoint(new Vector3(-.38f,2.28f,-.14f)));
        try{Save(camera,Output+"/"+name+".png",1400,1000);return Output+"/"+name+".png";}
        finally{Object.DestroyImmediate(go);}
    }
    public static string Back()
    {
        if(Application.isPlaying)throw new Exception("Review the asset back outside Play mode");
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(TinyRiders.Editor.CollectorQuestBuilder.BoardPath);
        var model=Object.Instantiate(source);model.hideFlags=HideFlags.HideAndDontSave;
        model.transform.SetPositionAndRotation(new Vector3(0,0,-300),Quaternion.identity);
        foreach(var t in model.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;
        var go=new GameObject("Temporary rear inspection camera");var camera=go.AddComponent<Camera>();
        camera.fieldOfView=43;camera.nearClipPlane=.05f;camera.farClipPlane=10;camera.cullingMask=1<<31;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.16f,.20f,.22f);
        go.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing=false;
        go.transform.position=model.transform.position+new Vector3(1.4f,1.6f,3.65f);go.transform.LookAt(model.transform.position+Vector3.up*1.3f);
        try{Save(camera,Output+"/finished_back.png",1200,1400);return "Finished back captured from a temporary prefab copy, without changing scene placement";}
        finally{Object.DestroyImmediate(go);Object.DestroyImmediate(model);}
    }
    public static string Front(string name)
    {
        var board=Object.FindFirstObjectByType<CollectorQuestBoard>();
        var go=new GameObject("Temporary board detail camera");var camera=go.AddComponent<Camera>();
        camera.fieldOfView=43;camera.nearClipPlane=.05f;camera.farClipPlane=80;
        go.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing=false;
        go.transform.position=board.transform.TransformPoint(new Vector3(0,1.55f,-3.75f));
        go.transform.LookAt(board.transform.TransformPoint(new Vector3(0,1.3f,0)));
        try{Save(camera,Output+"/"+name+".png",1400,1600);return Output+"/"+name+".png";}
        finally{Object.DestroyImmediate(go);}
    }
    public static string Capture(string stage)
    {
        Directory.CreateDirectory(Output);
        var board = Object.FindFirstObjectByType<CollectorQuestBoard>();
        var lighting = Object.FindFirstObjectByType<ShowroomLightingController>();
        // Restore the exact authored lighting, including any manual overrides, after review.
        var lights=lighting ? lighting.ceilingLights.Concat(new[]{lighting.keyLight}).Where(l=>l).Distinct().ToArray() : new Light[0];
        var lightData=lights.Select(EditorJsonUtility.ToJson).ToArray();var rotations=lights.Select(l=>l.transform.rotation).ToArray();
        var materials=lighting ? lighting.surfaces.Select(s=>s.renderer?s.renderer.sharedMaterial:null).ToArray() : new Material[0];
        var profile=lighting&&lighting.volume?lighting.volume.sharedProfile:null;
        var probe=lighting?lighting.reflectionProbe:null;var probeData=probe?EditorJsonUtility.ToJson(probe):null;
        var sky=RenderSettings.ambientSkyColor;var equator=RenderSettings.ambientEquatorColor;var ground=RenderSettings.ambientGroundColor;
        var mode=RenderSettings.ambientMode;float intensity=RenderSettings.ambientIntensity,reflection=RenderSettings.reflectionIntensity;
        bool fog=RenderSettings.fog;var sun=RenderSettings.sun;
        var go = new GameObject("Temporary Board Review Camera");
        var camera = go.AddComponent<Camera>();
        camera.fieldOfView = 43; camera.nearClipPlane = .05f; camera.farClipPlane = 80;
        camera.clearFlags = CameraClearFlags.Skybox;
        go.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = false;
        try
        {
            foreach (var theme in new[] {LightingTheme.Bright, LightingTheme.Dark})
            {
                if(lighting) lighting.ApplyTheme(theme);
                var views = new[] {new Vector3(0,1.55f,-3.75f), new Vector3(-1.8f,1.6f,-3.2f), new Vector3(1.8f,1.6f,-3.2f),new Vector3(0,1.65f,-1.95f),new Vector3(0,1.6f,3.7f)};
                var names = new[]{"front","left","right","detail","back"};
                for(int i=0;i<views.Length;i++)
                {
                    go.transform.position = board.transform.TransformPoint(views[i]);
                    go.transform.LookAt(board.transform.TransformPoint(new Vector3(0,i==3?1.58f:1.3f,0)));
                    Save(camera, Output+"/"+stage+"_"+theme+"_"+names[i]+".png",1400,1600);
                }
                go.transform.position = new Vector3(500,1.65f,-17.1f);
                go.transform.LookAt(board.transform.TransformPoint(new Vector3(0,1.35f,0)));
                Save(camera,Output+"/"+stage+"_"+theme+"_entrance.png",1600,1000);
            }
        }
        finally
        {
            for(int i=0;i<lights.Length;i++){EditorJsonUtility.FromJsonOverwrite(lightData[i],lights[i]);lights[i].transform.rotation=rotations[i];}
            if(lighting)for(int i=0;i<materials.Length;i++)if(lighting.surfaces[i].renderer)lighting.surfaces[i].renderer.sharedMaterial=materials[i];
            if(lighting&&lighting.volume)lighting.volume.sharedProfile=profile;if(probe)EditorJsonUtility.FromJsonOverwrite(probeData,probe);
            RenderSettings.ambientMode=mode;RenderSettings.ambientSkyColor=sky;RenderSettings.ambientEquatorColor=equator;RenderSettings.ambientGroundColor=ground;
            RenderSettings.ambientIntensity=intensity;RenderSettings.reflectionIntensity=reflection;RenderSettings.fog=fog;RenderSettings.sun=sun;
            Object.DestroyImmediate(go);
        }
        var c=board.GetComponent<BoxCollider>();
        var report=new {stage, position=board.transform.position.ToString("F4"),rotation=board.transform.rotation.ToString("F4"), scale=board.transform.localScale.ToString("F4"),layer=board.gameObject.layer,colliderCenter=c.center.ToString("F4"),colliderSize=c.size.ToString("F4"),ui=GlobalObjectId.GetGlobalObjectIdSlow(board.interfaceUI).ToString(),guid=AssetDatabase.AssetPathToGUID(TinyRiders.Editor.CollectorQuestBuilder.BoardPath),renderers=board.GetComponentsInChildren<Renderer>(true).Length,triangles=board.GetComponentsInChildren<MeshFilter>(true).Where(m=>m.sharedMesh).Sum(m=>m.sharedMesh.triangles.Length/3)};
        var json=report.ToString();
        File.WriteAllText(Output+"/"+stage+".txt",json);
        return json;
    }
    static void Save(Camera camera,string path,int width,int height)
    {
        var rt=RenderTexture.GetTemporary(width,height,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB,4);
        var old=RenderTexture.active;
        Texture2D image=null;
        try { camera.targetTexture=rt; camera.Render(); RenderTexture.active=rt; image=new Texture2D(width,height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG()); }
        finally {camera.targetTexture=null;RenderTexture.active=old;RenderTexture.ReleaseTemporary(rt);if(image)Object.DestroyImmediate(image);}
    }
}
