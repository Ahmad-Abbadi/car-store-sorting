using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
public static class SnapshotHierarchy
{
    public static void Run(string name)
    {
        var cat=JObject.Parse(File.ReadAllText("Assets/TinyRiders/ThemedCollection/Data/Themes/variant_catalog.json"));
        var entries=cat["entries"].Select(e=>{
            string path=(string)e["prefabPath"];var p=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            return new{ id=(string)e["collectibleId"],guid=AssetDatabase.AssetPathToGUID(path),nodes=p.GetComponentsInChildren<Transform>(true).Select(t=>new{path=AnimationUtility.CalculateTransformPath(t,p.transform),active=t.gameObject.activeSelf,position=t.localPosition.ToString("F5"),rotation=t.localRotation.ToString("F5"),scale=t.localScale.ToString("F5"),mesh=t.GetComponent<MeshFilter>()==null?null:AssetDatabase.GetAssetPath(t.GetComponent<MeshFilter>().sharedMesh)}).ToArray()};
        }).OrderBy(e=>e.id).ToArray();
        File.WriteAllText("ArtSource/ThemedCars/Revisions/R02/"+name+"_hierarchy.json",JsonConvert.SerializeObject(entries,Formatting.Indented));
    }
}
