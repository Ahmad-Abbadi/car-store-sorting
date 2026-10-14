using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace TinyRidersArt.Editor
{
    /// <summary>Editor-only, repeatable art assembly. Does not add gameplay components.</summary>
    public static class TinyRidersArtImporter
    {
        const string Root = "Assets/TinyRidersArt";
        [Serializable] class ProductList { public Product[] items; }
        [Serializable] class Product
        {
            public string sku, familyCode, bodyId, modelName, buildId, liveryName, rarity, displayNumber;
        }
        [Serializable] class PaletteList { public PaletteEntry[] items; }
        [Serializable] class PaletteEntry { public string bodyId, core, accessory; }
        static readonly Dictionary<string, Color> Core = new Dictionary<string, Color>();
        static readonly Dictionary<string, Color> Accessory = new Dictionary<string, Color>();

        static void Ensure(string path)
        {
            Directory.CreateDirectory(Path.Combine(Directory.GetParent(Application.dataPath).FullName,path));
        }
        static Material Material(string path, Texture tex, Color color, bool transparent=false, bool unlit=false)
        {
            var old = AssetDatabase.LoadAssetAtPath<Material>(path);
            var shader = Shader.Find(unlit?"Universal Render Pipeline/Unlit":"Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var m = old ?? new Material(shader);
            if(old!=null) m.shader=shader;
            else m.name = Path.GetFileNameWithoutExtension(path);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            if (m.HasProperty("_Color")) m.SetColor("_Color", color);
            if (tex != null)
            {
                if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
                if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", tex);
            }
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", transparent ? .82f : .48f);
            if (transparent)
            {
                if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 1);
                if (m.HasProperty("_Blend")) m.SetFloat("_Blend", 0);
                m.SetOverrideTag("RenderType", "Transparent");
                m.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                m.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                m.SetInt("_ZWrite", 0);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.renderQueue = (int)RenderQueue.Transparent;
            }
            if(old==null) AssetDatabase.CreateAsset(m,path);
            else EditorUtility.SetDirty(m);
            return m;
        }
        static Color Hex(string s) { ColorUtility.TryParseHtmlString(s,out var c); return c; }
        static Texture2D Tex(string path) { return AssetDatabase.LoadAssetAtPath<Texture2D>(path); }
        static void Init()
        {
            Ensure(Root+"/Materials/Shared"); Ensure(Root+"/Materials/Cars");
            Ensure(Root+"/Materials/Packages/Front"); Ensure(Root+"/Materials/Packages/Back");
            Ensure(Root+"/Materials/Environment");
            Ensure(Root+"/Prefabs/Cars"); Ensure(Root+"/Prefabs/Packages"); Ensure(Root+"/PreviewScenes");
            string palettePath=Path.Combine(Application.dataPath,"TinyRidersArt","Catalog","core_palette.json");
            var palette=JsonUtility.FromJson<PaletteList>("{\"items\":"+File.ReadAllText(palettePath)+"}");
            if(palette==null || palette.items==null || palette.items.Length!=50)
                throw new Exception("Core palette must contain 50 bodies");
            foreach(var entry in palette.items)
            {
                Core[entry.bodyId]=Hex(entry.core); Accessory[entry.bodyId]=Hex(entry.accessory);
            }
        }
        static Material Shared(string key, Color color)
        {
            return Material(Root+"/Materials/Shared/"+key+".mat", null, color);
        }
        static Material[] GetCarMaterials(Product p)
        {
            return new[] {
                Material(Root+"/Materials/Cars/"+p.sku+".mat",Tex(Root+"/Textures/Cars/"+p.sku+".png"),Color.white),
                Shared(p.bodyId+"_Paint",Core[p.bodyId]),
                Shared(p.bodyId+"_Accent",Accessory[p.bodyId]),
                Shared("DarkPlastic",new Color(.065f,.08f,.11f)),
                Shared("Rubber",new Color(.045f,.047f,.056f)),
                Shared("Rim",new Color(.77f,.80f,.84f)),
                Shared("Hub",new Color(.15f,.18f,.23f))
            };
        }
        static Material MatchCar(string name,Material[] m)
        {
            if (name.Contains("TR_PRINT_MATERIAL")) return m[0];
            if (name.Contains("TR_PAINT_BASE")) return m[1];
            if (name.Contains("TR_ACCENT_PART")) return m[2];
            if (name.Contains("TR_DARK_PLASTIC")) return m[3];
            if (name.Contains("TR_RUBBER")) return m[4];
            if (name.Contains("TR_RIM")) return m[5];
            if (name.Contains("TR_HUB")) return m[6];
            return m[3];
        }
        static void AssignCar(GameObject go,Material[] m)
        {
            foreach (var renderer in go.GetComponentsInChildren<Renderer>(true))
            {
                var list=renderer.sharedMaterials;
                for (int i=0;i<list.Length;i++) list[i]=MatchCar(list[i] != null ? list[i].name : renderer.name,m);
                renderer.sharedMaterials=list;
            }
        }
        static string BuildKey(Product p) { return p.bodyId+"-"+(p.buildId=="custom"?"C":"S"); }
        static bool MakeCar(Product p)
        {
            string dst=Root+"/Prefabs/Cars/"+p.sku+".prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(dst)!=null) return false;
            string path=Root+"/Builds/"+BuildKey(p)+".fbx";
            var model=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (model==null) throw new FileNotFoundException("Missing imported build",path);
            var root=new GameObject(p.sku); root.tag="Untagged";
            var child=(GameObject)PrefabUtility.InstantiatePrefab(model);
            child.transform.SetParent(root.transform,false); child.name=BuildKey(p);
            AssignCar(child,GetCarMaterials(p));
            PrefabUtility.SaveAsPrefabAsset(root,dst);
            UnityEngine.Object.DestroyImmediate(root);
            return true;
        }
        static Material[] GetPackageMaterials(Product p)
        {
            var front=Material(Root+"/Materials/Packages/Front/"+p.sku+".mat",
                Tex(Root+"/Textures/Packages/Front/"+p.sku+".png"),Color.white,unlit:true);
            var back=Material(Root+"/Materials/Packages/Back/"+p.sku+".mat",
                Tex(Root+"/Textures/Packages/Back/"+p.sku+".png"),Color.white,unlit:true);
            var edge=Shared("CardboardEdge",new Color(.72f,.64f,.51f));
            var clear=Material(Root+"/Materials/Shared/ClearBlister.mat",null,new Color(1f,1f,1f,.10f),true);
            return new[]{front,back,edge,clear};
        }
        static void AssignPackage(GameObject go,Material[] m)
        {
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                var list=r.sharedMaterials;
                for(int i=0;i<list.Length;i++)
                {
                    string n=list[i]!=null?list[i].name:"";
                    if(n.Contains("TR_CARD_FRONT")) list[i]=m[0];
                    else if(n.Contains("TR_CARD_BACK")) list[i]=m[1];
                    else if(n.Contains("TR_CARDBOARD_EDGE")) list[i]=m[2];
                    else if(n.Contains("TR_CLEAR_BLISTER")) list[i]=m[3];
                }
                r.sharedMaterials=list;
            }
        }
        static bool MakePackage(Product p)
        {
            string dst=Root+"/Prefabs/Packages/"+p.sku+".prefab";
            if(AssetDatabase.LoadAssetAtPath<GameObject>(dst)!=null) return false;
            var master=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Packaging/PackagingMaster.fbx");
            var car=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/Cars/"+p.sku+".prefab");
            if(master==null || car==null) throw new FileNotFoundException("Package master or car prefab missing for "+p.sku);
            var root=new GameObject(p.sku+"_PACKAGED");
            var card=(GameObject)PrefabUtility.InstantiatePrefab(master);
            card.transform.SetParent(root.transform,false); card.name="CardAndBlister";
            AssignPackage(card,GetPackageMaterials(p));
            var toy=(GameObject)PrefabUtility.InstantiatePrefab(car);
            toy.transform.SetParent(root.transform,false); toy.name="Car_"+p.sku;
            toy.transform.localScale=Vector3.one*.016f;
            // Blender (x,-y,z) becomes Unity (x,z,y) on FBX import.
            toy.transform.localPosition=new Vector3(0,.030f,.022f);
            toy.transform.localRotation=Quaternion.Euler(0,15,0);
            PrefabUtility.SaveAsPrefabAsset(root,dst);
            UnityEngine.Object.DestroyImmediate(root);
            return true;
        }
        [MenuItem("Tools/Tiny Riders/Repair Package Presentation")]
        public static void RepairPackagingVisuals()
        {
            Init(); AssetDatabase.Refresh();
            string json=File.ReadAllText(Path.Combine(Application.dataPath,"TinyRidersArt","Catalog","collectibles_500.json"));
            var parsed=JsonUtility.FromJson<ProductList>("{\"items\":"+json+"}");
            int repaired=0;
            foreach(var p in parsed.items)
            {
                GetPackageMaterials(p);
                string dst=Root+"/Prefabs/Packages/"+p.sku+".prefab";
                if(AssetDatabase.LoadAssetAtPath<GameObject>(dst)==null) continue;
                var root=PrefabUtility.LoadPrefabContents(dst);
                try
                {
                    var toy=root.transform.Find("Car_"+p.sku);
                    if(toy==null) throw new Exception("Missing package car "+p.sku);
                    toy.localScale=Vector3.one*.016f;
                    toy.localPosition=new Vector3(0,.030f,.022f);
                    toy.localRotation=Quaternion.Euler(0,15,0);
                    PrefabUtility.SaveAsPrefabAsset(root,dst);
                    repaired++;
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
            Debug.Log("TINY_RIDERS_PACKAGING_REPAIR_DONE packages="+repaired);
        }
        static void MakeGallery(Product[] products,bool packaged,string sceneName,int stride)
        {
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            float gap=packaged?1.22f:4.2f;
            int columns=packaged?10:10;
            int count=0;
            foreach(var p in products)
            {
                if(count%stride!=0) { count++; continue; }
                string path=Root+"/Prefabs/"+(packaged?"Packages":"Cars")+"/"+p.sku+".prefab";
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if(prefab==null) { count++; continue; }
                int index=count/stride;
                var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab);
                go.transform.position=new Vector3((index%columns)*gap, 0,(index/columns)*gap);
                if(packaged) go.transform.localScale=Vector3.one*8f;
                count++;
            }
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube); floor.name="StudioFloor";
            floor.transform.position=new Vector3(columns*gap/2,-.12f,Mathf.Ceil(count/(float)(stride*columns))*gap/2);
            floor.transform.localScale=new Vector3(columns*gap+3,.20f,Mathf.Ceil(count/(float)(stride*columns))*gap+3);
            floor.GetComponent<Renderer>().sharedMaterial=Shared("GalleryFloor",new Color(.82f,.87f,.93f));
            var sun=new GameObject("StudioSun"); var light=sun.AddComponent<Light>(); light.type=LightType.Directional; light.intensity=1.15f;
            sun.transform.rotation=Quaternion.Euler(48,-35,0);
            if(packaged)
            {
                var fill=new GameObject("PackageFrontFill"); var frontLight=fill.AddComponent<Light>();
                frontLight.type=LightType.Directional; frontLight.intensity=.85f;
                fill.transform.rotation=Quaternion.Euler(35,170,0);
            }
            var camera=new GameObject("GalleryCamera"); var cam=camera.AddComponent<Camera>(); cam.orthographic=true;
            cam.orthographicSize=packaged?8f:Mathf.Max(8,Mathf.Ceil(count/(float)(stride*columns))*gap*.68f);
            camera.transform.position=new Vector3(columns*gap/2,packaged?7:18,-8);
            camera.transform.LookAt(new Vector3(columns*gap/2,0,Mathf.Ceil(count/(float)(stride*columns))*gap/2));
            EditorSceneManager.SaveScene(scene,Root+"/PreviewScenes/"+sceneName+".unity");
        }
        static void MakeRoom(Product[] products,string sceneName,bool packagedShelves)
        {
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var room=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Environment/Models/SortingRoom_All.fbx");
            if(room==null) throw new FileNotFoundException("SortingRoom_All.fbx missing");
            var environment=(GameObject)PrefabUtility.InstantiatePrefab(room); environment.name="SortingRoom_Art";
            foreach(var r in environment.GetComponentsInChildren<Renderer>(true))
            {
                if(!r.name.EndsWith("_sign",StringComparison.Ordinal) || r.name.Length<2) continue;
                string code=r.name.Substring(0,2);
                var tex=Tex(Root+"/Environment/Textures/Signs/"+code+".png");
                if(tex==null) continue;
                var printed=Material(Root+"/Materials/Environment/"+code+"_Sign.mat",tex,Color.white,unlit:true);
                var list=r.sharedMaterials;
                for(int i=0;i<list.Length;i++)
                    if(list[i]!=null && list[i].name.Contains("_PRINTED_SIGN")) list[i]=printed;
                r.sharedMaterials=list;
            }
            var heroes=new List<Product>();
            foreach(var p in products)
                if(p.buildId=="standard" && p.liveryName=="CORE" && p.bodyId.EndsWith("-01")) heroes.Add(p);
            for(int i=0;i<heroes.Count;i++)
            {
                string path=Root+"/Prefabs/"+(packagedShelves?"Packages":"Cars")+"/"+heroes[i].sku+".prefab";
                var art=AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if(art==null) continue;
                var item=(GameObject)PrefabUtility.InstantiatePrefab(art);
                if(packagedShelves)
                {
                    item.transform.localScale=Vector3.one*2.5f;
                    item.transform.position=new Vector3(-4.45f+(i%5)*.55f, 1.20f+(i/5)*.80f,-3.60f);
                }
                else
                {
                    item.transform.localScale=Vector3.one*.25f;
                    if(i<3) item.transform.position=new Vector3(-4.4f+i*.95f,1.18f,-3.65f);
                    else if(i<6) item.transform.position=new Vector3(-.90f+(i-3)*.95f,1.57f,-3.65f);
                    else item.transform.position=new Vector3(1.40f+(i-6)*.65f,.30f,1.38f);
                }
            }
            var light=new GameObject("SoftShopSun").AddComponent<Light>();
            light.type=LightType.Directional; light.intensity=1.2f;
            light.transform.rotation=Quaternion.Euler(48,-34,0);
            var cam=new GameObject("RoomCamera").AddComponent<Camera>();
            cam.orthographic=true; cam.orthographicSize=7.5f;
            cam.transform.position=new Vector3(8,7,11);
            cam.transform.LookAt(new Vector3(0,1.2f,0));
            EditorSceneManager.SaveScene(scene,Root+"/PreviewScenes/"+sceneName+".unity");
        }
        [MenuItem("Tools/Tiny Riders/Build Art Collection")]
        public static void BuildAll()
        {
            Init(); AssetDatabase.Refresh();
            string json=File.ReadAllText(Path.Combine(Application.dataPath,"TinyRidersArt","Catalog","collectibles_500.json"));
            var parsed=JsonUtility.FromJson<ProductList>("{\"items\":"+json+"}");
            if(parsed==null || parsed.items==null || parsed.items.Length!=500) throw new Exception("Catalog must have 500 products");
            int cars=0,packages=0;
            foreach(var p in parsed.items) if(MakeCar(p)) cars++;
            AssetDatabase.SaveAssets();
            foreach(var p in parsed.items) if(MakePackage(p)) packages++;
            AssetDatabase.SaveAssets();
            MakeGallery(parsed.items,false,"TinyRiders_CarGallery",10);
            MakeGallery(parsed.items,true,"TinyRiders_PackagedGallery",10);
            MakeGallery(parsed.items,false,"TinyRiders_CompleteCollection",1);
            MakeGallery(parsed.items,false,"TinyRiders_StressPreview_ART",1);
            MakeRoom(parsed.items,"TinyRiders_SortingRoom_ART_PREVIEW",false);
            MakeRoom(parsed.items,"TinyRiders_ShelfDisplay",true);
            MakeRoom(parsed.items,"TinyRiders_PileDisplay",false);
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
            Debug.Log("TINY_RIDERS_ART_IMPORT_DONE cars="+cars+" packages="+packages);
        }
    }
}
