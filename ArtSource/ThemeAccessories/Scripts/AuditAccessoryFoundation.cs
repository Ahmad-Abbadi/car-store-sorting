using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;
using TinyRiders.Editor;
public static class AuditAccessoryFoundation
{
    static float[] V(Vector3 v)=>new[]{v.x,v.y,v.z};
    public static void Run()
    {
        string root=ThemedCarBuilder.Root,prod="TinyRidersProduction/ThemeAccessoryPass/";
        var cat=JsonUtility.FromJson<ThemedCarBuilder.Catalog>(File.ReadAllText(root+"/Data/Themes/variant_catalog.json"));
        var assignments=new List<object>();var geometry=new List<object>();
        foreach(var e in cat.entries)
        {
            var p=AssetDatabase.LoadAssetAtPath<GameObject>(e.prefabPath);
            assignments.Add(new{id=e.collectibleId,prefabPath=e.prefabPath,guid=AssetDatabase.AssetPathToGUID(e.prefabPath),nodes=p.GetComponentsInChildren<Transform>(true).Select(t=>new{path=AnimationUtility.CalculateTransformPath(t,p.transform),active=t.gameObject.activeSelf,position=V(t.localPosition),rotation=V(t.localEulerAngles),scale=V(t.localScale),components=t.GetComponents<Component>().Select(c=>c==null?"MISSING":c.GetType().FullName).ToArray(),mesh=t.GetComponent<MeshFilter>()==null?null:AssetDatabase.GetAssetPath(t.GetComponent<MeshFilter>().sharedMesh),materials=t.GetComponent<Renderer>()==null?null:t.GetComponent<Renderer>().sharedMaterials.Select(m=>AssetDatabase.GetAssetPath(m)).ToArray()}).ToArray()});
        }
        foreach(var e in cat.entries.GroupBy(e=>e.baseCarId).Select(g=>g.First()))
        {
            var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(e.prefabPath));
            try{
                var rs=go.GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled).ToArray();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
                geometry.Add(new{baseCarId=e.baseCarId,representative=e.collectibleId,boundsMin=V(b.min),boundsMax=V(b.max),renderers=rs.Select(r=>{
                    var f=r.GetComponent<MeshFilter>();var m=f.sharedMesh;var matrix=go.transform.worldToLocalMatrix*r.transform.localToWorldMatrix;
                    return new{name=r.name,path=AnimationUtility.CalculateTransformPath(r.transform,go.transform),meshPath=AssetDatabase.GetAssetPath(m),materials=r.sharedMaterials.Select(x=>AssetDatabase.GetAssetPath(x)).ToArray(),vertices=m.vertices.Select(v=>V(matrix.MultiplyPoint3x4(v))).ToArray(),submeshes=Enumerable.Range(0,m.subMeshCount).Select(i=>m.GetTriangles(i)).ToArray()};
                }).ToArray()});
            }finally{UnityEngine.Object.DestroyImmediate(go);}
        }
        var cameras=Resources.FindObjectsOfTypeAll<Camera>().Where(c=>c.gameObject.scene.IsValid()&&c.gameObject.scene.isLoaded).Select(c=>new{name=c.name,scene=c.gameObject.scene.path,position=V(c.transform.position),rotation=V(c.transform.eulerAngles),orthographic=c.orthographic,orthographicSize=c.orthographicSize,fieldOfView=c.fieldOfView,aspect=c.aspect,enabled=c.enabled}).ToArray();
        File.WriteAllText(prod+"baseline/assignments.json",JsonConvert.SerializeObject(assignments,Formatting.Indented));
        File.WriteAllText(prod+"baseline/base_geometry.json",JsonConvert.SerializeObject(geometry));
        File.WriteAllText(prod+"baseline/cameras.json",JsonConvert.SerializeObject(cameras,Formatting.Indented));
        Debug.Log("ACCESSORY_BASELINE_AUDIT "+assignments.Count+" variants; "+geometry.Count+" bases; "+cameras.Length+" cameras");
    }
}
