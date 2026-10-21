using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using TinyRiders;

using Object = UnityEngine.Object;

public static class GeometryProbe
{
    const string Root = "IntegrationEvidence/RenderingArtifacts";
    public static string Capture(string stage)
    {
        Directory.CreateDirectory(Root + "/" + stage);
        var shelves = Object.FindObjectsByType<SortingShelf>(FindObjectsSortMode.None);
        foreach (var theme in new[] { "TH_AVIATION", "TH_RAILWAY" })
        {
            var shelf = shelves.First(s => s.GetComponent<ThemeShelfBinding>().themeId == theme);
            var p = shelf.transform.position;
            Shot(stage + "/" + theme, p + Vector3.up * 1.6f + shelf.transform.forward * 2.3f,
                p + Vector3.up * 1.56f, 40);
        }
        var interior = GameObject.Find("Collector Promenade").transform;
        var origin = interior.position;
        Shot(stage + "/ceiling", origin + new Vector3(0,1.6f,-8), origin + new Vector3(0,3.2f,1),65);
        Shot(stage + "/wall", origin + new Vector3(9,2.6f,0), origin + new Vector3(12,2.6f,0),65);
        return stage;
    }
    public static string CaptureThemes()
    {
        var shelves=Object.FindObjectsByType<SortingShelf>(FindObjectsSortMode.None)
            .GroupBy(s=>s.GetComponent<ThemeShelfBinding>().themeId).OrderBy(g=>g.Key).Select(g=>g.First()).ToArray();
        var go=new GameObject("Theme contact sheet camera");var camera=go.AddComponent<Camera>();
        if(Camera.main)camera.CopyFrom(Camera.main);camera.enabled=false;camera.fieldOfView=40;
        var rt=new RenderTexture(512,288,24);var previous=RenderTexture.active;
        var sheet=new Texture2D(2560,2304,TextureFormat.RGB24,false);
        try
        {
            for(int i=0;i<shelves.Length;i++)
            {
                var shelf=shelves[i];var p=shelf.transform.position;
                camera.transform.position=p+Vector3.up*1.6f+shelf.transform.forward*2.3f;camera.transform.LookAt(p+Vector3.up*1.56f);
                camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
                sheet.ReadPixels(new Rect(0,0,512,288),(i%5)*512,(7-i/5)*288);
            }
            sheet.Apply();File.WriteAllBytes(Root+"/all-40-themes.png",sheet.EncodeToPNG());
        }
        finally {camera.targetTexture=null;RenderTexture.active=previous;Object.DestroyImmediate(sheet);Object.DestroyImmediate(rt);Object.DestroyImmediate(go);}
        return shelves.Length+" themes captured";
    }
    static void Shot(string name, Vector3 position, Vector3 target, float fov)
    {
        var go = new GameObject("Rendering probe"); var camera = go.AddComponent<Camera>();
        if(Camera.main) camera.CopyFrom(Camera.main);
        camera.enabled = false; camera.fieldOfView = fov;
        camera.transform.position = position; camera.transform.LookAt(target);
        var rt = new RenderTexture(1280,720,24); var previous = RenderTexture.active;
        var tex = new Texture2D(1280,720,TextureFormat.RGB24,false);
        try { camera.targetTexture=rt; camera.Render(); RenderTexture.active=rt;
            tex.ReadPixels(new Rect(0,0,1280,720),0,0); tex.Apply();
            File.WriteAllBytes(Root + "/" + name + ".png",tex.EncodeToPNG()); }
        finally { camera.targetTexture=null; RenderTexture.active=previous; Object.DestroyImmediate(tex); Object.DestroyImmediate(rt); Object.DestroyImmediate(go); }
    }
    public static string CompressCopies(bool menuOnly = false)
    {
        var filters=Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None)
            .Where(f=>f.sharedMesh && (AssetDatabase.GetAssetPath(f.sharedMesh).StartsWith("Assets/TinyRiders/Art/ShopInterior/Meshes/") ||
                !menuOnly && (AssetDatabase.GetAssetPath(f.sharedMesh).StartsWith("Assets/TinyRiders/Art/MutedShopSurfaces/Meshes/") ||
                AssetDatabase.GetAssetPath(f.sharedMesh).StartsWith("Assets/TinyRiders/Gameplay/Rendering/Shelf_")))).ToArray();
        const string cache="Assets/RenderingCompressionProbe";
        if(!AssetDatabase.IsValidFolder(cache)) AssetDatabase.CreateFolder("Assets","RenderingCompressionProbe");
        var report=new List<object>();
        foreach(var group in filters.GroupBy(f=>f.sharedMesh))
        {
            var source=group.Key; var sourceVertices=source.vertices;
            var path=cache+"/"+AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source))+".asset";
            var copy=Object.Instantiate(source); MeshUtility.SetMeshCompression(copy,ModelImporterMeshCompression.High);
            AssetDatabase.CreateAsset(copy,path); AssetDatabase.SaveAssets();
            Resources.UnloadAsset(copy); AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
            copy=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            var vertices=copy.vertices;
            var sourceUV=source.uv; var compressedUV=copy.uv;
            report.Add(new {source=AssetDatabase.GetAssetPath(source),vertices=vertices.Length,
                maxPositionError=sourceVertices.Length==vertices.Length ? sourceVertices.Select((v,i)=>Vector3.Distance(v,vertices[i])).Max() : -1,
                uniqueYBefore=sourceVertices.Select(v=>v.y).Distinct().Count(),uniqueYAfter=vertices.Select(v=>v.y).Distinct().Count(),
                maxUVError=sourceUV.Length==compressedUV.Length ? sourceUV.Select((v,i)=>Vector2.Distance(v,compressedUV[i])).DefaultIfEmpty().Max() : -1});
            foreach(var filter in group)filter.sharedMesh=copy;
        }
        File.WriteAllText(Root+"/"+(menuOnly?"menu-":"")+"compression-comparison.txt",string.Join(Environment.NewLine,report.Select(r=>r.ToString())));
        return "Compressed copies: "+report.Count;
    }
    public static string Menu(string stage)
    {
        if(stage=="original")UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/00_Project/00_Scenes/MainMenu Scene.unity");
        if(stage=="compressed")CompressCopies(true);
        var camera=Camera.main;
        Shot("menu-"+stage,camera.transform.position,camera.transform.position+camera.transform.forward*5,camera.fieldOfView);
        return "menu-"+stage;
    }
    public static string Gaps()
    {
        var lines=new List<string>();
        foreach(var entry in ThemedShelfArtBuilder.Catalog.entries)
        {
            string prefabGuid=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(entry.prefab));
            var mat=entry.materials.Single(m=>m.name=="Signature_"+entry.themeId);
            string path="Assets/TinyRiders/Gameplay/Rendering/Shelf_"+prefabGuid+"_"+AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(mat))+"_1_True.asset";
            var source=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            var compressed=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/RenderingCompressionProbe/"+AssetDatabase.AssetPathToGUID(path)+".asset");
            if(!source||!compressed)continue;
            var v=source.vertices;var cv=compressed.vertices;
            var filters=entry.prefab.GetComponentsInChildren<MeshFilter>(true);
            foreach(var icon in filters.Where(f=>f.name.StartsWith("SignatureIcon")))
            {
                var plaque=filters.Single(f=>f.name=="SignaturePlaque"+icon.name.Substring("SignatureIcon".Length));
                Vector3 Normal(MeshFilter f)=>entry.prefab.transform.worldToLocalMatrix.MultiplyVector(f.transform.localToWorldMatrix.inverse.transpose.MultiplyVector(f.sharedMesh.normals[0])).normalized;
                var n=Normal(icon);
                Vector3[] Points(MeshFilter f)=>f.sharedMesh.vertices.Select(p=>entry.prefab.transform.InverseTransformPoint(f.transform.TransformPoint(p))).ToArray();
                Vector3 Quantized(Vector3 p){int best=0;float distance=float.MaxValue;for(int i=0;i<v.Length;i++){float d=(v[i]-p).sqrMagnitude;if(d<distance){distance=d;best=i;}}if(distance>.000001f)throw new Exception("Unmatched source vertex "+entry.themeId+" / "+distance);return cv[best];}
                var ip=Points(icon);var pp=Points(plaque);
                float before=ip.Min(p=>Vector3.Dot(p,n))-pp.Max(p=>Vector3.Dot(p,n));
                float after=ip.Min(p=>Vector3.Dot(Quantized(p),n))-pp.Max(p=>Vector3.Dot(Quantized(p),n));
                lines.Add(entry.themeId+"/"+icon.name+": sourceGap="+before+" compressedGap="+after);
            }
        }
        File.WriteAllLines(Root+"/symbol-gap-comparison.txt",lines);return string.Join("\n",lines);
    }
}



