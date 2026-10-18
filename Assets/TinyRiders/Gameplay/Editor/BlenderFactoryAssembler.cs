using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace TinyRiders.Editor
{
    /// <summary>Integrates Blender meshes while retaining prefab and localized sign identities.</summary>
    public static class BlenderFactoryAssembler
    {
        public const string Art = UpgradeFactoryBuilder.Art + "/Blender";
        const string Source = "ArtSource/UpgradeStation/Blender/Export";

        public static void Build()
        {
            Import();
            var model=AssetDatabase.LoadAssetAtPath<GameObject>(Art+"/ToyRacingFactory.fbx");
            if(!model)throw new InvalidOperationException("Build the Blender source first: ArtSource/UpgradeStation/Blender/build_factory.py");
            var atlas=CreateMaterial();
            var glow=AssetDatabase.LoadAssetAtPath<Material>(UpgradeFactoryBuilder.Art+"/Mint phosphor.mat");
            var root=PrefabUtility.LoadPrefabContents(UpgradeFactoryBuilder.PrefabPath);
            try
            {
                var visual=root.transform.Find("Visual");
                var p=visual.GetComponent<UpgradeFactoryPresentation>();
                var signs=visual.GetComponentsInChildren<TMP_Text>(true);
                foreach(var sign in signs)sign.transform.SetParent(visual,false);
                var fx=p.completionParticles;
                if(fx)fx.transform.SetParent(visual,false);
                var scanner=p.scanner;var indicators=p.readyIndicators;
                foreach(Transform child in visual.Cast<Transform>().ToArray())
                    if(!child.GetComponent<TMP_Text>() && !child.GetComponent<ParticleSystem>() && child!=scanner && child!=indicators.transform)
                        Object.DestroyImmediate(child.gameObject);
                var imported=Object.Instantiate(model,visual);
                foreach(var r in imported.GetComponentsInChildren<MeshRenderer>())r.sharedMaterial=atlas;
                Transform Part(string name)=>imported.GetComponentsInChildren<Transform>().First(t=>t.name==name);
                var shell=Group("Shell",visual);
                var housing=Part("Housing");housing.SetParent(shell,true);
                Part("Button").SetParent(shell,true);
                var table=Part("Turntable");table.SetParent(visual,true);
                var left=Part("LeftArm");left.SetParent(visual,true);
                var right=Part("RightArm");right.SetParent(visual,true);
                Object.DestroyImmediate(imported);
                UpgradeFactoryBuilder.CreateDisplayCar(table);
                UpgradeFactoryBuilder.Combine(table.Find("Miniature rally car"),"BlenderDisplayCar");
                p.turntable=table;p.leftArm=left;p.rightArm=right;p.visibilityBody=housing.GetComponent<Renderer>();
                fx.transform.localPosition=new Vector3(0,1.22f,-.025f);
                scanner.localPosition=new Vector3(0,1.07f,-.025f);scanner.gameObject.SetActive(false);
                indicators.transform.localPosition=new Vector3(-.12f,1.036f,.292f);
                var lights=Group("Chamber illumination",shell);
                Strip(lights,new Vector3(0,1.709f,-.04f),new Vector3(.45f,.009f,.06f),glow);
                foreach(float x in new[]{-.257f,.257f})Strip(lights,new Vector3(x,1.34f,-.227f),new Vector3(.009f,.48f,.006f),glow);
                UpgradeFactoryBuilder.Combine(lights,"BlenderIllumination");
                PlaceSign(signs,"UPGRADES sign",new Vector3(0,1.814f,.283f),.49f,new Color(.015f,.19f,.24f),.55f,.095f);
                PlaceSign(signs,"Series plate",new Vector3(0,.455f,.346f),.19f,new Color(.015f,.19f,.24f),.4f,.055f);
                PlaceSign(signs,"Chamber caption",new Vector3(0,1.65f,-.229f),.17f,new Color(.015f,.28f,.31f),.43f,.04f);
                foreach(var t in visual.GetComponentsInChildren<Transform>(true))t.gameObject.layer=root.layer;
                PrefabUtility.SaveAsPrefabAsset(root,UpgradeFactoryBuilder.PrefabPath);
                AssetDatabase.SaveAssets();
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }

        static void Import()
        {
            Directory.CreateDirectory(Art);
            foreach(string name in new[]{"ToyRacingFactory.fbx","Factory_BaseColor.png","Factory_Normal.png","Factory_Occlusion.png","Factory_MetallicSmoothness.png"})
                if(File.Exists(Source+"/"+name))File.Copy(Source+"/"+name,Art+"/"+name,true);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach(string name in new[]{"BaseColor","Normal","Occlusion","MetallicSmoothness"})
            {
                var importer=AssetImporter.GetAtPath(Art+"/Factory_"+name+".png") as TextureImporter;
                if(!importer)throw new InvalidOperationException("Missing baked texture "+name);
                importer.textureType=name=="Normal"?TextureImporterType.NormalMap:TextureImporterType.Default;
                importer.sRGBTexture=name=="BaseColor";importer.maxTextureSize=2048;
                importer.mipmapEnabled=true;importer.anisoLevel=4;importer.alphaSource=TextureImporterAlphaSource.FromInput;
                importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.SaveAndReimport();
            }
            var fbx=AssetImporter.GetAtPath(Art+"/ToyRacingFactory.fbx") as ModelImporter;
            if(fbx)
            {
                fbx.importCameras=false;fbx.importLights=false;fbx.importAnimation=false;
                fbx.materialImportMode=ModelImporterMaterialImportMode.None;fbx.importNormals=ModelImporterNormals.Import;
                fbx.importTangents=ModelImporterTangents.CalculateMikk;fbx.bakeAxisConversion=true;fbx.SaveAndReimport();
            }
        }

        static Material CreateMaterial()
        {
            string path=Art+"/FactoryAtlas.mat";
            var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}
            Texture2D Map(string name)=>AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"/Factory_"+name+".png");
            mat.SetTexture("_BaseMap",Map("BaseColor"));mat.SetColor("_BaseColor",Color.white);
            mat.SetTexture("_BumpMap",Map("Normal"));mat.EnableKeyword("_NORMALMAP");mat.SetFloat("_BumpScale",.12f);
            mat.SetTexture("_MetallicGlossMap",Map("MetallicSmoothness"));mat.EnableKeyword("_METALLICSPECGLOSSMAP");mat.SetFloat("_Smoothness",.85f);
            mat.SetTexture("_OcclusionMap",Map("Occlusion"));mat.EnableKeyword("_OCCLUSIONMAP");mat.SetFloat("_OcclusionStrength",.25f);
            // Restrained albedo fill preserves saturated enamel in the shop's shadowed corners.
            mat.SetTexture("_EmissionMap",Map("BaseColor"));mat.EnableKeyword("_EMISSION");mat.SetColor("_EmissionColor",Color.white*.18f);
            EditorUtility.SetDirty(mat);return mat;
        }

        static void PlaceSign(TMP_Text[] signs,string name,Vector3 position,float size,Color color,float width,float height)
        {
            var sign=signs.First(t=>t.name==name);
            sign.transform.localPosition=position;sign.transform.localRotation=Quaternion.Euler(0,180,0);
            sign.color=color;sign.fontSize=size;sign.fontSizeMax=size;sign.fontSizeMin=size*.55f;
            sign.enableAutoSizing=true;sign.rectTransform.sizeDelta=new Vector2(width,height);
            sign.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
        }

        static Transform Group(string name,Transform parent)
        {var go=new GameObject(name);go.transform.SetParent(parent,false);return go.transform;}

        static void Strip(Transform parent,Vector3 position,Vector3 scale,Material mat)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name="Light strip";
            Object.DestroyImmediate(go.GetComponent<Collider>());go.transform.SetParent(parent,false);
            go.transform.localPosition=position;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=mat;
        }
    }
}

