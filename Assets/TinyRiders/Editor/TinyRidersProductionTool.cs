using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TinyRiders.Editor
{
    public static class TinyRidersProductionTool
    {
        const string GeneratorVersion = "initial-v1-10";
        const string AssetRoot = "Assets/TinyRiders";
        const string SupplierColor = "Assets/ithappy/Modular_Racing_Cars/Materials/Color.mat";
        const string SupplierMetal = "Assets/ithappy/Modular_Racing_Cars/Materials/Metal.mat";
        const string SupplierPalette = "Assets/ithappy/Modular_Racing_Cars/Textures/Textures1.png";
        static readonly string Project = Directory.GetParent(Application.dataPath).FullName;
        static readonly string Production = Path.Combine(Project, "TinyRidersProduction");

        [Serializable] class SourceCatalog { public int schemaVersion; public string collectionId, setId, releaseId; public BaseCar[] baseCars; public Build[] builds; public Livery[] liveries, buildLiveries, liveryOverrides; public Product[] products; }
        [Serializable] class BaseCar { public string id, sourceName, displayName, sourcePrefabPath, sourceGuid, paintAuditNote; public bool recolorNeutralPaint, tintBodyMetal; public float neutralPaintMinimumValue, neutralPaintOutputFloor; public Part[] parts; }
        [Serializable] class Part { public string name, sourcePath, sourceGuid; public bool defaultActive; }
        [Serializable] class Build { public string baseCarId, code, displayName, editionLabel, verification, wheelPrefabPath, wheelGuid; public string[] enableChildren, disableChildren; public bool supported; }
        [Serializable] class Livery { public string baseCarId, buildCode, code, name, colorHex; public float saturationScale = 1f, valueScale = 1f; public string[] colorTags; }
        [Serializable] class Product { public string variantId, baseCarId, buildCode, liveryCode, displayName, releaseId; public int contentRevision, displayOrder; public bool planned; }
        [Serializable] class State { public int schemaVersion = 1; public string generatedAtUtc; public AssetRecord[] assets; public ProductRecord[] products; }
        [Serializable] class AssetRecord { public string path, sha256; }
        [Serializable] class ProductRecord { public string variantId, status, inputHash, prefabPath, definitionPath, thumbnailPath, materialPath, error, nextAction; public bool visualReviewed; }

        static SourceCatalog catalog;
        static State state;
        static Dictionary<string, AssetRecord> assetRecords;
        static Dictionary<string, ProductRecord> productRecords;
        static Dictionary<string, Texture2D> paletteCache;

        static string Absolute(string assetPath) => Path.Combine(Project, assetPath.Replace('/', Path.DirectorySeparatorChar));
        static string HashBytes(byte[] bytes)
        {
            using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
        }
        static string HashFile(string path) => File.Exists(path) ? HashBytes(File.ReadAllBytes(path)) : "";
        static string HashText(string text) => HashBytes(Encoding.UTF8.GetBytes(text));
        static void Log(string message)
        {
            Directory.CreateDirectory(Production);
            File.AppendAllText(Path.Combine(Production, "execution.log"), DateTime.UtcNow.ToString("o") + " " + message + Environment.NewLine);
            Debug.Log("Tiny Riders: " + message);
        }
        static void Load()
        {
            catalog = JsonUtility.FromJson<SourceCatalog>(File.ReadAllText(Path.Combine(Production, "source_catalog.json")));
            string path = Path.Combine(Production, "production_state.json");
            state = File.Exists(path) ? JsonUtility.FromJson<State>(File.ReadAllText(path)) : new State();
            assetRecords = (state.assets ?? new AssetRecord[0]).Where(x => x != null).GroupBy(x => x.path).ToDictionary(x => x.Key, x => x.First(), StringComparer.Ordinal);
            productRecords = (state.products ?? new ProductRecord[0]).Where(x => x != null).GroupBy(x => x.variantId).ToDictionary(x => x.Key, x => x.First(), StringComparer.Ordinal);
            paletteCache = new Dictionary<string, Texture2D>(StringComparer.Ordinal);
        }
        static void Persist()
        {
            state.generatedAtUtc = DateTime.UtcNow.ToString("o");
            state.assets = assetRecords.Values.OrderBy(x => x.path, StringComparer.Ordinal).ToArray();
            state.products = productRecords.Values.OrderBy(x => x.variantId, StringComparer.Ordinal).ToArray();
            File.WriteAllText(Path.Combine(Production, "production_state.json"), JsonUtility.ToJson(state, true));
            ExportCsv();
        }
        static void CheckOwned(string path)
        {
            string absolute = Absolute(path);
            if (!File.Exists(absolute)) return;
            AssetRecord old;
            if (!assetRecords.TryGetValue(path, out old)) throw new InvalidOperationException("Existing untracked asset at " + path + "; preserve and resolve conflict");
            if (old.sha256 != HashFile(absolute)) throw new InvalidOperationException("Manual change conflict at " + path + "; preserve and resolve conflict");
        }
        static void Track(string path)
        {
            assetRecords[path] = new AssetRecord { path = path, sha256 = HashFile(Absolute(path)) };
        }
        static void EnsureFolder(string path)
        {
            string parent = Path.GetDirectoryName(Absolute(path));
            Directory.CreateDirectory(parent);
        }
        static T Find<T>(T[] values, Func<T, bool> predicate, string message) where T : class
        {
            T found = values == null ? null : Array.Find(values, new Predicate<T>(predicate));
            if (found == null) throw new InvalidOperationException(message);
            return found;
        }
        static Livery ResolveLivery(Product product)
        {
            var specialized = catalog.liveryOverrides == null ? null : Array.Find(catalog.liveryOverrides,
                x => x.baseCarId == product.baseCarId && x.buildCode == product.buildCode && x.code == product.liveryCode);
            if (specialized != null) return specialized;
            var recipes = product.buildCode == "CUS" ? catalog.buildLiveries : catalog.liveries;
            return Find(recipes, x => x.code == product.liveryCode, "Missing livery for " + product.variantId);
        }
        static void ValidateSource(BaseCar car, Build build)
        {
            if (AssetDatabase.AssetPathToGUID(car.sourcePrefabPath) != car.sourceGuid) throw new InvalidOperationException("Source GUID changed for " + car.id);
            if (AssetDatabase.LoadAssetAtPath<GameObject>(car.sourcePrefabPath) == null) throw new InvalidOperationException("Missing source prefab " + car.sourcePrefabPath);
            if (!build.supported) throw new InvalidOperationException("Unsupported build: " + build.verification);
            foreach (var part in car.parts ?? new Part[0])
                if (!string.IsNullOrEmpty(part.sourcePath) && AssetDatabase.AssetPathToGUID(part.sourcePath) != part.sourceGuid)
                    throw new InvalidOperationException("Part GUID changed: " + part.name);
            if (!string.IsNullOrEmpty(build.wheelPrefabPath) &&
                (AssetDatabase.AssetPathToGUID(build.wheelPrefabPath) != build.wheelGuid ||
                 AssetDatabase.LoadAssetAtPath<GameObject>(build.wheelPrefabPath) == null))
                throw new InvalidOperationException("Alternate wheel source changed for " + car.id);
        }
        static string InputHash(Product product, BaseCar car, Build build, Livery livery)
        {
            var input = new StringBuilder(GeneratorVersion).Append(catalog.collectionId).Append(catalog.setId).Append(catalog.releaseId)
                .Append(JsonUtility.ToJson(product)).Append(car.id).Append(car.sourceName).Append(car.displayName)
                .Append(car.sourcePrefabPath).Append(car.sourceGuid).Append(car.recolorNeutralPaint)
                .Append(build.baseCarId).Append(build.code).Append(build.displayName).Append(build.supported)
                .Append(build.wheelPrefabPath).Append(build.wheelGuid)
                .Append(JsonUtility.ToJson(livery));
            if (car.neutralPaintMinimumValue > 0f || car.neutralPaintOutputFloor > 0f)
                input.Append(car.neutralPaintMinimumValue).Append(car.neutralPaintOutputFloor);
            if (car.tintBodyMetal) input.Append("bodyMetalPaletteV2").Append(HashFile(Absolute(SupplierMetal)));
            foreach (var name in build.enableChildren ?? new string[0]) input.Append(name);
            foreach (var name in build.disableChildren ?? new string[0]) input.Append(name);
            foreach (var part in car.parts ?? new Part[0]) input.Append(part.name).Append(part.sourcePath).Append(part.sourceGuid).Append(part.defaultActive);
            input.Append(HashFile(Absolute(car.sourcePrefabPath))).Append(HashFile(Absolute(SupplierColor))).Append(HashFile(Absolute(SupplierPalette)));
            foreach (var part in car.parts ?? new Part[0])
                if (!string.IsNullOrEmpty(part.sourcePath)) input.Append(HashFile(Absolute(part.sourcePath)));
            if (!string.IsNullOrEmpty(build.wheelPrefabPath)) input.Append(HashFile(Absolute(build.wheelPrefabPath)));
            return HashText(input.ToString());
        }
        static Transform FindChild(Transform root, string name)
        {
            foreach (Transform child in root)
            {
                if (child.name == name) return child;
                var found = FindChild(child, name);
                if (found != null) return found;
            }
            return null;
        }
        static void ToggleBuild(GameObject root, Build build)
        {
            foreach (string name in build.disableChildren ?? new string[0])
            {
                var child = FindChild(root.transform, name);
                if (child == null) throw new InvalidOperationException("Missing part to disable: " + name);
                child.gameObject.SetActive(false);
                PrefabUtility.RecordPrefabInstancePropertyModifications(child.gameObject);
            }
            foreach (string name in build.enableChildren ?? new string[0])
            {
                var child = FindChild(root.transform, name);
                if (child == null) throw new InvalidOperationException("Missing part to enable: " + name);
                child.gameObject.SetActive(true);
                PrefabUtility.RecordPrefabInstancePropertyModifications(child.gameObject);
            }
        }
        static bool WheelsMatch(GameObject root, Build build)
        {
            if (string.IsNullOrEmpty(build.wheelPrefabPath)) return true;
            foreach (string position in new[] { "FL", "FR", "RL", "RR" })
            {
                var replacement = FindChild(root.transform, "TR_Wheel_" + position);
                if (replacement == null || AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(replacement.gameObject)) != build.wheelPrefabPath)
                    return false;
            }
            return true;
        }
        static void EnsureWheels(GameObject root, BaseCar car, Build build)
        {
            if (string.IsNullOrEmpty(build.wheelPrefabPath)) return;
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(build.wheelPrefabPath);
            foreach (string position in new[] { "FL", "FR", "RL", "RR" })
            {
                var original = FindChild(root.transform, car.sourceName + "_Wheel_" + position);
                if (original == null || original.gameObject.activeSelf)
                    throw new InvalidOperationException("Original wheel must exist and be disabled: " + car.id + " " + position);
                var replacement = FindChild(root.transform, "TR_Wheel_" + position);
                if (replacement != null && AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(replacement.gameObject)) != build.wheelPrefabPath)
                {
                    UnityEngine.Object.DestroyImmediate(replacement.gameObject);
                    replacement = null;
                }
                if (replacement == null)
                {
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
                    instance.name = "TR_Wheel_" + position;
                    instance.transform.SetParent(root.transform, false);
                    replacement = instance.transform;
                }
                replacement.localPosition = root.transform.InverseTransformPoint(original.position);
                replacement.localRotation = Quaternion.Inverse(root.transform.rotation) * original.rotation;
                replacement.localScale = Vector3.one;
                PrefabUtility.RecordPrefabInstancePropertyModifications(replacement);
                PrefabUtility.RecordPrefabInstancePropertyModifications(replacement.gameObject);
            }
        }
        static string BasePath(BaseCar car) => AssetRoot + "/Prefabs/BaseCars/" + car.id + "/" + car.id + ".prefab";
        static string BuildPath(BaseCar car, Build build) => AssetRoot + "/Prefabs/Builds/" + car.id + "/" + car.id + "-" + build.code + ".prefab";
        static string VariantPath(Product product) => AssetRoot + "/Prefabs/Variants/" + product.baseCarId + "/" + product.buildCode + "/" + product.variantId + ".prefab";
        static string MaterialPath(Product product) => AssetRoot + "/Materials/Variants/" + product.baseCarId + "/" + product.baseCarId + (product.buildCode == "CUS" ? "_CUS" : "") + "_" + product.liveryCode + "_Paint.mat";
        static string BodyMetalPath(Product product) => AssetRoot + "/Materials/Variants/" + product.baseCarId + "/" + product.baseCarId + "_" + product.buildCode + "_" + product.liveryCode + "_BodyMetal.mat";
        static string DefinitionPath(Product product) => AssetRoot + "/Data/Variants/" + product.variantId + ".asset";
        static string ThumbnailPath(Product product) => AssetRoot + "/Previews/Icons/" + product.baseCarId + "/" + product.variantId + ".png";

        static GameObject EnsureBase(BaseCar car)
        {
            string path = BasePath(car);
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            CheckOwned(path);
            if (existing != null && existing.transform.childCount == 1 && existing.transform.GetChild(0).localPosition == Vector3.zero &&
                Quaternion.Angle(existing.transform.GetChild(0).localRotation, Quaternion.identity) < 0.01f && existing.transform.GetChild(0).localScale == Vector3.one)
                return existing;
            EnsureFolder(path);
            var root = existing == null ? new GameObject(car.id) : PrefabUtility.LoadPrefabContents(path);
            try
            {
                GameObject source;
                if (existing == null)
                {
                    source = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(car.sourcePrefabPath));
                    source.transform.SetParent(root.transform, false);
                }
                else source = root.transform.GetChild(0).gameObject;
                source.transform.localPosition = Vector3.zero;
                source.transform.localRotation = Quaternion.identity;
                source.transform.localScale = Vector3.one;
                PrefabUtility.RecordPrefabInstancePropertyModifications(source.transform);
                var saved = PrefabUtility.SaveAsPrefabAsset(root, path);
                Track(path);
                return saved;
            }
            finally { if (existing == null) UnityEngine.Object.DestroyImmediate(root); else PrefabUtility.UnloadPrefabContents(root); }
        }
        static GameObject EnsureBuild(BaseCar car, Build build, GameObject basePrefab)
        {
            string path = BuildPath(car, build);
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            CheckOwned(path);
            if (existing != null)
            {
                bool matches = true;
                foreach (string name in build.disableChildren ?? new string[0]) { var child = FindChild(existing.transform, name); if (child == null || child.gameObject.activeSelf) matches = false; }
                foreach (string name in build.enableChildren ?? new string[0]) { var child = FindChild(existing.transform, name); if (child == null || !child.gameObject.activeSelf) matches = false; }
                if (matches && WheelsMatch(existing, build)) return existing;
            }
            EnsureFolder(path);
            GameObject root = existing == null ? new GameObject(car.id + "-" + build.code) : PrefabUtility.LoadPrefabContents(path);
            try
            {
                if (existing == null)
                {
                    var nested = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
                    nested.transform.SetParent(root.transform, false);
                }
                ToggleBuild(root, build);
                EnsureWheels(root, car, build);
                var saved = PrefabUtility.SaveAsPrefabAsset(root, path);
                Track(path);
                return saved;
            }
            finally { if (existing == null) UnityEngine.Object.DestroyImmediate(root); else PrefabUtility.UnloadPrefabContents(root); }
        }
        static Material EnsureMaterial(Product product, BaseCar car, Livery livery)
        {
            string path = MaterialPath(product);
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            CheckOwned(path);
            var source = AssetDatabase.LoadAssetAtPath<Material>(SupplierColor);
            if (source == null) throw new InvalidOperationException("Missing supplier paint material");
            Color color;
            if (!ColorUtility.TryParseHtmlString(livery.colorHex, out color)) throw new InvalidOperationException("Invalid color " + livery.colorHex);
            if (existing == null) EnsureFolder(path);
            var material = existing ?? new Material(source) { name = Path.GetFileNameWithoutExtension(path) };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
            if (material.HasProperty("_Color")) material.SetColor("_Color", Color.white);
            var palette = EnsurePalette(product, car, livery);
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", palette);
            if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", palette);
            if (existing == null) AssetDatabase.CreateAsset(material, path);
            else EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            Track(path);
            return material;
        }
        static Texture2D EnsureBodyMetalPalette(Product product, BaseCar car, Livery livery)
        {
            string key = car.id + "_" + product.buildCode + "_" + livery.code + "_BodyMetalPalette";
            Texture2D cached;
            if (paletteCache.TryGetValue(key, out cached)) return cached;
            string path = AssetRoot + "/Textures/Derived/" + key + ".png";
            CheckOwned(path);
            EnsureFolder(path);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, true);
            if (!texture.LoadImage(File.ReadAllBytes(Absolute(SupplierPalette))))
                throw new InvalidOperationException("Cannot decode supplier palette for body metal");
            Color target;
            if (!ColorUtility.TryParseHtmlString(livery.colorHex, out target))
                throw new InvalidOperationException("Invalid body metal color " + livery.colorHex);
            float hue, unusedS, unusedV;
            Color.RGBToHSV(target, out hue, out unusedS, out unusedV);
            var pixels = texture.GetPixels();
            for (int i = 0; i < pixels.Length; i++)
            {
                float oldHue, saturation, value;
                Color.RGBToHSV(pixels[i], out oldHue, out saturation, out value);
                if (value < 0.035f) continue;
                var mapped = Color.HSVToRGB(hue, Mathf.Max(0.72f, saturation), Mathf.Max(0.56f, value));
                mapped.a = pixels[i].a;
                pixels[i] = mapped;
            }
            texture.SetPixels(pixels);
            texture.Apply(true, false);
            File.WriteAllBytes(Absolute(path), texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var sourceImporter = (TextureImporter)AssetImporter.GetAtPath(SupplierPalette);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = sourceImporter.sRGBTexture;
            importer.mipmapEnabled = sourceImporter.mipmapEnabled;
            importer.wrapMode = sourceImporter.wrapMode;
            importer.filterMode = sourceImporter.filterMode;
            importer.anisoLevel = sourceImporter.anisoLevel;
            importer.maxTextureSize = sourceImporter.maxTextureSize;
            importer.textureCompression = sourceImporter.textureCompression;
            importer.SaveAndReimport();
            Track(path);
            cached = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            paletteCache[key] = cached;
            return cached;
        }
        static Material EnsureBodyMetal(Product product, BaseCar car, Livery livery)
        {
            if (!car.tintBodyMetal) return null;
            string path = BodyMetalPath(product);
            CheckOwned(path);
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            var source = AssetDatabase.LoadAssetAtPath<Material>(SupplierMetal);
            if (source == null) throw new InvalidOperationException("Missing supplier metal material");
            if (existing == null) EnsureFolder(path);
            var material = existing ?? new Material(source) { name = Path.GetFileNameWithoutExtension(path) };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
            if (material.HasProperty("_Color")) material.SetColor("_Color", Color.white);
            var palette = EnsureBodyMetalPalette(product, car, livery);
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", palette);
            if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", palette);
            if (existing == null) AssetDatabase.CreateAsset(material, path);
            else EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            Track(path);
            return material;
        }
        static Texture2D EnsurePalette(Product product, BaseCar car, Livery livery)
        {
            string key = (car.recolorNeutralPaint ? car.id + "_" : "") + (product.buildCode == "CUS" ? "CUS_" : "") + livery.code;
            Texture2D cached;
            if (paletteCache.TryGetValue(key, out cached)) return cached;
            string path = AssetRoot + "/Textures/Derived/" + key + "_PaintPalette.png";
            CheckOwned(path);
            EnsureFolder(path);
            var sourceBytes = File.ReadAllBytes(Absolute(SupplierPalette));
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, true);
            if (!texture.LoadImage(sourceBytes)) throw new InvalidOperationException("Cannot decode supplier palette");
            Color target;
            if (!ColorUtility.TryParseHtmlString(livery.colorHex, out target)) throw new InvalidOperationException("Invalid palette color");
            float targetHue, unusedS, unusedV;
            Color.RGBToHSV(target, out targetHue, out unusedS, out unusedV);
            var pixels = texture.GetPixels();
            for (int i = 0; i < pixels.Length; i++)
            {
                float hue, saturation, value;
                Color.RGBToHSV(pixels[i], out hue, out saturation, out value);
                if (value < 0.08f) continue;
                if (saturation < 0.18f)
                {
                    float minimum = car.neutralPaintMinimumValue > 0f ? car.neutralPaintMinimumValue : 0.4f;
                    if (!car.recolorNeutralPaint || value < minimum) continue;
                    saturation = 0.65f;
                    value = Mathf.Max(value, car.neutralPaintOutputFloor);
                }
                var mapped = Color.HSVToRGB(targetHue, Mathf.Clamp01(saturation * livery.saturationScale), Mathf.Clamp01(value * livery.valueScale));
                mapped.a = pixels[i].a;
                pixels[i] = mapped;
            }
            texture.SetPixels(pixels);
            texture.Apply(true, false);
            File.WriteAllBytes(Absolute(path), texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var sourceImporter = (TextureImporter)AssetImporter.GetAtPath(SupplierPalette);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = sourceImporter.sRGBTexture;
            importer.mipmapEnabled = sourceImporter.mipmapEnabled;
            importer.wrapMode = sourceImporter.wrapMode;
            importer.filterMode = sourceImporter.filterMode;
            importer.anisoLevel = sourceImporter.anisoLevel;
            importer.maxTextureSize = sourceImporter.maxTextureSize;
            importer.textureCompression = sourceImporter.textureCompression;
            importer.SaveAndReimport();
            Track(path);
            cached = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            paletteCache[key] = cached;
            return cached;
        }
        static void ApplyPaint(GameObject root, Product product, BaseCar car, Material paint, Material bodyMetal)
        {
            string legacyPaint = AssetRoot + "/Materials/Variants/" + product.baseCarId + "/" + product.baseCarId + "_" + product.liveryCode + "_Paint.mat";
            string currentPaint = MaterialPath(product);
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var t = renderer.transform;
                bool protectedPart = false;
                while (t != null)
                {
                    if (t.name.Contains("Wheel_") || t.name.StartsWith("Racer_", StringComparison.Ordinal)) { protectedPart = true; break; }
                    t = t.parent;
                }
                if (protectedPart) continue;
                var materials = renderer.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < materials.Length; i++)
                {
                    string materialPath = AssetDatabase.GetAssetPath(materials[i]);
                    if (materialPath == SupplierColor || materialPath == legacyPaint || materialPath == currentPaint)
                    {
                        materials[i] = paint;
                        changed = true;
                    }
                    else if (bodyMetal != null && renderer.name == car.sourceName &&
                             (materialPath == SupplierMetal || materialPath == BodyMetalPath(product)))
                    {
                        materials[i] = bodyMetal;
                        changed = true;
                    }
                }
                if (!changed) continue;
                renderer.sharedMaterials = materials;
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            }
        }
        static GameObject EnsureVariant(Product product, BaseCar car, GameObject buildPrefab, Material paint, Material bodyMetal)
        {
            string path = VariantPath(product);
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            CheckOwned(path);
            EnsureFolder(path);
            GameObject root = existing == null ? new GameObject(product.variantId) : PrefabUtility.LoadPrefabContents(path);
            try
            {
                if (existing == null)
                {
                    var nested = (GameObject)PrefabUtility.InstantiatePrefab(buildPrefab);
                    nested.transform.SetParent(root.transform, false);
                }
                var identity = root.GetComponent<CarVariantIdentity>();
                if (identity == null) identity = root.AddComponent<CarVariantIdentity>();
                identity.SetVariantId(product.variantId);
                ApplyPaint(root, product, car, paint, bodyMetal);
                var saved = PrefabUtility.SaveAsPrefabAsset(root, path);
                Track(path);
                return saved;
            }
            finally { if (existing == null) UnityEngine.Object.DestroyImmediate(root); else PrefabUtility.UnloadPrefabContents(root); }
        }

        static Sprite EnsureThumbnail(Product product, GameObject prefab)
        {
            string path = ThumbnailPath(product);
            CheckOwned(path);
            EnsureFolder(path);
            byte[] png = RenderIcon(prefab);
            File.WriteAllBytes(Absolute(path), png);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
            Track(path);
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        static byte[] RenderIcon(GameObject prefab)
        {
            var preview = TinyRidersRenderAudit.RenderIconTexture(prefab);
            int width = preview.width, height = preview.height;
            var pixels = preview.GetPixels32();
            UnityEngine.Object.DestroyImmediate(preview);
            var background = pixels[0];
            var outside = new bool[pixels.Length];
            var queue = new Queue<int>();
            outside[0] = true;
            queue.Enqueue(0);
            while (queue.Count > 0)
            {
                int index = queue.Dequeue();
                int x = index % width, y = index / width;
                if (x > 0) EnqueueBackground(index - 1, pixels, background, outside, queue);
                if (x < width - 1) EnqueueBackground(index + 1, pixels, background, outside, queue);
                if (y > 0) EnqueueBackground(index - width, pixels, background, outside, queue);
                if (y < height - 1) EnqueueBackground(index + width, pixels, background, outside, queue);
            }
            int visible = 0;
            for (int i = 0; i < pixels.Length; i++)
                if (outside[i]) pixels[i] = new Color32(0, 0, 0, 0);
                else visible++;
            if (visible < 100) throw new InvalidOperationException("Unity preview contains no visible car");
            var result = new Texture2D(width, height, TextureFormat.RGBA32, false);
            result.SetPixels32(pixels);
            result.Apply();
            var bytes = result.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(result);
            return bytes;
        }
        static void EnqueueBackground(int index, Color32[] pixels, Color32 background, bool[] outside, Queue<int> queue)
        {
            if (outside[index]) return;
            var pixel = pixels[index];
            int difference = Math.Abs(pixel.r - background.r) + Math.Abs(pixel.g - background.g) + Math.Abs(pixel.b - background.b);
            if (difference > 32) return;
            outside[index] = true;
            queue.Enqueue(index);
        }
        static CarVariantDefinition EnsureDefinition(Product product, BaseCar car, Build build, Livery livery, GameObject prefab, Sprite thumbnail)
        {
            string path = DefinitionPath(product);
            var definition = AssetDatabase.LoadAssetAtPath<CarVariantDefinition>(path);
            CheckOwned(path);
            if (definition == null)
            {
                EnsureFolder(path);
                definition = ScriptableObject.CreateInstance<CarVariantDefinition>();
                AssetDatabase.CreateAsset(definition, path);
            }
            definition.variantId = product.variantId;
            definition.baseCarId = product.baseCarId;
            definition.buildCode = product.buildCode;
            definition.liveryCode = product.liveryCode;
            definition.displayName = product.displayName;
            definition.baseCarDisplayName = car.displayName;
            definition.buildDisplayName = build.displayName;
            definition.colorwayName = livery.name;
            definition.collectionId = catalog.collectionId;
            definition.setId = catalog.setId;
            definition.colorTags = livery.colorTags;
            definition.releaseId = product.releaseId;
            definition.contentRevision = product.contentRevision;
            definition.displayOrder = product.displayOrder;
            definition.prefab = prefab;
            definition.thumbnail = thumbnail;
            EditorUtility.SetDirty(definition);
            AssetDatabase.SaveAssets();
            Track(path);
            return definition;
        }
        static void ValidateProduct(Product product)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(VariantPath(product));
            var definition = AssetDatabase.LoadAssetAtPath<CarVariantDefinition>(DefinitionPath(product));
            var thumbnail = AssetDatabase.LoadAssetAtPath<Sprite>(ThumbnailPath(product));
            if (prefab == null || definition == null || thumbnail == null) throw new InvalidOperationException("Missing final prefab, definition, or thumbnail");
            var identity = prefab.GetComponent<CarVariantIdentity>();
            if (identity == null || identity.VariantId != product.variantId || definition.variantId != product.variantId) throw new InvalidOperationException("Identity mismatch");
            if (definition.prefab != prefab || definition.thumbnail != thumbnail) throw new InvalidOperationException("Definition references mismatch");
            var renderers = prefab.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) throw new InvalidOperationException("No renderers");
            bool assignedPaint = false;
            foreach (var renderer in renderers)
                foreach (var material in renderer.sharedMaterials)
                {
                    if (material == null || material.shader == null) throw new InvalidOperationException("Missing renderer material or shader: " + renderer.name);
                    if (ActiveInPrefab(renderer.transform) && AssetDatabase.GetAssetPath(material) == MaterialPath(product)) assignedPaint = true;
                }
            if (!assignedPaint) throw new InvalidOperationException("Active car does not use its build-specific paint material");
            var car = Find(catalog.baseCars, x => x.id == product.baseCarId, "Missing validation car");
            var build = Find(catalog.builds, x => x.baseCarId == product.baseCarId && x.code == product.buildCode, "Missing validation build");
            var spoiler = FindChild(prefab.transform, car.sourceName + "_Spoiler");
            if (spoiler == null || spoiler.gameObject.activeSelf != (product.buildCode == "STD"))
                throw new InvalidOperationException("Spoiler state mismatch for " + product.variantId);
            if (car.tintBodyMetal && !renderers.Any(x => x.name == car.sourceName &&
                x.sharedMaterials.Any(m => AssetDatabase.GetAssetPath(m) == BodyMetalPath(product))))
                throw new InvalidOperationException("Main-body metal treatment missing for " + product.variantId);
            if (!string.IsNullOrEmpty(build.wheelPrefabPath) &&
                !WheelsMatch(AssetDatabase.LoadAssetAtPath<GameObject>(BuildPath(car, build)), build))
                throw new InvalidOperationException("Build wrapper wheel source mismatch for " + product.variantId);
            foreach (string position in new[] { "FL", "FR", "RL", "RR" })
            {
                var originalWheel = FindChild(prefab.transform, car.sourceName + "_Wheel_" + position);
                var replacementWheel = FindChild(prefab.transform, "TR_Wheel_" + position);
                bool swapped = !string.IsNullOrEmpty(build.wheelPrefabPath);
                if (originalWheel == null || originalWheel.gameObject.activeSelf == swapped)
                    throw new InvalidOperationException("Original wheel state mismatch: " + product.variantId + " " + position);
                string replacementSource = replacementWheel == null ? "" : AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(replacementWheel.gameObject));
                if (swapped && (replacementWheel == null || !replacementWheel.gameObject.activeSelf ||
                    (replacementSource != build.wheelPrefabPath && replacementSource != BuildPath(car, build)) ||
                    Vector3.Distance(replacementWheel.position, originalWheel.position) > 0.01f ||
                    Quaternion.Angle(replacementWheel.rotation, originalWheel.rotation) > 0.2f))
                    throw new InvalidOperationException("Alternate wheel fit mismatch: " + product.variantId + " " + position);
            }
            foreach (var transform in prefab.GetComponentsInChildren<Transform>(true))
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) != 0)
                    throw new InvalidOperationException("Missing script on " + transform.name);
        }
        static bool ActiveInPrefab(Transform child)
        {
            for (var current = child; current != null; current = current.parent)
                if (!current.gameObject.activeSelf) return false;
            return true;
        }
        static void Generate(Product product)
        {
            var car = Find(catalog.baseCars, x => x.id == product.baseCarId, "Missing base car for " + product.variantId);
            var build = Find(catalog.builds, x => x.baseCarId == product.baseCarId && x.code == product.buildCode, "Missing build for " + product.variantId);
            var livery = ResolveLivery(product);
            ValidateSource(car, build);
            string inputHash = InputHash(product, car, build, livery);
            ProductRecord record;
            if (productRecords.TryGetValue(product.variantId, out record) && record.inputHash == inputHash && record.status == "technical_validated")
            {
                ValidateProduct(product);
                return;
            }
            if (record == null) record = new ProductRecord { variantId = product.variantId };
            if (record.inputHash != inputHash) record.visualReviewed = false;
            record.prefabPath = VariantPath(product);
            record.definitionPath = DefinitionPath(product);
            record.thumbnailPath = ThumbnailPath(product);
            record.materialPath = MaterialPath(product);
            try
            {
                if (record.status == "preview_pending" && record.inputHash == inputHash)
                {
                    var existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(record.prefabPath);
                    var thumbnailOnly = EnsureThumbnail(product, existingPrefab);
                    EnsureDefinition(product, car, build, livery, existingPrefab, thumbnailOnly);
                    ValidateProduct(product);
                    record.status = "technical_validated";
                    record.error = "";
                    record.nextAction = "Inspect rendered thumbnail and contact sheet";
                    Log(product.variantId + " preview completed and technical validation passed");
                    productRecords[product.variantId] = record;
                    Persist();
                    return;
                }
                var basePrefab = EnsureBase(car);
                var buildPrefab = EnsureBuild(car, build, basePrefab);
                var material = EnsureMaterial(product, car, livery);
                var bodyMetal = EnsureBodyMetal(product, car, livery);
                var variant = EnsureVariant(product, car, buildPrefab, material, bodyMetal);
                AssetDatabase.ImportAsset(VariantPath(product), ImportAssetOptions.ForceSynchronousImport);
                variant = AssetDatabase.LoadAssetAtPath<GameObject>(VariantPath(product));
                var thumbnail = EnsureThumbnail(product, variant);
                EnsureDefinition(product, car, build, livery, variant, thumbnail);
                ValidateProduct(product);
                record.status = "technical_validated";
                record.inputHash = inputHash;
                record.error = "";
                record.nextAction = record.visualReviewed ? "Ready for release validation" : "Inspect rendered thumbnail and contact sheet";
                Log(product.variantId + " technical validation passed");
            }
            catch (Exception ex)
            {
                record.status = ex.Message.StartsWith("Preview pending", StringComparison.Ordinal) ? "preview_pending" : "blocked";
                if (record.status == "preview_pending") record.inputHash = inputHash;
                record.error = ex.Message;
                record.nextAction = record.status == "preview_pending" ? "Run generation again after Unity finishes preview rendering" : ex.Message.Contains("conflict") ? "Review manual asset change before regeneration" : "Repair source or generation issue, then regenerate this ID";
                Log(product.variantId + " blocked: " + ex.Message);
            }
            productRecords[product.variantId] = record;
            Persist();
        }
        static void ValidateCatalog()
        {
            if (catalog == null || catalog.schemaVersion != 1) throw new InvalidOperationException("Unsupported source catalog schema");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var product in catalog.products)
            {
                if (!ids.Add(product.variantId)) throw new InvalidOperationException("Duplicate variant ID " + product.variantId);
                if (product.variantId != "TR-" + product.baseCarId + "-" + product.buildCode + "-" + product.liveryCode) throw new InvalidOperationException("Identity fields disagree for " + product.variantId);
            }
            var baseIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var car in catalog.baseCars) if (!baseIds.Add(car.id)) throw new InvalidOperationException("Duplicate base ID " + car.id);
        }
        static string Csv(string value) => "\"" + (value ?? "").Replace("\"", "\"\"") + "\"";
        static void ExportCsv()
        {
            var lines = new List<string> { "variantId,sourceName,buildCode,liveryCode,colorwayName,releaseId,status,prefabPath,previewPath,validationNotes" };
            foreach (var product in catalog.products.OrderBy(x => x.displayOrder))
            {
                var car = Array.Find(catalog.baseCars, x => x.id == product.baseCarId);
                var livery = ResolveLivery(product);
                ProductRecord record;
                productRecords.TryGetValue(product.variantId, out record);
                string status = record == null ? "planned" : record.status == "technical_validated" && record.visualReviewed ? "ready" : record.status;
                lines.Add(string.Join(",", new[] { Csv(product.variantId), Csv(car?.sourceName), Csv(product.buildCode), Csv(product.liveryCode), Csv(livery?.name), Csv(product.releaseId), Csv(status), Csv(record?.prefabPath), Csv(record?.thumbnailPath), Csv(string.IsNullOrEmpty(record?.error) ? record?.nextAction : record.error) }));
            }
            File.WriteAllLines(Path.Combine(Production, "variant_catalog.csv"), lines, Encoding.UTF8);
        }
        [MenuItem("Tiny Riders/Validate Catalog and Compatibility")]
        public static void ValidateCatalogAndCompatibility()
        {
            Load(); ValidateCatalog();
            foreach (var product in catalog.products)
            {
                var car = Find(catalog.baseCars, x => x.id == product.baseCarId, "Missing car");
                var build = Find(catalog.builds, x => x.baseCarId == product.baseCarId && x.code == product.buildCode, "Missing build");
                ValidateSource(car, build);
            }
            ExportCsv();
            Log("Catalog and compatibility validated: " + catalog.products.Length + " planned products");
        }
        [MenuItem("Tiny Riders/Generate Missing or Changed Variants")]
        public static void GenerateMissingOrChangedVariants()
        {
            Load(); ValidateCatalog();
            foreach (var product in catalog.products.OrderBy(x => x.displayOrder)) Generate(product);
            AssetDatabase.SaveAssets();
            Log("Generation pass complete");
        }
        [MenuItem("Tiny Riders/Regenerate Selected Variant IDs")]
        public static void RegenerateSelectedVariantIDs()
        {
            var ids = Selection.objects.Select(x => x.name).Where(x => x.StartsWith("TR-", StringComparison.Ordinal)).ToArray();
            RegenerateIds(string.Join(",", ids));
        }
        public static void RegenerateIds(string commaSeparatedIds)
        {
            Load(); ValidateCatalog();
            var ids = new HashSet<string>((commaSeparatedIds ?? "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()), StringComparer.Ordinal);
            foreach (var product in catalog.products.Where(x => ids.Contains(x.variantId))) Generate(product);
            AssetDatabase.SaveAssets();
        }
        [MenuItem("Tiny Riders/Generate Previews")]
        public static void GeneratePreviewsAndContactSheets()
        {
            Load();
            foreach (var product in catalog.products)
            {
                ProductRecord record;
                if (!productRecords.TryGetValue(product.variantId, out record) || record.status != "technical_validated") continue;
                try { EnsureThumbnail(product, AssetDatabase.LoadAssetAtPath<GameObject>(VariantPath(product))); }
                catch (Exception ex) { Log(product.variantId + " preview blocked: " + ex.Message); }
            }
            Persist();
            Log("PNG previews checked. Run py -3.14 TinyRidersProduction/generate_contact_sheets.py from the project root to refresh contact sheets.");
        }
        public static void RefreshPreviewIds(string commaSeparatedIds)
        {
            Load();
            var ids = new HashSet<string>((commaSeparatedIds ?? "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()), StringComparer.Ordinal);
            foreach (var product in catalog.products.Where(x => ids.Contains(x.variantId)))
                EnsureThumbnail(product, AssetDatabase.LoadAssetAtPath<GameObject>(VariantPath(product)));
            Persist();
        }
        public static void MarkVisualReviewed(string commaSeparatedIds)
        {
            Load();
            var ids = new HashSet<string>((commaSeparatedIds ?? "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()), StringComparer.Ordinal);
            foreach (var record in productRecords.Values)
                if ((ids.Contains("ALL") || ids.Contains(record.variantId)) && record.status == "technical_validated") { record.visualReviewed = true; record.nextAction = "Ready for release validation"; }
            Persist();
        }
        [MenuItem("Tiny Riders/Validate Initial Release")]
        public static void ValidateInitialRelease()
        {
            Load(); ValidateCatalog();
            string path = AssetRoot + "/Data/Releases/initial_v1.asset";
            var definitions = new List<CarVariantDefinition>();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var product in catalog.products.OrderBy(x => x.displayOrder))
            {
                ProductRecord record;
                if (!productRecords.TryGetValue(product.variantId, out record) || record.status != "technical_validated" || !record.visualReviewed) continue;
                var car = Find(catalog.baseCars, x => x.id == product.baseCarId, "Missing base car for " + product.variantId);
                var build = Find(catalog.builds, x => x.baseCarId == product.baseCarId && x.code == product.buildCode, "Missing build for " + product.variantId);
                if (record.inputHash != InputHash(product, car, build, ResolveLivery(product)))
                    throw new InvalidOperationException("Reviewed product has changed inputs; regenerate and review " + product.variantId);
                ValidateProduct(product);
                if (!ids.Add(product.variantId)) throw new InvalidOperationException("Duplicate release ID " + product.variantId);
                definitions.Add(AssetDatabase.LoadAssetAtPath<CarVariantDefinition>(DefinitionPath(product)));
            }
            CheckOwned(path);
            var release = AssetDatabase.LoadAssetAtPath<CarCatalog>(path);
            if (release == null) { EnsureFolder(path); release = ScriptableObject.CreateInstance<CarCatalog>(); AssetDatabase.CreateAsset(release, path); }
            release.SetEntries(definitions);
            EditorUtility.SetDirty(release);
            AssetDatabase.SaveAssets();
            Track(path); Persist();
            Log("Initial release validated and published: " + definitions.Count + " entries");
        }
        [MenuItem("Tiny Riders/Generate Static Lineup Scene")]
        public static void GenerateStaticLineupScene()
        {
            Load();
            string path = AssetRoot + "/Scenes/ArtPreview/initial_v1_lineup.unity";
            CheckOwned(path);
            EnsureFolder(path);
            var previous = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            try
            {
                var collection = new GameObject("Collection_initial_v1");
                for (int baseIndex = 0; baseIndex < catalog.baseCars.Length; baseIndex++)
                {
                    var car = catalog.baseCars[baseIndex];
                    var group = new GameObject(car.id + "_" + car.sourceName);
                    group.transform.SetParent(collection.transform, false);
                    group.transform.localPosition = new Vector3((baseIndex % 5) * 22f, 0f, (baseIndex / 5) * 20f);
                    foreach (string buildCode in new[] { "STD", "CUS" })
                    {
                        var buildGroup = new GameObject(buildCode);
                        buildGroup.transform.SetParent(group.transform, false);
                        buildGroup.transform.localPosition = new Vector3(0f, 0f, buildCode == "STD" ? 0f : 7f);
                        for (int index = 1; index <= 5; index++)
                        {
                            string id = "TR-" + car.id + "-" + buildCode + "-L" + index.ToString("00");
                            ProductRecord record;
                            if (!productRecords.TryGetValue(id, out record) || record.status != "technical_validated" || !record.visualReviewed) continue;
                            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(record.prefabPath);
                            if (prefab == null) throw new InvalidOperationException("Missing lineup prefab " + id);
                            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                            instance.transform.SetParent(buildGroup.transform, false);
                            instance.transform.localPosition = new Vector3((index - 1) * 3.6f, 0f, 0f);
                        }
                    }
                }
                var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
                ground.name = "NeutralGround";
                ground.transform.position = new Vector3(50f, -0.05f, 24f);
                ground.transform.localScale = new Vector3(12f, 1f, 7f);
                var cameraObject = new GameObject("LineupCamera");
                var camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = 75f;
                cameraObject.transform.position = new Vector3(50f, 90f, -70f);
                cameraObject.transform.LookAt(new Vector3(50f, 0f, 24f));
                var lightObject = new GameObject("LineupLight");
                var light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.8f;
                lightObject.transform.rotation = Quaternion.Euler(40f, -30f, 0f);
                if (!EditorSceneManager.SaveScene(scene, path)) throw new InvalidOperationException("Could not save lineup scene");
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                Track(path);
                Persist();
                Log("Saved static initial-release lineup scene");
            }
            finally
            {
                SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        public static void RebuildOpenLineupScene()
        {
            string path = AssetRoot + "/Scenes/ArtPreview/initial_v1_lineup.unity";
            string backupPath = AssetRoot + "/Scenes/ArtPreview/lineup_before_variant_refresh.unity";
            var active = SceneManager.GetActiveScene();
            if (active.path == path)
            {
                if (active.isDirty)
                {
                    if (!EditorSceneManager.SaveScene(active, backupPath, true))
                        throw new InvalidOperationException("Could not back up unsaved lineup edits before rebuilding");
                }
            }
            if (active.path == path || string.IsNullOrEmpty(active.path))
                EditorSceneManager.OpenScene(backupPath, OpenSceneMode.Single);
            GenerateStaticLineupScene();
            EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        }
    }
}
