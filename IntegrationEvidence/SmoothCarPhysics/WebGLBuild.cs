using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class SmoothCarPhysicsWebGLBuild
{
    [Serializable] class Result { public string result; public int errors, warnings; public double seconds; public long bytes; }
    public static void Run()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode before building.");
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { "Assets/00_Project/00_Scenes/Initializer.unity", "Assets/00_Project/00_Scenes/MainMenu Scene.unity", "Assets/00_Project/00_Scenes/Loading Scene.unity", CompactCollectionBuilder.ScenePath },
            locationPathName = "Builds/SmoothCarPhysicsWebGL", target = BuildTarget.WebGL, options = BuildOptions.Development
        });
        var s = report.summary;
        File.WriteAllText("IntegrationEvidence/SmoothCarPhysics/webgl_build.json", JsonUtility.ToJson(new Result
        { result = s.result.ToString(), errors = s.totalErrors, warnings = s.totalWarnings, seconds = s.totalTime.TotalSeconds, bytes = (long)s.totalSize }, true));
        if (s.result != BuildResult.Succeeded) throw new InvalidOperationException("WebGL build " + s.result);
    }
}
