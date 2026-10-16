using System;
using System.IO;
using System.Linq;
using TinyRiders;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

public static class ReferenceInteriorReview
{
    const string Evidence = "IntegrationEvidence/ShopInterior/ReferenceMatch";
    static Transform Interior => GameObject.Find("Car Store Generated/Crowded Shop Interior").transform;
    static string PathOf(Transform t) => t.parent == null ? t.name : PathOf(t.parent) + "/" + t.name;
    public static string Snapshot()
    {
        var s = Object.FindFirstObjectByType<ThemedCollectionSession>();
        return Newtonsoft.Json.JsonConvert.SerializeObject(new {
            colliders = Interior.GetComponentsInChildren<Collider>(true).OrderBy(c=>PathOf(c.transform)).Select(c=>new {
                path=PathOf(c.transform),type=c.GetType().Name,position=c.transform.position.ToString("F5"),scale=c.transform.lossyScale.ToString("F5"),
                c.enabled,c.isTrigger,layer=c.gameObject.layer,material=AssetDatabase.GetAssetPath(c.sharedMaterial),
                center=c is BoxCollider b ? b.center.ToString("F5") : "",size=c is BoxCollider b2 ? b2.size.ToString("F5") : "" }),
            shelves=Object.FindObjectsByType<SortingShelf>(FindObjectsSortMode.None).OrderBy(v=>v.SaveId).Select(v=>new {v.SaveId,v.SpotCount,p=v.transform.position.ToString("F5"),q=v.transform.rotation.ToString("F5")}),
            signs=Interior.GetComponentsInChildren<TMPro.TextMeshPro>().Where(t=>t.name!="Shop Brand").OrderBy(t=>t.name).Select(t=>new{t.name,t.text,p=t.transform.position.ToString("F5"),q=t.transform.rotation.ToString("F5"),size=t.rectTransform.sizeDelta.ToString("F5"),color=t.color.ToString()}),
            lights=Interior.GetComponentsInChildren<Light>().OrderBy(t=>t.transform.position.x).ThenBy(t=>t.transform.position.z).Select(t=>new{t.name,t.type,t.intensity,t.range,t.shadows,p=t.transform.position.ToString("F5"),color=t.color.ToString()}),
            layout=AssetDatabase.GetAssetPath(s.roomLayout),pile=AssetDatabase.GetAssetPath(s.pileLayout),s.seed,s.visiblePileLimit,s.spawnEntireCollectionAtStart,s.boundedPile,
            floor=Interior.Find("Racing Floor Graphics").GetComponentsInChildren<MeshRenderer>().OrderBy(r=>r.name).Select(r=>new{r.name,mesh=AssetDatabase.GetAssetPath(r.GetComponent<MeshFilter>().sharedMesh),mat=EditorJsonUtility.ToJson(r.sharedMaterial)})
        },Newtonsoft.Json.Formatting.Indented);
    }
    public static void Baseline()
    {
        Directory.CreateDirectory(Evidence);
        File.WriteAllText(Evidence+"/preserved_before.json",Snapshot());
        Capture("Before_Empty");
    }
    public static string Audit()
    {
        if(File.ReadAllText(Evidence+"/preserved_before.json")!=Snapshot())throw new Exception("Preserved scene state differs from baseline.");
        var roots=new[]{Interior.Find("Shop Wall Finish"),Interior.Find("Shop Ceiling Finish")};
        if(roots.Any(r=>r==null)||roots.Any(r=>Interior.Cast<Transform>().Count(t=>t.name==r.name)!=1))throw new Exception("Missing or duplicate root.");
        var renderers=roots.SelectMany(r=>r.GetComponentsInChildren<MeshRenderer>()).ToArray();
        if(renderers.Sum(r=>r.sharedMaterials.Length)>8||roots.Any(r=>r.GetComponentsInChildren<Collider>(true).Length!=0))throw new Exception("Slot/collision budget exceeded.");
        if(renderers.Any(r=>r.gameObject.layer!=2||r.shadowCastingMode!=ShadowCastingMode.Off||!r.receiveShadows))throw new Exception("Decoration rendering/picking flags incorrect.");
        if(roots[1].GetComponentsInChildren<Renderer>().Any(r=>r.bounds.min.y<3.1199f))throw new Exception("Ceiling clearance violated.");
        var text="PASS: Original colliders, shelves, signs/text/colors, six shop lights, floor material data and pile configuration match baseline.\n"
            +"PASS: One wall root and one ceiling root; "+renderers.Length+" combined renderer/material slots; zero decoration colliders; Ignore Raycast; no shadow casting.\n"
            +"PASS: New ceiling geometry remains above 3.12 m.\n";
        File.WriteAllText(Evidence+"/structural_audit.txt",text);return text;
    }
    public static void Capture(string state)
    {
        Directory.CreateDirectory(Evidence);
        var s=Object.FindFirstObjectByType<ThemedCollectionSession>();var o=s.roomLayout.origin;
        File.WriteAllText(Evidence+"/"+state+"_capture.txt","Playing: "+Application.isPlaying+"\nLive pile: "+s.LivePileCount+"\nFrame: "+Time.frameCount);
        Render(state+"_entrance",o+new Vector3(0,1.65f,-11.1f),o+new Vector3(0,1.35f,2),false);
        Render(state+"_central",o+new Vector3(-1.7f,1.7f,-5.8f),o+new Vector3(.25f,1.5f,5),false);
        Render(state+"_outer",o+new Vector3(7.6f,1.7f,-10.9f),o+new Vector3(10.7f,2,1),false);
        Render(state+"_corner",o+new Vector3(7.7f,1.75f,9),o+new Vector3(11.8f,2.8f,12.8f),false);
        Render(state+"_directory",o+new Vector3(-3.2f,1.7f,-8.6f),o+new Vector3(-4.7f,2.7f,-12.95f),false);
        Render(state+"_ceiling",o+new Vector3(0,1.7f,-11),o+new Vector3(0,3.2f,-4),false);
        Render(state+"_detail",o+new Vector3(10.6f,2.6f,3.5f),o+new Vector3(12,2.8f,4),false);
        Render(state+"_hero",o+new Vector3(8.3f,1.85f,-7.5f),o+new Vector3(-2,1.7f,3),false);
        Render(state+"_logo",o+new Vector3(0,1.7f,-3.7f),o+new Vector3(0,3.16f,0),false);
        Render(state+"_theme01",o+new Vector3(-8.7f,1.8f,-8),o+new Vector3(-12,2.65f,-8),false);
        Render(state+"_theme02",o+new Vector3(-8.7f,1.8f,4),o+new Vector3(-12,2.65f,4),false);
        Render(state+"_theme03",o+new Vector3(8.2f,1.8f,9.7f),o+new Vector3(8.2f,2.65f,13),false);
        Render(state+"_theme04",o+new Vector3(8.7f,1.8f,2),o+new Vector3(12,2.65f,2),false);
        Render(state+"_theme05",o+new Vector3(8.7f,1.8f,-10),o+new Vector3(12,2.65f,-10),false);
        Render(state+"_theme06",o+new Vector3(-10,1.8f,-9.7f),o+new Vector3(-10,2.65f,-13),false);
        Render(state+"_theme07",o+new Vector3(4,1.8f,-9.7f),o+new Vector3(4,2.65f,-13),false);
        Render(state+"_theme08",o+new Vector3(9.3f,1.8f,-9.7f),o+new Vector3(9.3f,2.65f,-13),false);
        Render(state+"_overhead",o+new Vector3(0,30,0),o,true);
    }
    static void Render(string name,Vector3 position,Vector3 target,bool overhead)
    {
        var go=new GameObject("Temporary Interior Review Camera"){hideFlags=HideFlags.HideAndDontSave};var c=go.AddComponent<Camera>();
        if(Camera.main!=null)c.CopyFrom(Camera.main);
        c.enabled=false;c.fieldOfView=66;c.nearClipPlane=.03f;c.farClipPlane=80;c.transform.position=position;
        c.transform.rotation=overhead?Quaternion.Euler(90,0,0):Quaternion.LookRotation(target-position);
        c.orthographic=overhead;c.orthographicSize=14.2f;c.aspect=overhead?1:1.6f;
        var hidden=overhead?Interior.GetComponentsInChildren<Renderer>().Where(r=>r.enabled&&(r.name=="Store Ceiling"||r.name=="Ceiling Diffuser"||r.transform.IsChildOf(Interior.Find("Shop Ceiling Finish")??Interior.Find("Store Ceiling")))).ToArray():Array.Empty<Renderer>();
        int w=overhead?1400:1600,h=overhead?1400:1000;var rt=new RenderTexture(w,h,24);var previous=RenderTexture.active;Texture2D tex=null;
        try{foreach(var r in hidden)r.enabled=false;c.targetTexture=rt;c.Render();RenderTexture.active=rt;tex=new Texture2D(w,h,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,w,h),0,0);tex.Apply();File.WriteAllBytes(Evidence+"/"+name+".png",tex.EncodeToPNG());}
        finally{foreach(var r in hidden)r.enabled=true;c.targetTexture=null;RenderTexture.active=previous;Object.DestroyImmediate(go);Object.DestroyImmediate(rt);if(tex!=null)Object.DestroyImmediate(tex);}
    }
}
