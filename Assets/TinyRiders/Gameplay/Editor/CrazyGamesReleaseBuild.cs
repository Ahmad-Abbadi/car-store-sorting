using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Newtonsoft.Json.Linq;

/// <summary>Repeatable desktop release profile. Source images and non-WebGL imports are preserved.</summary>
public static class CrazyGamesReleaseBuild
{
    public const string Output = "Builds/CrazyGamesRelease";
    public const string Evidence = "IntegrationEvidence/CrazyGamesRelease";

    [MenuItem("Tools/Toy Car Sort/CrazyGames/Apply Desktop Release Profile")]
    public static void Configure()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode first.");
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
            throw new InvalidOperationException("Switch the Editor to WebGL first.");
        Directory.CreateDirectory(Evidence);
        var target = NamedBuildTarget.WebGL;
        PlayerSettings.SetIl2CppCodeGeneration(target, Il2CppCodeGeneration.OptimizeSize);
        PlayerSettings.SetManagedStrippingLevel(target, ManagedStrippingLevel.High);
        PlayerSettings.stripEngineCode = true;
        // OutlineFill is assigned at runtime and reads smooth normals from TEXCOORD3.
        // Keep channels that the original lit material does not consume.
        PlayerSettings.stripUnusedMeshComponents = false;
        PlayerSettings.WebGL.dataCaching = true;
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
        PlayerSettings.WebGL.decompressionFallback = false;
        // Startup, localization and save loading use exception handling.
        PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
        PlayerSettings.WebGL.debugSymbolMode = WebGLDebugSymbolMode.Off;
        PlayerSettings.WebGL.initialMemorySize = 256;
        PlayerSettings.WebGL.maximumMemorySize = 2048;
        PlayerSettings.WebGL.memoryGrowthMode = WebGLMemoryGrowthMode.Geometric;
        UnityEditor.WebGL.UserBuildSettings.codeOptimization = UnityEditor.WebGL.WasmCodeOptimization.DiskSizeLTO;
        EditorUserBuildSettings.development = false;
        EditorUserBuildSettings.allowDebugging = false;
        EditorUserBuildSettings.connectProfiler = false;
        if (EditorUserBuildSettings.webGLBuildSubtarget != WebGLTextureSubtarget.DXT)
        {
            EditorUserBuildSettings.webGLBuildSubtarget = WebGLTextureSubtarget.DXT;
            // Finish the subtarget's automatic reimport before changing importer metadata.
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        var inputs = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path)
            .Concat(AssetDatabase.GetAllAssetPaths().Where(p => p.StartsWith("Assets/") && p.Contains("/Resources/") && !AssetDatabase.IsValidFolder(p))).ToArray();
        var paths = AssetDatabase.GetDependencies(inputs, true).Distinct().OrderBy(p => p).ToArray();
        File.WriteAllLines(Path.Combine(Evidence, "runtime-dependencies.txt"), paths);
        var changes = new JArray();
        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (var path in paths)
            {
                if (!(AssetImporter.GetAtPath(path) is TextureImporter importer)) continue;
                // Keep text, input icons and font atlases sharp; scale only the oversized full-image UI art.
                bool largeUiArt = path.Contains("/UI/Branding/") || path.Contains("/UI/Loading/");
                if (!(largeUiArt || path.StartsWith("Assets/TinyRiders/") || path.StartsWith("Assets/TinyRidersPremium/") || path.StartsWith("Assets/ithappy/Modular_Racing_Cars/Textures/"))) continue;
                if ((!largeUiArt && importer.textureType == TextureImporterType.Sprite) || importer.textureType == TextureImporterType.SingleChannel) continue;
                var settings = importer.GetPlatformTextureSettings("WebGL");
                int cap = largeUiArt ? 1024 : importer.textureType == TextureImporterType.NormalMap ? 256 : 512;
                int quality = largeUiArt ? 95 : 65;
                int size = Math.Min(importer.maxTextureSize, settings.overridden ? settings.maxTextureSize : importer.maxTextureSize);
                size = Math.Min(size, cap);
                var format = importer.DoesSourceTextureHaveAlpha() || importer.textureType == TextureImporterType.NormalMap
                    ? TextureImporterFormat.DXT5Crunched : TextureImporterFormat.DXT1Crunched;
                if (settings.overridden && settings.maxTextureSize == size && settings.format == format && settings.compressionQuality == quality) continue;
                changes.Add(new JObject { ["path"] = path, ["previous"] = JObject.FromObject(settings), ["size"] = size, ["format"] = format.ToString() });
                settings.name = "WebGL";
                settings.overridden = true;
                settings.maxTextureSize = size;
                settings.format = format;
                settings.textureCompression = TextureImporterCompression.Compressed;
                settings.crunchedCompression = true;
                settings.compressionQuality = quality;
                importer.SetPlatformTextureSettings(settings);
                AssetDatabase.WriteImportSettingsIfDirty(path);
            }
        }
        finally { AssetDatabase.StopAssetEditing(); }
        string changeLog = Path.Combine(Evidence, "texture-profile-changes.json");
        if (changes.Count > 0 || !File.Exists(changeLog))
        {
            File.WriteAllText(changeLog, changes.ToString());
            File.WriteAllText(Path.Combine(Evidence, "texture-profile-changes-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff") + ".json"), changes.ToString());
        }
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.SaveAssets();
        ConfigureMusic();
        Debug.Log("CrazyGames desktop release profile saved; texture overrides changed: " + changes.Count);
    }

    private static void ConfigureMusic()
    {
        var changes = new JArray();
        foreach (var path in new[] {
            "Assets/00_Project/Audio/PackageSelection/Music/Gameplay_LifeIsSimple.wav",
            "Assets/00_Project/Audio/PackageSelection/Music/Menu_LifeIsSimple.wav"
        })
        {
            var importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer == null) throw new InvalidOperationException("Missing release music: " + path);
            bool overridden = importer.ContainsSampleSettingsOverride("WebGL");
            // Unity translates the default Vorbis profile to AAC for WebGL.
            var settings = importer.GetOverrideSampleSettings(BuildTargetGroup.WebGL);
            float quality = Mathf.Min(settings.quality, .60f);
            if (overridden && Mathf.Approximately(settings.quality, quality)) continue;
            changes.Add(new JObject { ["path"] = path, ["previousOverride"] = overridden,
                ["previousQuality"] = settings.quality, ["quality"] = quality,
                ["format"] = settings.compressionFormat.ToString(), ["loadType"] = settings.loadType.ToString() });
            settings.quality = quality;
            if (!importer.SetOverrideSampleSettings(BuildTargetGroup.WebGL, settings))
                throw new InvalidOperationException("WebGL music override rejected: " + path);
            importer.SaveAndReimport();
            if (!Mathf.Approximately(importer.GetOverrideSampleSettings("WebGL").quality, quality))
                throw new InvalidOperationException("WebGL music quality did not persist: " + path);
        }
        if (changes.Count > 0)
            File.WriteAllText(Path.Combine(Evidence, "audio-profile-changes.json"), changes.ToString());
    }

    [MenuItem("Tools/Toy Car Sort/CrazyGames/Build Desktop Release")]
    public static void Build()
    {
        if (CrazyGamesCarBuildDependencies.RequiresRecovery)
            throw new InvalidOperationException("Recover temporary car dependencies before building again.");
        // Prepare exact car geometry before temporary release mesh imports are applied.
        bool skipCarBake = CarVisualBatchBuilder.SkipForBenchmark;
        if (!skipCarBake) CarVisualBatchBuilder.BakeAll();
        Directory.CreateDirectory(Evidence);
        File.WriteAllText(Path.Combine(Evidence, "build-report.json"), new JObject { ["result"] = "Building", ["sizeGatePassed"] = false }.ToString());
        Configure();
        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        if (scenes.Length == 0 || !scenes[0].EndsWith("/Initializer.unity") || !scenes.Any(s => s.EndsWith("/Tutorial.unity")))
            throw new InvalidOperationException("Initializer must be first and Tutorial must be enabled.");
        try
        {
            CarVisualBatchBuilder.SkipForBenchmark = true;
            using (CrazyGamesCarBuildDependencies.Apply())
            using (CrazyGamesMeshCompression.Apply())
            {
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                    scenes = scenes, target = BuildTarget.WebGL, locationPathName = Output,
                    options = BuildOptions.DetailedBuildReport
                });
                WriteReport(report);
                if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("CrazyGames build failed.");
            }
        }
        finally { CarVisualBatchBuilder.SkipForBenchmark = skipCarBake; }
    }

    public static void WriteReport(BuildReport report)
    {
        Directory.CreateDirectory(Evidence);
        var files = Directory.Exists(Output) ? Directory.GetFiles(Output, "*", SearchOption.AllDirectories) : Array.Empty<string>();
        var total = files.Sum(p => new FileInfo(p).Length);
        var dependencies = new HashSet<string>(File.ReadAllLines(Path.Combine(Evidence, "runtime-dependencies.txt")));
        var errors = report.steps.SelectMany(s => s.messages).Where(m => m.type == LogType.Error || m.type == LogType.Exception).ToArray();
        var diagnostics = new JArray();
        int blockingErrors = Math.Max(0, (int)report.summary.totalErrors - errors.Length);
        int unrelatedAudioErrors = 0;
        foreach (var error in errors)
        {
            var match = System.Text.RegularExpressions.Regex.Match(error.content, @"^Errors during import of AudioClip (.+?):");
            string path = match.Success ? match.Groups[1].Value : "";
            bool unrelatedAudio = error.type == LogType.Error && match.Success
                && error.content.Contains("FSBTool ERROR: Internal error from FMOD sub-system.")
                && (path.StartsWith("Assets/Mobile Games Bundle/") || path.StartsWith("Assets/2000 Game Sound FX/"))
                && !dependencies.Contains(path);
            if (unrelatedAudio) unrelatedAudioErrors++; else blockingErrors++;
            diagnostics.Add(new JObject { ["message"] = error.content, ["unreferencedStockAudio"] = unrelatedAudio });
        }
        var summary = new JObject {
            ["result"] = report.summary.result.ToString(), ["bytes"] = total,
            ["completedUtc"] = DateTime.UtcNow.ToString("O"),
            ["options"] = report.summary.options.ToString(),
            ["developmentBuild"] = (report.summary.options & BuildOptions.Development) != 0,
            ["fileCount"] = files.Length, ["errors"] = report.summary.totalErrors,
            ["blockingErrors"] = blockingErrors, ["unreferencedAudioImportErrors"] = unrelatedAudioErrors,
            ["diagnostics"] = diagnostics,
            ["warnings"] = report.summary.totalWarnings, ["durationSeconds"] = report.summary.totalTime.TotalSeconds,
            // Conservative: requiring the entire package <= 50 MB also bounds first-gameplay downloads.
            ["sizeGatePassed"] = total <= 50000000 && files.Length < 1500,
            ["assets"] = new JArray(report.packedAssets.SelectMany(p => p.contents)
                .OrderByDescending(p => p.packedSize).Select(p => new JObject { ["path"] = p.sourceAssetPath, ["bytes"] = p.packedSize }))
        };
        File.WriteAllText(Path.Combine(Evidence, "build-report.json"), summary.ToString());
    }
}
