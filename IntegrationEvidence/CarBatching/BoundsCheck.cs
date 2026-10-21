using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using TinyRiders;

public static class BoundsCheck
{
    public static string Run()
    {
        var catalog=AssetDatabase.LoadAssetAtPath<ThemedCarCatalog>(ItemDetailBuilder.CatalogPath);
        Bounds Read(GameObject root)
        {
            var renderers=root.GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled).ToArray();
            var result=renderers[0].bounds;
            foreach(var renderer in renderers.Skip(1))result.Encapsulate(renderer.bounds);
            return result;
        }
        float error=0;int count=0;
        foreach(var entry in catalog.entries.Where(e=>e.enabled))
        {
            var generated=AssetDatabase.LoadAssetAtPath<GameObject>(CarVisualBatchBuilder.Root+"/"+
                AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(entry.prefab))+"/Visual.prefab");
            var a=Read(entry.prefab);var b=Read(generated);
            error=Math.Max(error,Math.Max((a.min-b.min).magnitude,(a.max-b.max).magnitude));count++;
        }
        string report="Compared "+count+" car bounds. Maximum corner displacement: "+error;
        File.WriteAllText(CarVisualBatchBuilder.Evidence+"/bounds.txt",report);
        if(error>.0005f)throw new Exception(report);
        return report;
    }
}
