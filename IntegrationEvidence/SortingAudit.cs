using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using Newtonsoft.Json;
using TinyRiders;
public static class SortingAudit {
 static float[] V(Vector3 v) => new[]{v.x,v.y,v.z}; public static void Run() {
 var scene=SceneManager.GetActiveScene();
 var cat=AssetDatabase.LoadAssetAtPath<ThemedCarCatalog>("Assets/TinyRiders/ThemedCollection/Data/Themes/ThemedCarCatalog.asset");
 var products=cat.entries.Select(e=> { var go=PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(e.prefab)); try { var rs=go.GetComponentsInChildren<Renderer>(false).Where(r=>r.enabled && (r is MeshRenderer || r is SkinnedMeshRenderer)).ToArray(); var b=rs[0].bounds; foreach(var r in rs.Skip(1)) b.Encapsulate(r.bounds); return new {e.collectibleId,e.themeId,size=V(b.size),min=V(b.min), accessories=go.GetComponentsInChildren<Transform>(true).Count(t=>t.name=="ThemeAccessories"), identity=go.GetComponent<ThemedCarIdentity>()?.collectibleId, renderers=rs.Length}; } finally{PrefabUtility.UnloadPrefabContents(go);} }).ToArray();
 File.WriteAllText("IntegrationEvidence/asset_audit.json",JsonConvert.SerializeObject(new {scene=scene.path,roots=scene.GetRootGameObjects().Select(r=>new {r.name,pos=V(r.transform.position),scale=V(r.transform.localScale),children=r.transform.Cast<Transform>().Take(20).Select(t=>t.name).ToArray()}),products,players=UnityEngine.Object.FindObjectsByType<Player>(FindObjectsSortMode.None).Select(p=>new {p.name,path=AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(p.gameObject)),position=V(p.transform.position),scale=V(p.transform.localScale),camera=Camera.main!=null?V(Camera.main.transform.position):null}),shelves=UnityEngine.Object.FindObjectsByType<SortingShelf>(FindObjectsSortMode.None).Length},Formatting.Indented));
 Debug.Log("SORTING_AUDIT_COMPLETE "+products.Length);
 }
}

