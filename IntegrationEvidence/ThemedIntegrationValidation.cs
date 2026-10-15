using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using TinyRiders;
using Newtonsoft.Json.Linq;
public static class ThemedIntegrationValidation {
 public static void Run(){
  var catalog=AssetDatabase.LoadAssetAtPath<ThemedCarCatalog>("Assets/TinyRiders/ThemedCollection/Data/Themes/ThemedCarCatalog.asset");var errors=new List<string>();int meshes=0;float maxHeldScreenFraction=0;
  var view=UnityEngine.Object.FindFirstObjectByType<Player>().LookTransform.GetComponent<Unity.Cinemachine.CinemachineCamera>();
  float tanHalf=Mathf.Tan((view!=null?view.Lens.FieldOfView:60)*Mathf.Deg2Rad*.5f);
  var player=UnityEngine.Object.FindFirstObjectByType<Player>();var original=PrefabUtility.GetCorrespondingObjectFromSource(player.Character);
  if(original==null||Mathf.Abs(player.Character.MaxStableMoveSpeed-original.MaxStableMoveSpeed*9)>.01f)errors.Add("Fixed-world movement speed was not persisted");
  if(catalog.entries.Select(e=>e.collectibleId).Distinct().Count()!=catalog.entries.Length)errors.Add("Duplicate IDs");
  foreach(var e in catalog.entries.Where(e=>e.enabled)){
   if(e.prefab==null||e.sortingItem==null||e.preview==null){errors.Add("Missing content "+e.collectibleId);continue;}
   if(e.sortingItem.SaveId!=e.collectibleId||e.sortingItem.Catagory.CatagoryID!=e.themeId)errors.Add("Metadata bridge mismatch "+e.collectibleId);
   if(!e.sortingItem.CameraFixed)errors.Add("Carry socket does not follow the view "+e.collectibleId);
   for(int x=-1;x<=1;x+=2)for(int y=0;y<=1;y++)for(int z=-1;z<=1;z+=2){var v=e.sortingItem.HeldPosition+Quaternion.Euler(e.sortingItem.HeldRotation)*new Vector3(x*e.displaySize.x*.5f,y*e.displaySize.y,z*e.displaySize.z*.5f);float extent=Mathf.Max(Mathf.Abs(v.x/(v.z*tanHalf*16f/9)),Mathf.Abs(v.y/(v.z*tanHalf)));maxHeldScreenFraction=Mathf.Max(maxHeldScreenFraction,extent);if(v.z<=.15f||extent>.96f)errors.Add("Carried bounds outside normal 16:9 view "+e.collectibleId);}
   var pick=e.sortingItem.WorldItem.transform.Find("Accessory Pick Bounds");
   if(pick==null||pick.GetComponent<BoxCollider>()==null||!pick.GetComponent<BoxCollider>().isTrigger)errors.Add("Accessory pick target missing "+e.collectibleId);
   var sourceMeshes=new HashSet<Mesh>(e.prefab.GetComponentsInChildren<MeshFilter>(true).Select(f=>f.sharedMesh));
   var sourceMaterials=new HashSet<Material>(e.prefab.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials));
   foreach(var form in new[]{e.sortingItem.WorldItem.gameObject,e.sortingItem.HandItem.gameObject,e.sortingItem.PlacedOnShelfPrefab.gameObject}){
    var identity=form.GetComponentInChildren<ThemedCarIdentity>(true);if(identity==null||identity.collectibleId!=e.collectibleId||identity.themeId!=e.themeId)errors.Add("Identity mismatch "+form.name);
    if(!form.GetComponentsInChildren<Transform>(true).Any(t=>t.name=="ThemeAccessories"))errors.Add("Accessory missing "+form.name);
    if(form.GetComponentsInChildren<MeshFilter>(true).Any(f=>!sourceMeshes.Contains(f.sharedMesh)))errors.Add("Replaced source mesh "+form.name);
    if(form.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).Any(m=>!sourceMaterials.Contains(m)))errors.Add("Replaced material "+form.name);
    if(form.transform.localScale!=Vector3.one||form.transform.GetChild(0).localScale!=e.prefab.transform.localScale)errors.Add("Art scale changed "+form.name);
    meshes+=form.GetComponentsInChildren<MeshFilter>(true).Length;
   }
  }
  var shelves=UnityEngine.Object.FindObjectsByType<SortingShelf>(FindObjectsSortMode.None);if(shelves.Length!=60)errors.Add("Shelf count mismatch");if(shelves.Select(s=>s.SaveId).Distinct().Count()!=shelves.Length)errors.Add("Duplicate shelf IDs");
  foreach(var s in shelves){if(s.SpotCount!=18)errors.Add("Wrong slot count "+s.SaveId);for(int i=0;i<s.SpotCount;i++){string expected="Slot_R"+(i/6+1).ToString("D2")+"_C"+(i%6+1).ToString("D2");if(s.GetSpot(i)==null||s.GetSpot(i).name!=expected)errors.Add("Slot marker mismatch "+s.SaveId+"/"+i);}}
  foreach(var s in shelves){var header=s.GetComponentInChildren<ThemeShelfHeader>();if(header==null||header.themeId!=s.GetComponent<ThemeShelfBinding>().themeId)errors.Add("Header binding missing "+s.SaveId);if(s.transform.Find("Batched Shelf Art")==null)errors.Add("Shelf batching missing "+s.SaveId);}
  File.WriteAllText("IntegrationEvidence/integration_validation.json",new JObject{["passed"]=errors.Count==0,["activeThemes"]=catalog.themes.Count(t=>t.enabled),["activeVariants"]=catalog.entries.Count(e=>e.enabled),["wrapperForms"]=catalog.entries.Count(e=>e.enabled)*3,["sourceMeshReferencesChecked"]=meshes,["maxHeldScreenFraction"]=maxHeldScreenFraction,["shelves"]=shelves.Length,["capacity"]=shelves.Sum(s=>s.SpotCount),["errors"]=new JArray(errors)}.ToString());if(errors.Count>0)throw new Exception(string.Join("\n",errors));
 }
}
