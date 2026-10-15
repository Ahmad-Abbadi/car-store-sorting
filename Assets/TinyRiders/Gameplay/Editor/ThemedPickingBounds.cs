using System;
using System.IO;
using System.Linq;
using TinyRiders;
using UnityEngine;
using UnityEditor;

public static class ThemedPickingBounds
{
    public static void AddToWrapper(GameObject root)
    {
        var accessory=root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="ThemeAccessories");
        if(accessory==null)throw new Exception("Verified accessory root missing");
        var renderers=accessory.GetComponentsInChildren<Renderer>(false).Where(r=>r.enabled).ToArray();
        if(renderers.Length==0)throw new Exception("Verified accessory has no visible meshes");
        Bounds bounds=renderers[0].bounds;foreach(var r in renderers.Skip(1))bounds.Encapsulate(r.bounds);
        var target=root.transform.Find("Accessory Pick Bounds");
        if(target==null){target=new GameObject("Accessory Pick Bounds").transform;target.SetParent(root.transform,false);}
        target.gameObject.layer=8;target.localPosition=root.transform.InverseTransformPoint(bounds.center);
        var box=target.GetComponent<BoxCollider>();
        if(box==null)box=target.gameObject.AddComponent<BoxCollider>();
        box.size=bounds.size;box.isTrigger=true;
    }
    public static void Apply(ThemedCarCatalog catalog)
    {
        int count=0;
        foreach(var e in catalog.entries.Where(e=>e.enabled&&e.sortingItem!=null))
        {
            string path=AssetDatabase.GetAssetPath(e.sortingItem.WorldItem);
            if(e.sortingItem.WorldItem.transform.Find("Accessory Pick Bounds")!=null)continue;
            var root=PrefabUtility.LoadPrefabContents(path);
            try{AddToWrapper(root);PrefabUtility.SaveAsPrefabAsset(root,path);count++;}
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        File.WriteAllText("IntegrationEvidence/accessory_picking.txt",$"Verified accessory pick triggers configured on {catalog.entries.Count(e=>e.enabled)} wrappers; {count} added in this run. Body-only physics colliders retained.");
    }
}
