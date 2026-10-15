using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Newtonsoft.Json.Linq;

// Exact geometry batching of the approved shelf. Original prefab/meshes/materials remain untouched.
public static class ThemedShelfBatcher
{
    const string Source = "Assets/TinyRiders/Art/Shelves/BaseShelf_01/Prefabs/TR_Shelf_Base_01.prefab";
    const string Cache = "Assets/TinyRiders/Gameplay/Rendering";
    sealed class Part { public Mesh mesh; public Material material; public ShadowCastingMode shadow; public bool receive; }
    static Bounds GeometryBounds(IEnumerable<MeshRenderer> renderers)
    {
        bool found=false;Bounds bounds=default;
        foreach(var renderer in renderers)
        {
            var filter=renderer.GetComponent<MeshFilter>();
            foreach(var vertex in filter.sharedMesh.vertices)
            {
                Vector3 point=filter.transform.TransformPoint(vertex);
                if(!found){bounds=new Bounds(point,Vector3.zero);found=true;}else bounds.Encapsulate(point);
            }
        }
        if(!found)throw new Exception("Shelf geometry is empty");
        return bounds;
    }
    public static void Apply(SortingShelf[] shelves)
    {
        Directory.CreateDirectory(Cache);AssetDatabase.Refresh();
        var source=PrefabUtility.LoadPrefabContents(Source);var parts=new List<Part>();int originalRenderers=0, trianglesBefore=0;
        try
        {
            var renderers=source.GetComponentsInChildren<MeshRenderer>(false).Where(r=>r.enabled).ToArray();originalRenderers=renderers.Length;
            foreach(var group in renderers.GroupBy(r=>new{material=r.sharedMaterial,r.shadowCastingMode,r.receiveShadows}))
            {
                if(group.Key.material==null)throw new Exception("Shelf source material is missing");
                var combine=new List<CombineInstance>();
                foreach(var renderer in group)
                {
                    var filter=renderer.GetComponent<MeshFilter>();if(filter==null||filter.sharedMesh==null)throw new Exception("Shelf source mesh is missing");
                    if(filter.sharedMesh.subMeshCount!=1||renderer.sharedMaterials.Length!=1)throw new Exception("Shelf batching requires explicit per-submesh authoring");
                    trianglesBefore+=filter.sharedMesh.triangles.Length/3;
                    combine.Add(new CombineInstance{mesh=filter.sharedMesh,subMeshIndex=0,transform=source.transform.worldToLocalMatrix*filter.transform.localToWorldMatrix});
                }
                string materialGuid=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(group.Key.material));
                string path=Cache+"/Shelf_"+materialGuid+"_"+(int)group.Key.shadowCastingMode+"_"+group.Key.receiveShadows+".asset";
                var mesh=new Mesh{name="Approved Shelf · "+group.Key.material.name,indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(combine.ToArray(),true,true,false);mesh.RecalculateBounds();
                var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(saved==null){AssetDatabase.CreateAsset(mesh,path);saved=mesh;}else{EditorUtility.CopySerialized(mesh,saved);UnityEngine.Object.DestroyImmediate(mesh);EditorUtility.SetDirty(saved);}
                parts.Add(new Part{mesh=saved,material=group.Key.material,shadow=group.Key.shadowCastingMode,receive=group.Key.receiveShadows});
            }
        }
        finally{PrefabUtility.UnloadPrefabContents(source);}
        int trianglesAfter=parts.Sum(p=>p.mesh.triangles.Length/3);if(trianglesBefore!=trianglesAfter)throw new Exception("Shelf batching triangle mismatch");
        foreach(var shelf in shelves)
        {
            var art=shelf.transform.Find("TR_Shelf_Base_01");if(art==null)throw new Exception("Approved shelf instance missing");
            var original=art.GetComponentsInChildren<MeshRenderer>(true);
            Bounds before=GeometryBounds(original);
            var batch=shelf.transform.Find("Batched Shelf Art");if(batch==null){batch=new GameObject("Batched Shelf Art").transform;batch.SetParent(shelf.transform,false);}
            batch.localPosition=art.localPosition;batch.localRotation=art.localRotation;batch.localScale=art.localScale;
            int i=0;foreach(var part in parts)
            {
                string name="Shared Geometry "+i++;var t=batch.Find(name);if(t==null){t=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer)).transform;t.SetParent(batch,false);}
                t.gameObject.layer=2;t.GetComponent<MeshFilter>().sharedMesh=part.mesh;var renderer=t.GetComponent<MeshRenderer>();renderer.sharedMaterial=part.material;renderer.shadowCastingMode=part.shadow;renderer.receiveShadows=part.receive;renderer.enabled=true;
            }
            var batched=batch.GetComponentsInChildren<MeshRenderer>();Bounds after=GeometryBounds(batched);
            if(Vector3.Distance(before.min,after.min)>.002f||Vector3.Distance(before.max,after.max)>.002f)throw new Exception("Shelf batching changed bounds: "+shelf.SaveId);
            foreach(var r in original){r.enabled=false;PrefabUtility.RecordPrefabInstancePropertyModifications(r);}
        }
        AssetDatabase.SaveAssets();
        File.WriteAllText("IntegrationEvidence/shelf_batching.json",new JObject{["sourceRenderersPerShelf"]=originalRenderers,["batchedRenderersPerShelf"]=parts.Count,["trianglesBefore"]=trianglesBefore,["trianglesAfter"]=trianglesAfter,["shelfInstances"]=shelves.Length,["boundsMatched"]=true,["sourcePrefabModified"]=false,["sourceMaterialsReplaced"]=false}.ToString());
    }
}
