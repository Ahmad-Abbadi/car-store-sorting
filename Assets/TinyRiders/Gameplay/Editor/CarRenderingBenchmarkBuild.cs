using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class CarRenderingBenchmarkBuild
{
    // Same release flags and settings for both sides; deliberately do not apply texture/quality profiles.
    public static void Build(string label)
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode first");
        if (label == "Baseline" && System.Linq.Enumerable.Any(ItemDetailBuilder.CatalogPrefabs(), p => p.GetComponentInChildren<CarBatchGeometry>(true) != null))
            throw new InvalidOperationException("Restore authored car visuals before creating a baseline.");
        if (label != "Baseline") CarVisualBatchBuilder.BakeAll();
        string output = "Builds/CarBatching" + label;
        bool skip = CarVisualBatchBuilder.SkipForBenchmark;
        var compression = PlayerSettings.WebGL.compressionFormat;
        CarVisualBatchBuilder.SkipForBenchmark = true;
        BuildReport report;
        try {
            // Local timing does not depend on download compression. Skip slow Brotli packaging.
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = new[] { CompactCollectionBuilder.ScenePath }, target = BuildTarget.WebGL,
            locationPathName = output, options = BuildOptions.None,
            extraScriptingDefines = new[] { "CAR_RENDER_BENCHMARK" }
        }); }
        finally {
            CarVisualBatchBuilder.SkipForBenchmark = skip;
            PlayerSettings.WebGL.compressionFormat = compression;
            AssetDatabase.SaveAssets();
        }
        Directory.CreateDirectory("IntegrationEvidence/CarBatching");
        File.WriteAllText("IntegrationEvidence/CarBatching/build-" + label + ".json",
            JsonUtility.ToJson(new Result { result = report.summary.result.ToString(), seconds = report.summary.totalTime.TotalSeconds,
                bytes = report.summary.totalSize, errors = report.summary.totalErrors, warnings = report.summary.totalWarnings }, true));
        if (report.summary.result != BuildResult.Succeeded) throw new Exception("Benchmark build failed");
        File.Copy("IntegrationEvidence/CarBatching/counters.js", output + "/car-counters.js", true);
        var path = output + "/index.html";
        File.WriteAllText(path, File.ReadAllText(path).Replace("</head>", "<script src=\"car-counters.js\"></script></head>"));
    }
    [Serializable] private class Result { public string result; public double seconds; public ulong bytes; public int errors, warnings; }
}
