using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using TinyRiders;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

public static class CarVisualBatchPreview
{
    public static void Capture()
    {
        string folder=CarVisualBatchBuilder.Evidence+"/VisualReview";Directory.CreateDirectory(folder);
        var catalog=AssetDatabase.LoadAssetAtPath<ThemedCarCatalog>(ItemDetailBuilder.CatalogPath);
        var representatives=catalog.entries.Where(e=>e.enabled).GroupBy(e=>e.baseCarId).Select(g=>g.First())
            .Concat(catalog.entries.Where(e=>e.enabled).GroupBy(e=>e.themeId).Select(g=>g.First())).Distinct().ToArray();
        var lods=AssetDatabase.LoadAssetAtPath<ItemLodSet>(ItemDetailBuilder.SetPath);
        var results=new JArray();
        var preview=new PreviewRenderUtility();
        bool asyncCompilation=ShaderUtil.allowAsyncCompilation;
        ShaderUtil.allowAsyncCompilation=false;
        try
        {
            preview.camera.fieldOfView=32;preview.camera.nearClipPlane=.01f;preview.camera.farClipPlane=100;
            preview.camera.clearFlags=CameraClearFlags.Color;preview.camera.backgroundColor=new Color(.13f,.16f,.20f,1);
            preview.lights[0].intensity=1.2f;preview.lights[0].transform.rotation=Quaternion.Euler(35,35,0);
            preview.lights[1].intensity=.6f;preview.lights[1].transform.rotation=Quaternion.Euler(330,220,0);
            foreach(var entry in representatives)
            {
                var generated=AssetDatabase.LoadAssetAtPath<GameObject>(CarVisualBatchBuilder.Root+"/"+
                    AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(entry.prefab))+"/Visual.prefab");
                for(int level=0;level<3;level++)
                {
                    Texture2D Render(GameObject prefab,bool outline)
                    {
                        var instance=Object.Instantiate(prefab);preview.AddSingleGO(instance);
                        var rs=instance.GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled).ToArray();
                        Bounds bounds=rs[0].bounds;foreach(var r in rs)bounds.Encapsulate(r.bounds);
                        var temporary=new System.Collections.Generic.List<Object>();
                        try
                        {
                            foreach(var r in instance.GetComponentsInChildren<MeshRenderer>(true))
                            {
                                var f=r.GetComponent<MeshFilter>();if(!f||!f.sharedMesh)continue;
                                if(lods.TryGet(f.sharedMesh,out var e)&&e.bakedMesh!=null)f.sharedMesh=e.bakedMesh;
                                r.forceMeshLod=(short)Mathf.Min(level,f.sharedMesh.lodCount-1);
                            }
                            if(outline)foreach(var r in rs) AddOutline(r,temporary);
                            preview.camera.transform.position=bounds.center+new Vector3(1,.65f,1).normalized*bounds.extents.magnitude*4;
                            preview.camera.transform.LookAt(bounds.center);
                            // Initialize the preview pipeline before capturing its first frame.
                            preview.BeginPreview(new Rect(0,0,512,512),GUIStyle.none);preview.Render(true);preview.EndPreview();
                            preview.BeginPreview(new Rect(0,0,512,512),GUIStyle.none);preview.Render(true);
                            var rt=(RenderTexture)preview.EndPreview();
                            var previous=RenderTexture.active;RenderTexture.active=rt;
                            var texture=new Texture2D(512,512,TextureFormat.RGBA32,false);
                            texture.ReadPixels(new Rect(0,0,512,512),0,0);texture.Apply();RenderTexture.active=previous;
                            return texture;
                        }
                        finally {Object.DestroyImmediate(instance);foreach(var asset in temporary)Object.DestroyImmediate(asset);}
                    }
                    // Include outlines on each of the fifteen base shapes at High and Low.
                    foreach(bool outline in new[]{false,true}.Where(o=>!o || level!=1 && representatives.Take(15).Contains(entry)))
                    {
                        var a=Render(entry.prefab,outline);var b=Render(generated,outline);
                        try
                        {
                            var ap=a.GetPixels32();var bp=b.GetPixels32();long error=0;int changed=0;
                            // A cold shader can otherwise produce an empty first preview and a false regression.
                            AssertVisible(ap,entry.collectibleId+" original"); AssertVisible(bp,entry.collectibleId+" batched");
                            for(int i=0;i<ap.Length;i++)
                            {
                                int d=Math.Abs(ap[i].r-bp[i].r)+Math.Abs(ap[i].g-bp[i].g)+Math.Abs(ap[i].b-bp[i].b);
                                error+=d;if(d>30)changed++;
                            }
                            double fraction=changed/(double)ap.Length;
                            string name=entry.collectibleId+"-LOD"+level+(outline?"-outline":"");
                            if(representatives.Take(3).Contains(entry)||fraction>.01)
                            {
                                File.WriteAllBytes(folder+"/"+name+"-original.png",a.EncodeToPNG());
                                File.WriteAllBytes(folder+"/"+name+"-batched.png",b.EncodeToPNG());
                            }
                            results.Add(new JObject{["id"]=entry.collectibleId,["level"]=level,["outline"]=outline,
                                ["changedPixelFraction"]=fraction,["meanChannelError"]=error/(double)(ap.Length*3*255)});
                        }
                        finally {Object.DestroyImmediate(a);Object.DestroyImmediate(b);}
                    }
                }
                File.WriteAllText(folder+"/comparisons.json",results.ToString());
            }
        }
        finally { ShaderUtil.allowAsyncCompilation=asyncCompilation; preview.Cleanup(); }
        if(results.Any(r=>(double)r["changedPixelFraction"]>.001))
            throw new InvalidOperationException("Car visual comparison exceeded the pixel-difference tolerance; inspect comparisons.json.");
    }

    static void AssertVisible(Color32[] pixels,string label)
    {
        var background=pixels[0];
        if(pixels.Count(p=>Math.Abs(p.r-background.r)+Math.Abs(p.g-background.g)+Math.Abs(p.b-background.b)>30)<1000)
            throw new InvalidOperationException("Empty car preview: "+label);
    }

    // Use isolated preview drawers, so visual review never creates objects in the user's active scene.
    static void AddOutline(MeshRenderer target,System.Collections.Generic.List<Object> temporary)
    {
        var source=target.GetComponent<MeshFilter>().sharedMesh;
        var mesh=Object.Instantiate(source);temporary.Add(mesh);
        var indices=new System.Collections.Generic.List<int>();var ranges=new System.Collections.Generic.List<MeshLodRange>();
        for(int level=0;level<source.lodCount;level++)
        {
            uint start=(uint)indices.Count;
            for(int sub=0;sub<source.subMeshCount;sub++)indices.AddRange(source.GetIndices(sub,level,true));
            ranges.Add(new MeshLodRange{indexStart=start,indexCount=(uint)indices.Count-start});
        }
        mesh.lodCount=1;mesh.subMeshCount=1;mesh.SetIndices(indices,MeshTopology.Triangles,0,false);
        mesh.lodCount=source.lodCount;mesh.SetLods(ranges.ToArray(),0);mesh.bounds=source.bounds;
        if(target.GetComponent<CarBatchGeometry>()==null)
        {
            var normals=mesh.normals;
            foreach(var group in mesh.vertices.Select((v,i)=>(v,i)).GroupBy(v=>v.v))
            {
                Vector3 sum=Vector3.zero;foreach(var p in group)sum+=normals[p.i];sum.Normalize();
                foreach(var p in group)normals[p.i]=sum;
            }
            mesh.SetUVs(3,normals.ToList());
        }
        foreach(bool mask in new[]{true,false})
        {
            var material=new Material(Resources.Load<Material>(mask?"Materials/OutlineMask":"Materials/OutlineFill"));temporary.Add(material);
            material.SetFloat("_ZTest",(float)(mask?CompareFunction.Always:CompareFunction.LessEqual));
            if(!mask){material.SetColor("_OutlineColor",Color.white);material.SetFloat("_OutlineWidth",SharedOutline.Width);}
            var go=new GameObject("Preview Outline",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(target.transform,false);
            go.layer=target.gameObject.layer;go.GetComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.forceMeshLod=target.forceMeshLod;
            renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            renderer.lightProbeUsage=LightProbeUsage.Off;renderer.reflectionProbeUsage=ReflectionProbeUsage.Off;
        }
    }
}
