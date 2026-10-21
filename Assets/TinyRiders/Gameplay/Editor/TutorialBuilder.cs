using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class TutorialBuilder
{
    public const string ScenePath = "Assets/00_Project/00_Scenes/Tutorial.unity";
    const string Root = "Assets/TinyRiders/Gameplay/TutorialAssets";
    static TMP_FontAsset font;
    static Material floor, wall, dado, ceiling, rail, oak, opal, gold;

    [MenuItem("Tools/Tiny Riders/Build First Shelf Tutorial")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play mode first.");
        if (Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty))
            throw new InvalidOperationException("Save your open scene changes before rebuilding the tutorial.");
        Directory.CreateDirectory(Root);
        AssetDatabase.Refresh();
        TutorialLocalization.Build();
        font = Resources.Load<RuntimeLocalizationData>("RuntimeLocalizationData").latinFont;
        gold = Material("Guide gold", new Color(1f,.72f,.16f));
        var source = EditorSceneManager.OpenScene("Assets/00_Project/00_Scenes/Level.unity");
        EditorSceneManager.SaveScene(source, ScenePath, true);
        var scene = EditorSceneManager.OpenScene(ScenePath);
        // Reuse the live store's shared art assets rather than approximating its palette.
        Material StoreMaterial(string name) => Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)
            .First(r=>r.name==name).sharedMaterial;
        floor=StoreMaterial("Store Floor"); wall=StoreMaterial("Gallery wall");
        dado=StoreMaterial("Teal dado"); ceiling=StoreMaterial("Store Ceiling");
        rail=StoreMaterial("Crown rail"); oak=StoreMaterial("Island oak spine"); opal=StoreMaterial("Opal light");
        var floorGraphics=Object.Instantiate(Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)
            .First(t=>t.name==RacingFloorBuilder.DecorationName).gameObject);
        floorGraphics.name="Tutorial Racing Floor Graphics";
        floorGraphics.transform.SetParent(null);
        floorGraphics.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
        floorGraphics.transform.localScale=new Vector3(6f/24.6f,1,6f/26.6f);
        var lighting=Object.FindFirstObjectByType<ShowroomLightingController>();
        var ceilingLight=Object.Instantiate(lighting.ceilingLights.First(l=>l));
        ceilingLight.name="Tutorial Ceiling Light"; ceilingLight.transform.SetParent(null);
        ceilingLight.transform.position=new Vector3(0,3.07f,0);
        var catalog = Object.FindFirstObjectByType<TinyRiders.ThemedCollectionSession>().catalog;
        var a = catalog.entries.First(e=>e.collectibleId == "TR-C001-TH_FIRE-V01").sortingItem;
        var b = catalog.entries.First(e=>e.collectibleId == "TR-C001-TH_ICE-V01").sortingItem;
        var sourceShelves = Object.FindObjectsByType<SortingShelf>(FindObjectsSortMode.None);
        var shelves = new[] { MakeShelf(sourceShelves.First(s=>s.GetComponent<TinyRiders.ThemeShelfBinding>()?.themeId == "TH_FIRE"),a,2,-1.25f), MakeShelf(sourceShelves.First(s=>s.GetComponent<TinyRiders.ThemeShelfBinding>()?.themeId == "TH_ICE"),b,1,1.25f) };
        var keep = new[] { "EventSystem", "========= < player > ========", "========== < Manager > ============", "=====<UI> ==========", "========< ENV > ======", "Touch Input", "HUD Counters Canvas", "Showroom Lighting", "Tutorial Racing Floor Graphics", "Tutorial Ceiling Light" };
        foreach (var root in scene.GetRootGameObjects())
            if (!keep.Contains(root.name) && !shelves.Any(s=>s.gameObject == root)) Object.DestroyImmediate(root);
        var env = scene.GetRootGameObjects().First(g=>g.name.Contains("< ENV >"));
        foreach (Transform child in env.transform.Cast<Transform>().ToArray())
            if (child.name != "Main Camera" && child.name != "Directional Light") Object.DestroyImmediate(child.gameObject);
        var ui = scene.GetRootGameObjects().First(g=>g.name.StartsWith("=====<UI>"));
        foreach (Transform child in ui.transform.Cast<Transform>().ToArray())
            if (!new[]{"Cursor_Canvas","Pause Panel Canvas ","Fading_Canvas","EquipmentItems_UI Canvas"}.Contains(child.name)) Object.DestroyImmediate(child.gameObject);
        var hud = scene.GetRootGameObjects().First(g=>g.name == "HUD Counters Canvas");
        var prompt = hud.GetComponentInChildren<ActionPromptController>(true);
        foreach (Transform child in hud.transform.Cast<Transform>().ToArray())
            if (prompt == null || !(prompt.transform == child || prompt.transform.IsChildOf(child))) Object.DestroyImmediate(child.gameObject);
        foreach (var c in hud.GetComponents<MonoBehaviour>())
            if (c is CoinsCounterUI || c is ItemsCounterUI) Object.DestroyImmediate(c);
        var player = Object.FindFirstObjectByType<Player>();
        foreach (var c in player.GetComponentsInChildren<MonoBehaviour>(true))
            if (c is PlayerSavePosition || c is HighlightItemsPower || c is AutoCollectPower) Object.DestroyImmediate(c);
        player.transform.SetPositionAndRotation(new Vector3(0,.05f,-2.25f),Quaternion.identity);
        player.Look.transform.localRotation = Quaternion.identity;
        Set(player.StashHandler,"startingItems",new Object[0]);
        var inventory = new SerializedObject(player.StashHandler.Inventory);
        inventory.FindProperty("items").ClearArray(); inventory.FindProperty("capacity").intValue = 3; inventory.ApplyModifiedPropertiesWithoutUndo();
        var controller = new GameObject("First Shelf Tutorial").AddComponent<TutorialController>();
        controller.player=player; controller.shelves=shelves; controller.firstCar=a; controller.secondCar=b;
        Room(controller);
        controller.pickupPoint = new GameObject("Pickup recovery point").transform;
        controller.pickupPoint.position = new Vector3(0,.94f,-.25f);
        Car(a,controller.pickupPoint.position,null);
        controller.extraCars = new GameObject("Your turn - two more cars");
        Car(a,new Vector3(-.62f,.94f,-.25f),controller.extraCars.transform);
        Car(b,new Vector3(.62f,.94f,-.25f),controller.extraCars.transform);
        controller.extraCars.SetActive(false);
        BuildHud(controller);
        BuildPause(controller);
        lighting.ceilingLights=new[]{ceilingLight};
        var roof=GameObject.Find("Tiny practice shop/Store Ceiling").GetComponent<Renderer>();
        var diffuser=GameObject.Find("Tiny practice shop/Opal light").GetComponent<Renderer>();
        lighting.surfaces=new[]{
            new ShowroomLightingController.Surface {renderer=roof,bright=MutedShopSurfaceBuilder.CeilingMaterial(false),dark=MutedShopSurfaceBuilder.CeilingMaterial(true)},
            new ShowroomLightingController.Surface {renderer=diffuser,bright=lighting.brightPreset.ceilingMaterials.First(m=>m.name.Contains("Opal")),dark=lighting.darkPreset.ceilingMaterials.First(m=>m.name.Contains("Opal"))}
        };
        if(lighting.reflectionProbe)
        {
            lighting.reflectionProbe.transform.position=new Vector3(0,1.7f,0);
            lighting.reflectionProbe.size=new Vector3(6,3.4f,6);
        }
        lighting.ApplyTheme(LightingThemeSettings.Current);
        EditorSceneManager.SaveScene(scene);
        var scenes=EditorBuildSettings.scenes.ToList();
        scenes.RemoveAll(s=>s.path == ScenePath); scenes.Add(new EditorBuildSettingsScene(ScenePath,true)); EditorBuildSettings.scenes=scenes.ToArray();
        WireMenu();
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene(ScenePath);
        Debug.Log("FIRST_SHELF_BUILT: three cars, two shelves, isolated practice scene.");
    }

    static SortingShelf MakeShelf(SortingShelf original, SortableItem_SO item, int count, float x)
    {
        var shelf=Object.Instantiate(original); shelf.name="Tutorial shelf - "+item.SourceName;
        shelf.transform.SetParent(null); shelf.transform.SetPositionAndRotation(new Vector3(x,0,2.25f),Quaternion.Euler(0,180,0));
        // Keep every renderer, material, divider, and sign exactly as authored in Level.
        // The collection-session binding is replaced only to isolate practice progress.
        Object.DestroyImmediate(shelf.GetComponent<TinyRiders.ThemeShelfBinding>());
        var so=new SerializedObject(shelf);
        var spots=so.FindProperty("placingSpots");
        var authoredSpots=count==2 ? new[]{shelf.GetSpot(1),shelf.GetSpot(3)} : new[]{shelf.GetSpot(2)};
        spots.arraySize=count;
        for(int i=0;i<count;i++)
            spots.GetArrayElementAtIndex(i).objectReferenceValue=authoredSpots[i];
        so.FindProperty("acceptedItem").objectReferenceValue=item;
        so.FindProperty("saveId").stringValue="Tutorial-"+item.SaveId;
        so.ApplyModifiedPropertiesWithoutUndo();
        return shelf;
    }

    static void Room(TutorialController c)
    {
        var root=new GameObject("Tiny practice shop").transform;
        Box("Store Floor",root,new Vector3(0,-.2f,0),new Vector3(6,.4f,6),floor);
        var roof=Box("Store Ceiling",root,new Vector3(0,3.5f,0),new Vector3(6.3f,.2f,6.3f),ceiling);
        roof.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        MetricUVs(roof);
        void Wall(Vector3 pos,Vector3 size)
        {
            var upper=Box("Gallery wall",root,pos,size,wall); MetricUVs(upper);
            var lower=Box("Teal dado",root,new Vector3(pos.x,.55f,pos.z),new Vector3(size.x+.025f,1.1f,size.z+.025f),dado,false); MetricUVs(lower);
            Box("Crown rail",root,new Vector3(pos.x,3.12f,pos.z),new Vector3(size.x+.04f,.07f,size.z+.04f),rail,false);
        }
        Wall(new Vector3(0,1.7f,3.15f),new Vector3(6.6f,3.4f,.3f));
        Wall(new Vector3(0,1.7f,-3.15f),new Vector3(6.6f,3.4f,.3f));
        Wall(new Vector3(-3.15f,1.7f,0),new Vector3(.3f,3.4f,6));
        Wall(new Vector3(3.15f,1.7f,0),new Vector3(.3f,3.4f,6));
        Box("Slim fixture housing",root,new Vector3(0,3.28f,0),new Vector3(3.05f,.08f,.75f),rail,false);
        Box("Opal light",root,new Vector3(0,3.23f,0),new Vector3(2.9f,.035f,.6f),opal,false);
        Box("Display table",root,new Vector3(0,.76f,-.25f),new Vector3(2.1f,.2f,.8f),oak);
        foreach(float x in new[]{-.85f,.85f}) Box("Table leg",root,new Vector3(x,.34f,-.25f),new Vector3(.12f,.68f,.6f),rail);
        var fx=new GameObject("Shelf celebration").AddComponent<ParticleSystem>(); fx.transform.position=new Vector3(0,1.8f,1.8f);
        var main=fx.main; main.playOnAwake=false; main.loop=false; main.duration=.8f; main.startLifetime=.9f; main.startSpeed=1.8f; main.startSize=.045f; main.startColor=new ParticleSystem.MinMaxGradient(new Color(1,.73f,.18f),new Color(.2f,.9f,.75f)); main.gravityModifier=.4f;
        var emission=fx.emission; emission.rateOverTime=0; emission.SetBursts(new[]{new ParticleSystem.Burst(0,35)});
        fx.GetComponent<ParticleSystemRenderer>().sharedMaterial=gold; fx.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear); c.celebration=fx;
    }

    static void MetricUVs(GameObject go)
    {
        var filter=go.GetComponent<MeshFilter>(); var mesh=Object.Instantiate(filter.sharedMesh);
        var vertices=mesh.vertices; var normals=mesh.normals; var uv=new Vector2[vertices.Length];
        for(int i=0;i<vertices.Length;i++)
        {
            var p=go.transform.TransformPoint(vertices[i]); var n=go.transform.TransformDirection(normals[i]);
            uv[i]=(Mathf.Abs(n.y)>.5f?new Vector2(p.x,p.z):Mathf.Abs(n.x)>.5f?new Vector2(p.z,p.y):new Vector2(p.x,p.y))*.5f;
        }
        mesh.uv=uv;
        string path=Root+"/"+go.name+" "+go.transform.localPosition.ToString("F2").Replace(",","_")+".asset";
        var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(saved) { EditorUtility.CopySerialized(mesh,saved); Object.DestroyImmediate(mesh); }
        else { AssetDatabase.CreateAsset(mesh,path); saved=mesh; }
        filter.sharedMesh=saved;
    }

    [MenuItem("Tools/Tiny Riders/Install In-store First Placement Guidance")]
    public static void InstallInStore()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play mode first.");
        var scene = SceneManager.GetActiveScene();
        if (scene.path != "Assets/00_Project/00_Scenes/Level.unity" || scene.isDirty)
            throw new InvalidOperationException("Open the saved Level scene first.");
        if (Object.FindFirstObjectByType<TutorialController>()) throw new InvalidOperationException("Guidance already installed.");
        TutorialLocalization.Build();
        font = Resources.Load<RuntimeLocalizationData>("RuntimeLocalizationData").latinFont;
        var c = new GameObject("In-store first placement guidance").AddComponent<TutorialController>();
        var serialized = new SerializedObject(c);
        serialized.FindProperty("inStore").boolValue = true;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        c.player = Object.FindFirstObjectByType<Player>();
        c.shelves = Object.FindObjectsByType<SortingShelf>(FindObjectsSortMode.None);
        c.pauseMenu = Object.FindFirstObjectByType<PauseMenu>();
        BuildHud(c);
        c.guidance.canvas.name = "First placement contextual instruction";
        c.guidance.canvas.enabled = false;
        var card = c.guidance.instructionCard;
        card.sizeDelta = new Vector2(520, 116);
        card.anchoredPosition = new Vector2(0, -28);
        c.instruction.rectTransform.anchoredPosition = new Vector2(0, 20);
        c.instruction.rectTransform.sizeDelta = new Vector2(488, 42);
        c.instruction.enableAutoSizing = true; c.instruction.fontSizeMin = 22; c.instruction.fontSizeMax = 28;
        c.controlHint.rectTransform.anchoredPosition = new Vector2(0, -23);
        c.controlHint.rectTransform.sizeDelta = new Vector2(488, 40);
        c.controlHint.fontSizeMin = 18; c.controlHint.fontSizeMax = 22;
        c.progress.gameObject.SetActive(false);
        c.guidance.targetArrow.rectTransform.sizeDelta = c.guidance.edgeArrow.rectTransform.sizeDelta = new Vector2(44, 44);
        var skip = Object.Instantiate(c.pauseMenu.Continue_Button, c.pauseMenu.Continue_Button.transform.parent);
        skip.name = "Skip first placement guidance";
        skip.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
        skip.GetComponentInChildren<LocalizedText>(true).SetKey("tutorial.skip");
        skip.transform.SetSiblingIndex(c.pauseMenu.Continue_Button.transform.GetSiblingIndex() + 1);
        skip.gameObject.SetActive(false);
        c.skipButton = skip;
        EditorUtility.SetDirty(c);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
    }

    static void BuildHud(TutorialController c)
    {
        var canvas=new GameObject("Tutorial instruction canvas",typeof(RectTransform),typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler));
        canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
        var scaler=canvas.GetComponent<UnityEngine.UI.CanvasScaler>(); scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution=new Vector2(1920,1080); scaler.matchWidthOrHeight=.5f;
        var panel=new GameObject("Lesson card",typeof(RectTransform),typeof(UnityEngine.UI.Image)); panel.transform.SetParent(canvas.transform,false);
        var rect=(RectTransform)panel.transform; rect.anchorMin=rect.anchorMax=new Vector2(.5f,1); rect.pivot=new Vector2(.5f,1); rect.anchoredPosition=new Vector2(0,-35); rect.sizeDelta=new Vector2(780,160);
        ToyShop.UI.ToyShopStyle.Surface(panel.GetComponent<UnityEngine.UI.Image>(),new Color(1,.98f,.91f,.97f)); panel.GetComponent<UnityEngine.UI.Image>().raycastTarget=false;
        c.instruction=Label("Instruction",rect,new Vector2(0,24),new Vector2(730,65),34);
        c.controlHint=Label("Control icons",rect,new Vector2(-80,-30),new Vector2(480,44),26);
        c.progress=Label("Progress",rect,new Vector2(270,-36),new Vector2(170,35),22);
        ConfigureGuidance(c);
    }

    [MenuItem("Tools/Tiny Riders/Refresh Tutorial Guidance")]
    public static void RefreshGuidance()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play mode first.");
        if(Enumerable.Range(0,SceneManager.sceneCount).Any(i=>SceneManager.GetSceneAt(i).isDirty))
            throw new InvalidOperationException("Save your open scene changes before refreshing tutorial guidance.");
        var scene=EditorSceneManager.OpenScene(ScenePath);
        TutorialLocalization.Build();
        ConfigureGuidance(Object.FindFirstObjectByType<TutorialController>());
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
    }

    static void ConfigureGuidance(TutorialController c)
    {
        var old=GameObject.Find("Gentle target glow"); if(old) Object.DestroyImmediate(old);
        var canvas=c.controlHint.GetComponentInParent<Canvas>();
        var presenter=canvas.GetComponent<TutorialGuidancePresenter>();
        if(!presenter) presenter=canvas.gameObject.AddComponent<TutorialGuidancePresenter>();
        presenter.tutorial=c; presenter.canvas=canvas; c.guidance=presenter;
        presenter.instructionCard=c.instruction.transform.parent as RectTransform;
        presenter.mouseIcons=AssetDatabase.LoadAssetAtPath<TMP_SpriteAsset>("Assets/00_Project/04_ِArt/UI/Input Icons/InputIcons_Mouse.asset");
        presenter.touchIcons=Resources.Load<TMP_SpriteAsset>("Sprite Assets/"+InputIcons.TouchAsset);
        c.controlHint.spriteAsset=presenter.mouseIcons; c.controlHint.richText=true;
        c.controlHint.enableAutoSizing=true; c.controlHint.fontSizeMin=18; c.controlHint.fontSizeMax=26;
        canvas.sortingOrder=120;
        var arrow=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/00_Project/04_ِArt/UI/Touch Icons/Touch_ArrowUp.png");
        UnityEngine.UI.Image Arrow(string name)
        {
            var child=canvas.transform.Find(name);
            var image=child?child.GetComponent<UnityEngine.UI.Image>():null;
            if(!image)
            {
                image=new GameObject(name,typeof(RectTransform)).AddComponent<UnityEngine.UI.Image>();
                image.transform.SetParent(canvas.transform,false);
            }
            image.sprite=arrow; image.raycastTarget=false; image.preserveAspect=true;
            image.color=new Color(1f,.76f,.2f);
            image.rectTransform.anchorMin=image.rectTransform.anchorMax=new Vector2(.5f,.5f);
            image.rectTransform.sizeDelta=new Vector2(90,90);
            var outline=image.GetComponent<UnityEngine.UI.Outline>();
            if(!outline) outline=image.gameObject.AddComponent<UnityEngine.UI.Outline>();
            outline.effectColor=new Color(.035f,.1f,.15f,.95f); outline.effectDistance=new Vector2(2,-2);
            image.gameObject.SetActive(false);
            return image;
        }
        presenter.targetArrow=Arrow("Target arrow"); presenter.edgeArrow=Arrow("Screen edge arrow");
        EditorUtility.SetDirty(c); EditorUtility.SetDirty(presenter);
    }

    static void BuildPause(TutorialController c)
    {
        var pause=Object.FindFirstObjectByType<PauseMenu>(); c.pauseMenu=pause;
        var reset=pause.GetComponent<ResetProgressButton>(); if(reset) Object.DestroyImmediate(reset);
        foreach(var t in pause.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Button_Reset"||t.name=="Confirm_Panel"||t.name=="Helper_Row").ToArray()) Object.DestroyImmediate(t.gameObject);
        var skip=Object.Instantiate(pause.Continue_Button,pause.Continue_Button.transform.parent); skip.name="Skip Tutorial";
        skip.onClick=new UnityEngine.UI.Button.ButtonClickedEvent();
        skip.GetComponentInChildren<LocalizedText>(true).SetKey("tutorial.skip");
        skip.transform.SetSiblingIndex(1); c.skipButton=skip;
    }

    static void WireMenu()
    {
        var scene=EditorSceneManager.OpenScene("Assets/00_Project/00_Scenes/MainMenu Scene.unity");
        var menu=Object.FindFirstObjectByType<MainMenu>(); var so=new SerializedObject(menu);
        var play=(UnityEngine.UI.Button)so.FindProperty("playButton").objectReferenceValue;
        var existing=so.FindProperty("tutorialButton").objectReferenceValue as UnityEngine.UI.Button;
        var button=existing?existing:Object.Instantiate(play,play.transform.parent);
        button.name="Learn to Play"; button.onClick=new UnityEngine.UI.Button.ButtonClickedEvent();
        button.transform.SetSiblingIndex(play.transform.GetSiblingIndex()+1);
        button.GetComponentInChildren<LocalizedText>(true).SetKey("tutorial.replay");
        so.FindProperty("tutorialButton").objectReferenceValue=button; so.ApplyModifiedPropertiesWithoutUndo();
        var card=play.transform.parent.parent as RectTransform;
        if(card && card.name=="Menu Card") card.sizeDelta=new Vector2(card.sizeDelta.x,640);
        if(card) ToyCarSortBranding.ApplyMenu(card.parent.gameObject);
        EditorSceneManager.SaveScene(scene);
    }

    static void Car(SortableItem_SO item,Vector3 pos,Transform parent)
    {
        var car=(WorldItemBase)PrefabUtility.InstantiatePrefab(item.WorldItem);
        car.transform.SetParent(parent); car.transform.SetPositionAndRotation(pos,Quaternion.Euler(0,90,0));
    }
    static GameObject Box(string name,Transform parent,Vector3 pos,Vector3 scale,Material mat,bool solid=true)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cube); go.name=name; go.transform.SetParent(parent,false); go.transform.localPosition=pos; go.transform.localScale=scale; go.GetComponent<Renderer>().sharedMaterial=mat;
        if(!solid) Object.DestroyImmediate(go.GetComponent<Collider>()); return go;
    }
    static Material Material(string name,Color color)
    {
        string path=Root+"/"+name+".mat"; var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m) { m=new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m,path); }
        m.SetColor("_BaseColor",color); m.SetFloat("_Smoothness",.2f); EditorUtility.SetDirty(m); return m;
    }
    static TextMeshPro Text(string name,Transform parent,Vector3 pos,Vector2 size,float height)
    {
        var t=new GameObject(name).AddComponent<TextMeshPro>(); t.transform.SetParent(parent,false); t.transform.localPosition=pos;
        t.font=font; t.fontSize=height*10; t.rectTransform.sizeDelta=size; t.alignment=TextAlignmentOptions.Center; t.color=new Color(.04f,.12f,.17f); return t;
    }
    static TextMeshProUGUI Label(string name,Transform parent,Vector2 pos,Vector2 size,int fontSize)
    {
        var t=new GameObject(name,typeof(RectTransform)).AddComponent<TextMeshProUGUI>(); t.transform.SetParent(parent,false);
        t.rectTransform.anchoredPosition=pos; t.rectTransform.sizeDelta=size; t.font=font; t.fontSize=fontSize; t.alignment=TextAlignmentOptions.Center; t.color=new Color(.04f,.12f,.17f); t.raycastTarget=false; return t;
    }
    static void Set(Object target,string field,Object[] values)
    { var so=new SerializedObject(target); var p=so.FindProperty(field); p.arraySize=values.Length; for(int i=0;i<values.Length;i++)p.GetArrayElementAtIndex(i).objectReferenceValue=values[i]; so.ApplyModifiedPropertiesWithoutUndo(); }
}
