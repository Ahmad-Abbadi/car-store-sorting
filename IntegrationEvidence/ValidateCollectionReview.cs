using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using TinyRiders;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
public static class ValidateCollectionReview
{
 public static string Run()
 {
  var scene=SceneManager.GetActiveScene();var errors=new List<string>();
  var cars=UnityEngine.Object.FindObjectsByType<ThemedCarIdentity>(FindObjectsSortMode.None);
  var rig=UnityEngine.Object.FindFirstObjectByType<ThemedCollectionReviewCamera>();var camera=rig.GetComponent<Camera>();
  if(scene.path!=ThemedCollectionReviewBuilder.ScenePath)errors.Add("Wrong active scene");
  if(SceneManager.sceneCount!=1)errors.Add("Review must be isolated from other scenes");
  if(cars.Length!=600||cars.Select(c=>c.collectibleId).Distinct().Count()!=600)errors.Add("600 unique designs required");
  if(rig.themeCenters.Length!=40)errors.Add("40 theme bays required");
  if(UnityEngine.Object.FindObjectsByType<LevelSaveManager>(FindObjectsSortMode.None).Length!=0)errors.Add("Unexpected save system");
  var labels=UnityEngine.Object.FindObjectsByType<TextMeshPro>(FindObjectsSortMode.None);
  if(labels.Count(l=>l.name.StartsWith("ID - "))!=600)errors.Add("Missing car ID labels");
  int framed=0;camera.aspect=1.6f;
  for(int theme=0;theme<rig.themeCenters.Length;theme++)
  {
   var group=rig.themeCenters[theme].parent;var entries=group.GetComponentsInChildren<ThemedCarIdentity>();
   if(entries.Length!=15)errors.Add("Incomplete bay "+group.name);
   rig.ShowTheme(theme);
   foreach(var entry in entries)
   {
    var renderers=entry.GetComponentsInChildren<Renderer>().Where(r=>r.enabled).ToArray();var bounds=renderers[0].bounds;
    foreach(var r in renderers.Skip(1))bounds.Encapsulate(r.bounds);
    bool inside=true;
    foreach(float x in new[]{bounds.min.x,bounds.max.x})foreach(float y in new[]{bounds.min.y,bounds.max.y})foreach(float z in new[]{bounds.min.z,bounds.max.z})
    {var v=camera.WorldToViewportPoint(new Vector3(x,y,z));if(v.x<0||v.x>1||v.y<0||v.y>1||v.z<camera.nearClipPlane||v.z>camera.farClipPlane)inside=false;}
    if(inside)framed++;else errors.Add("Camera clips "+entry.collectibleId);
   }
   foreach(var label in group.GetComponentsInChildren<TextMeshPro>())
   {
    label.ForceMeshUpdate();var bounds=label.GetComponent<Renderer>().bounds;bool inside=true;
    foreach(float x in new[]{bounds.min.x,bounds.max.x})foreach(float y in new[]{bounds.min.y,bounds.max.y})foreach(float z in new[]{bounds.min.z,bounds.max.z})
    {var v=camera.WorldToViewportPoint(new Vector3(x,y,z));if(v.x<0||v.x>1||v.y<0||v.y>1||v.z<camera.nearClipPlane)inside=false;}
    if(!inside)errors.Add("Camera clips label "+group.name+" / "+label.name);
   }
  }
  rig.ShowTheme(rig.initialTheme);camera.ResetAspect();
  var report=new JObject{["passed"]=errors.Count==0,["scene"]=scene.path,["uniqueCars"]=cars.Length,["themes"]=rig.themeCenters.Length,["labels"]=labels.Length,["carsFullyFramedAcross40Views"]=framed,["isolatedScene"]=SceneManager.sceneCount==1,["errors"]=new JArray(errors)};
  File.WriteAllText("IntegrationEvidence/collection_review_validation.json",report.ToString());
  if(errors.Count>0)throw new Exception(report.ToString());return report.ToString();
 }
 public static string Overview(){var rig=UnityEngine.Object.FindFirstObjectByType<ThemedCollectionReviewCamera>();rig.GetComponent<Camera>().aspect=1.6f;rig.ShowOverview();return "All 600 designs in overview";}
 public static string Restore(){var rig=UnityEngine.Object.FindFirstObjectByType<ThemedCollectionReviewCamera>();rig.GetComponent<Camera>().ResetAspect();rig.ShowTheme(rig.initialTheme);return "Initial Egyptian review view restored";}
 public static string Save(){Restore();UnityEditor.SceneManagement.EditorSceneManager.SaveScene(SceneManager.GetActiveScene());return SceneManager.GetActiveScene().path;}
 public static string Runtime()
 {
  var rig=UnityEngine.Object.FindFirstObjectByType<ThemedCollectionReviewCamera>();var camera=rig.GetComponent<Camera>();
  var first=camera.transform.position;rig.ShowTheme(rig.initialTheme+1);
  bool next=Vector3.Distance(first,camera.transform.position)>1;
  rig.ShowOverview();bool overview=camera.orthographic;
  rig.ShowTheme(rig.initialTheme);bool restored=!camera.orthographic&&Vector3.Distance(first,camera.transform.position)<.01f;
  var report=new JObject{["frame"]=Time.frameCount,["cars"]=UnityEngine.Object.FindObjectsByType<ThemedCarIdentity>(FindObjectsSortMode.None).Length,["nextThemeWorks"]=next,["overviewWorks"]=overview,["restoresInitialTheme"]=restored};
  File.WriteAllText("IntegrationEvidence/collection_review_runtime.json",report.ToString());
  if(!next||!overview||!restored)throw new Exception(report.ToString());return report.ToString();
 }
}
