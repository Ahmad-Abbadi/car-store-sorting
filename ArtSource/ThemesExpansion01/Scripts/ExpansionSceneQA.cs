using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json.Linq;
using TinyRiders.Editor;
public class ExpansionSceneQA {
 const string D="TinyRidersProduction/ThemesExpansion01";
 static Vector3 V(JToken j)=>new Vector3((float)j[0],(float)j[1],(float)j[2]);
 static void Save(PreviewRenderUtility pr,string path,int w,int h){Directory.CreateDirectory(Path.GetDirectoryName(path));pr.BeginStaticPreview(new Rect(0,0,w,h));pr.camera.Render();var tex=pr.EndStaticPreview();File.WriteAllBytes(path,tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);}
 static void Light(PreviewRenderUtility pr){pr.camera.clearFlags=CameraClearFlags.SolidColor;pr.camera.backgroundColor=new Color(.91f,.93f,.95f);pr.camera.nearClipPlane=.01f;pr.camera.farClipPlane=100;pr.ambientColor=new Color(.48f,.48f,.48f);pr.lights[0].intensity=1.25f;pr.lights[0].transform.rotation=Quaternion.Euler(35,-35,0);pr.lights[1].intensity=.8f;pr.lights[1].transform.rotation=Quaternion.Euler(40,150,0);}
 public static void Run(){
  var products=JArray.Parse(File.ReadAllText(D+"/products.json"));var representatives=products.GroupBy(x=>(string)x["themeId"]).Select(g=>g.First(x=>(string)x["baseCarId"]=="C001")).ToArray();
  var cameraData=JObject.Parse(File.ReadAllText("TinyRidersProduction/ThemeAccessoryPass/actual_camera_test.json"));var main=UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).FirstOrDefault(c=>c.name=="Main Camera");
  foreach(var p in representatives){var pr=new PreviewRenderUtility();GameObject obj=null;try{obj=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>((string)p["prefabPath"]));pr.AddSingleGO(obj);obj.transform.position=V(cameraData["position"]);obj.transform.eulerAngles=V(cameraData["rotation"]);if(main!=null)pr.camera.CopyFrom(main);pr.camera.scene=obj.scene;Light(pr);pr.camera.orthographic=false;pr.camera.fieldOfView=(float)cameraData["fieldOfView"];pr.camera.aspect=(float)cameraData["aspect"];pr.camera.transform.position=V(cameraData["cameraPosition"]);pr.camera.transform.eulerAngles=V(cameraData["cameraRotation"]);Save(pr,D+"/Views/ActualCamera/"+p["variantId"]+".png",960,540);}finally{pr.Cleanup();if(obj!=null)UnityEngine.Object.DestroyImmediate(obj);}}
  foreach(var p in representatives){
   var obj=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>((string)p["prefabPath"]));var mats=new List<Material>();try{
    foreach(var rr in obj.GetComponentsInChildren<Renderer>()){rr.sharedMaterials=rr.sharedMaterials.Select(m=>{var copy=new Material(m);copy.DisableKeyword("_EMISSION");if(copy.HasProperty("_EmissionMap"))copy.SetTexture("_EmissionMap",null);if(copy.HasProperty("_EmissionColor"))copy.SetColor("_EmissionColor",Color.black);mats.Add(copy);return copy;}).ToArray();}
    var profile=JArray.Parse(File.ReadAllText(D+"/fitting_profiles.json")).First(x=>(string)x["baseCarId"]==(string)p["baseCarId"]);var bounds=new Bounds();bounds.SetMinMax(V(profile["sourceBoundsMin"]),V(profile["sourceBoundsMax"]));ThemeAccessoryBuilder.Render(obj,D+"/Views/NoEmission/"+p["variantId"]+".png",bounds);
   }finally{UnityEngine.Object.DestroyImmediate(obj);foreach(var m in mats)UnityEngine.Object.DestroyImmediate(m);}
  }
  for(int batch=0;batch<3;batch++){var pr=new PreviewRenderUtility();var objects=new List<GameObject>();var placement=new JArray();try{Light(pr);int count=Math.Min(8,representatives.Length-batch*7);for(int j=0;j<count;j++){var p=representatives[batch*7+j];var obj=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>((string)p["prefabPath"]));pr.AddSingleGO(obj);objects.Add(obj);int row=j/4,col=j%4;obj.transform.position=new Vector3((col-1.5f)*2.25f,row==0?0:.85f,row*3.1f-1.5f);obj.transform.eulerAngles=new Vector3(row==0?0:13,(j%3-1)*28,row==0?0:(j%2==0?17:-15));placement.Add(new JObject{["variantId"]=(string)p["variantId"],["position"]=new JArray(obj.transform.position.x,obj.transform.position.y,obj.transform.position.z),["rotation"]=new JArray(obj.transform.eulerAngles.x,obj.transform.eulerAngles.y,obj.transform.eulerAngles.z)});}pr.camera.orthographic=true;pr.camera.orthographicSize=6.4f;var target=new Vector3(0,1.2f,0);pr.camera.transform.position=target+new Vector3(.40f,.65f,1.5f).normalized*22;pr.camera.transform.LookAt(target);Save(pr,D+"/ContactSheets/StaticMixedPile_"+batch+".png",1400,1000);File.WriteAllText(D+"/Reports/pile_"+batch+".json",placement.ToString());}finally{pr.Cleanup();foreach(var o in objects)if(o!=null)UnityEngine.Object.DestroyImmediate(o);}}
  File.WriteAllText(D+"/Reports/actual_camera_test.json",cameraData.ToString());ThemeAccessoryBuilder.PreserveFoundationMaterialFlags();Debug.Log("EXPANSION_CAMERA_AND_PILE_COMPLETE");
 }
}
