using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TinyRidersArt
{
    public static class UnityArtAudit
    {
        [Serializable] class Products { public Product[] items; }
        [Serializable] class Product { public string sku; }
        [Serializable] class Audit
        {
            public string generatedAtUtc;
            public int catalogProducts,carPrefabs,packagePrefabs,carPrints,packageFronts,packageBacks;
            public int cardUvValid,toyPlacementValid,environmentSigns,previewScenes;
            public string[] failures;
        }
        static string Root="Assets/TinyRidersArt";
        static string TexPath(Material m)
        {
            if(m==null) return "";
            var tex=m.HasProperty("_BaseMap")?m.GetTexture("_BaseMap"):null;
            return tex==null?"":AssetDatabase.GetAssetPath(tex);
        }
        public static string Run()
        {
            string project=Directory.GetParent(Application.dataPath).FullName;
            string catalog=File.ReadAllText(Path.Combine(project,"Assets","TinyRidersArt","Catalog","collectibles_500.json"));
            var products=JsonUtility.FromJson<Products>("{\"items\":"+catalog+"}").items;
            var a=new Audit { generatedAtUtc=DateTime.UtcNow.ToString("o"),catalogProducts=products.Length };
            var fails=new List<string>();
            foreach(var p in products)
            {
                string sku=p.sku;
                var car=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/Cars/"+sku+".prefab");
                if(car==null) { fails.Add(sku+": missing car prefab"); continue; }
                a.carPrefabs++;
                bool carPrint=false;
                foreach(var r in car.GetComponentsInChildren<Renderer>(true))
                    foreach(var m in r.sharedMaterials)
                        if(TexPath(m)==Root+"/Textures/Cars/"+sku+".png") carPrint=true;
                if(carPrint) a.carPrints++; else fails.Add(sku+": car texture mismatch");
                var pack=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/Packages/"+sku+".prefab");
                if(pack==null) { fails.Add(sku+": missing package prefab"); continue; }
                a.packagePrefabs++;
                var renderers=pack.GetComponentsInChildren<Renderer>(true);
                Renderer card=null;
                foreach(var r in renderers) if(r.name.Contains("PackagingCard")) { card=r; break; }
                if(card==null) { fails.Add(sku+": missing card renderer"); continue; }
                bool front=false,back=false;
                foreach(var m in card.sharedMaterials)
                {
                    string path=TexPath(m);
                    if(path==Root+"/Textures/Packages/Front/"+sku+".png" && m.shader.name.Contains("Unlit")) front=true;
                    if(path==Root+"/Textures/Packages/Back/"+sku+".png" && m.shader.name.Contains("Unlit")) back=true;
                }
                if(front) a.packageFronts++; else fails.Add(sku+": missing front print or unlit material");
                if(back) a.packageBacks++; else fails.Add(sku+": missing back print or unlit material");
                var filter=card.GetComponent<MeshFilter>();
                int covered=0;
                if(filter!=null && filter.sharedMesh!=null)
                {
                    var uv=filter.sharedMesh.uv;
                    foreach(var v in uv) if(v.sqrMagnitude>.0001f) covered++;
                    if(uv.Length>0 && covered>uv.Length*.8f) a.cardUvValid++;
                    else fails.Add(sku+": card UV0 collapsed");
                }
                else fails.Add(sku+": missing card mesh");
                var toy=pack.transform.Find("Car_"+sku);
                if(toy!=null && Math.Abs(toy.localScale.x-.016f)<.0001f &&
                   Math.Abs(toy.localEulerAngles.y-15f)<.1f) a.toyPlacementValid++;
                else fails.Add(sku+": toy placement mismatch");
            }
            foreach(var code in new[]{"CC","TC","RR","DP","BW","RC","WY","SS","FF","CR"})
            {
                var m=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/Environment/"+code+"_Sign.mat");
                if(m!=null && TexPath(m)==Root+"/Environment/Textures/Signs/"+code+".png") a.environmentSigns++;
                else fails.Add(code+": missing environment sign material");
            }
            foreach(var scene in new[]{"TinyRiders_CarGallery","TinyRiders_PackagedGallery","TinyRiders_CompleteCollection",
                     "TinyRiders_StressPreview_ART","TinyRiders_SortingRoom_ART_PREVIEW","TinyRiders_ShelfDisplay","TinyRiders_PileDisplay"})
            {
                if(AssetDatabase.LoadAssetAtPath<SceneAsset>(Root+"/PreviewScenes/"+scene+".unity")!=null) a.previewScenes++;
                else fails.Add(scene+": missing Unity scene");
            }
            foreach(var capture in new[]{"UnityCarCloseup.png","UnityCityPackageFront_Final.png","UnitySortingRoom_Repaired.png"})
                if(!File.Exists(Path.Combine(project,"TinyRidersArt","qa",capture))) fails.Add("missing visual capture "+capture);
            a.failures=fails.ToArray();
            string output=Path.Combine(project,"TinyRidersArt","reports","UNITY_AUDIT.json");
            File.WriteAllText(output,JsonUtility.ToJson(a,true));
            return "Unity audit: "+a.carPrefabs+" cars, "+a.packagePrefabs+" packages, "+a.previewScenes+
                   " scenes, "+a.cardUvValid+" card UVs, "+fails.Count+" failures; "+output;
        }
    }
}
