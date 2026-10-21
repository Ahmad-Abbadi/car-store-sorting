using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
using Newtonsoft.Json.Linq;

/// <summary>Compress build meshes temporarily; retain exact source files for restoration/recovery.</summary>
public sealed class CrazyGamesMeshCompression : IDisposable
{
    private readonly string folder;
    private readonly JArray files = new JArray();
    private HashSet<string> preciseMeshPaths;
    private bool disposed;
    private string Manifest => Path.Combine(folder, "manifest.json");
    private static string Status => Path.Combine(CrazyGamesReleaseBuild.Evidence, "mesh-restoration.json");

    private CrazyGamesMeshCompression()
    {
        folder = Path.Combine(CrazyGamesReleaseBuild.Evidence, "mesh-backup-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff"));
        Directory.CreateDirectory(folder);
    }

    public static CrazyGamesMeshCompression Apply()
    {
        var scope = new CrazyGamesMeshCompression();
        try { scope.Prepare(); return scope; }
        catch
        {
            try { scope.Dispose(); } catch (Exception e) { Debug.LogException(e); }
            throw;
        }
    }

    private static bool Owned(string path) => path.StartsWith("Assets/TinyRiders/", StringComparison.Ordinal)
        || path.StartsWith("Assets/TinyRidersPremium/", StringComparison.Ordinal)
        || path.StartsWith("Assets/ithappy/Modular_Racing_Cars/", StringComparison.Ordinal);

    // These meshes contain millimetre-separated faces or metric/repeating UVs. High
    // compression quantizes each mesh independently, so neighbouring layers can
    // cross and UVs on otherwise identical room panels can acquire visible seams.
    internal static ModelImporterMeshCompression CompressionFor(string path, ISet<string> preciseDependencies = null)
    {
        path = path.Replace('\\', '/');
        return (preciseDependencies != null && preciseDependencies.Contains(path))
            || path.StartsWith("Assets/TinyRiders/Art/Shelves/", StringComparison.Ordinal)
            || path.StartsWith("Assets/TinyRiders/Gameplay/Rendering/Shelf_", StringComparison.Ordinal)
            || path.StartsWith("Assets/TinyRiders/Gameplay/Rendering/CarBatches/", StringComparison.Ordinal)
            || path.StartsWith("Assets/TinyRiders/Art/ShopInterior/", StringComparison.Ordinal)
            || path.StartsWith("Assets/TinyRiders/Art/MutedShopSurfaces/", StringComparison.Ordinal)
            || path.StartsWith("Assets/TinyRiders/Art/RacingFloor/", StringComparison.Ordinal)
            || path.StartsWith("Assets/TinyRiders/Gameplay/TutorialAssets/", StringComparison.Ordinal)
            ? ModelImporterMeshCompression.Off : ModelImporterMeshCompression.High;
    }

    private static string Hash(string path)
    {
        using (var sha = SHA256.Create())
        using (var stream = File.OpenRead(path))
            return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
    }

    private void Backup(string path)
    {
        if (!File.Exists(path) || files.Any(f => (string)f["source"] == path)) return;
        string name = files.Count.ToString("D5") + Path.GetExtension(path);
        File.Copy(path, Path.Combine(folder, name));
        files.Add(new JObject { ["source"] = path, ["backup"] = name, ["originalHash"] = Hash(path) });
    }

    private void Prepare()
    {
        // Resolve the current inputs: the last report can refer to an earlier
        // scene or omit newly rebuilt meshes entirely.
        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        var paths = scenes.Length > 0
            ? AssetDatabase.GetDependencies(scenes, true)
            : AssetDatabase.FindAssets("t:Mesh", new[] { "Assets/TinyRiders", "Assets/TinyRidersPremium", "Assets/ithappy/Modular_Racing_Cars" }).Select(AssetDatabase.GUIDToAssetPath);
        var candidates = paths.Where(p => !string.IsNullOrEmpty(p) && Owned(p)).Distinct().ToArray();
        preciseMeshPaths = new HashSet<string>(CarVisualBatchBuilder.PrecisionMeshPaths(), StringComparer.Ordinal);
        preciseMeshPaths.UnionWith(CrazyGamesCarBuildDependencies.ActivePrecisionMeshPaths());
        var models = new List<string>();
        var meshes = new List<Mesh>();
        foreach (var path in candidates)
        {
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            var compression = CompressionFor(path, preciseMeshPaths);
            if (importer != null)
            {
                if (importer.meshCompression == compression) continue;
                Backup(path + ".meta");
                models.Add(path);
            }
            else if (path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase))
            {
                var found = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Mesh>()
                    .Where(m => MeshUtility.GetMeshCompression(m) != compression).ToArray();
                if (found.Length == 0) continue;
                Backup(path);
                Backup(path + ".meta");
                meshes.AddRange(found);
            }
        }
        WriteManifest();
        File.WriteAllText(Path.Combine(CrazyGamesReleaseBuild.Evidence, "last-mesh-backup.txt"), folder);
        File.WriteAllText(Status, new JObject { ["restored"] = false, ["backup"] = folder }.ToString());
        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (var mesh in meshes)
            {
                MeshUtility.SetMeshCompression(mesh, CompressionFor(AssetDatabase.GetAssetPath(mesh), preciseMeshPaths));
                EditorUtility.SetDirty(mesh);
            }
            foreach (var path in models)
            {
                var importer = (ModelImporter)AssetImporter.GetAtPath(path);
                importer.meshCompression = CompressionFor(path, preciseMeshPaths);
                AssetDatabase.WriteImportSettingsIfDirty(path);
            }
            AssetDatabase.SaveAssets();
        }
        finally { AssetDatabase.StopAssetEditing(); }
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        File.WriteAllText(Path.Combine(CrazyGamesReleaseBuild.Evidence, "mesh-precision.json"),
            new JObject {
                ["losslessEnvironmentAssets"] = new JArray(candidates.Where(p => CompressionFor(p, preciseMeshPaths) == ModelImporterMeshCompression.Off)
                    .Where(p => AssetImporter.GetAtPath(p) is ModelImporter ||
                        (p.EndsWith(".asset", StringComparison.OrdinalIgnoreCase) && AssetDatabase.LoadAllAssetsAtPath(p).OfType<Mesh>().Any()))),
                ["exactCarMeshDependencies"] = new JArray(preciseMeshPaths.OrderBy(p => p, StringComparer.Ordinal)),
                ["changedGeneratedMeshes"] = meshes.Count, ["changedModelImports"] = models.Count
            }.ToString());
        foreach (JObject record in files) record["appliedHash"] = Hash((string)record["source"]);
        WriteManifest();
        Debug.Log($"CrazyGames temporary mesh compression: {meshes.Count} generated meshes, {models.Count} model imports.");
    }

    private void WriteManifest() => File.WriteAllText(Manifest, files.ToString());

    public void Dispose()
    {
        if (disposed) return;
        Restore(folder, files, false);
        disposed = true;
    }

    private static void Restore(string folder, JArray records, bool recovery)
    {
        string root = Path.GetFullPath(".") + Path.DirectorySeparatorChar;
        foreach (JObject record in records)
        {
            string source = (string)record["source"];
            if (!Owned(source.Replace(".meta", "")) || !Path.GetFullPath(source).StartsWith(root, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Invalid mesh restoration path: " + source);
            string backup = Path.GetFullPath(Path.Combine(folder, (string)record["backup"]));
            if (!backup.StartsWith(Path.GetFullPath(folder) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Invalid backup path.");
            if (Hash(backup) != (string)record["originalHash"]) throw new InvalidOperationException("Mesh backup checksum mismatch.");
            string applied = (string)record["appliedHash"];
            if (!recovery && applied != null && (!File.Exists(source) || (Hash(source) != applied && Hash(source) != (string)record["originalHash"])))
                throw new InvalidOperationException("Mesh source changed during build; backup retained: " + source);
        }
        // BuildPlayer can leave native mesh files memory-mapped on Windows.
        // Release those handles before copying the verified source bytes back.
        AssetDatabase.ReleaseCachedFileHandles();
        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (JObject record in records)
            {
                string source = (string)record["source"];
                string backup = Path.Combine(folder, (string)record["backup"]);
                if (File.Exists(source) && Hash(source) == (string)record["originalHash"]) continue;
                try { File.Copy(backup, source, true); }
                catch (IOException)
                {
                    // A mapped view can outlive its cached handle on Windows.
                    // Replacing the directory entry preserves those readers while restoring the source.
                    string temporary = Path.Combine(folder, Guid.NewGuid().ToString("N") + ".restore");
                    File.Copy(backup, temporary);
                    try { File.Replace(temporary, source, null); }
                    finally { if (File.Exists(temporary)) File.Delete(temporary); }
                }
            }
        }
        finally { AssetDatabase.StopAssetEditing(); }
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        bool restored = records.All(r => Hash((string)r["source"]) == (string)r["originalHash"]);
        File.WriteAllText(Status, new JObject { ["restored"] = restored, ["backup"] = folder, ["files"] = records.Count }.ToString());
        if (!restored) throw new InvalidOperationException("Mesh restoration checksums did not match; backups retained.");
    }

    [MenuItem("Tools/Toy Car Sort/CrazyGames/Restore Temporary Mesh Imports")]
    public static void Recover()
    {
        string folder = File.ReadAllText(Path.Combine(CrazyGamesReleaseBuild.Evidence, "last-mesh-backup.txt"));
        string evidence = Path.GetFullPath(CrazyGamesReleaseBuild.Evidence) + Path.DirectorySeparatorChar;
        if (!Path.GetFullPath(folder).StartsWith(evidence, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Invalid mesh backup folder.");
        Restore(folder, JArray.Parse(File.ReadAllText(Path.Combine(folder, "manifest.json"))), true);
    }
}
