var shelves=UnityEngine.Object.FindObjectsByType<SortingShelf>(UnityEngine.FindObjectsSortMode.None);
var toppers=shelves.Select(s=>s.transform.Find("TR_Shelf_Base_01").GetComponentsInChildren<UnityEngine.Transform>(true).Single(t=>t.name=="ThemeTopper")).ToArray();
float highest=toppers.SelectMany(t=>t.GetComponentsInChildren<UnityEngine.MeshRenderer>(true)).Max(r=>r.bounds.max.y);
var renderers=UnityEngine.Object.FindObjectsByType<UnityEngine.MeshRenderer>(UnityEngine.FindObjectsSortMode.None);
var signs=renderers.Where(r=>(r.name.StartsWith("Category ")&&r.GetComponent<TMPro.TMP_Text>()==null)||r.name=="Directory Board").ToArray();
float lowestSign=signs.Min(r=>r.bounds.min.y);
float ceiling=renderers.Single(r=>r.name=="Store Ceiling").bounds.min.y;
var overlaps=new System.Collections.Generic.List<string>();
for(int i=0;i<signs.Length;i++)for(int j=i+1;j<signs.Length;j++)if(signs[i].bounds.Intersects(signs[j].bounds))overlaps.Add(signs[i].name+" / "+signs[j].name);
// Legacy solid-color structural/tray pieces do not sample a texture. Check every textured piece.
var badUv=ThemedShelfArtBuilder.Catalog.entries.Where(e=>e.prefab.GetComponentsInChildren<UnityEngine.MeshFilter>(true).Any(f=>f.GetComponent<UnityEngine.MeshRenderer>().sharedMaterial.GetTexture("_BaseMap")!=null && f.sharedMesh.uv.Length!=f.sharedMesh.vertexCount)).Select(e=>e.themeId).ToArray();
var signLabels=UnityEngine.Object.FindObjectsByType<TMPro.TextMeshPro>(UnityEngine.FindObjectsSortMode.None).Where(t=>t.name.StartsWith("Category Name")||t.name=="Store Directory").ToArray();
foreach(var label in signLabels)label.ForceMeshUpdate();
var overflow=signLabels.Where(t=>t.isTextOverflowing).Select(t=>t.name).ToArray();
var result=new {toppers=toppers.Length,posts=toppers.Sum(t=>t.GetComponentsInChildren<UnityEngine.Transform>(true).Count(p=>p.name=="TopperPost")),highestTopper=highest,minimumSignClearance=lowestSign-highest,ceilingClearance=ceiling-highest,signOverlaps=overlaps.ToArray(),signTextOverflow=overflow,modelsWithMissingUVs=badUv,qaActive=UnityEditor.SessionState.GetBool("TinyRiders.Compact.QA.Active",false),upgradesAwaitingRestore=UnityEditor.SessionState.GetBool("CompactQA.restoreUpgrades",false),playing=UnityEditor.EditorApplication.isPlaying,scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,sceneDirty=UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty};
System.IO.File.WriteAllText(ThemedShelfArtBuilder.Evidence+"/final_scene_audit.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
if(toppers.Length!=80||lowestSign-highest<.1f||ceiling-highest<.1f||overlaps.Count>0||overflow.Length>0||badUv.Length>0||result.qaActive||result.upgradesAwaitingRestore||result.playing)throw new System.Exception("Signature scene audit failed; see final_scene_audit.json");
return result;
