using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TinyRiders;
using TinyRiders.Editor;
public class PublishExpansion {
 public static void Run(){ThemesExpansionBuilder.Publish();string root="Assets/TinyRiders/ThemedCollection/Data/Themes/";var files=new[]{"variant_catalog.json","ThemedCarCatalog.asset","theme_recipes.json","id_registry.json"};var before=files.ToDictionary(f=>f,f=>File.ReadAllBytes(root+f));ThemesExpansionBuilder.Publish();var errors=new List<string>();foreach(var f in files)if(!before[f].SequenceEqual(File.ReadAllBytes(root+f)))errors.Add("Publication not idempotent "+f);var catalog=AssetDatabase.LoadAssetAtPath<ThemedCarCatalog>(root+"ThemedCarCatalog.asset");if(catalog.entries.Length!=410)errors.Add("Wrong catalog count");if(catalog.entries.Select(e=>e.collectibleId).Distinct().Count()!=410)errors.Add("Duplicate IDs");foreach(var e in catalog.entries){if(e.prefab==null||e.preview==null)errors.Add("Missing reference "+e.collectibleId);else if(e.prefab.GetComponent<ThemedCarIdentity>().collectibleId!=e.collectibleId)errors.Add("Identity mismatch "+e.collectibleId);}var products=JArray.Parse(File.ReadAllText("TinyRidersProduction/ThemesExpansion01/products.json"));var portable=(JArray)JObject.Parse(File.ReadAllText(root+"variant_catalog.json"))["entries"];foreach(var p in products){string id=(string)p["collectibleId"];var runtimeEntry=catalog.entries.Single(e=>e.collectibleId==id);var data=portable.Single(e=>(string)e["collectibleId"]==id);if(runtimeEntry.displayName!=(string)p["displayName"]||(string)data["displayName"]!=(string)p["displayName"])errors.Add("Display name mismatch "+id);if(AssetDatabase.GetAssetPath(runtimeEntry.prefab)!=(string)p["prefabPath"]||AssetDatabase.GetAssetPath(runtimeEntry.preview)!=(string)p["thumbnailPath"])errors.Add("Product reference mismatch "+id);}var report=new{entryCount=catalog.entries.Length,themeCount=catalog.entries.Select(e=>e.themeId).Distinct().Count(),allPrefabAndPreviewReferencesResolved=errors.Count==0,publicationRepeatedWithoutChanges=errors.Count==0,errors,passed=errors.Count==0};File.WriteAllText("TinyRidersProduction/ThemesExpansion01/Reports/runtime_catalog_validation.json",JsonConvert.SerializeObject(report,Formatting.Indented));if(errors.Count>0)throw new Exception(string.Join("\n",errors));Debug.Log("EXPANSION_RUNTIME_CATALOG_VERIFIED 410");}
}
