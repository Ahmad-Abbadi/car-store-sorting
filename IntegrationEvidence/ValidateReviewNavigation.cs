using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using TinyRiders;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using Newtonsoft.Json.Linq;
public static class ValidateReviewNavigation
{
 static void Click(Button button)
 {
  typeof(Clickable).GetMethod("Invoke",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Public).Invoke(button.clickable,new object[]{null});
 }
 public static string Run()
 {
  var errors=new List<string>();
  var rig=UnityEngine.Object.FindFirstObjectByType<ThemedCollectionReviewCamera>();
  var camera=rig.GetComponent<Camera>();
  var cars=UnityEngine.Object.FindObjectsByType<ThemedCarIdentity>(FindObjectsInactive.Include,FindObjectsSortMode.None);
  if(cars.Length!=600||cars.Select(c=>c.collectibleId).Distinct().Count()!=600||cars.Any(c=>!c.gameObject.activeInHierarchy))errors.Add("600 active unique cars required");
  if(UnityEngine.Object.FindObjectsByType<LODGroup>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length!=0)errors.Add("Unexpected LODGroup");
  int focused=0;
  camera.aspect=1.6f;
  for(int theme=0;theme<40;theme++)
  {
   rig.ShowTheme(theme);
   if(camera.farClipPlane!=10000||camera.useOcclusionCulling||camera.layerCullDistances.Any(d=>d!=0))errors.Add("Clipping at theme "+theme);
   for(int car=0;car<15;car++)
   {
    rig.ShowCar(car);
    bool inside=true;
    foreach(var r in rig.Cars(theme)[car].GetComponentsInChildren<Renderer>().Where(r=>r.enabled))
    foreach(float x in new[]{r.bounds.min.x,r.bounds.max.x})foreach(float y in new[]{r.bounds.min.y,r.bounds.max.y})foreach(float z in new[]{r.bounds.min.z,r.bounds.max.z})
    {var p=camera.WorldToViewportPoint(new Vector3(x,y,z));if(p.x<0||p.x>1||p.y<0||p.y>1||p.z<camera.nearClipPlane||p.z>camera.farClipPlane)inside=false;}
    if(inside)focused++;else errors.Add("Focus clips "+theme+" / "+car);
   }
  }
  rig.ShowOverview();if(camera.farClipPlane!=10000)errors.Add("Overview clips");
  int distant=0;
  foreach(int theme in new[]{0,39})
  {
   rig.ShowTheme(theme);
   camera.transform.LookAt(rig.collectionBounds.center);
   foreach(var car in cars)
   {var p=camera.WorldToViewportPoint(car.transform.position);if(p.z>=10000)errors.Add("Distant car beyond far clip");if(p.z>85&&p.x>=0&&p.x<=1&&p.y>=0&&p.y<=1)distant++;}
  }
  if(distant==0)errors.Add("Distant-car check did not cover any cars");
  rig.ShowTheme(rig.initialTheme);camera.ResetAspect();
  VisualElement root;
  if(Application.isPlaying) root=UnityEngine.Object.FindFirstObjectByType<ThemedCollectionReviewNavigation>().GetComponent<UIDocument>().rootVisualElement;
  else root=EditorWindow.GetWindow<ThemedCollectionReviewWindow>().rootVisualElement;
  if(root.Q("cars").childCount!=15)errors.Add("Missing car buttons");
  var search=root.Q<TextField>("themeSearch");search.value="egypt";
  var matches=root.Q<ScrollView>("themes").Children().OfType<Button>().ToArray();
  if(matches.Length!=1)errors.Add("Search must find Egypt only");
  search.value="no such theme";
  if(root.Q<ScrollView>("themes").Children().OfType<Button>().Any())errors.Add("Empty search result failed");
  search.value="";
  if(root.Q<ScrollView>("themes").Children().OfType<Button>().Count()!=40)errors.Add("Missing theme buttons");
  int original=rig.CurrentTheme;
  Click(root.Q<Button>("next"));
  if(rig.CurrentTheme!=(original+1)%40)errors.Add("Next button failed");
  Click(root.Q<Button>("previous"));
  if(rig.CurrentTheme!=original)errors.Add("Previous button failed");
  var before=camera.transform.position;
  Click(root.Q("cars").Children().OfType<Button>().First());
  if(Vector3.Distance(before,camera.transform.position)<1)errors.Add("Car button failed");
  Click(root.Q<Button>("overview"));
  if(!camera.orthographic||camera.farClipPlane!=10000)errors.Add("Overview button failed");
  rig.ShowTheme(rig.initialTheme);
  ReviewSceneClipping.ApplyAll();
  var settings=SceneView.lastActiveSceneView.cameraSettings;
  if(settings.dynamicClip||settings.farClip!=10000||settings.occlusionCulling)errors.Add("Scene view clipping not configured");
  var report=new JObject{["passed"]=errors.Count==0,["playMode"]=Application.isPlaying,["frame"]=Time.frameCount,["activeCars"]=cars.Length,["focusedCars"]=focused,["distantCarsWithinFrustumBeyondOldCutoff"]=distant,["farClip"]=camera.farClipPlane,["errors"]=new JArray(errors)};
  File.WriteAllText("IntegrationEvidence/review_navigation_"+(Application.isPlaying?"runtime":"editor")+".json",report.ToString());
  if(errors.Count>0)throw new Exception(report.ToString());return report.ToString();
 }
}
