using System;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json.Linq;
public static class InspectNewMounts
{
 public static string Run()
 {
  var result=new JArray();
  var measure=typeof(CompleteThemedCollectionBuilder).GetMethod("PaintedHeight",BindingFlags.NonPublic|BindingFlags.Static);
  foreach(string id in new[]{"C006","C013"})
  {
   var root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TinyRiders/ThemedCollection/Prefabs/Themed/TH_PIRATE/TR-"+id+"-TH_PIRATE-V01.prefab"));
   try {for(float z=-1.4f;z<=1.5f;z+=.2f){try{result.Add(new JObject{["car"]=id,["z"]=z,["height"]=(float)measure.Invoke(null,new object[]{root,0f,z})});}catch(TargetInvocationException){}}}
   finally{UnityEngine.Object.DestroyImmediate(root);}
  }
  return result.ToString();
 }
}
