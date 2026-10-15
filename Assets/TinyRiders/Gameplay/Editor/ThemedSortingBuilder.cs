using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Newtonsoft.Json.Linq;
using TinyRiders;
using TMPro;
public static class ThemedSortingBuilder
{
 const string R="Assets/TinyRiders/Gameplay";
 const string C="Assets/TinyRiders/ThemedCollection/Data/Themes/";
 const string ScenePath="Assets/TinyRiders/Scenes/ThemedSorting.unity";
 const float Scale=9f;
 static void Set(UnityEngine.Object obj,string field, UnityEngine.Object value) { var so=new SerializedObject(obj);so.FindProperty(field).objectReferenceValue=value;so.ApplyModifiedPropertiesWithoutUndo(); }
 static void String(UnityEngine.Object obj,string field,string value) { var so=new SerializedObject(obj);so.FindProperty(field).stringValue=value;so.ApplyModifiedPropertiesWithoutUndo(); }
 static void Float(UnityEngine.Object obj,string field,float value) {var so=new SerializedObject(obj);so.FindProperty(field).floatValue=value;so.ApplyModifiedPropertiesWithoutUndo();}
 static T Asset<T>(string path) where T:ScriptableObject {var a=AssetDatabase.LoadAssetAtPath<T>(path);if(a==null){a=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(a,path);}return a;}
 static Bounds Bounds(GameObject o) {var rr=o.GetComponentsInChildren<Renderer>(false).Where(r=>r.enabled&&(r is MeshRenderer||r is SkinnedMeshRenderer)).ToArray();if(rr.Length==0)throw new Exception("No visible art "+o.name);var b=rr[0].bounds;foreach(var r in rr.Skip(1))b.Encapsulate(r.bounds);return b;}
 internal static GameObject Wrap(ThemedCarCatalog.Entry e, SortableItem_SO item,string kind, out Vector3 size)
 {
  var root=new GameObject(kind+" "+e.collectibleId);root.layer=8;
  try {
   var visual=(GameObject)PrefabUtility.InstantiatePrefab(e.prefab);visual.transform.SetParent(root.transform,false);
   // Only wrapper physics changes. Source artwork, materials and accessory transforms remain referenced.
   foreach(var body in visual.GetComponentsInChildren<Rigidbody>(true))UnityEngine.Object.DestroyImmediate(body);
   foreach(var col in visual.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(col);
   foreach(var tr in visual.GetComponentsInChildren<Transform>(true)) tr.gameObject.layer=2;
   var b=Bounds(visual);size=b.size;
   visual.transform.localPosition=new Vector3(-b.center.x,-b.min.y,-b.center.z);
   var group=root.AddComponent<ItemVisualGroup>();Set(group,"visualRoot",visual.transform);
   root.AddComponent<ThemedWorldInstance>();
   if(kind=="World") {var w=root.AddComponent<SortableWorldItem>();Set(w,"data",item);var rb=root.AddComponent<Rigidbody>();rb.isKinematic=true;rb.mass=.3f;rb.interpolation=RigidbodyInterpolation.Interpolate;}
   else if(kind=="Held") {var h=root.AddComponent<HandItemBase>();Set(h,"data",item);}
   else {var p=root.AddComponent<PlacedOnShelfItemBase>();Float(p,"punchDuration",.15f);var so=new SerializedObject(p);so.FindProperty("punchScale").vector3Value=Vector3.zero;so.ApplyModifiedPropertiesWithoutUndo();}
   if(kind!="Held") {var box=root.AddComponent<BoxCollider>();box.center=new Vector3(0,size.y*.5f,0);box.size=size;}
   if(kind=="World")ThemedPickingBounds.AddToWrapper(root);
   return PrefabUtility.SaveAsPrefabAsset(root,R+"/Prefabs/"+kind+"_"+e.collectibleId+".prefab");
  } finally {UnityEngine.Object.DestroyImmediate(root);}
 }
 [MenuItem("Tools/Tiny Riders/Sorting/Build Themed Showroom")]
 public static void Build()
 {
  if(EditorApplication.isPlaying)throw new Exception("Exit Play mode first");
  Directory.CreateDirectory(R+"/Prefabs");Directory.CreateDirectory(R+"/Items");Directory.CreateDirectory(R+"/Themes");AssetDatabase.Refresh();
  var settings=ThemedSortingAuthoringSettings.Load();
  var catalog=AssetDatabase.LoadAssetAtPath<ThemedCarCatalog>(C+"ThemedCarCatalog.asset");
  var previousThemes=(catalog.themes??Array.Empty<ThemedCarCatalog.Theme>()).ToDictionary(t=>t.id,t=>t.enabled);
  var recipes=(JArray)JObject.Parse(File.ReadAllText(C+"theme_recipes.json"))["themes"];
  var portable=(JArray)JObject.Parse(File.ReadAllText(C+"variant_catalog.json"))["entries"];
  var reg=JObject.Parse(File.ReadAllText(C+"id_registry.json"));var initial=new HashSet<string>(((JArray)reg["immutableIds"]).Select(x=>(string)x));
  catalog.themes=recipes.Select(t=> {ColorUtility.TryParseHtmlString((string)t["paint"],out var color);return new ThemedCarCatalog.Theme{id=(string)t["id"],displayName=(string)t["name"],motif=(string)t["icon"],color=color,shelfGroup=(string)t["id"],enabled=!previousThemes.ContainsKey((string)t["id"])||previousThemes[(string)t["id"]]};}).ToArray();
  var cats=new Dictionary<string,Catagory_SO>();
  foreach(var t in catalog.themes){var cat=Asset<Catagory_SO>(R+"/Themes/"+t.id+".asset");cat.CatagoryID=t.id;cat.CatagoryName=t.displayName;cats.Add(t.id,cat);}
  var unavailable=new List<string>();var items=new List<SortableItem_SO>();
  foreach(var e in catalog.entries) {
   bool requested=e.sortingItem==null||e.enabled;
   e.unavailableReason="";e.enabled=false;
   if(e.prefab==null){e.unavailableReason="Missing approved prefab";unavailable.Add(e.collectibleId+": "+e.unavailableReason);continue;}
   var identity=e.prefab.GetComponent<ThemedCarIdentity>();
   if(identity==null||identity.collectibleId!=e.collectibleId||identity.themeId!=e.themeId||!cats.ContainsKey(e.themeId)||!e.prefab.GetComponentsInChildren<Transform>(true).Any(x=>x.name=="ThemeAccessories")) {e.unavailableReason="Identity, theme or accessory validation failed";unavailable.Add(e.collectibleId+": "+e.unavailableReason);continue;}
   var record=portable.FirstOrDefault(p=>(string)p["collectibleId"]==e.collectibleId); if(record==null){e.unavailableReason="Missing registry membership";continue;}
   e.releaseId=initial.Contains(e.collectibleId)?"themed_initial_v1":(string)record["releaseId"];
   var item=Asset<SortableItem_SO>(R+"/Items/"+e.collectibleId+".asset");String(item,"saveId",e.collectibleId);String(item,"displayName",e.displayName);item.Catagory=cats[e.themeId];
   // World units match the unchanged cars. Same scale for world, hand and shelf forms.
   item.CameraFixed=true;item.HeldPosition=new Vector3(1.6f,-2.1f,7.8f);item.HeldRotation=new Vector3(0,150,0);
   var previewPath=AssetDatabase.GetAssetPath(e.preview);if(!string.IsNullOrEmpty(previewPath))item.ItemImage=AssetDatabase.LoadAssetAtPath<Sprite>(previewPath);
   var world=Wrap(e,item,"World",out var size);var held=Wrap(e,item,"Held",out _);var placed=Wrap(e,item,"Shelf",out _);
   e.displaySize=size;e.sortingItem=item;e.enabled=requested;
   if(size.x>.464f*Scale||size.y>.54f*Scale||size.z>.648f*Scale) {e.enabled=false;e.unavailableReason="Measured accessory exceeds fixed shelf clearance";unavailable.Add(e.collectibleId+": "+e.unavailableReason);}
   Set(item,"worldItem",world.GetComponent<WorldItemBase>());Set(item,"handItem",held.GetComponent<HandItemBase>());item.PlacedOnShelfPrefab=placed.GetComponent<PlacedOnShelfItemBase>();EditorUtility.SetDirty(item);items.Add(item);
  }
  foreach(var cat in cats.Values){cat.Items=items.Where(i=>i.Catagory==cat).ToList();EditorUtility.SetDirty(cat);}
  EditorUtility.SetDirty(catalog);
  var db=Asset<ItemDatabaseSO>(R+"/ThemedItemDatabase.asset");var dbso=new SerializedObject(db);var list=dbso.FindProperty("items");list.arraySize=items.Count;for(int i=0;i<items.Count;i++)list.GetArrayElementAtIndex(i).objectReferenceValue=items[i];dbso.ApplyModifiedPropertiesWithoutUndo();
  AssetDatabase.SaveAssets();
  // Copy the existing template. Original Level and all old saves remain available.
  int units=catalog.themes.Where(t=>t.enabled).Sum(t=>Mathf.CeilToInt(catalog.entries.Count(e=>e.enabled&&e.themeId==t.id)*settings.copiesPerDesign/18f));
  int perSide=Mathf.Max(1,Mathf.CeilToInt(units/4f));float radius=Mathf.Max(250,(perSide-1)*17f+34f);
  var template=EditorSceneManager.OpenScene("Assets/00_Project/00_Scenes/Level.unity",OpenSceneMode.Single);
  EditorSceneManager.SaveScene(template,ScenePath,true);var scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
  var generated=scene.GetRootGameObjects().First(r=>r.name=="Car Store Generated");
  foreach(Transform child in generated.transform.Cast<Transform>().ToArray()) if(child.name!="Store Architecture"&&child.name!="Store UI Controller")UnityEngine.Object.DestroyImmediate(child.gameObject);
  var architecture=generated.transform.Find("Store Architecture");
  // Expand this copied room once about the original room centre; no shelf geometry is rebuilt.
  var calibration=new GameObject("Fixed Room Calibration").transform;calibration.position=new Vector3(500,0,0);architecture.SetParent(calibration,true);calibration.localScale=Vector3.one*Mathf.Max(14,(radius+24)/20f);
  foreach(var wi in UnityEngine.Object.FindObjectsByType<WorldItemBase>(FindObjectsInactive.Include,FindObjectsSortMode.None))UnityEngine.Object.DestroyImmediate(wi.gameObject);
  foreach(var s in UnityEngine.Object.FindObjectsByType<SortingShelf>(FindObjectsInactive.Include,FindObjectsSortMode.None))UnityEngine.Object.DestroyImmediate(s.gameObject);
  foreach(var section in UnityEngine.Object.FindObjectsByType<Section>(FindObjectsInactive.Include,FindObjectsSortMode.None))UnityEngine.Object.DestroyImmediate(section.gameObject);
  var orphan=scene.GetRootGameObjects().FirstOrDefault(r=>r.name=="TR_Shelf_Base_01");if(orphan!=null)UnityEngine.Object.DestroyImmediate(orphan);
  foreach(var legacy in UnityEngine.Object.FindObjectsByType<CarVisualCulling>(FindObjectsInactive.Include,FindObjectsSortMode.None))UnityEngine.Object.DestroyImmediate(legacy);
  var sessionObject=new GameObject("Themed Continuous Collection");var session=sessionObject.AddComponent<ThemedCollectionSession>();session.catalog=catalog;session.worldScale=Scale;session.visiblePileLimit=settings.visiblePileLimit;session.copiesPerDesign=settings.copiesPerDesign;session.seed=settings.seed;session.pileSpacing=new Vector3(5.8f,4f,6.4f);
  var pile=new GameObject("Mixed Themed Pile Origin").transform;pile.position=new Vector3(500,0,-70);session.pileOrigin=pile;
  var shelfArt=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TinyRiders/Art/Shelves/BaseShelf_01/Prefabs/TR_Shelf_Base_01.prefab");
  var assignments=new List<string>();int shelfIndex=0;
  foreach(var theme in catalog.themes.Where(t=>t.enabled)) {
   int count=Mathf.CeilToInt(catalog.entries.Count(e=>e.enabled&&e.themeId==theme.id)*settings.copiesPerDesign/18f);
   for(int unit=1;unit<=count;unit++) {
    int side=shelfIndex/perSide;int col=shelfIndex%perSide;float along=(col-(perSide-1)*.5f)*34f;
    Vector3 pos;float yaw;
    if(side==0){pos=new Vector3(500+along,0,radius);yaw=180;}else if(side==1){pos=new Vector3(500+radius,0,-along);yaw=-90;}else if(side==2){pos=new Vector3(500-along,0,-radius);yaw=0;}else{pos=new Vector3(500-radius,0,along);yaw=90;}
    var root=new GameObject("Theme Shelf "+theme.id+" "+unit);root.layer=8;root.transform.SetPositionAndRotation(pos,Quaternion.Euler(0,yaw,0));
    var art=(GameObject)PrefabUtility.InstantiatePrefab(shelfArt,scene);art.transform.SetParent(root.transform,false);art.transform.localScale=Vector3.one*Scale;
    foreach(var tr in art.GetComponentsInChildren<Transform>(true))tr.gameObject.layer=2;
    foreach(var collision in art.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(collision);
    var markers=art.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("Slot_R",StringComparison.Ordinal)).OrderBy(t=>t.name,StringComparer.Ordinal).ToArray();if(markers.Length!=18)throw new Exception("Approved shelf must have 18 markers");
    var shelf=root.AddComponent<SortingShelf>();var so=new SerializedObject(shelf);var spots=so.FindProperty("placingSpots");spots.arraySize=18;for(int n=0;n<18;n++)spots.GetArrayElementAtIndex(n).objectReferenceValue=markers[n];so.FindProperty("_catagory").objectReferenceValue=cats[theme.id];so.FindProperty("saveId").stringValue=theme.id+"-S"+unit.ToString("D3");so.FindProperty("jumpDuration").floatValue=.2f;so.FindProperty("jumpPower").floatValue=.07f;so.FindProperty("fullPunchScale").vector3Value=Vector3.one*.003f;so.ApplyModifiedPropertiesWithoutUndo();
    var binding=root.AddComponent<ThemeShelfBinding>();binding.themeId=theme.id;binding.clearance=new Vector3(.464f,.54f,.648f)*Scale;
    var surface=new GameObject("Theme Placement Surface");surface.layer=8;surface.transform.SetParent(root.transform,false);surface.transform.localPosition=new Vector3(0,1.8f*Scale,.50f*Scale);var box=surface.AddComponent<BoxCollider>();box.size=new Vector3(3.48f,3.2f,.08f)*Scale;surface.AddComponent<SortingShelfPlacementSurface>();
    var back=root.AddComponent<BoxCollider>();back.center=new Vector3(0,1.8f,-.4f)*Scale;back.size=new Vector3(3.5f,3.5f,.15f)*Scale;
    var label=new GameObject("Replaceable Theme Header");label.transform.SetParent(root.transform,false);label.transform.localPosition=new Vector3(0,3.22f,.54f)*Scale;label.transform.localRotation=Quaternion.Euler(0,180,0);var txt=label.AddComponent<TextMeshPro>();txt.text=theme.displayName;txt.fontSize=12;txt.alignment=TextAlignmentOptions.Center;txt.color=Color.white;txt.rectTransform.sizeDelta=new Vector2(30,3);
    assignments.Add(shelf.SaveId+" = "+theme.id+" / 18 slots");shelfIndex++;
   }
  }
  var player=UnityEngine.Object.FindFirstObjectByType<Player>();
  foreach(var t in player.GetComponentsInChildren<Transform>(true)) if(t!=player.transform)t.localPosition*=Scale;
  player.transform.position=new Vector3(500,.05f,-125);
  if(player.Motor!=null)player.Motor.SetCapsuleDimensions(.35f*Scale,1.8f*Scale,.9f*Scale);
  if(player.Character!=null){player.Character.MaxStableMoveSpeed*=Scale;player.Character.MaxAirMoveSpeed*=Scale;player.Character.Gravity*=Scale;player.Character.JumpUpSpeed*=Scale;}
  var savePosition=UnityEngine.Object.FindFirstObjectByType<PlayerSavePosition>();if(savePosition!=null)String(savePosition,"saveKey","TinyRiders.Themed.PlayerPosition.v1");
  foreach(var camera in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include,FindObjectsSortMode.None)){camera.farClipPlane=1600;camera.nearClipPlane=.15f;}
  foreach(var vc in UnityEngine.Object.FindObjectsByType<Unity.Cinemachine.CinemachineCamera>(FindObjectsInactive.Include,FindObjectsSortMode.None)){var lens=vc.Lens;lens.FarClipPlane=1600;lens.NearClipPlane=.15f;vc.Lens=lens;}
  var save=UnityEngine.Object.FindFirstObjectByType<LevelSaveManager>();Set(save,"database",AssetDatabase.LoadAssetAtPath<ItemDatabaseSO>(R+"/ThemedItemDatabase.asset"));String(save,"saveKey","TinyRiders.Themed.Sorting.v1");Float(save,"autosaveInterval",8f);
  var physics=UnityEngine.Object.FindFirstObjectByType<ItemPhysicsCulling>();Float(physics,"activeRadius",12f);Float(physics,"freezeBuffer",8f);var ps=new SerializedObject(physics);ps.FindProperty("maxDynamic").intValue=24;ps.ApplyModifiedPropertiesWithoutUndo();
  var upgradeBox=UnityEngine.Object.FindFirstObjectByType<UpgradeBox>();if(upgradeBox!=null){upgradeBox.transform.position=new Vector3(535,0,-135);upgradeBox.transform.localScale*=Scale;}
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
  // Preserve the normal startup path; the existing world mapper now opens the continuous themed showroom.
  foreach(string guid in AssetDatabase.FindAssets("t:WorldData")){var w=AssetDatabase.LoadAssetAtPath<WorldData>(AssetDatabase.GUIDToAssetPath(guid));if(w.SceneNames.Contains("Level")){w.SceneNames=w.SceneNames.Select(n=>n=="Level"?"ThemedSorting":n).ToList();EditorUtility.SetDirty(w);}}
  var builds=EditorBuildSettings.scenes.ToList();if(!builds.Any(s=>s.path==ScenePath))builds.Add(new EditorBuildSettingsScene(ScenePath,true));EditorBuildSettings.scenes=builds.ToArray();AssetDatabase.SaveAssets();
  File.WriteAllLines("IntegrationEvidence/shelf_assignments.txt",assignments);File.WriteAllLines("IntegrationEvidence/unavailable_assets.txt",unavailable);
  File.WriteAllText("IntegrationEvidence/build_summary.json",new JObject{["scene"]=ScenePath,["fixedShelfScale"]=Scale,["variants"]=catalog.entries.Count(e=>e.enabled),["themes"]=catalog.themes.Length,["physicalInstances"]=catalog.entries.Count(e=>e.enabled)*settings.copiesPerDesign,["shelves"]=shelfIndex,["capacity"]=shelfIndex*18,["visiblePileLimit"]=session.visiblePileLimit,["dynamicPhysicsLimit"]=24}.ToString());
  BakeThemedPile.Run();
  FinishThemedScene.Run();
  Debug.Log("THEMED_SORTING_BUILD_COMPLETE "+items.Count+" variants, "+shelfIndex+" shelves");
 }
}




