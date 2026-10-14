using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TinyRiders;
using TinyRiders.Editor;
public static class ValidateAccessories
{
    const string D=ThemeAccessoryBuilder.Prod;
    static float[] V(Vector3 v)=>new[]{v.x,v.y,v.z};
    static bool Same(Vector3 v,JToken j)=>Vector3.Distance(v,new Vector3((float)j[0],(float)j[1],(float)j[2]))<.001f;
    public static void Run(string pass="first")
    {
        var baseline=JArray.Parse(File.ReadAllText(D+"/baseline/assignments.json"));
        var cat=JObject.Parse(File.ReadAllText(ThemeAccessoryBuilder.Cars+"/Data/Themes/variant_catalog.json"));
        var runtime=AssetDatabase.LoadAssetAtPath<ThemedCarCatalog>(ThemeAccessoryBuilder.Cars+"/Data/Themes/ThemedCarCatalog.asset");
        var errors=new List<string>();var rows=new List<object>();var geometry=new List<object>();
        foreach(var e in cat["entries"])
        {
            string id=(string)e["collectibleId"],path=(string)e["prefabPath"],theme=(string)e["themeId"];
            var old=baseline.Single(r=>(string)r["id"]==id);var p=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if(AssetDatabase.AssetPathToGUID(path)!=(string)old["guid"])errors.Add(id+" prefab GUID changed");
            foreach(var n in old["nodes"])
            {
                string np=(string)n["path"];var t=np==""?p.transform:p.transform.Find(np);
                if(t==null){errors.Add(id+" missing old node "+np);continue;}
                if(t.gameObject.activeSelf!=(bool)n["active"]||!Same(t.localPosition,n["position"])||!Same(t.localEulerAngles,n["rotation"])||!Same(t.localScale,n["scale"]))errors.Add(id+" changed original transform/activity "+np);
                var comps=t.GetComponents<Component>().Select(c=>c==null?"MISSING":c.GetType().FullName).ToArray();
                if(!JToken.DeepEquals(JArray.FromObject(comps),n["components"]))errors.Add(id+" changed components "+np);
                var mf=t.GetComponent<MeshFilter>();if(mf!=null&&AssetDatabase.GetAssetPath(mf.sharedMesh)!=(string)n["mesh"])errors.Add(id+" changed mesh "+np);
                var r=t.GetComponent<Renderer>();if(r!=null&&!JToken.DeepEquals(JArray.FromObject(r.sharedMaterials.Select(m=>AssetDatabase.GetAssetPath(m)).ToArray()),n["materials"]))errors.Add(id+" changed materials "+np);
            }
            var roots=p.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="ThemeAccessories").ToArray();
            if(roots.Length!=1){errors.Add(id+" root count "+roots.Length);continue;}
            var ar=roots[0];if(ar.childCount!=1)errors.Add(id+" kit count "+ar.childCount);
            var prop=ar.GetChild(0);string kitPath=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(prop.gameObject);
            if(kitPath!=ThemeAccessoryBuilder.Art+"/Prefabs/"+theme+"/KIT_"+theme+".prefab")errors.Add(id+" missing shared kit instance");
            if(prop.GetComponentsInChildren<Component>().Any(c=>!(c is Transform)&&!(c is MeshFilter)&&!(c is MeshRenderer)))errors.Add(id+" unexpected accessory component");
            var meshes=prop.GetComponentsInChildren<MeshFilter>();var triangles=meshes.Sum(m=>m.sharedMesh.triangles.Length/3);
            if(triangles>2500||triangles==0)errors.Add(id+" geometry budget "+triangles);
            foreach(var mf in meshes)
            {
                if(!AssetDatabase.GetAssetPath(mf.sharedMesh).StartsWith(ThemeAccessoryBuilder.Art+"/Meshes/"+theme))errors.Add(id+" accessory mesh source");
                if(mf.sharedMesh.uv.Length!=mf.sharedMesh.vertexCount)errors.Add(id+" missing UVs");
                if(mf.sharedMesh.uv.Any(v=>float.IsNaN(v.x)||float.IsNaN(v.y)||v.x<0||v.x>1||v.y<0||v.y>1))errors.Add(id+" invalid UVs");
                var mat=mf.GetComponent<MeshRenderer>().sharedMaterials;if(mat.Length!=1||mat.Any(m=>m==null||m.shader.name!="Universal Render Pipeline/Lit"))errors.Add(id+" material/slot error");
            }
            if(p.GetComponent<ThemedCarIdentity>().contentRevision!=3||(int)e["contentRevision"]!=3||runtime.entries.Single(x=>x.collectibleId==id).contentRevision!=3)errors.Add(id+" revision mismatch");
            var identity=p.GetComponent<ThemedCarIdentity>();var runtimeEntry=runtime.entries.Single(x=>x.collectibleId==id);
            if(identity.collectibleId!=id||identity.baseCarId!=(string)e["baseCarId"]||identity.themeId!=theme||identity.colorwayId!=(string)e["colorwayId"])errors.Add(id+" prefab identity mismatch");
            if(runtimeEntry.prefab!=p||AssetDatabase.GetAssetPath(runtimeEntry.preview)!=(string)e["previewPath"])errors.Add(id+" catalog reference mismatch");
            rows.Add(new{id,rootCount=roots.Length,kitCount=ar.childCount,triangles,kitPath,prefabGuid=AssetDatabase.AssetPathToGUID(path),kitGuid=AssetDatabase.AssetPathToGUID(kitPath),meshGuids=meshes.Select(m=>AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(m.sharedMesh))).ToArray()});
            geometry.Add(new{id,vertices=meshes.SelectMany(m=>m.sharedMesh.vertices.Select(v=>V(p.transform.InverseTransformPoint(m.transform.TransformPoint(v))))).ToArray()});
        }
        var sceneCameras=Resources.FindObjectsOfTypeAll<Camera>().Where(c=>c.gameObject.scene.IsValid()&&c.gameObject.scene.isLoaded).ToArray();
        var renderers=Resources.FindObjectsOfTypeAll<MeshRenderer>().Where(r=>r.gameObject.scene.IsValid()&&r.gameObject.scene.isLoaded&&r.enabled&&r.gameObject.activeInHierarchy).ToArray();
        var sceneInfo=new{cameras=sceneCameras.Select(c=>new{c.name,scene=c.gameObject.scene.path,position=V(c.transform.position),rotation=V(c.transform.eulerAngles),c.fieldOfView,c.aspect,c.orthographic,visibleObjects=renderers.Where(r=>GeometryUtility.TestPlanesAABB(GeometryUtility.CalculateFrustumPlanes(c),r.bounds)).Take(100).Select(r=>new{r.name,center=V(r.bounds.center),size=V(r.bounds.size),prefab=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(r.gameObject)}).ToArray()}).ToArray()};
        File.WriteAllText(D+"/scene_camera_audit.json",JsonConvert.SerializeObject(sceneInfo,Formatting.Indented));
        File.WriteAllText(D+"/accessory_world_vertices.json",JsonConvert.SerializeObject(geometry));
        var report=new{pass,variantCount=rows.Count,errors,variants=rows,materialCount=AssetDatabase.FindAssets("t:Material",new[]{ThemeAccessoryBuilder.Art}).Length,kitCount=AssetDatabase.FindAssets("t:Prefab",new[]{ThemeAccessoryBuilder.Art+"/Prefabs"}).Length};
        File.WriteAllText(D+"/unity_validation_"+pass+".json",JsonConvert.SerializeObject(report,Formatting.Indented));
        if(errors.Count>0)throw new Exception("Accessory audit: "+errors.Count+" errors; see validation JSON");
        Debug.Log("ACCESSORY_VALIDATED "+pass+" "+rows.Count);
    }
}
