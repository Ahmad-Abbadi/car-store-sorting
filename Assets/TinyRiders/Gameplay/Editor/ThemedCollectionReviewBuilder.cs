using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using TinyRiders;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class ThemedCollectionReviewBuilder
{
    public const string ScenePath = "Assets/TinyRiders/Scenes/ThemedCollectionReview.unity";
    const string MaterialsPath = "Assets/TinyRiders/Gameplay/Review/Materials";

    static Material Material(string name, Color color)
    {
        string path = MaterialsPath + "/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(material, path);
        }
        material.SetColor("_BaseColor", color); material.SetFloat("_Smoothness", .18f);
        material.enableInstancing = true; EditorUtility.SetDirty(material);
        return material;
    }

    static GameObject Block(string name, Transform parent, Vector3 position, Vector3 size, Material material)
    {
        var block = GameObject.CreatePrimitive(PrimitiveType.Cube); block.name = name;
        block.transform.SetParent(parent, false); block.transform.localPosition = position; block.transform.localScale = size;
        block.GetComponent<Renderer>().sharedMaterial = material;
        UnityEngine.Object.DestroyImmediate(block.GetComponent<Collider>());
        return block;
    }

    static void Label(string name, string text, Transform parent, Vector3 position, float size, Vector2 area, TMP_FontAsset font)
    {
        var label = new GameObject(name); label.transform.SetParent(parent, false);
        label.transform.localPosition = position; label.transform.localRotation = Quaternion.Euler(0,180,0);
        var tmp = label.AddComponent<TextMeshPro>(); tmp.font = font; tmp.text = text;
        tmp.fontSize = size; tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(.10f,.15f,.21f); tmp.rectTransform.sizeDelta = area;
    }

    static Bounds VisibleBounds(GameObject root)
    {
        var renderers = root.GetComponentsInChildren<Renderer>().Where(r=>r.enabled).ToArray();
        if (renderers.Length == 0) throw new Exception("No visible artwork: " + root.name);
        var bounds = renderers[0].bounds;
        foreach(var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
        return bounds;
    }

    [MenuItem("Tools/Tiny Riders/Review/Build Complete Collection Review Scene")]
    public static void Build()
    {
        if (EditorApplication.isPlaying) throw new Exception("Review scene authoring requires Edit mode");
        var catalog = AssetDatabase.LoadAssetAtPath<ThemedCarCatalog>("Assets/TinyRiders/ThemedCollection/Data/Themes/ThemedCarCatalog.asset");
        var themes = catalog.themes.OrderBy(t=>t.displayName,StringComparer.Ordinal).ToArray();
        if (catalog.entries.Length != 600 || themes.Length != 40) throw new Exception("Expected the complete 600-design / 40-theme catalog");
        foreach(var theme in themes)
        {
            var entries=catalog.entries.Where(e=>e.themeId==theme.id).ToArray();
            if(entries.Length!=15||entries.Any(e=>e.prefab==null)||entries.Select(e=>e.baseCarId).Distinct().Count()!=15)
                throw new Exception("Incomplete review lineup: "+theme.id);
        }
        Directory.CreateDirectory(MaterialsPath);AssetDatabase.Refresh();
        var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/00_Project/04_ِArt/UI/Fonts/Fredoka-SemiBold SDF.asset");
        if(font==null)font=TMP_Settings.defaultFontAsset;
        var groundMat=Material("Review Ground",new Color(.43f,.48f,.55f));
        var platformMat=Material("Review Platforms",new Color(.66f,.69f,.74f));
        var previousScenes=Enumerable.Range(0,SceneManager.sceneCount).Select(SceneManager.GetSceneAt).ToArray();
        foreach(var previous in previousScenes)
            if(previous.isDirty&&!EditorSceneManager.SaveScene(previous))throw new Exception("Could not preserve the open scene before review");
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        foreach(var previous in previousScenes)EditorSceneManager.CloseScene(previous,true);
        var collection=new GameObject("COMPLETE COLLECTION - 40 Themes - 600 Designs");
        var centers=new Transform[themes.Length];int count=0;
        bool found=false;Bounds collectionBounds=default;
        for(int themeIndex=0;themeIndex<themes.Length;themeIndex++)
        {
            var theme=themes[themeIndex];
            var group=new GameObject((themeIndex+1).ToString("D2")+" - "+theme.displayName+" ["+theme.id+"]");
            group.transform.SetParent(collection.transform,false);group.transform.localPosition=new Vector3((themeIndex%8)*42,0,-(themeIndex/8)*36);
            var center=new GameObject("Review Camera Target").transform;center.SetParent(group.transform,false);center.localPosition=new Vector3(14,1,-9);centers[themeIndex]=center;
            var accent=Material(theme.id+" Accent",Color.Lerp(theme.color,Color.white,.15f));
            Block("Theme Bay",group.transform,new Vector3(14,-.35f,-9),new Vector3(36,.25f,30),groundMat);
            Block("Theme Color Strip",group.transform,new Vector3(14,-.10f,5.8f),new Vector3(36,.14f,.5f),accent);
            Label("THEME - "+theme.id,theme.displayName+"  |  15 CAR SHAPES",group.transform,new Vector3(14,.13f,5.3f),12f,new Vector2(39,3),font);
            group.transform.Find("THEME - "+theme.id).localRotation=Quaternion.Euler(90,180,0);
            var entries=catalog.entries.Where(e=>e.themeId==theme.id).OrderBy(e=>e.baseCarId,StringComparer.Ordinal).ToArray();
            for(int index=0;index<entries.Length;index++)
            {
                var entry=entries[index];var slot=new GameObject(entry.baseCarId+" - "+entry.collectibleId);
                slot.transform.SetParent(group.transform,false);slot.transform.localPosition=new Vector3((4-index%5)*7,0,-(2-index/5)*9);
                Block("Display Plinth",slot.transform,new Vector3(0,-.06f,0),new Vector3(5.8f,.18f,7),platformMat);
                var art=(GameObject)PrefabUtility.InstantiatePrefab(entry.prefab,scene);art.transform.SetParent(slot.transform,false);
                var bounds=VisibleBounds(art);
                var localCenter=slot.transform.InverseTransformPoint(bounds.center);
                float localBottom=slot.transform.InverseTransformPoint(new Vector3(bounds.center.x,bounds.min.y,bounds.center.z)).y;
                art.transform.localPosition+=new Vector3(-localCenter.x,.08f-localBottom,-localCenter.z);
                PrefabUtility.RecordPrefabInstancePropertyModifications(art.transform);
                foreach(var body in art.GetComponentsInChildren<Rigidbody>(true))
                {body.isKinematic=true;body.useGravity=false;PrefabUtility.RecordPrefabInstancePropertyModifications(body);}
                foreach(var collider in art.GetComponentsInChildren<Collider>(true))
                {collider.enabled=false;PrefabUtility.RecordPrefabInstancePropertyModifications(collider);}
                Label("ID - "+entry.baseCarId,entry.baseCarId,slot.transform,new Vector3(0,.65f,3.6f),10f,new Vector2(6,1.8f),font);
                var world=VisibleBounds(art);if(!found){collectionBounds=world;found=true;}else collectionBounds.Encapsulate(world);
                count++;
            }
        }
        var main=new GameObject("Review Camera - PageUp PageDown Themes - Home Overview");main.tag="MainCamera";
        var camera=main.AddComponent<Camera>();camera.nearClipPlane=.1f;camera.backgroundColor=new Color(.83f,.87f,.92f);camera.clearFlags=CameraClearFlags.SolidColor;
        main.AddComponent<AudioListener>();
        var navigation=main.AddComponent<ThemedCollectionReviewCamera>();navigation.themeCenters=centers;navigation.collectionBounds=collectionBounds;
        navigation.initialTheme=Array.FindIndex(themes,t=>t.id=="TH_ANCIENT_EGYPT");navigation.ShowTheme(navigation.initialTheme);
        var light=new GameObject("Review Key Light").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1f;light.shadows=LightShadows.Soft;light.transform.rotation=Quaternion.Euler(48,-35,0);
        var fill=new GameObject("Review Fill Light").AddComponent<Light>();fill.type=LightType.Directional;fill.intensity=.25f;fill.shadows=LightShadows.None;fill.transform.rotation=Quaternion.Euler(35,145,0);
        RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.3f,.32f,.36f);RenderSettings.fog=false;
        new GameObject("CONTROLS - PageUp/PageDown or Arrows: theme - Home: all - RMB + WASD/QE: fly - Shift: fast - Wheel: zoom");
        EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene,ScenePath))throw new Exception("Could not save review scene");
        AssetDatabase.SaveAssets();
        Selection.activeGameObject=centers[navigation.initialTheme].parent.gameObject;
        if(SceneView.lastActiveSceneView!=null)
            SceneView.lastActiveSceneView.LookAt(centers[navigation.initialTheme].position,camera.transform.rotation,27,false,true);
        var ids=collection.GetComponentsInChildren<ThemedCarIdentity>(true);
        if(ids.Length!=600||ids.Select(i=>i.collectibleId).Distinct().Count()!=600)throw new Exception("Review scene identity count failed");
        Directory.CreateDirectory("IntegrationEvidence");
        File.WriteAllText("IntegrationEvidence/collection_review_scene.json",new JObject{["scene"]=ScenePath,["themes"]=themes.Length,["cars"]=count,["uniqueDesigns"]=ids.Select(i=>i.collectibleId).Distinct().Count(),["copiesPerDesign"]=1,["layout"]="8 x 5 theme bays, each a 5 x 3 car grid",["initialTheme"]=themes[navigation.initialTheme].id,["noGameplayOrSaveManagers"]=UnityEngine.Object.FindObjectsByType<ThemedCollectionSession>(FindObjectsSortMode.None).All(s=>s.gameObject.scene!=scene)}.ToString());
        Debug.Log("COLLECTION_REVIEW_SCENE_READY "+count+" cars / "+themes.Length+" themes");
        ThemedCollectionReviewWindow.Setup();
    }
}
