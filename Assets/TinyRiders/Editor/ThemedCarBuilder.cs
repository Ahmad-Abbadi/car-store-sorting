using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TinyRiders.Editor
{
    public static class ThemedCarBuilder
    {
        public const string Root = "Assets/TinyRiders/ThemedCollection";
        public const string Source = "Assets/ithappy/Modular_Racing_Cars";
        [Serializable] public class RecipeSet { public Theme[] themes; }
        [Serializable] public class Theme { public string id,name,paint,trim,light,icon,secondary,ink; public int[] cars; public int contentRevision=2; public float smoothness=.38f,metallic=.08f,trimSmoothness=.4f,trimMetallic=.3f; }
        [Serializable] public class Record { public string collectibleId,baseCarId,themeId,colorwayId,displayName,prefabPath,previewPath,sourcePrefabPath,sourceGuid; public string[] materialPaths; public int contentRevision=1; public string gameplayPreviewPath,thumbnailPath,validationNotes; }
        [Serializable] public class Catalog { public int schemaVersion=1; public Record[] entries; }
        static Color Hex(string s) { ColorUtility.TryParseHtmlString(s,out var c); return c; }
        static void Folder(string path) { Directory.CreateDirectory(Path.GetDirectoryName(path)); }
        static Texture2D Texture(string path,bool alpha=false)
        {
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var imp=(TextureImporter)AssetImporter.GetAtPath(path);
            imp.textureType=TextureImporterType.Default; imp.sRGBTexture=true;imp.alphaIsTransparency=alpha;
            imp.mipmapEnabled=true;imp.wrapMode=alpha?TextureWrapMode.Repeat:TextureWrapMode.Clamp;
            imp.filterMode=FilterMode.Bilinear;imp.maxTextureSize=alpha?1024:256;imp.textureCompression=TextureImporterCompression.Compressed;
            imp.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        static Material Material(string path,string source,Texture2D texture,Color? tint=null,float smooth=.38f)
        {
            Folder(path);var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            var src=AssetDatabase.LoadAssetAtPath<Material>(Source+"/Materials/"+source+".mat");
            if(m==null){m=new Material(src);AssetDatabase.CreateAsset(m,path);}else m.CopyPropertiesFromMaterial(src);
            m.name=Path.GetFileNameWithoutExtension(path);
            if(texture!=null){m.SetTexture("_BaseMap",texture);m.SetTexture("_MainTex",texture);}
            if(tint.HasValue){m.SetColor("_BaseColor",tint.Value);m.SetColor("_Color",tint.Value);}
            if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",smooth);
            if(source=="Color"||source=="Metal")m.SetFloat("_Metallic",.08f);
            // The supplier paint also emits its ORIGINAL palette. Replacing only BaseMap leaks racing colors.
            m.DisableKeyword("_EMISSION");
            if(m.HasProperty("_EmissionMap"))m.SetTexture("_EmissionMap",null);
            if(m.HasProperty("_EmissionColor"))m.SetColor("_EmissionColor",Color.black);
            EditorUtility.SetDirty(m);return m;
        }
        static GameObject Base(int c)
        {
            string id="C"+c.ToString("000"),path=Root+"/Cars/Base/"+id+".prefab";
            var existing=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if(existing!=null)
            {
                var content=PrefabUtility.LoadPrefabContents(path);
                try{CleanBase(content);return PrefabUtility.SaveAsPrefabAsset(content,path);}
                finally{PrefabUtility.UnloadPrefabContents(content);}
            }
            string name="Car_"+c.ToString("00");var root=new GameObject(id);
            try
            {
                var nested=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Source+"/Prefabs/"+name+"/"+name+".prefab"));
                nested.transform.SetParent(root.transform,false);
                CleanBase(root);
                Folder(path);return PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        static void CleanBase(GameObject root)
        {
            foreach(var t in root.GetComponentsInChildren<Transform>(true))
                if(t.name.EndsWith("_Spoiler")||t.name.EndsWith("_Turbine")||t.name.EndsWith("_Engine"))
                {t.gameObject.SetActive(false);PrefabUtility.RecordPrefabInstancePropertyModifications(t.gameObject);}
        }
        static readonly int[] NeutralCars={3,4,5,9,12,13,15};
        static readonly int[] MetalCars={4,5,9};
        public static void BuildSample() { Build(true); }
        public static void BuildTheme(string themeId) { Build(false,themeId); }
        [MenuItem("Tiny Riders/Themes/Build Collection")]
        public static void BuildAll() { Build(false); }
        static void Build(bool sample,string onlyTheme=null)
        {
            // R03 keeps the R02 body foundation; the legacy builder cannot recreate its accessory overrides.
            var currentCatalogPath=Root+"/Data/Themes/variant_catalog.json";
            if(File.Exists(currentCatalogPath)&&JsonUtility.FromJson<Catalog>(File.ReadAllText(currentCatalogPath)).entries.Any(e=>e.contentRevision>=3))
                throw new InvalidOperationException("This collection contains R03 accessory fits. Use ThemeAccessoryBuilder.Apply to regenerate them; the R02 builder would replace their overrides. See TinyRidersProduction/ThemeAccessoryPass/integration_notes.md.");
            var recipes=JsonUtility.FromJson<RecipeSet>(File.ReadAllText(Root+"/Data/Themes/theme_recipes.json"));
            var records=new List<Record>();var entries=new List<ThemedCarCatalog.Entry>();
            if(onlyTheme!=null)
            {
                records.AddRange(JsonUtility.FromJson<Catalog>(File.ReadAllText(Root+"/Data/Themes/variant_catalog.json")).entries.Where(e=>e.themeId!=onlyTheme));
                entries.AddRange(AssetDatabase.LoadAssetAtPath<ThemedCarCatalog>(Root+"/Data/Themes/ThemedCarCatalog.asset").entries.Where(e=>e.themeId!=onlyTheme));
            }
            foreach(var theme in recipes.themes)
            {
                if(onlyTheme!=null&&theme.id!=onlyTheme)continue;
                string texdir=Root+"/Textures/Themes/"+theme.id+"/",matdir=Root+"/Materials/Themes/"+theme.id+"/";
                var paintMaps=new Dictionary<string,Texture2D>();
                foreach(string mode in new[]{"Paint","Neutral","LowNeutral","BodyMetal","Trim","Secondary"})paintMaps[mode]=Texture(texdir+mode+"Palette.png");
                var rim=Material(matdir+"WheelAccent.mat","Color",paintMaps["Trim"],Color.white);
                var wheelEmission=Material(matdir+"WheelInset.mat","Emission",paintMaps["Trim"],Hex(theme.trim));
                var trim=Material(matdir+"Trim.mat","Metal",paintMaps["Trim"],Color.white);
                trim.SetFloat("_Metallic",theme.trimMetallic);trim.SetFloat("_Smoothness",theme.trimSmoothness);
                var secondary=Material(matdir+"Secondary.mat","Color",paintMaps["Secondary"],Color.white,theme.smoothness);
                secondary.SetFloat("_Metallic",theme.metallic);
                var lightMaterials=new Dictionary<string,Material>();
                foreach(string lightName in new[]{"Light_Headlight","Light_Fog","Light_Engine","Light_Turbine","Light_Pipe"})
                {
                    var light=Material(matdir+lightName+".mat",lightName,null,Hex(theme.light),.45f);
                    light.SetTexture("_BaseMap",null);light.SetTexture("_MainTex",null);
                    light.SetFloat("_Metallic",0);light.SetColor("_EmissionColor",Hex(theme.light)*.35f);light.EnableKeyword("_EMISSION");lightMaterials[lightName]=light;
                }
                foreach(int c in theme.cars)
                {
                    if(sample && !(theme.id=="TH_PIRATE"||theme.id=="TH_SPACE"||theme.id=="TH_CANDY"||theme.id=="TH_SPOOKY"))continue;
                    string baseId="C"+c.ToString("000"),id="TR-"+baseId+"-"+theme.id+"-V01";
                    string mode=(c==5||c==9)?"LowNeutral":NeutralCars.Contains(c)?"Neutral":"Paint";
                    var paint=Material(matdir+mode+".mat","Color",paintMaps[mode],Color.white,theme.smoothness);
                    paint.SetFloat("_Metallic",theme.metallic);
                    Material metal=MetalCars.Contains(c)?Material(matdir+"BodyMetal.mat","Metal",paintMaps["BodyMetal"],Color.white):null;
                    if(metal!=null){metal.SetFloat("_Metallic",theme.metallic);metal.SetFloat("_Smoothness",theme.smoothness);}
                    var decals=Material(matdir+baseId+"_Decals.mat",c==7||c==8||c==11||c==12||c==13||c==14?"Decals_2":"Decals_1",Texture(texdir+baseId+"_ThemeDecals.png",true),Color.white,.2f);
                    string path=Root+"/Prefabs/Themed/"+theme.id+"/"+id+".prefab";
                    bool exists=AssetDatabase.LoadAssetAtPath<GameObject>(path)!=null;
                    var root=exists?PrefabUtility.LoadPrefabContents(path):(GameObject)PrefabUtility.InstantiatePrefab(Base(c));root.name=id;
                    try
                    {
                        var identity=root.GetComponent<ThemedCarIdentity>()??root.AddComponent<ThemedCarIdentity>();
                        identity.collectibleId=id;identity.baseCarId=baseId;identity.themeId=theme.id;identity.colorwayId="V01";
                        identity.contentRevision=theme.contentRevision;
                        foreach(var r in root.GetComponentsInChildren<Renderer>(true))
                        {
                            bool wheel=false;for(var t=r.transform;t!=null;t=t.parent)if(t.name.Contains("Wheel_"))wheel=true;
                            bool character=false;for(var t=r.transform;t!=null;t=t.parent)if(t.name.StartsWith("Racer_"))character=true;
                            if(character)continue;
                            var mats=r.sharedMaterials;
                            var original=PrefabUtility.GetCorrespondingObjectFromOriginalSource(r);
                            var sourceMats=original!=null?original.sharedMaterials:mats;
                            for(int k=0;k<mats.Length;k++)
                            {
                                string old=AssetDatabase.GetAssetPath(sourceMats[k]);
                                bool secondaryPart=r.name.Contains("FrontBumper")||r.name.Contains("RearBumper")||r.name.Contains("Spoiler");
                                if(old==Source+"/Materials/Color.mat")mats[k]=wheel?rim:secondaryPart?secondary:paint;
                                else if(old.EndsWith("/Decals_1.mat")||old.EndsWith("/Decals_2.mat"))mats[k]=decals;
                                else if(old==Source+"/Materials/Metal.mat")mats[k]=metal!=null&&r.name=="Car_"+c.ToString("00")?metal:trim;
                                else if(old==Source+"/Materials/Emission.mat")mats[k]=wheelEmission;
                                else if(lightMaterials.TryGetValue(Path.GetFileNameWithoutExtension(old),out var light))mats[k]=light;
                            }
                            r.sharedMaterials=mats;PrefabUtility.RecordPrefabInstancePropertyModifications(r);
                        }
                        Folder(path);var saved=PrefabUtility.SaveAsPrefabAsset(root,path);
                        string preview=Root+"/Preview/Themes/"+theme.id+"/"+id+".png";
                        Render(saved,preview,new Vector3(1,.8f,1.25f),512);
                        Render(saved,Root+"/Preview/Reverse/"+theme.id+"/"+id+".png",new Vector3(-1,.65f,-1.25f),384);
                        string game=Root+"/Preview/Gameplay/"+theme.id+"/"+id+".png";
                        string small=Root+"/Preview/Thumbnails/"+theme.id+"/"+id+".png";
                        Render(saved,game,new Vector3(1,.65f,1.2f),256,true);
                        Render(saved,small,new Vector3(1,.8f,1.25f),96);
                        var materialPaths=saved.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).Select(m=>AssetDatabase.GetAssetPath(m)).Distinct().ToArray();
                        var record=new Record{collectibleId=id,baseCarId=baseId,themeId=theme.id,colorwayId="V01",displayName=theme.name+" · "+baseId,prefabPath=path,previewPath=preview,sourcePrefabPath=Source+"/Prefabs/Car_"+c.ToString("00")+"/Car_"+c.ToString("00")+".prefab",materialPaths=materialPaths};
                        record.sourceGuid=AssetDatabase.AssetPathToGUID(record.sourcePrefabPath);records.Add(record);
                        record.contentRevision=theme.contentRevision;record.gameplayPreviewPath=game;record.thumbnailPath=small;
                        record.validationNotes="R02 generated; visual review pending. Original mesh/UV bounds constrain coverage.";
                        entries.Add(new ThemedCarCatalog.Entry{collectibleId=id,baseCarId=baseId,themeId=theme.id,colorwayId="V01",displayName=record.displayName,prefab=saved,preview=AssetDatabase.LoadAssetAtPath<Texture2D>(preview),contentRevision=theme.contentRevision});
                        Debug.Log("THEMED_BUILT "+id);
                    }
                    finally{if(exists)PrefabUtility.UnloadPrefabContents(root);else UnityEngine.Object.DestroyImmediate(root);}
                }
            }
            string catalogPath=Root+"/Data/Themes/ThemedCarCatalog.asset";
            var catalog=AssetDatabase.LoadAssetAtPath<ThemedCarCatalog>(catalogPath);
            if(catalog==null){catalog=ScriptableObject.CreateInstance<ThemedCarCatalog>();AssetDatabase.CreateAsset(catalog,catalogPath);}
            catalog.entries=entries.ToArray();EditorUtility.SetDirty(catalog);
            File.WriteAllText(Root+"/Data/Themes/variant_catalog.json",JsonUtility.ToJson(new Catalog{entries=records.ToArray()},true));
            AssetDatabase.SaveAssets();AssetDatabase.Refresh();Validate();
        }
        static void Render(GameObject prefab,string path,Vector3 direction,int size,bool perspective=false)
        {
            var preview=new PreviewRenderUtility();GameObject instance=null;
            try
            {
                instance=UnityEngine.Object.Instantiate(prefab);preview.AddSingleGO(instance);
                var rs=instance.GetComponentsInChildren<Renderer>().Where(r=>r.enabled).ToArray();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
                var camera=preview.camera;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.91f,.93f,.95f,1);
                camera.orthographic=true;camera.orthographicSize=b.extents.magnitude*.82f;camera.nearClipPlane=.01f;camera.farClipPlane=100;
                camera.transform.position=b.center+direction.normalized*12;camera.transform.LookAt(b.center);
                if(perspective){camera.orthographic=false;camera.fieldOfView=60;camera.transform.position=b.center+direction.normalized*Mathf.Max(b.size.x,b.size.y,b.size.z)*1.5f;camera.transform.LookAt(b.center);}
                preview.ambientColor=new Color(.48f,.48f,.48f);
                preview.lights[0].intensity=1.25f;preview.lights[0].transform.rotation=Quaternion.Euler(35,-35,0);
                preview.lights[1].intensity=.8f;preview.lights[1].transform.rotation=Quaternion.Euler(40,150,0);
                preview.BeginStaticPreview(new Rect(0,0,size,size));camera.Render();var texture=preview.EndStaticPreview();
                Folder(path);File.WriteAllBytes(path,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            }
            finally{preview.Cleanup();if(instance!=null)UnityEngine.Object.DestroyImmediate(instance);}
        }
        [MenuItem("Tiny Riders/Themes/Validate Collection")]
        public static void Validate()
        {
            var catalog=JsonUtility.FromJson<Catalog>(File.ReadAllText(Root+"/Data/Themes/variant_catalog.json"));
            var errors=new List<string>();var ids=new HashSet<string>();int meshes=0,decals=0;
            foreach(var e in catalog.entries)
            {
                if(!ids.Add(e.collectibleId))errors.Add("Duplicate "+e.collectibleId);
                var p=AssetDatabase.LoadAssetAtPath<GameObject>(e.prefabPath);if(p==null){errors.Add("Missing "+e.prefabPath);continue;}
                var identity=p.GetComponent<ThemedCarIdentity>();if(identity==null||identity.collectibleId!=e.collectibleId||identity.themeId!=e.themeId||identity.baseCarId!=e.baseCarId)errors.Add("Identity mismatch "+e.collectibleId);
                if(PrefabUtility.GetPrefabAssetType(p)!=PrefabAssetType.Variant)errors.Add("Not a prefab variant "+e.collectibleId);
                foreach(var f in p.GetComponentsInChildren<MeshFilter>(true))
                {
                    meshes++;if(!AssetDatabase.GetAssetPath(f.sharedMesh).StartsWith(Source+"/Meshes/"))errors.Add("Non-supplier mesh "+e.collectibleId+" "+f.name);
                }
                foreach(var r in p.GetComponentsInChildren<Renderer>(true))foreach(var m in r.sharedMaterials)
                {
                    if(m==null){errors.Add("Null material "+e.collectibleId);continue;}
                    if(m.shader==null||m.shader.name.Contains("InternalError"))errors.Add("Invalid shader "+e.collectibleId);
                    string mp=AssetDatabase.GetAssetPath(m);
                    if(mp==Source+"/Materials/Decals_1.mat"||mp==Source+"/Materials/Decals_2.mat")errors.Add("Racing graphics remain "+e.collectibleId);
                    if(mp.EndsWith("_Decals.mat")){decals++;if(!AssetDatabase.GetAssetPath(m.GetTexture("_BaseMap")).StartsWith(Root))errors.Add("Wrong decal texture "+e.collectibleId);}
                }
                if(!File.Exists(e.previewPath))errors.Add("Missing preview "+e.collectibleId);
                if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(p)>0)errors.Add("Missing component "+e.collectibleId);
            }
            var report=new{variantCount=catalog.entries.Length,themeCount=catalog.entries.Select(x=>x.themeId).Distinct().Count(),supplierMeshReferencesChecked=meshes,themedDecalSlots=decals,errors=errors.ToArray(),passed=errors.Count==0};
            File.WriteAllText(Root+"/Data/Themes/unity_validation.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
            if(errors.Count>0)throw new Exception(string.Join("\n",errors));
            Debug.Log("THEMED_VALIDATION_OK "+catalog.entries.Length);
        }
        [MenuItem("Tiny Riders/Themes/Audit Decal UVs")]
        public static void Audit()
        {
            var items = new List<object>();
            for (int c=1;c<=15;c++)
            {
                string name = "Car_"+c.ToString("00");
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Source+"/Prefabs/"+name+"/"+name+".prefab");
                var records = new List<object>();
                foreach(var f in prefab.GetComponentsInChildren<MeshFilter>(true))
                {
                    var r=f.GetComponent<Renderer>();
                    var m=f.sharedMesh;
                    records.Add(new {name=f.name,mesh=AssetDatabase.GetAssetPath(m),materials=r.sharedMaterials.Select(x=>AssetDatabase.GetAssetPath(x)).ToArray(),uv=m.uv.Select(x=>new[]{x.x,x.y}).ToArray(),vertices=m.vertices.Select(x=>new[]{x.x,x.y,x.z}).ToArray(),triangles=m.triangles,submeshes=Enumerable.Range(0,m.subMeshCount).Select(i=>m.GetTriangles(i)).ToArray()});
                }
                items.Add(new {car=c,renderers=records});
            }
            Directory.CreateDirectory("ArtSource/ThemedCars");
            File.WriteAllText("ArtSource/ThemedCars/uv_audit.json",Newtonsoft.Json.JsonConvert.SerializeObject(items));
        }
    }
}
