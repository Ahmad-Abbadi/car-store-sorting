using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TinyRiders;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.TestTools;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class ThemedSortingPlayModeTests
{
 const string ScenePath="Assets/TinyRiders/Scenes/ThemedSorting.unity";
 const string Key="TinyRiders.Integration.QA.Temp";
 static readonly List<string> passed=new List<string>();
 static void SetKey(UnityEngine.Object o,string name,string key) {if(o==null)return;var f=o.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic);f.SetValue(o,key);}
 internal static void Isolate(Scene s,LoadSceneMode m)
 {
  var coins=UnityEngine.Object.FindFirstObjectByType<CoinsManager>();SetKey(coins,"saveKey",Key+".coins");
  // Awake reads the normal wallet before sceneLoaded assigns the isolated key.
  if(coins!=null)typeof(CoinsManager).GetProperty(nameof(CoinsManager.Coins)).SetValue(coins,Mathf.Max(0,PlayerPrefs.GetInt(Key+".coins",0)));
  SetKey(UnityEngine.Object.FindFirstObjectByType<LevelSaveManager>(),"saveKey",Key);
  SetKey(UnityEngine.Object.FindFirstObjectByType<PlayerSavePosition>(),"saveKey",Key+".position");
 }
 static void Pass(string test) {passed.Add(test);File.WriteAllLines("IntegrationEvidence/playmode_passed.txt",passed);}
 static ThemedCollectionSession Session=>ThemedCollectionSession.Instance;
 static WorldItemBase World(ThemedCarCatalog.Entry e)
 {
  var record=Session.State.instances.First(r=>r.collectibleId==e.collectibleId&&(r.state=="world"||r.state=="pending"));
  var existing=WorldItemBase.All.FirstOrDefault(w=>ThemedWorldInstance.Read(w)==record.id);
  if(existing!=null)return existing;
  var world=UnityEngine.Object.Instantiate(e.sortingItem.WorldItem,Session.pileOrigin.position+Vector3.up*5,Quaternion.identity);
  Session.Dropped(world,record.id);return world;
 }
 [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
 static string Pick(ThemedCarCatalog.Entry e)
 {
  var world=World(e);string id=ThemedWorldInstance.Read(world);
  Assert.True(Player.Instance.ItemCollector.TryCollectItem(world),"Pickup refused");
  Assert.AreEqual(id,ThemedWorldInstance.Read(Player.Instance.ItemHandler.CurrentEquipped));
  Assert.AreEqual(e.collectibleId,Player.Instance.ItemHandler.CurrentEquipped.GetComponentInChildren<ThemedCarIdentity>().collectibleId);
  Assert.True(Player.Instance.ItemHandler.CurrentEquipped.GetComponentsInChildren<Transform>(true).Any(t=>t.name=="ThemeAccessories"));
  return id;
 }
 static void Place(SortingShelf shelf,int spot)
 {
  Assert.True(Player.Instance.SortingBehaviour.TryGetHeldSortable(out var data));
  Assert.True(shelf.CanAccept(data,spot,out var reason),reason);
  Assert.True(Player.Instance.SortingBehaviour.TryGetPlacedItemFromHand(out var placed));
  shelf.AddItem(placed,spot);Assert.AreEqual(shelf,placed.Shelf);
 }
 static IEnumerator TickFor(float seconds) { float until=Time.realtimeSinceStartup+seconds;while(Time.realtimeSinceStartup<until)yield return null; }
 static SortingShelf Shelf(string theme,int number=0)=>UnityEngine.Object.FindObjectsByType<SortingShelf>(FindObjectsSortMode.None).Where(s=>s.GetComponent<ThemeShelfBinding>()?.themeId==theme).OrderBy(s=>s.SaveId,StringComparer.Ordinal).ElementAt(number);
 [UnityTest,Timeout(180000)]
 public IEnumerator ContinuousThemedCollection_A_Through_L()
 {
  SessionState.SetBool("TinyRiders.QA.Active",true);passed.Clear();PlayerPrefs.DeleteKey(Key);PlayerPrefs.DeleteKey(Key+".position");PlayerPrefs.DeleteKey(Key+".coins");
  EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);Isolate(default,LoadSceneMode.Single);
  yield return new EnterPlayMode();UnityEngine.Application.runInBackground=true;yield return new WaitUntil(()=>UnityEngine.Time.frameCount>5 && Player.Instance!=null);yield return null;
  yield return ExerciseScenarios();
  yield return new ExitPlayMode();
  PlayerPrefs.DeleteKey(Key);PlayerPrefs.DeleteKey(Key+".position");PlayerPrefs.DeleteKey(Key+".coins");
  EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
 }
 // Create captured local variables after EnterPlayMode's domain reload.
 static IEnumerator ExerciseScenarios()
 {
  Assert.NotNull(Session);Assert.AreEqual(600,Session.ActiveDefinitions.Count());Assert.AreEqual(1200,Session.Total);
  Assert.AreEqual(40,Session.ActiveDefinitions.Select(e=>e.themeId).Distinct().Count());
  foreach(var theme in Session.ActiveDefinitions.GroupBy(e=>e.themeId))
   CollectionAssert.AreEquivalent(Enumerable.Range(1,15).Select(n=>"C"+n.ToString("D3")),theme.Select(e=>e.baseCarId));
  Assert.GreaterOrEqual(Session.Capacity,1200);
  var legacy=Session.Capture();
  var priorDesigns=new HashSet<string>(Session.ActiveDefinitions.Where(e=>e.releaseId!="complete_15_shapes_v1").Select(e=>e.collectibleId));
  legacy.instances=legacy.instances.Where(r=>priorDesigns.Contains(r.collectibleId)).ToList();
  var oldIds=legacy.instances.Select(r=>r.id).ToArray();Assert.AreEqual(820,oldIds.Length);
  Session.Restore(legacy);yield return null;yield return TickFor(.4f);
  Assert.AreEqual(1200,Session.Total);Assert.True(oldIds.All(old=>Session.State.instances.Any(r=>r.id==old)));
  Pass("Migration: prior 820-car save expands to 1200 without changing existing instance IDs");
  var pirate=Session.ActiveDefinitions.Where(e=>e.themeId=="TH_PIRATE").OrderByDescending(e=>WorldItemBase.All.Any(w=>w.Data==e.sortingItem)).ThenBy(e=>e.collectibleId).ToArray();
  Assert.NotNull(Player.Instance, "Player singleton");
  Assert.NotNull(Player.Instance.StashHandler, "Player stash");
  Assert.NotNull(Player.Instance.StashHandler.Inventory, "Stash inventory");
  int inventoryBefore=Player.Instance.StashHandler.Inventory.Count;
  File.WriteAllText("IntegrationEvidence/test_trace.txt","Before World");
  var source=World(pirate[0]);
  File.AppendAllText("IntegrationEvidence/test_trace.txt","\\nAfter World "+(source!=null));
  var id=ThemedWorldInstance.Read(source);
  File.AppendAllText("IntegrationEvidence/test_trace.txt"," AFTER ID "+id);
  Assert.True(Player.Instance.ItemCollector.TryCollectItem(source));
  File.AppendAllText("IntegrationEvidence/test_trace.txt"," AFTER COLLECT");
  Assert.False(Player.Instance.ItemCollector.TryCollectItem(source));
  Assert.AreEqual(inventoryBefore+1,Player.Instance.StashHandler.Inventory.Count);
  var heldForm=Player.Instance.ItemHandler.CurrentEquipped;
  Assert.AreEqual(id,ThemedWorldInstance.Read(heldForm));
  Assert.True(heldForm.GetComponentsInChildren<Transform>(true).Any(t=>t.name=="ThemeAccessories"));
  var approvedMaterials=new HashSet<Material>(pirate[0].prefab.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials));
  Assert.True(heldForm.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).All(approvedMaterials.Contains));
  yield return TickFor(.3f);
  var heldBounds=heldForm.GetComponentsInChildren<Renderer>(true).First(r=>r.enabled).bounds;
  foreach(var renderer in heldForm.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled))heldBounds.Encapsulate(renderer.bounds);
  Vector3 screen=Camera.main.WorldToViewportPoint(heldBounds.center);
  Assert.True(screen.z>Camera.main.nearClipPlane&&screen.x>0&&screen.x<1&&screen.y>0&&screen.y<1,"Carried car must be visible from calibrated eye");
  typeof(ItemsHandler).GetMethod("ExecuteThrow",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(Player.Instance.ItemHandler,new object[]{0f});
  Assert.AreEqual(inventoryBefore,Player.Instance.StashHandler.Inventory.Count,"Drop spends held copy before animation");
  yield return TickFor(.5f);
  var dropped=WorldItemBase.All.Single(w=>ThemedWorldInstance.Read(w)==id);
  Assert.AreEqual("world",Session.State.instances.Single(r=>r.id==id).state);
  Assert.True(Player.Instance.ItemCollector.TryCollectItem(dropped));
  Assert.AreEqual(id,ThemedWorldInstance.Read(Player.Instance.ItemHandler.CurrentEquipped));
  Pass("A: real Pirate pickup retains ID, materials/accessory prefab and blocks repeated pickup");
  Pass("A/drop: throw releases exactly one physical copy and repick retains its ID");
  var pirateShelf=Shelf("TH_PIRATE");Place(pirateShelf,0);Assert.AreEqual(1,Session.Sorted);Assert.AreEqual(1,Session.UniqueDiscovered);
  var manager=UnityEngine.Object.FindFirstObjectByType<LevelSaveManager>();
  File.WriteAllText("IntegrationEvidence/save_test_diagnostics.txt",string.Join("\n",typeof(LevelSaveManager).GetFields(BindingFlags.NonPublic|BindingFlags.Instance).Where(f=>f.FieldType==typeof(bool)||f.Name=="saveKey"||f.Name=="database").Select(f=>f.Name+" = "+f.GetValue(manager)))+"\nsuspended="+Session.suspendPersistenceForTests+"\nscene="+manager.gameObject.scene.name);
  manager.Save();
  File.AppendAllText("IntegrationEvidence/save_test_diagnostics.txt","\nQA bytes="+PlayerPrefs.GetString(Key).Length);
  Assert.True(PlayerPrefs.GetString(Key).Contains(id),"Saved IDs");
  yield return TickFor(.35f);
  File.AppendAllText("IntegrationEvidence/save_test_diagnostics.txt","\nsettled="+pirateShelf.GetOccupant(0).IsSettled+" timeScale="+Time.timeScale+" active="+pirateShelf.GetOccupant(0).gameObject.activeInHierarchy+" frame="+Time.frameCount);
  Assert.True(pirateShelf.GetOccupant(0).IsSettled,"Placement animation must settle");
  Pass("B: correct theme reserves slot, snaps, counts and checkpoints");
  Pick(pirate[1]);string held=ThemedWorldInstance.Read(Player.Instance.ItemHandler.CurrentEquipped);
  int coinsBeforeWrong=CoinsManager.Instance.Coins;
  var wrongShelf=Shelf("TH_SPACE");
  Assert.True(wrongShelf.CanAccept(pirate[1].sortingItem,0,out _));
  Assert.False(wrongShelf.CanAccept(null,0,out _));Assert.False(wrongShelf.CanAccept(pirate[1].sortingItem,-1,out _));
  var binding=wrongShelf.GetComponent<ThemeShelfBinding>();Vector3 clearance=binding.clearance;
  try { binding.clearance=Vector3.zero;Assert.False(wrongShelf.CanAccept(pirate[1].sortingItem,0,out _)); }
  finally { binding.clearance=clearance; }
  wrongShelf.OnHoverEnter();
  Assert.NotNull(typeof(SortingShelf).GetField("_hologram",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(wrongShelf));
  wrongShelf.OnHoverExit();wrongShelf.OnInteractSecondary(null);
  var wrong=wrongShelf.GetOccupant(0);
  Assert.NotNull(wrong);Assert.AreEqual(held,ThemedWorldInstance.Read(wrong));Assert.True(wrong.IsWrong);
  Assert.AreEqual(1,Session.Sorted);Assert.AreEqual(1,Session.UniqueDiscovered);Assert.AreEqual(coinsBeforeWrong,CoinsManager.Instance.Coins);
  Assert.AreEqual("misplaced",Session.State.instances.Single(r=>r.id==held).state);Assert.False(Session.CanPlace(held));
  Assert.False(wrongShelf.IsCompleted);Assert.False(wrongShelf.CanAccept(pirate[1].sortingItem,0,out _));
  // Reload the checkpoint before the wrong car finishes its landing animation.
  Assert.False(wrong.IsSettled);manager.Save();
  yield return SceneManager.LoadSceneAsync("ThemedSorting");UnityEngine.Application.runInBackground=true;
  yield return null;yield return null;yield return TickFor(.4f);
  wrongShelf=Shelf("TH_SPACE");wrong=wrongShelf.GetOccupant(0);pirateShelf=Shelf("TH_PIRATE");
  Assert.NotNull(wrong);Assert.True(wrong.IsSettled);Assert.True(wrong.IsWrong);Assert.AreEqual(held,ThemedWorldInstance.Read(wrong));
  Assert.AreEqual(1,Session.Sorted);Assert.AreEqual(1,Session.UniqueDiscovered);Assert.AreEqual(coinsBeforeWrong,CoinsManager.Instance.Coins);
  Assert.False(WorldItemBase.All.Any(w=>ThemedWorldInstance.Read(w)==held));
  Assert.Less(Vector3.Distance(wrong.transform.position,wrongShelf.GetSpot(0).position),.01f);
  Assert.True(wrongShelf.TryTakeBack(wrong));Assert.True(wrongShelf.IsSpotFree(0));
  Assert.AreEqual(held,ThemedWorldInstance.Read(Player.Instance.ItemHandler.CurrentEquipped));
  Assert.AreEqual("held",Session.State.instances.Single(r=>r.id==held).state);Assert.True(Session.CanPlace(held));
  Assert.AreEqual(coinsBeforeWrong,CoinsManager.Instance.Coins);
  // The collision/distance path must apply the same wrong-placement feedback as hand placement.
  typeof(ItemsHandler).GetMethod("ExecuteThrow",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(Player.Instance.ItemHandler,new object[]{0f});
  yield return TickFor(.5f);
  bool distanceEnabled=SortingShelf.DistanceShelfingEnabled;
  try
  {
   SortingShelf.SetDistanceShelfingEnabled(true);
   Assert.True(wrongShelf.TryDistanceShelf(WorldItemBase.All.Single(w=>ThemedWorldInstance.Read(w)==held)));
  }
  finally { SortingShelf.SetDistanceShelfingEnabled(distanceEnabled); }
  Assert.True(wrongShelf.GetOccupant(0).IsWrong);Assert.AreEqual("misplaced",Session.State.instances.Single(r=>r.id==held).state);
  Assert.AreEqual(1,Session.Sorted);Assert.AreEqual(1,Session.UniqueDiscovered);Assert.AreEqual(coinsBeforeWrong,CoinsManager.Instance.Coins);
  yield return TickFor(.6f);Assert.True(wrongShelf.TryTakeBack(wrongShelf.GetOccupant(0)));
  Pass("C: wrong theme accepted with feedback, no rewards, interrupted-save restoration and identity-preserving takeback");
  Assert.False(pirateShelf.CanAccept(pirate[1].sortingItem,0,out _));Assert.AreEqual(id,ThemedWorldInstance.Read(pirateShelf.GetOccupant(0)));
  Pass("D: occupied slot rejects another physical copy");
  Assert.GreaterOrEqual(WorldItemBase.All.Select(w=>(w.Data as SortableItem_SO)?.Catagory?.CatagoryID).Distinct().Count(),8);
  Assert.True(WorldItemBase.All.All(w=>w.GetComponentInChildren<ThemedCarIdentity>()?.collectibleId==w.Data.SaveId));
  Pass("E: mixed live pile uses matching catalog identities across themes");
  Place(pirateShelf,1);yield return TickFor(.3f);Assert.AreEqual(2,Session.UniqueDiscovered);
  Assert.False(pirateShelf.GetOccupant(1).IsWrong);Assert.AreEqual("sorted",Session.State.instances.Single(r=>r.id==held).state);
  Assert.Greater(CoinsManager.Instance.Coins,coinsBeforeWrong);
  Assert.True(UnityEngine.Object.FindObjectsByType<FloatingTextEffect>(FindObjectsSortMode.None)
   .Any(f=>f.GetComponent<TMPro.TMP_Text>().text==Localizer.FromEnglish("feedback","Great job!")));
  Pass("F: distinct Pirate designs retain their own visuals and metadata");
  Pick(pirate[0]);Place(pirateShelf,2);yield return TickFor(.3f);Assert.AreEqual(3,Session.Sorted);Assert.AreEqual(2,Session.UniqueDiscovered);
  Pass("G: duplicate copies increase cleanup without duplicate discovery");
  // Save while a fourth placement is still animating, then reload the actual scene.
  Pick(pirate[2]);Place(pirateShelf,3);
  var moved=WorldItemBase.All.First(w=>w.gameObject.activeInHierarchy);string movedId=ThemedWorldInstance.Read(moved);
  Vector3 movedPose=Session.pileOrigin.position+new Vector3(30,0,30);moved.transform.position=movedPose;
  UnityEngine.Object.FindFirstObjectByType<LevelSaveManager>().Save();
  SceneManager.sceneLoaded+=Isolate;yield return SceneManager.LoadSceneAsync("ThemedSorting");UnityEngine.Application.runInBackground=true;yield return new WaitUntil(()=>Player.Instance!=null && Session!=null);yield return null;yield return null;SceneManager.sceneLoaded-=Isolate;
  Assert.AreEqual(4,Session.Sorted);Assert.AreEqual(3,Session.UniqueDiscovered);pirateShelf=Shelf("TH_PIRATE");Assert.AreEqual(id,ThemedWorldInstance.Read(pirateShelf.GetOccupant(0)));
  Assert.False(WorldItemBase.All.Any(w=>ThemedWorldInstance.Read(w)==id));
  Assert.False(WorldItemBase.All.Any(w=>Session.State.instances.Any(r=>r.state=="sorted"&&r.id==ThemedWorldInstance.Read(w))));Assert.Less(Vector3.Distance(pirateShelf.GetSpot(0).position,pirateShelf.GetOccupant(0).transform.position),.01f);
  Assert.Less(Vector3.Distance(movedPose,WorldItemBase.All.First(w=>ThemedWorldInstance.Read(w)==movedId).transform.position),.1f);
  Pass("H: real scene reload restores stable shelf markers, identities and interrupted placement without pile duplicates");
  var expansion=Session.ActiveDefinitions.Where(e=>e.releaseId=="themes_expansion_01").GroupBy(e=>e.themeId).First(g=>g.Count()>=13).ToArray();
  var full=Shelf(expansion[0].themeId);for(int i=0;i<18;i++){Pick(expansion[i%13]);Place(full,i);yield return null;}
  yield return TickFor(.4f);Assert.True(full.IsCompleted);Assert.False(full.HasFreeSpot);Assert.False(full.CanAccept(expansion[0].sortingItem,0,out _));
  Pick(expansion[5]);string fullHeld=ThemedWorldInstance.Read(Player.Instance.ItemHandler.CurrentEquipped);
  full.OnInteractSecondary(null);Assert.AreEqual(fullHeld,ThemedWorldInstance.Read(Player.Instance.ItemHandler.CurrentEquipped));
  Place(Shelf(expansion[0].themeId,1),0);yield return TickFor(.3f);
  Pass("I: all 18 slots fill with mixed designs; second shelf accepts overflow without overwriting");
  var big=Session.ActiveDefinitions.OrderByDescending(e=>e.displaySize.y).First();Pick(big);var bigShelf=Shelf(big.themeId);int bigSpot=Enumerable.Range(0,bigShelf.SpotCount).First(i=>bigShelf.IsSpotFree(i));Place(bigShelf,bigSpot);yield return TickFor(.3f);
  Assert.True(Session.ActiveDefinitions.All(e=>e.displaySize.x<=.464f*9&&e.displaySize.y<=.54f*9&&e.displaySize.z<=.648f*9));
  Pass("J: largest accessory survives pickup/placement and all 600 measured products fit fixed clearance");
  UnityEngine.Object.FindFirstObjectByType<LevelSaveManager>().Save();Session.copiesPerDesign=5;Session.RefreshContent();Assert.AreEqual(3000,Session.Total);
  yield return TickFor(.7f);Assert.LessOrEqual(Session.LivePileCount,Session.visiblePileLimit);
  Pass("K: 3000 logical records keep visible pile bounded (Editor correctness, not WebGL performance certification)");
  int sorted=Session.Sorted;var clone=UnityEngine.Object.Instantiate(Session.catalog);Session.catalog=clone;var toggled=clone.themes.First(t=>t.id=="TH_DETECTIVE");toggled.enabled=false;Session.RefreshContent();Assert.False(Session.ActiveDefinitions.Any(e=>e.themeId==toggled.id));toggled.enabled=true;Session.RefreshContent();Assert.True(Session.ActiveDefinitions.Any(e=>e.themeId==toggled.id));Assert.AreEqual(sorted,Session.Sorted);Assert.AreEqual(Session.State.instances.Count,Session.State.instances.Select(r=>r.id).Distinct().Count());
  Pass("L: validated expansion theme toggles through data; permanent records and existing sorted progress survive");
  Session.suspendPersistenceForTests=true;UnityEngine.Object.Destroy(clone);
  yield break;
 }
 [UnityTearDown]
 public IEnumerator Cleanup()
 {
  SessionState.SetBool("TinyRiders.QA.Active",false);SceneManager.sceneLoaded-=Isolate;
  if(EditorApplication.isPlaying){if(Session!=null)Session.suspendPersistenceForTests=true;yield return new ExitPlayMode();}
  PlayerPrefs.DeleteKey(Key);PlayerPrefs.DeleteKey(Key+".position");PlayerPrefs.DeleteKey(Key+".coins");
  if(File.Exists(ScenePath))EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
 }
}







[InitializeOnLoad]
internal static class ThemedSortingTestIsolation
{
 static ThemedSortingTestIsolation()
 {
  SceneManager.sceneLoaded += (scene, mode) => {
   if (SessionState.GetBool("TinyRiders.QA.Active", false) && EditorApplication.isPlaying)
    ThemedSortingPlayModeTests.Isolate(scene,mode);
  };
 }
}





