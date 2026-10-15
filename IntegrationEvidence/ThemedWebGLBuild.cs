using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEditor.Build.Reporting;
using Newtonsoft.Json.Linq;
public static class ThemedWebGLBuild {
 public static void Run() {
  if(EditorApplication.isPlaying)throw new Exception("Exit Play mode before building");
  ThemedWebGLImportProfile.Apply();
  Directory.CreateDirectory("Builds/ThemedSortingWebGL_Final");
  var started=DateTime.UtcNow;
  var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/TinyRiders/Scenes/ThemedSorting.unity"},locationPathName="Builds/ThemedSortingWebGL_Final",target=BuildTarget.WebGL,options=BuildOptions.Development});
  var summary=report.summary;
  File.WriteAllText("IntegrationEvidence/webgl_build_final.json",new JObject{["result"]=summary.result.ToString(),["totalSizeBytes"]=(long)summary.totalSize,["durationSeconds"]=summary.totalTime.TotalSeconds,["errors"]=summary.totalErrors,["warnings"]=summary.totalWarnings,["startedUtc"]=started.ToString("O"),["endedUtc"]=DateTime.UtcNow.ToString("O"),["directoryBytes"]=Directory.GetFiles("Builds/ThemedSortingWebGL_Final","*",SearchOption.AllDirectories).Sum(f=>new FileInfo(f).Length),["messages"]=new JArray(report.steps.SelectMany(s=>s.messages).Where(m=>m.type==LogType.Error||m.type==LogType.Exception).Select(m=>m.content))}.ToString());
  if(summary.result!=BuildResult.Succeeded)throw new Exception("WebGL build "+summary.result);
 }
}
