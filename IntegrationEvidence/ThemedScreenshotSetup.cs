using System;
using System.Linq;
using System.Reflection;
using TinyRiders;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class ThemedScreenshotSetup
{
 public static void Prepare(){
  SessionState.SetBool("TinyRiders.QA.Active",true);
  foreach(var suffix in new[]{"",".position",".coins"})PlayerPrefs.DeleteKey("TinyRiders.Integration.QA.Temp"+suffix);
  EditorSceneManager.OpenScene("Assets/TinyRiders/Scenes/ThemedSorting.unity");
 }
 public static void Shelf(){
  var session=ThemedCollectionSession.Instance;session.suspendPersistenceForTests=true;
  var shelf=UnityEngine.Object.FindObjectsByType<SortingShelf>(FindObjectsSortMode.None).First(s=>s.GetComponent<ThemeShelfBinding>()?.themeId=="TH_PIRATE");
  int slot=0;
  foreach(var entry in session.ActiveDefinitions.Where(e=>e.themeId=="TH_PIRATE").Take(6)){
   var record=session.State.instances.First(r=>r.collectibleId==entry.collectibleId&&(r.state=="world"||r.state=="pending"));
   var world=WorldItemBase.All.FirstOrDefault(w=>ThemedWorldInstance.Read(w)==record.id);
   if(world==null){world=UnityEngine.Object.Instantiate(entry.sortingItem.WorldItem,session.pileOrigin.position+Vector3.up*5,Quaternion.identity);session.Dropped(world,record.id);}
   if(!Player.Instance.ItemCollector.TryCollectItem(world))throw new Exception("Capture pickup failed");
   if(!Player.Instance.SortingBehaviour.TryGetPlacedItemFromHand(out var placed))throw new Exception("Capture held form failed");
   shelf.AddItem(placed,slot++);
  }
  Player.Instance.Motor.SetPosition(shelf.transform.position+shelf.transform.forward*38+Vector3.up*.1f);
  Player.Instance.Look.SetLook(shelf.transform.eulerAngles.y+180,12);
 }
 public static void Cleanup(){
  SessionState.SetBool("TinyRiders.QA.Active",false);
  foreach(var suffix in new[]{"",".position",".coins"})PlayerPrefs.DeleteKey("TinyRiders.Integration.QA.Temp"+suffix);
 }
 public static void Hold(){
  var session=ThemedCollectionSession.Instance;session.suspendPersistenceForTests=true;
  var record=session.State.instances.First(r=>(r.state=="world"||r.state=="pending")&&session.ActiveDefinitions.Any(e=>e.collectibleId==r.collectibleId&&e.themeId=="TH_PIRATE"));
  var entry=session.ActiveDefinitions.First(e=>e.collectibleId==record.collectibleId);
  var world=WorldItemBase.All.FirstOrDefault(w=>ThemedWorldInstance.Read(w)==record.id);
  if(world==null){world=UnityEngine.Object.Instantiate(entry.sortingItem.WorldItem,session.pileOrigin.position+Vector3.up*5,Quaternion.identity);session.Dropped(world,record.id);}
  if(!Player.Instance.ItemCollector.TryCollectItem(world))throw new Exception("Capture held pickup failed");
 }
}
