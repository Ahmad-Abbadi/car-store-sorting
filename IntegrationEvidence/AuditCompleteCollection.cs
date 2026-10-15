using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using TinyRiders;
using Newtonsoft.Json.Linq;
public static class AuditCompleteCollection
{
 public static string Run()
 {
  var errors=new List<string>();
  var catalog=AssetDatabase.LoadAssetAtPath<ThemedCarCatalog>("Assets/TinyRiders/ThemedCollection/Data/Themes/ThemedCarCatalog.asset");
  if(catalog.entries.Length!=600)errors.Add("Expected 600 designs");
  if(catalog.themes.Length!=40||catalog.themes.Any(t=>!t.enabled))errors.Add("Expected 40 active themes");
  foreach(var theme in catalog.themes)
  {
   var entries=catalog.entries.Where(e=>e.themeId==theme.id).ToArray();
   if(entries.Length!=15||entries.Select(e=>e.baseCarId).Distinct().Count()!=15)errors.Add("Incomplete lineup "+theme.id);
  }
  int forms=0;
  foreach(var e in catalog.entries)
  {
   if(!e.enabled||e.prefab==null||e.preview==null||e.sortingItem==null){errors.Add("Unavailable "+e.collectibleId);continue;}
   var id=e.prefab.GetComponent<ThemedCarIdentity>();
   if(id==null||id.collectibleId!=e.collectibleId||id.baseCarId!=e.baseCarId||id.themeId!=e.themeId)errors.Add("Art identity "+e.collectibleId);
   if(e.sortingItem.SaveId!=e.collectibleId)errors.Add("Save ID "+e.collectibleId);
   foreach(var form in new[]{e.sortingItem.WorldItem.gameObject,e.sortingItem.HandItem.gameObject,e.sortingItem.PlacedOnShelfPrefab.gameObject})
   {
    forms++;var fid=form.GetComponentInChildren<ThemedCarIdentity>();
    if(fid==null||fid.collectibleId!=e.collectibleId||fid.baseCarId!=e.baseCarId||fid.themeId!=e.themeId)errors.Add("Wrapper identity "+e.collectibleId);
    if(!form.GetComponentsInChildren<Transform>(true).Any(t=>t.name=="ThemeAccessories"))errors.Add("Accessory "+e.collectibleId);
   }
   if(e.displaySize.x>4.176f||e.displaySize.y>4.86f||e.displaySize.z>5.832f)errors.Add("Fit "+e.collectibleId);
   if(e.releaseId=="complete_15_shapes_v1")foreach(var material in e.prefab.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials))
   {
    var path=AssetDatabase.GetAssetPath(material);
    if((path.StartsWith("Assets/TinyRiders/ThemedCollection/Materials/Themes/")||path.StartsWith("Assets/TinyRiders/Art/ThemeAccessories/Materials/"))&&!path.Contains("/"+e.themeId+"/"))errors.Add("Wrong theme material "+e.collectibleId+" "+path);
   }
  }
  var shelves=UnityEngine.Object.FindObjectsByType<SortingShelf>(FindObjectsSortMode.None).Where(s=>s.GetComponent<ThemeShelfBinding>()!=null).ToArray();
  if(shelves.Length!=80||shelves.Select(s=>s.SaveId).Distinct().Count()!=80)errors.Add("Shelf count/IDs");
  foreach(var theme in catalog.themes)
  {
   var units=shelves.Where(s=>s.GetComponent<ThemeShelfBinding>().themeId==theme.id).ToArray();
   if(units.Sum(s=>s.SpotCount)<30)errors.Add("Insufficient capacity "+theme.id);
   foreach(var e in catalog.entries.Where(e=>e.themeId==theme.id))foreach(var unit in units)
   {
    var binding=unit.GetComponent<ThemeShelfBinding>();
    if(e.displaySize.x>binding.clearance.x||e.displaySize.y>binding.clearance.y||e.displaySize.z>binding.clearance.z)errors.Add("Shelf clearance "+e.collectibleId);
   }
  }
  var session=UnityEngine.Object.FindFirstObjectByType<ThemedCollectionSession>();
  var ids=ThemedPileSequence.InstanceIds(catalog.entries,session.copiesPerDesign,session.seed).ToArray();
  var first=ThemedPileSequence.Designs(catalog.entries,session.seed).Take(108).ToArray();
  if(ids.Length!=1200||ids.Distinct().Count()!=1200)errors.Add("Physical copy counts");
  if(first.Select(e=>e.themeId).Distinct().Count()!=40||first.Select(e=>e.baseCarId).Distinct().Count()!=15||first.Select(e=>e.collectibleId).Distinct().Count()!=108)errors.Add("Initial pile diversity");
  var report=new JObject{["passed"]=errors.Count==0,["themes"]=catalog.themes.Length,["designs"]=catalog.entries.Length,["shapesPerTheme"]=15,["copiesPerDesign"]=session.copiesPerDesign,["physicalCars"]=ids.Length,["wrapperForms"]=forms,["shelves"]=shelves.Length,["slots"]=shelves.Sum(s=>s.SpotCount),["visiblePileLimit"]=session.visiblePileLimit,["initialPileThemes"]=first.Select(e=>e.themeId).Distinct().Count(),["initialPileShapes"]=first.Select(e=>e.baseCarId).Distinct().Count(),["errors"]=new JArray(errors)};
  File.WriteAllText("IntegrationEvidence/complete_collection_validation.json",report.ToString());
  if(errors.Count>0)throw new Exception(report.ToString());
  return report.ToString();
 }
}
