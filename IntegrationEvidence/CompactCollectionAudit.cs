using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using TinyRiders;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Newtonsoft.Json.Linq;
public static class CompactCollectionAudit
{
 public static string Run()
 {
  var errors=new List<string>();
  var catalog=AssetDatabase.LoadAssetAtPath<ThemedCarCatalog>(CompactCollectionBuilder.CatalogPath);
  var original=AssetDatabase.LoadAssetAtPath<ThemedCarCatalog>("Assets/TinyRiders/ThemedCollection/Data/Themes/ThemedCarCatalog.asset");
  var session=UnityEngine.Object.FindFirstObjectByType<ThemedCollectionSession>();
  var shelves=UnityEngine.Object.FindObjectsByType<SortingShelf>(FindObjectsSortMode.None);
  if(SceneManager.GetActiveScene().path!=CompactCollectionBuilder.ScenePath||session.catalog!=catalog)errors.Add("Wrong gameplay scene/catalog");
  if(catalog.entries.Length!=600||catalog.themes.Length!=40||catalog.entries.Select(e=>e.collectibleId).Distinct().Count()!=600)errors.Add("Catalog incomplete");
  int forms=0;
  foreach(var entry in catalog.entries)
  {
   var source=original.entries.Single(e=>e.collectibleId==entry.collectibleId);
   if(entry.prefab!=source.prefab||entry.preview!=source.preview||entry.sortingItem==source.sortingItem)errors.Add("Artwork source/isolation "+entry.collectibleId);
   if(entry.sortingItem.SaveId!=entry.collectibleId)errors.Add("Invalid save ID");
   var wrappers=new[]{entry.sortingItem.WorldItem.gameObject,entry.sortingItem.HandItem.gameObject,entry.sortingItem.PlacedOnShelfPrefab.gameObject};
   foreach(var wrapper in wrappers)
   {
    forms++;var id=wrapper.GetComponentInChildren<ThemedCarIdentity>(true);
    if(id==null||id.collectibleId!=entry.collectibleId||id.themeId!=entry.themeId||id.baseCarId!=entry.baseCarId)errors.Add("Wrapper identity "+entry.collectibleId);
    if(!wrapper.GetComponentsInChildren<Transform>(true).Any(t=>t.name=="ThemeAccessories"))errors.Add("Missing accessories "+entry.collectibleId);
    if(!AssetDatabase.GetAssetPath(wrapper).StartsWith(CompactCollectionBuilder.Root))errors.Add("Shared wrapper modified");
   }
   foreach(var wrapper in wrappers)
   {
    bool found=false;Bounds b=default;
    foreach(var r in wrapper.GetComponentsInChildren<Renderer>().Where(r=>r.enabled))
    {
     var mesh=r.GetComponent<MeshFilter>()?.sharedMesh;if(mesh==null)continue;var local=mesh.bounds;
     foreach(float x in new[]{local.min.x,local.max.x})foreach(float y in new[]{local.min.y,local.max.y})foreach(float z in new[]{local.min.z,local.max.z})
     {var p=r.transform.TransformPoint(new Vector3(x,y,z));if(!found){b=new Bounds(p,Vector3.zero);found=true;}else b.Encapsulate(p);}
    }
    if(wrapper==wrappers[0]&&Mathf.Max(b.size.x,b.size.z)>.581f)errors.Add("World scale "+entry.collectibleId);
    if(wrapper==wrappers[1]&&Mathf.Max(b.size.x,b.size.z)>.351f)errors.Add("Held scale "+entry.collectibleId);
    if(wrapper==wrappers[2]&&Vector3.Distance(b.size,entry.displaySize)>.002f)errors.Add("Serialized shelf scale "+entry.collectibleId);
   }
   foreach(var shelf in shelves.Where(s=>s.GetComponent<ThemeShelfBinding>()?.themeId==entry.themeId))
   {var c=shelf.GetComponent<ThemeShelfBinding>().clearance;if(entry.displaySize.x>c.x||entry.displaySize.y>c.y||entry.displaySize.z>c.z)errors.Add("Shelf fit "+entry.collectibleId);}
  }
  if(shelves.Length!=80||shelves.Sum(s=>s.SpotCount)!=1440||shelves.Select(s=>s.SaveId).Distinct().Count()!=80)errors.Add("Shelf capacity/IDs");
  foreach(var theme in catalog.themes)
  {
   var entries=catalog.entries.Where(e=>e.themeId==theme.id).ToArray();
   if(entries.Length!=15||entries.Select(e=>e.baseCarId).Distinct().Count()!=15)errors.Add("Theme lineup "+theme.id);
   var group=shelves.Where(s=>s.GetComponent<ThemeShelfBinding>()?.themeId==theme.id).ToArray();
   if(group.Length!=2||group.Sum(s=>s.SpotCount)!=36)errors.Add("Theme capacity "+theme.id);
   if(group.Length==2&&Vector3.Distance(group[0].transform.position,group[1].transform.position)>2)errors.Add("Separated theme shelves "+theme.id);
  }
  foreach(var shelf in shelves)
  {
   if(shelf.AcceptedItem!=null)errors.Add("Legacy exact variant restriction");
   if(shelf.GetComponentInChildren<SortingShelfPlacementSurface>()==null)errors.Add("Missing aiming surface");
   for(int i=0;i<shelf.SpotCount;i++)
   {var p=shelf.GetSpot(i).position;if(p.x<480||p.x>520||p.z<-25||p.z>25||p.y<0||p.y>2)errors.Add("Slot outside room/reach "+shelf.SaveId);}
  }
  var first=ThemedPileSequence.Designs(catalog.entries,session.seed).Take(600).ToArray();
  var sequence=ThemedPileSequence.InstanceIds(catalog.entries,session.copiesPerDesign,session.seed).ToArray();
  if(sequence.Length!=1200||sequence.Distinct().Count()!=1200||first.Select(e=>e.themeId).Distinct().Count()!=40||first.Select(e=>e.baseCarId).Distinct().Count()!=15)errors.Add("Pile diversity/copy sequence");
  if(!session.boundedPile||!session.completeStoreWhenSorted||session.visiblePileLimit!=1200||!session.spawnEntireCollectionAtStart)errors.Add("Wrong session configuration");
  if(UnityEngine.Object.FindObjectsByType<CarVisualCulling>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length!=0||UnityEngine.Object.FindObjectsByType<ThemedVisibility>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length!=0)errors.Add("Distance hiding remains");
  var player=UnityEngine.Object.FindFirstObjectByType<Player>();
  if(Vector3.Distance(player.transform.position,new Vector3(500,1.15f,-21))>.01f)errors.Add("Player start moved");
  var save=UnityEngine.Object.FindFirstObjectByType<LevelSaveManager>();
  if(new SerializedObject(save).FindProperty("saveKey").stringValue!=CompactCollectionBuilder.SaveKey)errors.Add("Save not isolated");
  var floor=SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).First(t=>t.name=="Store Floor");
  if(Vector3.Distance(floor.lossyScale,new Vector3(40,.7f,50))>.01f)errors.Add("Room dimensions changed");
  var report=new JObject{["passed"]=errors.Count==0,["designs"]=600,["themes"]=40,["wrapperForms"]=forms,["copies"]=sequence.Length,["startingCars"]=sequence.Length,["allCarsAtStart"]=session.spawnEntireCollectionAtStart,["shelves"]=shelves.Length,["slots"]=shelves.Sum(s=>s.SpotCount),["initialPileThemes"]=first.Select(e=>e.themeId).Distinct().Count(),["initialPileShapes"]=first.Select(e=>e.baseCarId).Distinct().Count(),["roomAndPlayerPreserved"]=true,["errors"]=new JArray(errors)};
  File.WriteAllText("IntegrationEvidence/compact_collection_audit.json",report.ToString());
  if(errors.Count!=0)throw new Exception(errors.Count+" audit errors: "+string.Join("; ",errors.Take(8)));return report.ToString();
 }
}

