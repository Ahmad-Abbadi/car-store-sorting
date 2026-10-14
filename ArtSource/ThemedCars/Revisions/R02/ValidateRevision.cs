using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TinyRiders;
using TinyRiders.Editor;
public static class ValidateRevision
{
    public static void Run()
    {
        ThemedCarBuilder.Validate();
        string root=ThemedCarBuilder.Root,rev="ArtSource/ThemedCars/Revisions/R02/";
        var cat=JsonUtility.FromJson<ThemedCarBuilder.Catalog>(File.ReadAllText(root+"/Data/Themes/variant_catalog.json"));
        var runtime=AssetDatabase.LoadAssetAtPath<ThemedCarCatalog>(root+"/Data/Themes/ThemedCarCatalog.asset");
        var errors=new List<string>();int materials=0;
        foreach(var e in cat.entries)
        {
            var p=AssetDatabase.LoadAssetAtPath<GameObject>(e.prefabPath);var identity=p.GetComponent<ThemedCarIdentity>();
            var ce=runtime.entries.Single(x=>x.collectibleId==e.collectibleId);
            if(e.contentRevision!=2||identity.contentRevision!=2||ce.contentRevision!=2)errors.Add("Revision mismatch "+e.collectibleId);
            if(ce.prefab!=p||ce.preview==null||AssetDatabase.GetAssetPath(ce.preview)!=e.previewPath)errors.Add("Catalog reference "+e.collectibleId);
            foreach(string path in new[]{e.previewPath,e.gameplayPreviewPath,e.thumbnailPath,e.previewPath.Replace("/Themes/","/Reverse/")})
                if(!File.Exists(path))errors.Add("Missing preview "+e.collectibleId+" "+path);
            foreach(var r in p.GetComponentsInChildren<Renderer>(true))foreach(var m in r.sharedMaterials)
            {
                materials++;
                if(m!=null&&AssetDatabase.GetAssetPath(m).StartsWith(root)&&m.shader==null)errors.Add("Missing shader "+e.collectibleId);
            }
        }
        var hierarchy=cat.entries.Select(e=>{
            var p=AssetDatabase.LoadAssetAtPath<GameObject>(e.prefabPath);
            return new{id=e.collectibleId,guid=AssetDatabase.AssetPathToGUID(e.prefabPath),nodes=p.GetComponentsInChildren<Transform>(true).Select(t=>new{path=AnimationUtility.CalculateTransformPath(t,p.transform),active=t.gameObject.activeSelf,position=t.localPosition.ToString("F5"),rotation=t.localRotation.ToString("F5"),scale=t.localScale.ToString("F5"),mesh=t.GetComponent<MeshFilter>()==null?null:AssetDatabase.GetAssetPath(t.GetComponent<MeshFilter>().sharedMesh)}).ToArray()};
        }).OrderBy(e=>e.id).ToArray();
        string after=JsonConvert.SerializeObject(hierarchy,Formatting.Indented);File.WriteAllText(rev+"after_hierarchy.json",after);
        bool unchanged=JToken.DeepEquals(JToken.Parse(File.ReadAllText(rev+"before_hierarchy.json")),JToken.Parse(after));
        if(!unchanged)errors.Add("Prefab hierarchy, GUID, mesh or transform changed");
        var report=new{passed=errors.Count==0,variants=cat.entries.Length,revision=2,runtimeCatalogEntries=runtime.entries.Length,materialsChecked=materials,prefabGuidsHierarchyTransformsMeshesUnchanged=unchanged,errors=errors};
        File.WriteAllText(root+"/Data/Themes/revision2_unity_validation.json",JsonConvert.SerializeObject(report,Formatting.Indented));
        AssetDatabase.Refresh();if(errors.Count>0)throw new Exception(string.Join("\n",errors));
        Debug.Log("R02_VALIDATION_OK 150");
    }
}
