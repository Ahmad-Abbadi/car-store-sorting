using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Newtonsoft.Json;
using TinyRiders.Editor;
public static class RenderMixed
{
    public static void Run()
    {
        string root=ThemedCarBuilder.Root;
        var cat=JsonUtility.FromJson<ThemedCarBuilder.Catalog>(File.ReadAllText(root+"/Data/Themes/variant_catalog.json"));
        var families=cat.entries.GroupBy(e=>e.themeId).Select(g=>g.OrderBy(e=>e.baseCarId).ToArray()).ToArray();
        var entries=Enumerable.Range(0,8).SelectMany(i=>families.Where(f=>f.Length>i).Select(f=>f[i])).ToArray();
        var membership=new List<object>();
        for(int page=0;page<15;page++)
        {
            var items=entries.Skip(page*10).Take(10).ToArray();
            var preview=new PreviewRenderUtility();var instances=new List<GameObject>();
            string path=root+"/Preview/MixedGroups/Mixed_"+(page+1).ToString("00")+".png";
            try
            {
                Bounds bounds=new Bounds();bool first=true;
                for(int i=0;i<items.Length;i++)
                {
                    var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(items[i].prefabPath));instances.Add(go);
                    var rs=go.GetComponentsInChildren<Renderer>().Where(r=>r.enabled).ToArray();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
                    go.transform.localScale*=2.9f/Mathf.Max(b.size.x,b.size.z);
                    go.transform.rotation=Quaternion.Euler(0,20+(i%3-1)*12,0);
                    go.transform.position=new Vector3((i%5-2)*2.5f,0,(i/5)*4f);
                    preview.AddSingleGO(go);
                    foreach(var r in rs){if(first){bounds=r.bounds;first=false;}else bounds.Encapsulate(r.bounds);}
                    membership.Add(new{collectibleId=items[i].collectibleId,mixedPreviewPath=path,slot=i+1});
                }
                var cam=preview.camera;cam.orthographic=true;cam.orthographicSize=5.9f;cam.nearClipPlane=.01f;cam.farClipPlane=100;
                cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.91f,.93f,.95f,1);
                cam.transform.position=bounds.center+new Vector3(.15f,1.6f,2f).normalized*25;cam.transform.LookAt(bounds.center);
                preview.ambientColor=new Color(.48f,.48f,.48f);
                preview.lights[0].intensity=1.25f;preview.lights[0].transform.rotation=Quaternion.Euler(35,-35,0);
                preview.lights[1].intensity=.8f;preview.lights[1].transform.rotation=Quaternion.Euler(40,150,0);
                preview.BeginStaticPreview(new Rect(0,0,1600,1000));cam.Render();var tex=preview.EndStaticPreview();
                Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllBytes(path,tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);
            }
            finally{preview.Cleanup();foreach(var go in instances)if(go!=null)UnityEngine.Object.DestroyImmediate(go);}
        }
        File.WriteAllText("ArtSource/ThemedCars/Revisions/R02/mixed_membership.json",JsonConvert.SerializeObject(membership,Formatting.Indented));
        AssetDatabase.Refresh();
        ThemedCarBuilder.Validate();
        Debug.Log("R02_MIXED_COMPLETE 150");
    }
}
