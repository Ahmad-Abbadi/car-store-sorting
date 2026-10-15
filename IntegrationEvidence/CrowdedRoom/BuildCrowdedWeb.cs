using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
public static class BuildCrowdedWeb
{
    public static void Run()
    {
        if(EditorApplication.isPlaying)throw new Exception("Exit Play mode before building");
        CollectorRoomBuilder.Audit();
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{
            scenes=new[]{"Assets/00_Project/00_Scenes/Initializer.unity","Assets/00_Project/00_Scenes/MainMenu Scene.unity","Assets/00_Project/00_Scenes/Loading Scene.unity",CompactCollectionBuilder.ScenePath},
            locationPathName="Builds/CrowdedCollectorWebGL",target=BuildTarget.WebGL,options=BuildOptions.Development});
        var s=report.summary;
        File.WriteAllText("IntegrationEvidence/CrowdedRoom/webgl_build.json",new JObject{
            ["result"]=s.result.ToString(),["seconds"]=s.totalTime.TotalSeconds,["bytes"]=(long)s.totalSize,["errors"]=s.totalErrors,["warnings"]=s.totalWarnings,
            ["messages"]=new JArray(report.steps.SelectMany(v=>v.messages).Where(m=>m.type==UnityEngine.LogType.Error||m.type==UnityEngine.LogType.Exception).Select(m=>m.content))}.ToString());
        if(s.result!=BuildResult.Succeeded)throw new Exception("Collector shop WebGL build: "+s.result);
    }
}
