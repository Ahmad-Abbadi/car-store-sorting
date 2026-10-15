using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using TinyRiders;
using Newtonsoft.Json.Linq;
public static class BakeThemedPile
{
 static uint Rank(string id,int seed){unchecked{uint h=(uint)seed;foreach(char c in id)h=(h^c)*16777619;return h;}}
 public static void Run()
 {
  if(EditorApplication.isPlaying)throw new Exception("Exit Play mode first");
  var session=UnityEngine.Object.FindFirstObjectByType<ThemedCollectionSession>();
  var entries=session.catalog.entries.Where(e=>e.enabled&&e.sortingItem!=null&&session.catalog.themes.Any(t=>t.id==e.themeId&&t.enabled)).ToDictionary(e=>e.collectibleId);
  var picks=ThemedPileSequence.InstanceIds(entries.Values,session.copiesPerDesign,session.seed).Take(session.visiblePileLimit)
   .Select(id=>new{id,entry=entries[id.Split(':')[1]]}).ToArray();
  var preview=EditorSceneManager.NewPreviewScene();var physics=preview.GetPhysicsScene();
  var objects=new System.Collections.Generic.List<GameObject>();var json=new JArray();
  try {
   var floor=new GameObject("Bake ground");SceneManager.MoveGameObjectToScene(floor,preview);var ground=floor.AddComponent<BoxCollider>();ground.size=new Vector3(140,1,140);ground.center=new Vector3(0,-.5f,0);
   var random=new System.Random(session.seed);
   foreach(var pick in picks) {
    var o=(GameObject)PrefabUtility.InstantiatePrefab(pick.entry.sortingItem.WorldItem.gameObject,preview);objects.Add(o);
    // Pile contact uses the car body, so tall hats and wings do not create unstable box stacks.
    var accessory=o.GetComponentsInChildren<Transform>(true).First(t=>t.name=="ThemeAccessories");
    var rr=o.GetComponentsInChildren<Renderer>(false).Where(r=>r.enabled&&!r.transform.IsChildOf(accessory)).ToArray();var b=rr[0].bounds;foreach(var r in rr.Skip(1))b.Encapsulate(r.bounds);
    var collider=o.GetComponent<BoxCollider>();collider.center=b.center;collider.size=b.size;
    double theta=random.NextDouble()*Math.PI*2;float radius=14*Mathf.Sqrt((float)random.NextDouble());
    o.transform.SetPositionAndRotation(new Vector3((float)Math.Cos(theta)*radius,12+objects.Count*.24f,(float)Math.Sin(theta)*radius),Quaternion.Euler((float)random.NextDouble()*18-9,(float)random.NextDouble()*360,(float)random.NextDouble()*14-7));
    var rb=o.GetComponent<Rigidbody>();rb.isKinematic=false;rb.linearDamping=.4f;rb.angularDamping=.7f;rb.solverIterations=10;
   }
   for(int step=0;step<1100;step++)physics.Simulate(.02f);
   for(int i=0;i<objects.Count;i++){var t=objects[i].transform;var p=t.position;var e=t.eulerAngles;json.Add(new JObject{["id"]=picks[i].id,["position"]=new JObject{["x"]=p.x,["y"]=p.y,["z"]=p.z},["euler"]=new JObject{["x"]=e.x,["y"]=e.y,["z"]=e.z}});}
   Directory.CreateDirectory("Assets/TinyRiders/Gameplay/Data");File.WriteAllText("Assets/TinyRiders/Gameplay/Data/PileLayout.json",new JObject{["revision"]=1,["placements"]=json}.ToString());
   File.WriteAllText("IntegrationEvidence/pile_bake.json",new JObject{["objects"]=objects.Count,["simulationSteps"]=1100,["simulatedSeconds"]=22,["seed"]=session.seed,["movingAfterBake"]=objects.Count(o=>o.GetComponent<Rigidbody>().linearVelocity.sqrMagnitude>.01f),["maxHeight"]=objects.Max(o=>o.transform.position.y)}.ToString());
  } finally{EditorSceneManager.ClosePreviewScene(preview);}
  // Only generated gameplay wrappers are updated; the approved art assets remain byte-identical.
  foreach(var e in session.catalog.entries.Where(e=>e.enabled)) {
   var path=AssetDatabase.GetAssetPath(e.sortingItem.WorldItem);var o=PrefabUtility.LoadPrefabContents(path);
   try{var accessory=o.GetComponentsInChildren<Transform>(true).First(t=>t.name=="ThemeAccessories");var rr=o.GetComponentsInChildren<Renderer>(false).Where(r=>r.enabled&&!r.transform.IsChildOf(accessory)).ToArray();var b=rr[0].bounds;foreach(var r in rr.Skip(1))b.Encapsulate(r.bounds);var c=o.GetComponent<BoxCollider>();c.center=b.center;c.size=b.size;PrefabUtility.SaveAsPrefabAsset(o,path);}finally{PrefabUtility.UnloadPrefabContents(o);}
  }
  AssetDatabase.Refresh();Debug.Log("THEMED_PILE_BAKED "+objects.Count);
 }
}

